using System;
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
  readonly DigitLabel bpmLabel = new DigitLabel(), spo2Label = new DigitLabel();
  readonly Label bpmUnit = new Label(), spo2Unit = new Label();
  readonly Label infoLabel = new Label(), statusLabel = new Label();
  readonly Button startButton = new Button(), stopButton = new Button();
  readonly WavePanel wave = new WavePanel();
  readonly System.Windows.Forms.Timer uiTimer = new System.Windows.Forms.Timer();
  PpgStream stream;
  DateTime lastEstimate;
  // Last accepted readings, kept so a second or two of movement does not blank the display.
  const int HoldSeconds = 8;
  const double FirstWindow = 5.0;  // shorter window used only until the first reading appears
  double lastGoodBpm, lastGoodSpO2;
  DateTime lastGoodAt;

  // Dark palette: near-black navy behind slightly lighter cards, with blue accents.
  static readonly Color Ink = Color.FromArgb(9, 12, 20);          // window background
  static readonly Color Card = Color.FromArgb(19, 25, 40);        // panels
  static readonly Color CardEdge = Color.FromArgb(32, 42, 64);    // panel outline
  static readonly Color Accent = Color.FromArgb(56, 132, 255);    // primary blue
  static readonly Color AccentSoft = Color.FromArgb(120, 190, 255); // lighter blue
  static readonly Color TextDim = Color.FromArgb(128, 145, 175);  // labels
  static readonly Color TextFaint = Color.FromArgb(96, 112, 140); // fine print
  static readonly Color Held = Color.FromArgb(86, 104, 136);      // a reading being held through movement

  readonly CardPanel bpmCard = new CardPanel(), spo2Card = new CardPanel(), infoCard = new CardPanel();
  readonly CardPanel sessionCard = new CardPanel();
  readonly TextBox nameBox = new TextBox();
  readonly Button recordButton = new Button(), folderButton = new Button();
  readonly Label sessionLabel = new Label();
  SessionRecorder recorder;

  public MainForm() {
    Text = "Heart Rate & SpO2 Tester (MAX86171)";
    ClientSize = new Size(820, 600);
    MinimumSize = new Size(760, 560);
    BackColor = Ink;
    ForeColor = TextDim;
    Font = new Font("Segoe UI", 10);

    StyleButton(startButton, "Start", Accent, Color.White);
    startButton.SetBounds(16, 16, 118, 38); startButton.Click += (s, e) => StartStream();
    StyleButton(stopButton, "Stop", Color.FromArgb(28, 36, 54), AccentSoft);
    stopButton.SetBounds(146, 16, 118, 38); stopButton.Enabled = false; stopButton.Click += (s, e) => StopStream();

    var big = DisplayFont.Digits(62);
    var small = new Font("Segoe UI", 9);

    bpmCard.Accent = AccentSoft; bpmCard.SetBounds(16, 68, 264, 132);
    spo2Card.Accent = Accent; spo2Card.SetBounds(292, 68, 264, 132);
    infoCard.Accent = Color.FromArgb(48, 64, 96); infoCard.SetBounds(568, 68, 236, 132);
    infoCard.Anchor = AnchorStyles.Top | AnchorStyles.Right;

    bpmLabel.Text = "--"; bpmLabel.Font = big; bpmLabel.ForeColor = AccentSoft;
    bpmLabel.SetBounds(14, 6, 236, 92); bpmLabel.TextAlign = ContentAlignment.MiddleLeft;
    bpmUnit.Text = "HEART RATE  ·  BPM"; bpmUnit.SetBounds(18, 100, 240, 20);
    bpmUnit.ForeColor = TextDim; bpmUnit.Font = small;

    spo2Label.Text = "--"; spo2Label.Font = big; spo2Label.ForeColor = Accent;
    spo2Label.SetBounds(14, 6, 236, 92); spo2Label.TextAlign = ContentAlignment.MiddleLeft;
    spo2Unit.Text = "BLOOD OXYGEN  ·  %  (uncalibrated)"; spo2Unit.SetBounds(18, 100, 240, 20);
    spo2Unit.ForeColor = TextDim; spo2Unit.Font = small;

    infoLabel.SetBounds(14, 10, 210, 112); infoLabel.ForeColor = TextFaint; infoLabel.Font = small;
    infoLabel.Text = "Idle.\nPress Start to connect\nto the sensor board.";

    bpmCard.Controls.AddRange(new Control[] { bpmLabel, bpmUnit });
    spo2Card.Controls.AddRange(new Control[] { spo2Label, spo2Unit });
    infoCard.Controls.Add(infoLabel);

    wave.SetBounds(16, 216, 788, 250);
    wave.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;

    // Session recording: name the person, record, and the readings land in a CSV file.
    sessionCard.Accent = Color.FromArgb(48, 64, 96); sessionCard.SetBounds(16, 478, 788, 72);
    sessionCard.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
    var recordTitle = new Label { Text = "RECORD A SESSION", Font = small, ForeColor = TextDim };
    recordTitle.SetBounds(16, 10, 200, 18);
    var nameCaption = new Label { Text = "Name", Font = small, ForeColor = TextFaint };
    nameCaption.SetBounds(16, 40, 44, 20);
    nameBox.SetBounds(60, 36, 190, 26);
    nameBox.BackColor = Color.FromArgb(28, 36, 54); nameBox.ForeColor = Color.White;
    nameBox.BorderStyle = BorderStyle.FixedSingle; nameBox.Font = new Font("Segoe UI", 10);
    StyleButton(recordButton, "Record", Color.FromArgb(28, 36, 54), AccentSoft);
    recordButton.SetBounds(264, 34, 110, 30); recordButton.Click += (s, e) => ToggleRecording();
    StyleButton(folderButton, "Open folder", Color.FromArgb(28, 36, 54), TextDim);
    folderButton.SetBounds(384, 34, 110, 30); folderButton.Click += (s, e) => OpenSessionFolder();
    sessionLabel.SetBounds(508, 14, 268, 52); sessionLabel.Font = small; sessionLabel.ForeColor = TextFaint;
    sessionLabel.Text = "Not recording.\nReadings are saved as CSV, one file per session.";
    sessionCard.Controls.AddRange(new Control[] { recordTitle, nameCaption, nameBox, recordButton, folderButton, sessionLabel });

    statusLabel.SetBounds(18, 558, 786, 30); statusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
    statusLabel.ForeColor = TextFaint; statusLabel.Font = small;
    statusLabel.Text = "Close the Maxim GUI, switch the board on, then press Start.";

    Controls.AddRange(new Control[] { startButton, stopButton, bpmCard, spo2Card, infoCard, wave, sessionCard, statusLabel });
    uiTimer.Interval = 50; uiTimer.Tick += (s, e) => RefreshUi();
    FormClosing += (s, e) => StopStream();
    HandleCreated += (s, e) => UseDarkTitleBar(Handle);
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

  void ToggleRecording() {
    if (recorder != null) {
      var s = recorder.Stop();
      recorder = null;
      StyleButton(recordButton, "Record", Color.FromArgb(28, 36, 54), AccentSoft);
      sessionLabel.Text = string.Format("Saved {0}\n{1} s, {2} readings ({3:F0}% of the time)\n{4}",
        System.IO.Path.GetFileName(s.Path), s.Seconds, s.Readings, s.Coverage,
        s.Readings > 0 ? string.Format("mean {0:F0} BPM, range {1:F0}-{2:F0}", s.MeanBpm, s.MinBpm, s.MaxBpm) : "no readings captured");
      statusLabel.Text = "Session saved to " + s.Path;
      return;
    }
    if (stream == null) { statusLabel.Text = "Press Start first - there is nothing to record yet."; return; }
    recorder = new SessionRecorder(nameBox.Text);
    StyleButton(recordButton, "Stop recording", Color.FromArgb(150, 40, 60), Color.White);
    sessionLabel.Text = "Recording " + recorder.Subject + "...\n0 s";
    statusLabel.Text = "Recording to " + recorder.Path;
  }

  void OpenSessionFolder() {
    try {
      System.IO.Directory.CreateDirectory(SessionRecorder.Folder);
      System.Diagnostics.Process.Start("explorer.exe", "\"" + SessionRecorder.Folder + "\"");
    } catch (Exception ex) { statusLabel.Text = "Could not open the folder: " + ex.Message; }
  }

  void SetStatus(string msg) {
    if (IsDisposed) return;
    BeginInvoke((Action)(() => statusLabel.Text = msg));
  }

  void StartStream() {
    startButton.Enabled = false;
    bpmLabel.Text = "--"; spo2Label.Text = "--"; infoLabel.Text = "Connecting...";
    lastGoodBpm = lastGoodSpO2 = 0;
    var s = new PpgStream();
    s.Log = SetStatus;
    ThreadPool.QueueUserWorkItem(_ => {
      try {
        s.Start();
        BeginInvoke((Action)(() => { stream = s; stopButton.Enabled = true; uiTimer.Start(); }));
      } catch (Exception ex) {
        s.Dispose();
        BeginInvoke((Action)(() => { startButton.Enabled = true; infoLabel.Text = ""; statusLabel.Text = "Error: " + ex.Message; }));
      }
    });
  }

  void StopStream() {
    uiTimer.Stop();
    if (stream != null) {
      // Bluetooth calls must not run on the UI (STA) thread.
      var s = stream; stream = null;
      var t = new Thread(s.Dispose); t.SetApartmentState(ApartmentState.MTA); t.Start(); t.Join(5000);
    }
    startButton.Enabled = true; stopButton.Enabled = false;
    if (!IsDisposed) infoLabel.Text = "Stopped";
  }

  void RefreshUi() {
    if (stream == null) return;
    lock (stream.Sync) {
      var est = stream.Estimator;
      wave.Data = est.Tail((int)(est.SampleRate * 6));
      wave.Invalidate();
      if ((DateTime.Now - lastEstimate).TotalSeconds < 1) return;
      lastEstimate = DateTime.Now;

      string counts = "Samples " + stream.Samples + " @ " + est.SampleRate.ToString("F1") + " Hz\nDropped packets " + stream.DroppedPackets;
      if (stream.Samples == 0) { infoLabel.Text = "Waiting for data...\n" + counts; Record(0, 0, null, null, "waiting for data"); return; }
      if (est.Count < est.SampleRate * FirstWindow) {
        int left = (int)Math.Ceiling(FirstWindow - est.Count / est.SampleRate);
        infoLabel.Text = "Measuring... keep still.\nFirst reading in " + left + " s.\n" + counts;
        Record(0, 0, null, null, "warming up");
        return;
      }

      double bpm, q;
      // The steady-state estimate uses 8 s. Before there is anything on screen, a 5 s window is tried as well:
      // measured on the 21 Sep recordings it brings the first reading forward 2-3 s with no wrong readings,
      // while 4 s starts inventing sub-50 BPM values.
      bool ok = est.Estimate(out bpm, out q);
      if (!ok && lastGoodBpm == 0) ok = est.Estimate(out bpm, out q, FirstWindow);
      if (!ok) {
        // Movement drowns the pulse for a second or two at a time. Blanking the screen each time makes the
        // tool look broken, so the last good reading is held briefly and shown dimmed with its age.
        int age = (int)(DateTime.Now - lastGoodAt).TotalSeconds;
        if (lastGoodBpm > 0 && age <= HoldSeconds) {
          bpmLabel.ForeColor = Held; bpmLabel.Text = lastGoodBpm.ToString("F0");
          if (lastGoodSpO2 > 0 && age <= HoldSeconds) { spo2Label.ForeColor = Held; spo2Label.Text = lastGoodSpO2.ToString("F0"); }
          infoLabel.Text = "Movement - no clear pulse.\nHolding the last reading\n(" + age + " s old).\n" + counts;
        } else {
          bpmLabel.ForeColor = Held; spo2Label.ForeColor = Held;
          bpmLabel.Text = "--"; spo2Label.Text = "--";
          lastGoodBpm = lastGoodSpO2 = 0;
          infoLabel.Text = "No clear pulse.\nRest the sensor lightly against the skin and keep still.\n" + counts;
        }
        // Recorded as a gap: a session file should show honestly when the sensor could not read.
        Record(0, 0, null, null, age <= HoldSeconds ? "movement - holding" : "no clear pulse");
        return;
      }
      bpmLabel.ForeColor = AccentSoft; bpmLabel.Text = bpm.ToString("F0");
      lastGoodBpm = bpm; lastGoodAt = DateTime.Now;
      string info = "Pulse quality " + (q * 100).ToString("F0") + "% via " + est.BestChannel + "\n";

      var ox = stream.Oximeter.Estimate(bpm);
      if (ox.Problem == null) {
        spo2Label.ForeColor = Accent; spo2Label.Text = ox.SpO2.ToString("F0"); lastGoodSpO2 = ox.SpO2;
        info += (ox.Spread > 3 ? "SpO2 UNSTABLE" : "SpO2") + " +/-" + ox.Spread.ToString("F0") + "%, R " + ox.R.ToString("F2") + "\nvia " + ox.Path + ", signal/noise " + ox.Snr.ToString("F0") + "\n";
      } else {
        // SpO2 needs a longer clean stretch than the heart rate, so it is held for longer before blanking.
        int oxAge = (int)(DateTime.Now - lastGoodAt).TotalSeconds;
        if (lastGoodSpO2 > 0 && oxAge <= HoldSeconds) { spo2Label.ForeColor = Held; spo2Label.Text = lastGoodSpO2.ToString("F0"); }
        else { spo2Label.ForeColor = Held; spo2Label.Text = "--"; lastGoodSpO2 = 0; }
        info += "SpO2: " + ox.Problem + "\n";
      }
      infoLabel.Text = info + counts;
      Record(bpm, q, est.BestChannel, ox, ox.Problem == null ? "ok" : "heart rate only");
    }
  }

  // One row per second into the open session file, and a live count on the session card.
  void Record(double bpm, double q, string channel, SpO2Estimator.Result ox, string state) {
    if (recorder == null) return;
    bool haveOx = ox != null && ox.Problem == null;
    recorder.Add(bpm, q, channel,
      haveOx ? ox.SpO2 : 0, haveOx ? ox.Spread : 0, haveOx ? ox.R : 0, ox != null ? ox.Snr : 0, state);
    sessionLabel.Text = string.Format("Recording {0}...\n{1} s, {2} readings", recorder.Subject, recorder.Seconds, recorder.WithReading);
  }
}

// The display font (Chakra Petch Bold Italic, SIL OFL - see assets/) is embedded in the exe and loaded from
// memory, so nothing has to be installed on the machine. Falls back to a stock font if anything goes wrong.
static class DisplayFont {
  static readonly System.Drawing.Text.PrivateFontCollection Collection = new System.Drawing.Text.PrivateFontCollection();
  static readonly FontFamily Family;

  static DisplayFont() {
    try {
      using (var s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("hrfont")) {
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
  static readonly Color Line = Color.FromArgb(190, 255, 60); // luminous green-yellow, like a bedside monitor
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
    var d = Data;
    using (var dim = new SolidBrush(Color.FromArgb(96, 112, 140)))
      g.DrawString("PULSE WAVEFORM  ·  GREEN", new Font("Segoe UI", 8), dim, 10, 8);
    if (d == null || d.Length < 2) return;
    double min = double.MaxValue, max = double.MinValue;
    foreach (var v in d) { if (v < min) min = v; if (v > max) max = v; }
    double span = Math.Max(max - min, 1e-9);
    var pts = new PointF[d.Length];
    for (int i = 0; i < d.Length; i++)
      pts[i] = new PointF(i * (Width - 1f) / (d.Length - 1), (float)(Height - 14 - (d[i] - min) / span * (Height - 44)));
    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
    // Fill under the trace, then the trace itself with a soft glow behind it.
    var fill = new PointF[pts.Length + 2];
    Array.Copy(pts, fill, pts.Length);
    fill[pts.Length] = new PointF(pts[pts.Length - 1].X, Height);
    fill[pts.Length + 1] = new PointF(pts[0].X, Height);
    using (var under = new SolidBrush(Color.FromArgb(14, Line))) g.FillPolygon(under, fill);
    using (var glow = new Pen(Color.FromArgb(48, Line), 7)) g.DrawLines(glow, pts);
    using (var pen = new Pen(Line, 2)) g.DrawLines(pen, pts);
  }
}

