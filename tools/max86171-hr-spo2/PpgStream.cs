using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.Devices.Bluetooth.Advertisement;

// Connects to the MAX86171 board and uses every LED and all four photodiodes.
//
// Sensor board (MAX86171_OSB): two single green LEDs in the middle (D3, D4) and two OSRAM SFH 7013 packages
// (green/red/IR; pin 1 green, 2 IR, 4 red), one near PD1 and one near PD2. Driver-select codes mapped on this board:
//   green: A:00 = D3, B:10 = D4, C:11 = package near PD1, B:11 = package near PD2
//   red:   B:00 near PD1, C:10 near PD2        infrared: B:01 near PD1, A:11 near PD2
// A measurement can drive at most three LEDs and read two photodiodes (PPG1: PD1 or PD3, PPG2: PD2 or PD4),
// so each color is measured twice, once on PD1/PD2 and once on PD3/PD4:
//   MEAS1 green x3, MEAS2 green x1, MEAS3 red x2, MEAS4 IR x2   -> PD1/PD2
//   MEAS5..MEAS8 the same                                       -> PD3/PD4
// Heart rate uses the cleanest green channel; SpO2 uses the cleanest red/IR photodiode path.
class PpgStream : IDisposable {
  public readonly object Sync = new object();
  public MultiHeartRate Estimator;
  public MultiPathOximeter Oximeter;
  public Action<string> Log = delegate { };

  const byte CurrentGreen = 0x0A, CurrentRed = 0x0A, CurrentIr = 0x10;
  const byte PdPair12 = 0x18, PdPair34 = 0x78; // MEASx Configuration 1: TINT max + PDSEL bits (AVER added separately)
  const byte Aver8 = 0x03;
  static readonly string[] Detectors = { "detector 1", "detector 2", "detector 3", "detector 4" };

  OS61 board;
  readonly List<byte> frame = new List<byte>();
  int lastCounter = -1;
  DateTime firstSample;
  double configuredFs;
  bool rateChecked;

  public long Samples, DroppedPackets, Frames;
  public string LastFrameDebug = "";
  public readonly double[] LastGreen = new double[4], LastRed = new double[4], LastIr = new double[4];
  public List<int> RawLog; // optional: green (detector 1) samples, for diagnostics

  public static ulong FindBoard(int seconds) {
    ulong found = 0;
    var w = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
    w.Received += (s, e) => { if (e.Advertisement.LocalName.StartsWith("OS61")) found = e.BluetoothAddress; };
    w.Start();
    var end = DateTime.Now.AddSeconds(seconds);
    while (found == 0 && DateTime.Now < end) Thread.Sleep(100);
    w.Stop();
    return found;
  }

  public void Start() {
    Log("Searching for the board (OS61 Demo)...");
    ulong addr = FindBoard(15);
    if (addr == 0) throw new Exception("Board not found. Make sure it is switched on and the Maxim GUI is closed, then try again in a few seconds.");
    Log("Connecting to " + addr.ToString("X12") + "...");
    board = new OS61(addr);

    board.EnableSensors(false);
    Thread.Sleep(300);
    try { board.UseAccel(false); } catch (Exception ex) { Log("Note: could not turn off accelerometer (" + ex.Message + ")"); }

    // Disable every measurement before reconfiguring. Changing settings while other measurements are still enabled
    // (e.g. left over from the Maxim GUI or a previous session) can leave the sensor producing almost no data.
    board.WriteBits(0x0D, 7, 0, 0x00);
    board.WriteBits(0x0C, 7, 7, 0);

    byte part = board.ReadReg(0xFF);
    if (part != 0x2C) Log("Warning: unexpected part ID 0x" + part.ToString("X2") + " (MAX86171 is 0x2C)");

    // 25 Hz frame rate (32000 Hz / 1280): enough time for eight measurements with 8x averaging.
    board.WriteBits(0x15, 5, 5, 0);
    board.WriteBits(0x16, 7, 0, 0x05);
    board.WriteBits(0x17, 7, 0, 0x00);

    for (int pair = 0; pair < 2; pair++) {
      byte pds = pair == 0 ? PdPair12 : PdPair34;
      int m0 = 1 + 4 * pair;
      ConfigureMeas(m0,     0x38, pds, CurrentGreen, CurrentGreen, CurrentGreen); // A:00 D3, B:10 D4, C:11 package-1 green
      ConfigureMeas(m0 + 1, 0x0C, pds, 0, CurrentGreen, 0);                        // B:11 package-2 green
      ConfigureMeas(m0 + 2, 0x20, pds, 0, CurrentRed, CurrentRed);                 // B:00 + C:10 both red
      ConfigureMeas(m0 + 3, 0x07, pds, CurrentIr, CurrentIr, 0);                   // B:01 + A:11 both infrared
    }

    // System config: MEAS9 off, both PPG channels on, not in shutdown; enable MEAS1-8.
    board.WriteBits(0x0C, 3, 2, 0);
    board.WriteBits(0x0C, 1, 1, 0);
    board.WriteBits(0x0D, 7, 0, 0xFF);

    byte sys1 = board.ReadReg(0x0C), sys2 = board.ReadReg(0x0D), st2 = board.ReadReg(0x01);
    if ((sys1 & 0x8E) != 0 || sys2 != 0xFF) Log("Warning: sensor config did not stick (0x0C=0x" + sys1.ToString("X2") + ", 0x0D=0x" + sys2.ToString("X2") + ")");
    if ((st2 & 0x04) != 0) Log("Warning: sensor reports an invalid configuration.");

    configuredFs = ReadFrameRate();
    Log("Frame rate from registers: " + configuredFs.ToString("F1") + " Hz");
    CreateEstimators(configuredFs);

    board.WriteBits(0x0A, 4, 4, 1); // flush FIFO
    board.Packet += OnPacket;
    board.EnableNotifications(true);
    board.EnableSensors(true);
    Log("Streaming with all LEDs and 4 detectors. Rest a fingertip lightly on the sensor and keep still.");
  }

  void CreateEstimators(double fs) {
    Estimator = new MultiHeartRate(fs, Detectors);
    Oximeter = new MultiPathOximeter(fs, Detectors);
  }

  void ConfigureMeas(int m, byte selects, byte pdSelect, byte drvA, byte drvB, byte drvC) {
    byte bs = (byte)(0x18 + 8 * (m - 1));
    board.WriteBits(bs, 7, 0, selects);
    board.WriteBits((byte)(bs + 1), 7, 0, (byte)(pdSelect | Aver8));
    board.WriteBits((byte)(bs + 2), 7, 0, 0x3A); // filter/LED/ADC ranges as the Maxim GUI default
    board.WriteBits((byte)(bs + 3), 7, 0, 0x50); // settling times as the Maxim GUI default
    board.WriteBits((byte)(bs + 4), 7, 0, drvA);
    board.WriteBits((byte)(bs + 5), 7, 0, drvB);
    board.WriteBits((byte)(bs + 6), 7, 0, drvC);
  }

  double ReadFrameRate() {
    byte sel = board.ReadReg(0x15), hi = board.ReadReg(0x16), lo = board.ReadReg(0x17);
    double clk = (sel & 0x20) != 0 ? 32768.0 : 32000.0;
    int div = ((hi & 0x7F) << 8) | lo;
    double fs = div > 0 ? clk / div : 0;
    if (fs < 5 || fs > 4096) { Log("Frame-rate registers look odd (div=" + div + "); assuming 25 Hz until measured."); fs = 25; }
    return fs;
  }

  void OnPacket(byte[] p) {
    lock (Sync) {
      if (lastCounter >= 0 && p[0] != (byte)(lastCounter + 1)) DroppedPackets += (byte)(p[0] - lastCounter - 1);
      lastCounter = p[0];
      int type = p[1];
      if (type == 0 && frame.Count > 0) { ProcessFrame(frame.ToArray()); frame.Clear(); }
      if (type == 0 || type == 1 || type == 2 || type == 10) for (int i = 2; i < 20; i++) frame.Add(p[i]);
    }
  }

  // Each 3-byte value: top 4 bits = measurement tag (1-8), low 20 bits = signed ADC value; tag 0 is padding.
  // A sample is MEAS1..MEAS8, each twice (PPG1 then PPG2). Samples are assembled across frames because the
  // sensor FIFO can start mid-sample, which shifts every following frame (e.g. tags 6,6,7,7,8,8,1,1,...).
  double[,] pending = new double[9, 2];
  int[] pendingSeen = new int[9];

  void ProcessFrame(byte[] b) {
    Frames++;
    if (Frames <= 3) LastFrameDebug += "[" + b.Length + " bytes, tags " + string.Join(",", Enumerable.Range(0, b.Length / 3).Select(k => (b[3 * k] >> 4).ToString())) + "] ";
    for (int i = 0; i + 3 <= b.Length; i += 3) {
      int tag = b[i] >> 4;
      int x = ((b[i] << 16) | (b[i + 1] << 8) | b[i + 2]) & 0xFFFFF;
      if ((x & 0x80000) != 0) x -= 0x100000;
      if (tag < 1 || tag > 8) continue;
      if (tag == 1 && pendingSeen[1] == 2) ResetPending(); // MEAS1 again: a new sample begins
      int occ = pendingSeen[tag]++;
      if (occ > 1) { ResetPending(); continue; }           // out of order: resynchronise on the next MEAS1
      pending[tag, occ] = x;
      if (tag == 8 && occ == 1) {
        if (pendingSeen.Skip(1).All(c => c == 2)) EmitSample(pending);
        ResetPending();
      }
    }
  }

  void ResetPending() { pending = new double[9, 2]; pendingSeen = new int[9]; }

  void EmitSample(double[,] v) {
    if (Samples == 0) firstSample = DateTime.Now;
    Samples++;
    for (int pd = 0; pd < 4; pd++) {
      int m0 = pd < 2 ? 1 : 5, occ = pd % 2;
      double green = v[m0, occ] + v[m0 + 1, occ]; // all four green LEDs seen by this photodiode
      double red = v[m0 + 2, occ], ir = v[m0 + 3, occ];
      // Saturation check on the individual green readings (the sum can legitimately exceed full scale).
      bool greenSat = Math.Abs(v[m0, occ]) >= 523287 || Math.Abs(v[m0 + 1, occ]) >= 523287;
      LastGreen[pd] = green; LastRed[pd] = red; LastIr[pd] = ir;
      Estimator.Add(pd, green, greenSat);
      Oximeter.Add(pd, red, ir);
    }
    if (RawLog != null) RawLog.Add((int)LastGreen[0]);
    CheckRate();
  }

  // BLE timing is bursty, but over several seconds the average sample rate is reliable.
  void CheckRate() {
    if (rateChecked) return;
    double secs = (DateTime.Now - firstSample).TotalSeconds;
    if (secs < 8) return;
    rateChecked = true;
    double measured = Samples / secs;
    Log("Measured sample rate: " + measured.ToString("F1") + " Hz");
    if (Math.Abs(measured - configuredFs) / configuredFs > 0.15) {
      Log("Using measured rate instead of register value.");
      CreateEstimators(measured);
    }
  }

  public void Dispose() {
    if (board == null) return;
    try { board.Packet -= OnPacket; board.EnableSensors(false); } catch { }
    board.Dispose();
    board = null;
  }
}
