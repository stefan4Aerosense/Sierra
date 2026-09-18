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
    if (args.Length == 3 && args[0] == "--replayrec")
      return SelfTest.ReplayRec(double.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture), args[2]);
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
  readonly Label bpmLabel = new Label(), bpmUnit = new Label(), spo2Label = new Label(), spo2Unit = new Label();
  readonly Label infoLabel = new Label(), statusLabel = new Label();
  readonly Button startButton = new Button(), stopButton = new Button();
  readonly WavePanel wave = new WavePanel();
  readonly SignalBar signalBar = new SignalBar();
  readonly Label hintLabel = new Label();
  readonly System.Windows.Forms.Timer uiTimer = new System.Windows.Forms.Timer();
  PpgStream stream;
  DateTime lastEstimate;

  public MainForm() {
    Text = "Heart Rate & SpO2 Tester (MAX86171)";
    ClientSize = new Size(780, 520);
    MinimumSize = new Size(640, 480);
    BackColor = Color.White;
    Font = new Font("Segoe UI", 10);

    startButton.Text = "Start"; startButton.SetBounds(16, 16, 110, 36); startButton.Click += (s, e) => StartStream();
    stopButton.Text = "Stop"; stopButton.SetBounds(136, 16, 110, 36); stopButton.Enabled = false; stopButton.Click += (s, e) => StopStream();

    var big = new Font("Segoe UI", 60, FontStyle.Bold);
    bpmLabel.Text = "--"; bpmLabel.Font = big; bpmLabel.ForeColor = Color.FromArgb(200, 30, 50);
    bpmLabel.SetBounds(16, 60, 250, 100); bpmLabel.TextAlign = ContentAlignment.MiddleLeft;
    bpmUnit.Text = "Heart rate (BPM)"; bpmUnit.SetBounds(22, 160, 250, 22); bpmUnit.ForeColor = Color.DimGray;

    spo2Label.Text = "--"; spo2Label.Font = big; spo2Label.ForeColor = Color.FromArgb(30, 90, 190);
    spo2Label.SetBounds(290, 60, 250, 100); spo2Label.TextAlign = ContentAlignment.MiddleLeft;
    spo2Unit.Text = "SpO2 % (uncalibrated estimate)"; spo2Unit.SetBounds(296, 160, 250, 22); spo2Unit.ForeColor = Color.DimGray;

    infoLabel.SetBounds(560, 64, 204, 120); infoLabel.ForeColor = Color.DimGray; infoLabel.Font = new Font("Segoe UI", 9);
    infoLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

    signalBar.SetBounds(16, 196, 220, 22);
    hintLabel.SetBounds(246, 196, 518, 24); hintLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
    hintLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; hintLabel.TextAlign = ContentAlignment.MiddleLeft;

    wave.SetBounds(16, 226, 748, 240); wave.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
    statusLabel.SetBounds(16, 478, 748, 30); statusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
    statusLabel.ForeColor = Color.DimGray; statusLabel.Text = "Close the Maxim GUI, switch the board on, then press Start.";

    Controls.AddRange(new Control[] { startButton, stopButton, bpmLabel, bpmUnit, spo2Label, spo2Unit, infoLabel, signalBar, hintLabel, wave, statusLabel });
    uiTimer.Interval = 50; uiTimer.Tick += (s, e) => RefreshUi();
    FormClosing += (s, e) => StopStream();
  }

  void SetStatus(string msg) {
    if (IsDisposed) return;
    BeginInvoke((Action)(() => statusLabel.Text = msg));
  }

  void StartStream() {
    startButton.Enabled = false;
    bpmLabel.Text = "--"; spo2Label.Text = "--"; infoLabel.Text = "Connecting...";
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

      ShowPlacement(est);
      string counts = "Samples " + stream.Samples + " @ " + est.SampleRate.ToString("F1") + " Hz\nDropped packets " + stream.DroppedPackets;
      if (stream.Samples == 0) { infoLabel.Text = "Waiting for data...\n" + counts; return; }
      if (est.Count < est.SampleRate * 8) { infoLabel.Text = "Measuring... keep still\n" + counts; return; }

      double bpm, q;
      if (!est.Estimate(out bpm, out q)) {
        bpmLabel.Text = "--"; spo2Label.Text = "--";
        infoLabel.Text = "No clear pulse.\nRest fingertip lightly on the sensor and keep still.\n" + counts;
        return;
      }
      bpmLabel.Text = bpm.ToString("F0");
      string info = "Pulse quality " + (q * 100).ToString("F0") + "% via " + est.BestChannel + "\n";

      var ox = stream.Oximeter.Estimate(bpm);
      if (ox.Problem == null) {
        spo2Label.Text = ox.SpO2.ToString("F0") + (ox.LowConfidence ? "?" : "");
        spo2Label.ForeColor = ox.LowConfidence ? Color.FromArgb(180, 120, 30) : Color.FromArgb(30, 90, 190);
        spo2Unit.Text = ox.LowConfidence ? "SpO2 % - LOW CONFIDENCE" : "SpO2 % (uncalibrated estimate)";
        info += "SpO2 +/-" + ox.Spread.ToString("F0") + "%, R " + ox.R.ToString("F2") + "\nvia " + ox.Path + ", signal/noise " + ox.Snr.ToString("F0") + "\n";
      } else {
        spo2Label.Text = "--"; spo2Label.ForeColor = Color.FromArgb(30, 90, 190);
        spo2Unit.Text = "SpO2 % (uncalibrated estimate)";
        info += "SpO2: " + ox.Problem + "\n";
      }
      infoLabel.Text = info + counts;
    }
  }

  // Signal-strength bar + a plain instruction, so a weak reading is something the user can fix.
  void ShowPlacement(MultiHeartRate est) {
    MultiHeartRate.Placement p;
    double pi = est.PerfusionIndex(out p);
    signalBar.Level = pi; signalBar.Invalidate();
    switch (p) {
      case MultiHeartRate.Placement.NoFinger:  Hint("Place a fingertip over the sensor", Color.DimGray); break;
      case MultiHeartRate.Placement.Saturated: Hint("Too much light - cover the sensor fully with your fingertip", Color.FromArgb(200, 30, 50)); break;
      case MultiHeartRate.Placement.Moving:    Hint("Movement - keep your finger still", Color.FromArgb(200, 30, 50)); break;
      case MultiHeartRate.Placement.Weak:      Hint("Weak pulse - press MORE LIGHTLY, warm your finger", Color.FromArgb(200, 30, 50)); break;
      case MultiHeartRate.Placement.Fair:      Hint("Fair signal - rest lightly and hold still", Color.FromArgb(180, 120, 30)); break;
      case MultiHeartRate.Placement.Good:      Hint("Good signal - hold still", Color.FromArgb(21, 128, 61)); break;
      default:                                 Hint("Measuring signal strength...", Color.DimGray); break;
    }
  }

  void Hint(string text, Color c) { hintLabel.Text = text; hintLabel.ForeColor = c; }
}

// Pulse strength (perfusion index, %) on a log scale 0.03 .. 5 %, red / amber / green zones.
class SignalBar : Panel {
  public double Level;
  public SignalBar() { DoubleBuffered = true; BackColor = Color.FromArgb(235, 235, 238); BorderStyle = BorderStyle.FixedSingle; }
  protected override void OnPaint(PaintEventArgs e) {
    base.OnPaint(e);
    double lo = Math.Log10(0.03), hi = Math.Log10(5);
    double frac = Level <= 0 ? 0 : Math.Max(0, Math.Min(1, (Math.Log10(Level) - lo) / (hi - lo)));
    var c = Level < 0.3 ? Color.FromArgb(200, 30, 50) : Level < 1 ? Color.FromArgb(217, 150, 30) : Color.FromArgb(21, 128, 61);
    using (var b = new SolidBrush(c)) e.Graphics.FillRectangle(b, 0, 0, (float)(frac * (Width - 2)), Height - 2);
    e.Graphics.DrawString("Signal " + (Level > 0 ? Level.ToString("F2") + " %" : "--"), Font, Brushes.Black, 4, 2);
  }
}

class WavePanel : Panel {
  public double[] Data = new double[0];
  public WavePanel() { DoubleBuffered = true; BackColor = Color.FromArgb(248, 248, 250); BorderStyle = BorderStyle.FixedSingle; }
  protected override void OnPaint(PaintEventArgs e) {
    base.OnPaint(e);
    var d = Data;
    if (d == null || d.Length < 2) { e.Graphics.DrawString("Pulse waveform (green LED)", Font, Brushes.Gray, 8, 8); return; }
    double min = double.MaxValue, max = double.MinValue;
    foreach (var v in d) { if (v < min) min = v; if (v > max) max = v; }
    double span = Math.Max(max - min, 1e-9);
    var pts = new PointF[d.Length];
    for (int i = 0; i < d.Length; i++)
      pts[i] = new PointF(i * (Width - 1f) / (d.Length - 1), (float)(Height - 10 - (d[i] - min) / span * (Height - 20)));
    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
    using (var pen = new Pen(Color.FromArgb(200, 30, 50), 2)) e.Graphics.DrawLines(pen, pts);
  }
}

