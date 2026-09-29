using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

static class Program {
  [STAThread]
  static int Main(string[] args) {
    if (args.Length > 0 && args[0] == "--selftest") return SelfTest.Run();
    if (args.Length == 4 && args[0] == "--replay") return SelfTest.Replay(args[1], args[2], args[3]);
    if (args.Length == 7 && args[0] == "--replay2")
      return SelfTest.Replay2(double.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture), args[2], args[3], args[4], args[5], args[6]);
    if (args.Length > 0 && args[0] == "--xiao") {
      int rc2 = 0, secs2 = args.Length > 1 ? int.Parse(args[1]) : 30;
      var t2 = new Thread(() => rc2 = XiaoDiag(secs2));
      t2.SetApartmentState(ApartmentState.MTA);
      t2.Start(); t2.Join();
      return rc2;
    }
    if (args.Length > 0 && args[0] == "--diag") {
      // WinRT Bluetooth calls hang on an STA thread without a message loop, so run on an MTA thread.
      int rc = 0, secs = args.Length > 1 ? int.Parse(args[1]) : 30;
      var t = new Thread(() => rc = Diag(secs));
      t.SetApartmentState(ApartmentState.MTA);
      t.Start(); t.Join();
      return rc;
    }
    Application.EnableVisualStyles();
    // --shot <file.png>: render the window to an image and exit (used to check the layout without hardware).
    if (args.Length >= 2 && args[0] == "--shot") {
      using (var f = new MainForm()) {
        f.Show(); Application.DoEvents();
        if (args.Length > 2 && args[2] == "demo") { f.ShowDemo(); Application.DoEvents(); }
        using (var bmp = new Bitmap(f.Width, f.Height)) {
          f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
          bmp.Save(args[1], System.Drawing.Imaging.ImageFormat.Png);
        }
      }
      return 0;
    }
    Application.Run(new MainForm());
    return 0;
  }

  // Console run for the USB sensor: stream for N seconds, printing our reading beside the device's own.
  static int XiaoDiag(int seconds) {
    using (var s = new XiaoStream()) {
      s.Log = m => Console.WriteLine(DateTime.Now.ToString("HH:mm:ss ") + m);
      try { s.Start(); } catch (Exception ex) { Console.WriteLine("ERROR: " + ex.Message); return 1; }
      for (int t = 1; t <= seconds; t++) {
        Thread.Sleep(1000);
        lock (s.Sync) {
          double bpm, q;
          bool ok = s.Estimator.Estimate(out bpm, out q);
          var d = s.DeviceReading;
          string ours = ok ? bpm.ToString("F0") + " BPM (q=" + q.ToString("F2") + ")" : "no HR";
          string theirs = d.Fresh ? d.Bpm.ToString("F0") + " BPM (conf " + d.Confidence + ", SpO2 " + d.SpO2.ToString("F0") + "%)" : "no valid reading";
          string ox = "";
          if (ok) { var r = s.Oximeter.Estimate(bpm); ox = r.Problem != null ? "SpO2: " + r.Problem : "SpO2 " + r.SpO2.ToString("F0") + "% (R=" + r.R.ToString("F2") + ", SNR=" + r.Snr.ToString("F0") + ")"; }
          Console.WriteLine("t={0,3}s n={1,6} @{2,5:F1}Hz drop={3}  OURS: {4,-24} {5,-34}  DEVICE: {6}",
            t, s.Samples, s.SampleRate, s.Dropped, ours, ox, theirs);
        }
      }
    }
    return 0;
  }

  // Console run for troubleshooting: stream for N seconds and print readings.
  static int Diag(int seconds) {
    using (var s = new PpgStream()) {
      s.Log = m => Console.WriteLine(DateTime.Now.ToString("HH:mm:ss ") + m);
      var dump = Environment.GetEnvironmentVariable("HR_DUMP");
      if (!string.IsNullOrEmpty(dump)) s.RawLog = new System.Collections.Generic.List<int>();
      try { s.Start(); } catch (Exception ex) { Console.WriteLine("ERROR: " + ex); return 1; }
      for (int t = 1; t <= seconds; t++) {
        Thread.Sleep(1000);
        lock (s.Sync) {
          double bpm, q;
          bool ok = s.Estimator.Estimate(out bpm, out q);
          string ox = "";
          if (ok) {
            var r = s.Oximeter.Estimate(bpm);
            ox = r.Problem != null ? "SpO2: " + r.Problem : string.Format("SpO2 ~{0:F0}% +-{4:F0} via {5} (R={1:F2}, PI_ir={2:F3}%, SNR={6:F0}, phase={3:F0} deg)", r.SpO2, r.R, r.PerfusionIr, r.PhaseDeg, r.Spread, r.Path, r.Snr);
          }
          Func<double[], string> fmt = a => string.Join("/", a.Select(x => (x / 1000).ToString("F0") + "k"));
          Console.WriteLine("t={0,3}s n={1,5} frames={8} drop={2} G={3} R={4} IR={5} -> {6} {7}",
            t, s.Samples, s.DroppedPackets, fmt(s.LastGreen), fmt(s.LastRed), fmt(s.LastIr),
            ok ? bpm.ToString("F0") + " BPM via " + s.Estimator.BestChannel + " (q=" + q.ToString("F2") + ")" : "no HR (q=" + q.ToString("F2") + ")", ox, s.Frames);
        }
      }
      Console.WriteLine("first frames: " + s.LastFrameDebug);
      if (s.RawLog != null) lock (s.Sync) System.IO.File.WriteAllLines(dump, s.RawLog.ConvertAll(x => x.ToString()));
    }
    return 0;
  }
}

class MainForm : Form {
  // Two sensors, each on its own tab: 0 = the Bluetooth board, 1 = the USB sensor. Tab 2 compares them.
  const int TabBle = 0, TabUsb = 1, TabCompare = 2;
  readonly ISensorSource[] sources = new ISensorSource[2];
  int activeTab = TabBle;

  readonly DigitLabel bpmLabel = new DigitLabel(), spo2Label = new DigitLabel();
  readonly Label bpmUnit = new Label(), spo2Unit = new Label();
  readonly Label infoLabel = new Label(), statusLabel = new Label();
  readonly Button startButton = new Button(), stopButton = new Button();
  readonly Button[] tabButtons = new Button[3];
  readonly WavePanel wave = new WavePanel();
  readonly System.Windows.Forms.Timer uiTimer = new System.Windows.Forms.Timer();
  DateTime lastEstimate;

  // Last accepted readings per sensor, so a second or two of movement does not blank the display.
  const int HoldSeconds = 8;
  const double FirstWindow = 5.0;  // shorter window used only until the first reading appears
  readonly double[] lastGoodBpm = new double[2], lastGoodSpO2 = new double[2];
  readonly DateTime[] lastGoodAt = new DateTime[2];
  readonly Reading[] latest = { new Reading(), new Reading() };

  // What one sensor produced this second.
  class Reading {
    public bool Ok;
    public double Bpm, Quality, SpO2, Spread, R, Snr;
    public string Channel, Problem, State = "not connected";
    public DeviceReading Device;
  }

  // Dark palette: near-black navy behind slightly lighter cards, with blue accents.
  static readonly Color Ink = Color.FromArgb(9, 12, 20);          // window background
  static readonly Color Card = Color.FromArgb(19, 25, 40);        // panels
  static readonly Color CardEdge = Color.FromArgb(32, 42, 64);    // panel outline
  static readonly Color Accent = Color.FromArgb(56, 132, 255);    // primary blue
  static readonly Color AccentSoft = Color.FromArgb(120, 190, 255); // lighter blue
  static readonly Color TextDim = Color.FromArgb(128, 145, 175);  // labels
  static readonly Color TextFaint = Color.FromArgb(96, 112, 140); // fine print
  static readonly Color Pulse = Color.FromArgb(255, 58, 70);      // luminous red for the heart rate
  static readonly Color Oxygen = Color.FromArgb(159, 238, 254);   // pale cyan for the blood oxygen
  static readonly Color Held = Color.FromArgb(86, 104, 136);      // a reading being held through movement
  static readonly Color TabOff = Color.FromArgb(19, 25, 40);
  static readonly Color TabOn = Color.FromArgb(34, 48, 78);

  readonly CardPanel bpmCard = new CardPanel(), spo2Card = new CardPanel(), infoCard = new CardPanel();
  readonly CardPanel sessionCard = new CardPanel();
  readonly PictureBox logo = new PictureBox();
  readonly TextBox nameBox = new TextBox();
  readonly Button recordButton = new Button(), folderButton = new Button(), openLastButton = new Button();
  string lastSessionFile;
  readonly Label sessionLabel = new Label();
  SessionRecorder recorder;

  public MainForm() {
    Text = "Heart Rate & SpO2 Tester";
    ClientSize = new Size(830, 640);
    MinimumSize = new Size(780, 600);
    BackColor = Ink;
    ForeColor = TextDim;
    Font = new Font("Segoe UI", 10);

    var big = DisplayFont.Digits(62);
    var small = new Font("Segoe UI", 9);

    // Tabs: pick which monitor to watch, or compare the two.
    string[] names = { "BLUETOOTH · MAX86171", "USB · XIAO", "COMPARE" };
    for (int i = 0; i < tabButtons.Length; i++) {
      var b = new Button();
      StyleButton(b, names[i], TabOff, TextDim);
      b.SetBounds(16 + i * 196, 12, 190, 34);
      int which = i;
      b.Click += (s, e) => SelectTab(which);
      tabButtons[i] = b;
    }

    logo.Image = Embedded.Lighten(Embedded.Image("hrlogo"), 0.30f);
    logo.SizeMode = PictureBoxSizeMode.Zoom;
    logo.BackColor = Color.Transparent;
    logo.SetBounds(ClientSize.Width - 16 - 176, 8, 176, 44);
    logo.Anchor = AnchorStyles.Top | AnchorStyles.Right;

    StyleButton(startButton, "Connect", Accent, Color.White);
    startButton.SetBounds(16, 56, 118, 34); startButton.Click += (s, e) => StartStream();
    StyleButton(stopButton, "Stop", Color.FromArgb(28, 36, 54), AccentSoft);
    stopButton.SetBounds(142, 56, 118, 34); stopButton.Enabled = false; stopButton.Click += (s, e) => StopStream();

    bpmCard.Accent = Pulse; bpmCard.SetBounds(16, 100, 264, 132);
    spo2Card.Accent = Oxygen; spo2Card.SetBounds(292, 100, 264, 132);
    infoCard.Accent = Color.FromArgb(48, 64, 96); infoCard.SetBounds(568, 100, 246, 132);
    infoCard.Anchor = AnchorStyles.Top | AnchorStyles.Right;

    bpmLabel.Text = "--"; bpmLabel.Font = big; bpmLabel.ForeColor = Pulse;
    bpmLabel.SetBounds(14, 6, 236, 92); bpmLabel.TextAlign = ContentAlignment.MiddleLeft;
    bpmUnit.Text = "HEART RATE  ·  BPM"; bpmUnit.SetBounds(18, 100, 240, 20);
    bpmUnit.ForeColor = TextDim; bpmUnit.Font = small;

    spo2Label.Text = "--"; spo2Label.Font = big; spo2Label.ForeColor = Oxygen;
    spo2Label.SetBounds(14, 6, 236, 92); spo2Label.TextAlign = ContentAlignment.MiddleLeft;
    spo2Unit.Text = "BLOOD OXYGEN  ·  %  (uncalibrated)"; spo2Unit.SetBounds(18, 100, 240, 20);
    spo2Unit.ForeColor = TextDim; spo2Unit.Font = small;

    infoLabel.SetBounds(14, 10, 220, 112); infoLabel.ForeColor = TextFaint; infoLabel.Font = small;
    infoLabel.Text = "Idle.\nPick a monitor above,\nthen press Connect.";

    bpmCard.Controls.AddRange(new Control[] { bpmLabel, bpmUnit });
    spo2Card.Controls.AddRange(new Control[] { spo2Label, spo2Unit });
    infoCard.Controls.Add(infoLabel);

    wave.SetBounds(16, 248, 798, 230);
    wave.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;

    // Session recording: name the person, record, and every connected sensor is captured at once.
    sessionCard.Accent = Color.FromArgb(48, 64, 96); sessionCard.SetBounds(16, 490, 798, 72);
    sessionCard.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
    var recordTitle = new Label { Text = "RECORD A SESSION  ·  every connected monitor at once", Font = small, ForeColor = TextDim };
    recordTitle.SetBounds(16, 10, 400, 18);
    var nameCaption = new Label { Text = "Name", Font = small, ForeColor = TextFaint };
    nameCaption.SetBounds(16, 40, 44, 20);
    nameBox.SetBounds(60, 36, 190, 26);
    nameBox.BackColor = Color.FromArgb(28, 36, 54); nameBox.ForeColor = Color.White;
    nameBox.BorderStyle = BorderStyle.FixedSingle; nameBox.Font = new Font("Segoe UI", 10);
    StyleButton(recordButton, "Record", Color.FromArgb(28, 36, 54), AccentSoft);
    recordButton.SetBounds(258, 34, 104, 30); recordButton.Click += (s, e) => ToggleRecording();
    StyleButton(folderButton, "Open folder", Color.FromArgb(28, 36, 54), TextDim);
    folderButton.SetBounds(370, 34, 104, 30); folderButton.Click += (s, e) => OpenSessionFolder();
    StyleButton(openLastButton, "Open report", Color.FromArgb(28, 36, 54), TextDim);
    openLastButton.SetBounds(478, 34, 118, 30); openLastButton.Enabled = false;
    openLastButton.Click += (s, e) => OpenLastSession();
    sessionLabel.SetBounds(608, 12, 182, 56); sessionLabel.Font = small; sessionLabel.ForeColor = TextFaint;
    sessionLabel.Text = "Not recording.\nEach session is saved as\na PDF report and a CSV.";
    sessionCard.Controls.AddRange(new Control[] { recordTitle, nameCaption, nameBox, recordButton, folderButton, openLastButton, sessionLabel });

    statusLabel.SetBounds(18, 570, 796, 30); statusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
    statusLabel.ForeColor = TextFaint; statusLabel.Font = small;
    statusLabel.Text = "Bluetooth: switch the board on and close the Maxim GUI. USB: plug the sensor in.";

    Controls.AddRange(new Control[] { logo, tabButtons[0], tabButtons[1], tabButtons[2],
      startButton, stopButton, bpmCard, spo2Card, infoCard, wave, sessionCard, statusLabel });
    uiTimer.Interval = 50; uiTimer.Tick += (s, e) => RefreshUi();
    uiTimer.Start();
    FormClosing += (s, e) => { StopAll(); };
    HandleCreated += (s, e) => UseDarkTitleBar(Handle);
    SelectTab(TabBle);
  }

  void SelectTab(int tab) {
    activeTab = tab;
    for (int i = 0; i < tabButtons.Length; i++) {
      bool on = i == tab;
      tabButtons[i].BackColor = on ? TabOn : TabOff;
      tabButtons[i].ForeColor = on ? Color.White : TextDim;
      tabButtons[i].FlatAppearance.BorderColor = on ? Accent : CardEdge;
    }
    bool device = tab < 2;
    startButton.Visible = stopButton.Visible = device;
    if (device) {
      bool connected = sources[tab] != null;
      startButton.Enabled = !connected;
      stopButton.Enabled = connected;
      bpmUnit.Text = "HEART RATE  ·  BPM";
      spo2Unit.Text = "BLOOD OXYGEN  ·  %  (uncalibrated)";
      bpmCard.Accent = Pulse; spo2Card.Accent = Oxygen;
      bpmLabel.ForeColor = Pulse; spo2Label.ForeColor = Oxygen;
    } else {
      // Compare: the two cards become one heart rate per sensor.
      bpmUnit.Text = "BLUETOOTH  ·  BPM";
      spo2Unit.Text = "USB SENSOR  ·  BPM";
      bpmCard.Accent = WavePanel.SeriesA; spo2Card.Accent = WavePanel.SeriesB;
      bpmLabel.ForeColor = WavePanel.SeriesA; spo2Label.ForeColor = WavePanel.SeriesB;
    }
    Invalidate(true);
    RefreshUi();
  }

  [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
  static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

  // Windows 10 2004+ / 11: paint the title bar dark to match. Older builds simply ignore it.
  static void UseDarkTitleBar(IntPtr hwnd) {
    int on = 1;
    try { if (DwmSetWindowAttribute(hwnd, 20, ref on, sizeof(int)) != 0) DwmSetWindowAttribute(hwnd, 19, ref on, sizeof(int)); } catch { }
  }

  // Fills the window with representative values so the layout can be checked without hardware.
  public void ShowDemo() {
    uiTimer.Stop(); // the demo shot is a still: stop the live refresh from overwriting it
    bpmLabel.Text = "72"; spo2Label.Text = "98";
    infoLabel.Text = "Pulse quality 86% via\ndetector 3 (LED group)\n\nSpO2 +/-1%, R 0.52\nsignal/noise 24\n\nSamples 1483 @ 25.2 Hz";
    statusLabel.Text = "Streaming with all LEDs and 4 detectors.";
    var d = new double[150];
    for (int i = 0; i < d.Length; i++) {
      double ph = 2 * Math.PI * 1.2 * i / 25.0;
      d[i] = Math.Sin(ph) + 0.35 * Math.Sin(2 * ph + 0.8) + 0.05 * Math.Sin(2 * Math.PI * 0.2 * i / 25.0);
    }
    wave.Data = d; wave.Invalidate();
  }

  static void StyleButton(Button b, string text, Color back, Color fore) {
    b.Text = text;
    b.FlatStyle = FlatStyle.Flat;
    b.FlatAppearance.BorderSize = 1;
    b.FlatAppearance.BorderColor = CardEdge;
    b.FlatAppearance.MouseOverBackColor = ControlPaint.Light(back, 0.2f);
    b.BackColor = back;
    b.ForeColor = fore;
    b.Font = new Font("Segoe UI Semibold", 10);
    b.UseVisualStyleBackColor = false;
  }

  // Hands the file to Windows, which opens it in whatever spreadsheet or reader is installed.
  void OpenLastSession() {
    if (lastSessionFile == null) { statusLabel.Text = "No session has been recorded yet."; return; }
    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(lastSessionFile) { UseShellExecute = true }); }
    catch (Exception ex) { statusLabel.Text = "Could not open " + lastSessionFile + " (" + ex.Message + ")"; }
  }

  void OpenSessionFolder() {
    try {
      System.IO.Directory.CreateDirectory(SessionRecorder.Folder);
      System.Diagnostics.Process.Start("explorer.exe", "\"" + SessionRecorder.Folder + "\"");
    } catch (Exception ex) { statusLabel.Text = "Could not open the folder: " + ex.Message; }
  }

  void SetStatus(string msg) {
    if (IsDisposed) return;
    try { BeginInvoke((Action)(() => statusLabel.Text = msg)); } catch (Exception) { }
  }

  // Connects whichever monitor the current tab shows. Both can run at the same time.
  void StartStream() {
    int tab = activeTab;
    if (tab >= 2 || sources[tab] != null) return;
    startButton.Enabled = false;
    lastGoodBpm[tab] = lastGoodSpO2[tab] = 0;
    infoLabel.Text = "Connecting...";
    ISensorSource s = tab == TabBle ? (ISensorSource)new PpgStream() : new XiaoStream();
    s.Log = SetStatus;
    ThreadPool.QueueUserWorkItem(_ => {
      try {
        s.Start();
        BeginInvoke((Action)(() => {
          sources[tab] = s;
          if (activeTab == tab) stopButton.Enabled = true;
          recordButton.Enabled = true;
        }));
      } catch (Exception ex) {
        s.Dispose();
        BeginInvoke((Action)(() => {
          if (activeTab == tab) startButton.Enabled = true;
          infoLabel.Text = "";
          statusLabel.Text = "Error: " + ex.Message;
        }));
      }
    });
  }

  void StopStream() { StopSource(activeTab); }

  void StopSource(int tab) {
    if (tab >= 2 || sources[tab] == null) return;
    var s = sources[tab];
    sources[tab] = null;
    latest[tab] = new Reading();
    lastGoodBpm[tab] = lastGoodSpO2[tab] = 0;
    // Bluetooth calls must not run on the UI (STA) thread.
    var t = new Thread(s.Dispose); t.SetApartmentState(ApartmentState.MTA); t.Start(); t.Join(5000);
    if (activeTab == tab) { startButton.Enabled = true; stopButton.Enabled = false; }
    if (sources[0] == null && sources[1] == null) {
      recordButton.Enabled = recorder != null;
      if (!IsDisposed) infoLabel.Text = "Stopped.";
    }
  }

  void StopAll() {
    if (recorder != null) ToggleRecording();
    uiTimer.Stop();
    StopSource(0); StopSource(1);
  }

  // --- reading and display -------------------------------------------------------------------------

  void RefreshUi() {
    // The waveform redraws fast; the numbers once a second.
    var shown = activeTab < 2 ? sources[activeTab] : null;
    if (activeTab == TabCompare) {
      wave.Data = sources[0] != null ? sources[0].Estimator.Tail((int)(sources[0].SampleRate * 6)) : null;
      wave.Data2 = sources[1] != null ? sources[1].Estimator.Tail((int)(sources[1].SampleRate * 6)) : null;
      wave.Caption = "PULSE WAVEFORM  ·  BOTH MONITORS";
    } else if (shown != null) {
      wave.Data = shown.Estimator.Tail((int)(shown.SampleRate * 6));
      wave.Data2 = null;
      wave.Caption = activeTab == TabBle ? "PULSE WAVEFORM  ·  GREEN" : "PULSE WAVEFORM  ·  INFRARED";
    } else {
      wave.Data = wave.Data2 = null;
    }
    wave.Invalidate();

    if ((DateTime.Now - lastEstimate).TotalSeconds < 1) return;
    lastEstimate = DateTime.Now;

    if (recorder != null) recorder.Tick();
    for (int i = 0; i < 2; i++) latest[i] = Measure(i);
    if (activeTab == TabCompare) ShowComparison(); else ShowSensor(activeTab);
  }

  // Reads one sensor and, if a session is running, records what it produced this second.
  Reading Measure(int index) {
    var src = sources[index];
    var r = new Reading();
    if (src == null) return r;
    lock (src.Sync) {
      var est = src.Estimator;
      r.Device = src.HasDeviceValues ? src.DeviceReading : null;
      if (src.Samples == 0) { r.State = "waiting for data"; }
      else if (est.Count < est.SampleRate * FirstWindow) { r.State = "warming up"; }
      else {
        double bpm, q;
        // Steady state uses 8 s; before anything is on screen a 5 s window is tried as well.
        bool ok = est.Estimate(out bpm, out q);
        if (!ok && lastGoodBpm[index] == 0) ok = est.Estimate(out bpm, out q, FirstWindow);
        if (ok) {
          r.Ok = true; r.Bpm = bpm; r.Quality = q; r.Channel = est.BestChannel; r.State = "ok";
          lastGoodBpm[index] = bpm; lastGoodAt[index] = DateTime.Now;
          var ox = src.Oximeter.Estimate(bpm);
          r.Problem = ox.Problem; r.Snr = ox.Snr; r.R = ox.R;
          if (ox.Problem == null) { r.SpO2 = ox.SpO2; r.Spread = ox.Spread; lastGoodSpO2[index] = ox.SpO2; }
          else r.State = "heart rate only";
        } else {
          int age = (int)(DateTime.Now - lastGoodAt[index]).TotalSeconds;
          r.State = lastGoodBpm[index] > 0 && age <= HoldSeconds ? "movement - holding" : "no clear pulse";
          if (r.State == "no clear pulse") lastGoodBpm[index] = lastGoodSpO2[index] = 0;
        }
      }
    }
    if (recorder != null && src != null)
      recorder.Add(src.Name, r.Ok ? r.Bpm : 0, r.Quality, r.Channel, r.SpO2, r.Spread, r.R, r.Snr, r.Device, r.State);
    return r;
  }

  void ShowSensor(int index) {
    var src = sources[index];
    if (src == null) {
      bpmLabel.Text = "--"; spo2Label.Text = "--";
      bpmLabel.ForeColor = spo2Label.ForeColor = Held;
      infoLabel.Text = index == TabBle
        ? "Not connected.\nSwitch the board on and\npress Connect."
        : "Not connected.\nPlug the USB sensor in\nand press Connect.";
      return;
    }
    var r = latest[index];
    string counts = "Samples " + src.Samples + " @ " + src.SampleRate.ToString("F1") + " Hz\nDropped " + src.Dropped;
    string deviceLine = "";
    if (r.Device != null)
      deviceLine = r.Device.Fresh
        ? "\nSensor's own: " + r.Device.Bpm.ToString("F0") + " BPM (conf " + r.Device.Confidence + ")"
          + (r.Device.SpO2 > 0 ? ", SpO2 " + r.Device.SpO2.ToString("F0") + "%" : "")
        : "\nSensor's own: no valid reading";

    if (r.State == "waiting for data") { infoLabel.Text = "Waiting for data...\n" + counts; return; }
    if (r.State == "warming up") {
      double left = FirstWindow - src.Estimator.Count / src.Estimator.SampleRate;
      infoLabel.Text = "Measuring... keep still.\nFirst reading in " + Math.Max(1, (int)Math.Ceiling(left)) + " s.\n" + counts + deviceLine;
      return;
    }
    if (!r.Ok) {
      int age = (int)(DateTime.Now - lastGoodAt[index]).TotalSeconds;
      if (lastGoodBpm[index] > 0 && age <= HoldSeconds) {
        bpmLabel.ForeColor = Held; bpmLabel.Text = lastGoodBpm[index].ToString("F0");
        if (lastGoodSpO2[index] > 0) { spo2Label.ForeColor = Held; spo2Label.Text = lastGoodSpO2[index].ToString("F0"); }
        infoLabel.Text = "Movement - no clear pulse.\nHolding the last reading\n(" + age + " s old).\n" + counts + deviceLine;
      } else {
        bpmLabel.ForeColor = spo2Label.ForeColor = Held;
        bpmLabel.Text = "--"; spo2Label.Text = "--";
        infoLabel.Text = "No clear pulse.\nRest the sensor lightly\nagainst the skin, keep still.\n" + counts + deviceLine;
      }
      return;
    }
    bpmLabel.ForeColor = Pulse; bpmLabel.Text = r.Bpm.ToString("F0");
    string info = "Pulse quality " + (r.Quality * 100).ToString("F0") + "% via " + r.Channel + "\n";
    if (r.Problem == null) {
      spo2Label.ForeColor = Oxygen; spo2Label.Text = r.SpO2.ToString("F0");
      info += (r.Spread > 3 ? "SpO2 UNSTABLE" : "SpO2") + " +/-" + r.Spread.ToString("F0") + "%, R " + r.R.ToString("F2")
            + "\nsignal/noise " + r.Snr.ToString("F0") + "\n";
    } else {
      int age = (int)(DateTime.Now - lastGoodAt[index]).TotalSeconds;
      if (lastGoodSpO2[index] > 0 && age <= HoldSeconds) { spo2Label.ForeColor = Held; spo2Label.Text = lastGoodSpO2[index].ToString("F0"); }
      else { spo2Label.ForeColor = Held; spo2Label.Text = "--"; lastGoodSpO2[index] = 0; }
      info += "SpO2: " + r.Problem + "\n";
    }
    infoLabel.Text = info + counts + deviceLine;
  }

  // Compare view: one heart rate per monitor, and the difference between them.
  void ShowComparison() {
    Func<int, string> show = i => sources[i] == null ? "--" : (latest[i].Ok ? latest[i].Bpm.ToString("F0")
                                : (lastGoodBpm[i] > 0 && (DateTime.Now - lastGoodAt[i]).TotalSeconds <= HoldSeconds ? lastGoodBpm[i].ToString("F0") : "--"));
    bpmLabel.Text = show(0); spo2Label.Text = show(1);
    bpmLabel.ForeColor = sources[0] != null && latest[0].Ok ? WavePanel.SeriesA : Held;
    spo2Label.ForeColor = sources[1] != null && latest[1].Ok ? WavePanel.SeriesB : Held;

    var lines = new List<string>();
    if (sources[0] == null || sources[1] == null) {
      lines.Add("Connect both monitors to");
      lines.Add("compare them: use the tabs,");
      lines.Add("press Connect on each.");
      lines.Add("");
      lines.Add("Bluetooth: " + (sources[0] == null ? "not connected" : "connected"));
      lines.Add("USB: " + (sources[1] == null ? "not connected" : "connected"));
    } else if (latest[0].Ok && latest[1].Ok) {
      double diff = latest[0].Bpm - latest[1].Bpm;
      lines.Add("Difference " + diff.ToString("+0.0;-0.0;0.0") + " BPM");
      lines.Add("");
      lines.Add("Bluetooth " + latest[0].Bpm.ToString("F0") + " BPM, SpO2 " + (latest[0].SpO2 > 0 ? latest[0].SpO2.ToString("F0") + "%" : "--"));
      lines.Add("USB " + latest[1].Bpm.ToString("F0") + " BPM, SpO2 " + (latest[1].SpO2 > 0 ? latest[1].SpO2.ToString("F0") + "%" : "--"));
      if (latest[1].Device != null && latest[1].Device.Fresh)
        lines.Add("USB sensor's own " + latest[1].Device.Bpm.ToString("F0") + " BPM");
    } else {
      lines.Add("Waiting for both to read.");
      lines.Add("");
      lines.Add("Bluetooth: " + latest[0].State);
      lines.Add("USB: " + latest[1].State);
    }
    infoLabel.Text = string.Join("\n", lines.ToArray());
  }

  // --- session recording ---------------------------------------------------------------------------

  void ToggleRecording() {
    if (recorder != null) {
      var s = recorder.Stop();
      recorder = null;
      StyleButton(recordButton, "Record", Color.FromArgb(28, 36, 54), AccentSoft);
      lastSessionFile = s.ReportPath != null && System.IO.File.Exists(s.ReportPath) ? s.ReportPath : s.Path;
      openLastButton.Enabled = true;
      var text = new List<string> { "Saved. " + s.Seconds + " s" };
      foreach (var st in s.PerSource)
        text.Add(st.Source.Replace(" · ", " ") + ": " + (st.Readings > 0 ? st.MeanBpm.ToString("F0") + " BPM mean" : "no readings"));
      if (s.Compared != null && s.Compared.Paired > 0)
        text.Add("Agreement " + s.Compared.MeanDifference.ToString("+0.0;-0.0;0.0") + " BPM");
      sessionLabel.Text = string.Join("\n", text.ToArray());
      statusLabel.Text = "Saved " + System.IO.Path.GetFileName(lastSessionFile) + " (PDF report and CSV) in " + SessionRecorder.Folder
        + (s.ReportError != null ? " - PDF failed: " + s.ReportError : "");
      return;
    }
    var connected = new List<string>();
    for (int i = 0; i < 2; i++) if (sources[i] != null) connected.Add(sources[i].Name);
    if (connected.Count == 0) { statusLabel.Text = "Connect a monitor first - there is nothing to record yet."; return; }
    recorder = new SessionRecorder(nameBox.Text, connected.ToArray());
    StyleButton(recordButton, "Stop recording", Color.FromArgb(150, 40, 60), Color.White);
    sessionLabel.Text = "Recording " + recorder.Subject + "...\n" + connected.Count + " monitor" + (connected.Count > 1 ? "s" : "");
    statusLabel.Text = "Recording to " + recorder.Path;
  }
}

// Files built into the exe (see /res: in build.cmd), so the built file carries everything it needs.
static class Embedded {
  public static System.IO.Stream Open(string name) {
    return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
  }
  public static Image Image(string name) {
    try { using (var s = Open(name)) return s == null ? null : System.Drawing.Image.FromStream(s); }
    catch { return null; }
  }

  // The logo's own colours are meant for white paper and go muddy on a near-black background, so it is
  // lifted towards white on load. Transparency is preserved and the original file is left untouched.
  public static Image Lighten(Image src, float amount) {
    if (src == null) return null;
    var bmp = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
    using (var g = Graphics.FromImage(bmp))
    using (var attr = new System.Drawing.Imaging.ImageAttributes()) {
      var m = new System.Drawing.Imaging.ColorMatrix(new[] {
        new[] { 1f, 0f, 0f, 0f, 0f },
        new[] { 0f, 1f, 0f, 0f, 0f },
        new[] { 0f, 0f, 1f, 0f, 0f },
        new[] { 0f, 0f, 0f, 1f, 0f },
        new[] { amount, amount, amount, 0f, 1f } });
      attr.SetColorMatrix(m);
      g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, attr);
    }
    src.Dispose();
    return bmp;
  }
}

// The display font (Chakra Petch Bold Italic, SIL OFL - see assets/) is embedded in the exe and loaded from
// memory, so nothing has to be installed on the machine. Falls back to a stock font if anything goes wrong.
static class DisplayFont {
  static readonly System.Drawing.Text.PrivateFontCollection Collection = new System.Drawing.Text.PrivateFontCollection();
  static readonly FontFamily Family;

  static DisplayFont() {
    try {
      using (var s = Embedded.Open("hrfont")) {
        if (s != null) {
          var bytes = new byte[s.Length];
          s.Read(bytes, 0, bytes.Length);
          IntPtr p = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(bytes.Length);
          System.Runtime.InteropServices.Marshal.Copy(bytes, 0, p, bytes.Length);
          Collection.AddMemoryFont(p, bytes.Length);
          Family = Collection.Families[0];
        }
      }
    } catch { }
  }

  public static Font Digits(float pixelSize) {
    if (Family != null) return new Font(Family, pixelSize, FontStyle.Bold, GraphicsUnit.Pixel);
    return new Font("Segoe UI Light", pixelSize, FontStyle.Regular, GraphicsUnit.Pixel);
  }
}

// Draws its text stretched horizontally, which gives the wide slanted look of the reference numerals.
class DigitLabel : Label {
  public float Stretch = 1.3f;
  public DigitLabel() { DoubleBuffered = true; }
  protected override void OnPaint(PaintEventArgs e) {
    var g = e.Graphics;
    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
    var size = g.MeasureString(Text, Font);
    var state = g.Save();
    g.MultiplyTransform(new System.Drawing.Drawing2D.Matrix(Stretch, 0, 0, 1, 0, 0));
    using (var br = new SolidBrush(ForeColor))
      g.DrawString(Text, Font, br, 0, (Height - size.Height) / 2);
    g.Restore(state);
  }
}

// A flat dark card with a thin coloured bar down its left edge.
class CardPanel : Panel {
  public Color Accent = Color.FromArgb(56, 132, 255);
  public CardPanel() { DoubleBuffered = true; BackColor = Color.FromArgb(19, 25, 40); }
  protected override void OnPaint(PaintEventArgs e) {
    base.OnPaint(e);
    using (var edge = new Pen(Color.FromArgb(32, 42, 64))) e.Graphics.DrawRectangle(edge, 0, 0, Width - 1, Height - 1);
    using (var bar = new SolidBrush(Accent)) e.Graphics.FillRectangle(bar, 0, 0, 3, Height);
  }
}

class WavePanel : Panel {
  public double[] Data = new double[0];
  public double[] Data2;                  // second monitor, drawn on the same axes when comparing
  public string Caption = "PULSE WAVEFORM";
  // The trace colour when one monitor is shown, and one colour per monitor when comparing.
  static readonly Color Line = Color.FromArgb(190, 255, 60);      // luminous green-yellow
  public static readonly Color SeriesA = Color.FromArgb(120, 190, 255);  // Bluetooth board
  public static readonly Color SeriesB = Color.FromArgb(255, 176, 64);   // USB sensor

  public WavePanel() { DoubleBuffered = true; BackColor = Color.FromArgb(13, 18, 30); }

  protected override void OnPaint(PaintEventArgs e) {
    base.OnPaint(e);
    var g = e.Graphics;
    using (var edge = new Pen(Color.FromArgb(32, 42, 64))) g.DrawRectangle(edge, 0, 0, Width - 1, Height - 1);
    // Faint grid, so the pulse has something to be read against.
    using (var grid = new Pen(Color.FromArgb(20, 27, 43))) {
      for (int i = 1; i < 6; i++) { int y = Height * i / 6; g.DrawLine(grid, 1, y, Width - 2, y); }
      for (int i = 1; i < 12; i++) { int x = Width * i / 12; g.DrawLine(grid, x, 1, x, Height - 2); }
    }
    var small = new Font("Segoe UI", 8);
    using (var dim = new SolidBrush(Color.FromArgb(96, 112, 140))) g.DrawString(Caption, small, dim, 10, 8);
    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

    bool comparing = Data2 != null && Data2.Length > 1;
    if (comparing) {
      // Two monitors: each keeps its own half of the panel, so neither hides the other.
      DrawSeries(g, Data, SeriesA, 24, Height / 2 - 6, false);
      DrawSeries(g, Data2, SeriesB, Height / 2 + 4, Height - 10, false);
      using (var b = new SolidBrush(SeriesA)) g.DrawString("Bluetooth", small, b, Width - 150, 8);
      using (var b = new SolidBrush(SeriesB)) g.DrawString("USB sensor", small, b, Width - 80, 8);
    } else {
      DrawSeries(g, Data, Line, 24, Height - 10, true);
    }
  }

  // Draws one trace scaled into the band between top and bottom.
  static void DrawSeries(Graphics g, double[] d, Color colour, float top, float bottom, bool fill) {
    if (d == null || d.Length < 2 || bottom - top < 10) return;
    double min = double.MaxValue, max = double.MinValue;
    foreach (var v in d) { if (v < min) min = v; if (v > max) max = v; }
    double span = Math.Max(max - min, 1e-9);
    int w = (int)g.VisibleClipBounds.Width;
    var pts = new PointF[d.Length];
    for (int i = 0; i < d.Length; i++)
      pts[i] = new PointF(i * (w - 1f) / (d.Length - 1), (float)(bottom - (d[i] - min) / span * (bottom - top)));
    if (fill) {
      var poly = new PointF[pts.Length + 2];
      Array.Copy(pts, poly, pts.Length);
      poly[pts.Length] = new PointF(pts[pts.Length - 1].X, bottom + 20);
      poly[pts.Length + 1] = new PointF(pts[0].X, bottom + 20);
      using (var under = new SolidBrush(Color.FromArgb(14, colour))) g.FillPolygon(under, poly);
    }
    using (var glow = new Pen(Color.FromArgb(48, colour), 7)) g.DrawLines(glow, pts);
    using (var pen = new Pen(colour, 2)) g.DrawLines(pen, pts);
  }
}
