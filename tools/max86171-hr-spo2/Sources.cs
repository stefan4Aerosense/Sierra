using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Threading;

// The app can read from more than one sensor. Everything downstream (display, waveform, session
// recording) works through this interface, so a new sensor only has to implement it.
interface ISensorSource : IDisposable {
  string Name { get; }                  // shown on the tab
  object Sync { get; }                  // lock this before touching the estimators
  MultiHeartRate Estimator { get; }
  MultiPathOximeter Oximeter { get; }
  long Samples { get; }
  long Dropped { get; }
  double SampleRate { get; }
  Action<string> Log { get; set; }
  void Start();

  // Some devices compute their own heart rate and SpO2 on board. When they do, we show theirs next to
  // ours rather than choosing between them.
  bool HasDeviceValues { get; }
  DeviceReading DeviceReading { get; }
}

class DeviceReading {
  public double Bpm, SpO2;
  public int Confidence, ContactState;
  public bool Valid;
  public DateTime At;
  public bool Fresh { get { return Valid && (DateTime.Now - At).TotalSeconds < 3; } }
}

// Seeed XIAO RP2040 + SparkFun SEN-15219 (MAX30101 optics behind a MAX32664 hub), reached over USB
// serial. The firmware (xiao_hr_usb 0.3.1, by HOTEL) prints one JSON object per line:
//   {"type":"s",  ...,"c1":[IR...],"c2":[red...],"c3":[0...],"c4":[hub counter...]}
//   {"type":"hr", "heart_rate_bpm":76.5,"confidence":99,"spo2_pct":98.6,"finger_state":3,"valid":true}
// c1 = IR and c2 = red were established by measurement, not assumed (HANDOVER 9.3). c3 is always 0:
// green cannot be read on this board at all, so heart rate here runs off infrared.
class XiaoStream : ISensorSource {
  public const int Vid = 0x2E8A; // Raspberry Pi RP2040
  public const int Pid = 0x000A;

  readonly object sync = new object();
  SerialPort port;
  Thread reader;
  volatile bool running;
  readonly DeviceReading device = new DeviceReading();
  double fs = 100;                       // the device runs at 100/s with a finger on it, 25/s without
  DateTime rateSince;
  long samplesAtRateCheck;

  public string Name { get { return "USB · XIAO"; } }
  public object Sync { get { return sync; } }
  public MultiHeartRate Estimator { get; private set; }
  public MultiPathOximeter Oximeter { get; private set; }
  public long Samples { get; private set; }
  public long Dropped { get; private set; }          // gaps in the hub's own sample counter
  public double SampleRate { get { return fs; } }
  public Action<string> Log { get; set; }
  public bool HasDeviceValues { get { return true; } }
  public DeviceReading DeviceReading { get { return device; } }
  public string PortName { get; private set; }
  public string Firmware { get; private set; }

  public XiaoStream() { Log = delegate { }; }

  // Returns the COM port the XIAO is on, or null. Matched by USB VID/PID, not by port number,
  // because the port moves between replugs.
  public static string FindPort() {
    try {
      var search = new System.Management.ManagementObjectSearcher(
        "SELECT Name, DeviceID FROM Win32_PnPEntity WHERE PNPDeviceID LIKE 'USB\\\\VID_2E8A&PID_000A%'");
      foreach (var o in search.Get()) {
        string name = Convert.ToString(o["Name"]);
        int open = name.LastIndexOf("(COM", StringComparison.OrdinalIgnoreCase);
        if (open >= 0) {
          int close = name.IndexOf(')', open);
          if (close > open) return name.Substring(open + 1, close - open - 1);
        }
      }
    } catch (Exception) { }
    // Fall back to any single serial port, so a machine without WMI still works.
    var ports = SerialPort.GetPortNames();
    return ports.Length == 1 ? ports[0] : null;
  }

  public void Start() {
    PortName = FindPort();
    if (PortName == null)
      throw new Exception("No XIAO sensor found. Plug it in over USB and check it appears as a COM port.");
    Log("Opening " + PortName + "...");
    port = new SerialPort(PortName, 115200, Parity.None, 8, StopBits.One);
    port.ReadTimeout = 2000;
    port.DtrEnable = true;                 // the firmware needs DTR for the USB serial link to open
    port.NewLine = "\n";
    port.Open();
    CreateEstimators(fs);
    rateSince = DateTime.Now; samplesAtRateCheck = 0;
    running = true;
    reader = new Thread(ReadLoop);
    reader.IsBackground = true;
    reader.Start();
    try { port.WriteLine("info"); } catch (Exception) { }
    Log("Streaming from the USB sensor. Rest a fingertip lightly on the window and keep still.");
  }

  void CreateEstimators(double rate) {
    Estimator = new MultiHeartRate(rate, "infrared");
    Oximeter = new MultiPathOximeter(rate, "sensor");
  }

  void ReadLoop() {
    while (running) {
      string line;
      try { line = port.ReadLine(); }
      catch (TimeoutException) { continue; }
      catch (Exception ex) { if (running) Log("Serial error: " + ex.Message); return; }
      try { Handle(line); } catch (Exception ex) { Log("Could not read a line: " + ex.Message); }
    }
  }

  void Handle(string line) {
    string type = Str(line, "type");
    if (type == "s") {
      var ir = Ints(line, "c1");
      var red = Ints(line, "c2");
      var counter = Ints(line, "c4");
      lock (sync) {
        int n = Math.Min(ir.Count, red.Count);
        for (int i = 0; i < n; i++) {
          // The MAX30101 counts light going UP as the pulse pushes blood away; our estimators expect
          // the same convention as the MAX86171, which Add() already inverts.
          Estimator.Add(0, ir[i]);
          Oximeter.Add(0, red[i], ir[i]);
          Samples++;
        }
        if (counter.Count > 0) {
          if (lastCounter >= 0 && counter[0] != lastCounter + 1) Dropped += counter[0] - lastCounter - 1;
          lastCounter = counter[counter.Count - 1];
        }
        CheckRate();
      }
    } else if (type == "hr") {
      lock (sync) {
        device.Bpm = Num(line, "heart_rate_bpm");
        device.SpO2 = Num(line, "spo2_pct");
        device.Confidence = (int)Num(line, "confidence");
        device.ContactState = (int)Num(line, "finger_state");
        device.Valid = line.Contains("\"valid\":true");
        device.At = DateTime.Now;
      }
    } else if (type == "info") {
      Firmware = Str(line, "fw");
      Log("Device: " + Firmware + ", hub " + Str(line, "hub_fw") + " on " + PortName);
    } else if (type == "error") {
      Log("Sensor error: " + Str(line, "what") + " at " + Str(line, "where"));
    }
  }

  int lastCounter = -1;

  // The hub switches between 25/s (nothing on the sensor) and 100/s (something is). The estimators are
  // built for one rate, so they are rebuilt when the real rate moves away from it.
  void CheckRate() {
    double secs = (DateTime.Now - rateSince).TotalSeconds;
    if (secs < 5) return;
    double measured = (Samples - samplesAtRateCheck) / secs;
    rateSince = DateTime.Now; samplesAtRateCheck = Samples;
    if (measured < 5) return;
    if (Math.Abs(measured - fs) / fs > 0.15) {
      Log("Sample rate is now " + measured.ToString("F0") + " Hz; restarting the estimators.");
      fs = measured;
      CreateEstimators(fs);
    }
  }

  // --- the small amount of JSON this firmware actually emits ---------------------------------------

  static string Str(string line, string key) {
    int i = line.IndexOf("\"" + key + "\":\"", StringComparison.Ordinal);
    if (i < 0) return null;
    i += key.Length + 4;
    int j = line.IndexOf('"', i);
    return j < 0 ? null : line.Substring(i, j - i);
  }

  static double Num(string line, string key) {
    int i = line.IndexOf("\"" + key + "\":", StringComparison.Ordinal);
    if (i < 0) return 0;
    i += key.Length + 3;
    int j = i;
    while (j < line.Length && (char.IsDigit(line[j]) || line[j] == '-' || line[j] == '.' || line[j] == '+' || line[j] == 'e' || line[j] == 'E')) j++;
    double v;
    return double.TryParse(line.Substring(i, j - i), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0;
  }

  static List<int> Ints(string line, string key) {
    var list = new List<int>();
    int i = line.IndexOf("\"" + key + "\":[", StringComparison.Ordinal);
    if (i < 0) return list;
    i += key.Length + 4;
    int end = line.IndexOf(']', i);
    if (end < 0) return list;
    foreach (var part in line.Substring(i, end - i).Split(',')) {
      int v;
      if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) list.Add(v);
    }
    return list;
  }

  public void Dispose() {
    running = false;
    try { if (reader != null) reader.Join(1500); } catch (Exception) { }
    try { if (port != null && port.IsOpen) port.Close(); } catch (Exception) { }
    port = null;
  }
}

// A stand-in sensor fed with a synthetic pulse. Used by "--shot <png> compare" to render the compare
// tab without hardware, so the layout can be checked and regressions caught.
class FakeSource : ISensorSource {
  readonly object sync = new object();
  public string Name { get; private set; }
  public object Sync { get { return sync; } }
  public MultiHeartRate Estimator { get; private set; }
  public MultiPathOximeter Oximeter { get; private set; }
  public long Samples { get; private set; }
  public long Dropped { get { return 0; } }
  public double SampleRate { get; private set; }
  public Action<string> Log { get; set; }
  public bool HasDeviceValues { get; private set; }
  public DeviceReading DeviceReading { get; private set; }

  public FakeSource(string name, double fs, double bpm, bool deviceValues) {
    Name = name; SampleRate = fs; Log = delegate { };
    HasDeviceValues = deviceValues;
    Estimator = new MultiHeartRate(fs, "channel");
    Oximeter = new MultiPathOximeter(fs, "path");
    if (deviceValues) DeviceReading = new DeviceReading { Bpm = bpm + 1, SpO2 = 98, Confidence = 99, Valid = true, At = DateTime.Now };
    var rnd = new Random(4);
    for (int i = 0; i < fs * 30; i++) {
      double t = i / fs, ph = 2 * Math.PI * (bpm / 60.0) * t;
      double pulse = -Math.Sin(ph) - 0.4 * Math.Sin(2 * ph + 0.8);
      double ir = 300000 + 2200 * pulse + 900 * Math.Sin(2 * Math.PI * 0.25 * t) + 120 * (rnd.NextDouble() - 0.5);
      double red = 240000 + 900 * pulse + 700 * Math.Sin(2 * Math.PI * 0.25 * t) + 120 * (rnd.NextDouble() - 0.5);
      Estimator.Add(0, ir); Oximeter.Add(0, red, ir); Samples++;
    }
  }
  public void Start() { }
  public void Dispose() { }
}
