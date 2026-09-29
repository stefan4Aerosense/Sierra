using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;

// Records a measurement session to disk: a PDF report and a CSV per session, plus one index of all sessions.
// A session can hold more than one sensor at once, which is how two monitors get compared: they see the same
// person at the same moment, so the difference between them is the measurement, not the minute.
// Everything is plain text on this machine - nothing is sent anywhere.
class SessionRecorder {
  public readonly string Subject;
  public readonly DateTime Started;
  public readonly string Path;
  public readonly string[] Sources;
  readonly StreamWriter file;
  readonly List<object[]> rows = new List<object[]>();
  readonly Dictionary<string, List<double>> rates = new Dictionary<string, List<double>>();
  readonly Dictionary<string, List<double>> oxygen = new Dictionary<string, List<double>>();
  // second -> source -> bpm, for comparing sources at the same instant
  readonly Dictionary<int, Dictionary<string, double>> bySecond = new Dictionary<int, Dictionary<string, double>>();
  int seconds;
  static readonly IFormatProvider Inv = CultureInfo.InvariantCulture;

  public static string Folder {
    get { return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessions"); }
  }

  public int Seconds { get { return seconds; } }
  public int WithReading { get { return rates.Values.Sum(v => v.Count); } }

  public SessionRecorder(string subject, params string[] sources) {
    Subject = string.IsNullOrWhiteSpace(subject) ? "unnamed" : subject.Trim();
    Sources = sources.Length > 0 ? sources : new[] { "sensor" };
    foreach (var s in Sources) { rates[s] = new List<double>(); oxygen[s] = new List<double>(); }
    Started = DateTime.Now;
    Directory.CreateDirectory(Folder);
    string safe = new string(Subject.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());
    Path = System.IO.Path.Combine(Folder, Started.ToString("yyyy-MM-dd_HHmmss") + "_" + safe + ".csv");
    file = new StreamWriter(Path);
    file.WriteLine("# subject," + Csv(Subject));
    file.WriteLine("# started," + Started.ToString("yyyy-MM-dd HH:mm:ss"));
    file.WriteLine("# sources," + Csv(string.Join(" | ", Sources)));
    file.WriteLine("# note,SpO2 is an uncalibrated estimate and is not a medical measurement");
    file.WriteLine("time,seconds,source,bpm,pulse_quality,channel,spo2,spo2_spread,ratio_R,signal_noise,device_bpm,device_spo2,device_confidence,state");
    file.Flush();
  }

  public void Tick() { seconds++; }

  // Called once a second per source. bpm/spo2 of 0 mean "no reading this second", recorded as a gap.
  public void Add(string source, double bpm, double quality, string channel, double spo2, double spread,
                  double r, double snr, DeviceReading device, string state) {
    if (!rates.ContainsKey(source)) { rates[source] = new List<double>(); oxygen[source] = new List<double>(); }
    if (bpm > 0) {
      rates[source].Add(bpm);
      if (!bySecond.ContainsKey(seconds)) bySecond[seconds] = new Dictionary<string, double>();
      bySecond[seconds][source] = bpm;
    }
    if (spo2 > 0) oxygen[source].Add(spo2);

    bool hasDev = device != null && device.Fresh;
    rows.Add(new object[] {
      DateTime.Now.ToString("HH:mm:ss"), (double)seconds, source,
      bpm > 0 ? (object)Math.Round(bpm) : null,
      bpm > 0 ? (object)Math.Round(quality, 2) : null,
      channel ?? "",
      spo2 > 0 ? (object)Math.Round(spo2) : null,
      spo2 > 0 ? (object)Math.Round(spread, 1) : null,
      spo2 > 0 ? (object)Math.Round(r, 3) : null,
      snr > 0 ? (object)Math.Round(snr, 1) : null,
      hasDev ? (object)Math.Round(device.Bpm) : null,
      hasDev && device.SpO2 > 0 ? (object)Math.Round(device.SpO2) : null,
      hasDev ? (object)device.Confidence : null,
      state });

    file.WriteLine(string.Join(",",
      DateTime.Now.ToString("HH:mm:ss"), seconds.ToString(), Csv(source),
      bpm > 0 ? bpm.ToString("F0", Inv) : "",
      bpm > 0 ? quality.ToString("F2", Inv) : "",
      Csv(channel ?? ""),
      spo2 > 0 ? spo2.ToString("F0", Inv) : "",
      spo2 > 0 ? spread.ToString("F1", Inv) : "",
      spo2 > 0 ? r.ToString("F3", Inv) : "",
      snr > 0 ? snr.ToString("F1", Inv) : "",
      hasDev ? device.Bpm.ToString("F0", Inv) : "",
      hasDev && device.SpO2 > 0 ? device.SpO2.ToString("F0", Inv) : "",
      hasDev ? device.Confidence.ToString() : "",
      Csv(state)));
    file.Flush(); // a crash or a flat battery must not cost the session
  }

  public class SourceStats {
    public string Source;
    public int Readings;
    public double MeanBpm, MinBpm, MaxBpm, MeanSpO2;
  }

  // How two sensors compared over the seconds when both produced a reading.
  public class Comparison {
    public string A, B;
    public int Paired;
    public double MeanDifference;   // A - B, signed: shows a consistent bias
    public double MedianAbsolute;   // typical size of the disagreement
    public double WorstAbsolute;
    public double WithinFive;       // % of paired seconds within 5 BPM
  }

  public class Summary {
    public string Subject, Path, ReportPath, ReportError;
    public int Seconds;
    public List<SourceStats> PerSource = new List<SourceStats>();
    public Comparison Compared;
    public int Readings { get { return PerSource.Sum(s => s.Readings); } }
    public double Coverage {
      get {
        if (Seconds == 0 || PerSource.Count == 0) return 0;
        return 100.0 * PerSource.Max(s => s.Readings) / Seconds;
      }
    }
  }

  public Summary Stop() {
    var s = new Summary { Subject = Subject, Path = Path, Seconds = seconds };
    foreach (var src in Sources) {
      var st = new SourceStats { Source = src, Readings = rates[src].Count };
      if (st.Readings > 0) { st.MeanBpm = rates[src].Average(); st.MinBpm = rates[src].Min(); st.MaxBpm = rates[src].Max(); }
      if (oxygen[src].Count > 0) st.MeanSpO2 = oxygen[src].Average();
      s.PerSource.Add(st);
    }
    if (Sources.Length >= 2) s.Compared = Compare(Sources[0], Sources[1]);

    file.WriteLine("# ended," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    foreach (var st in s.PerSource)
      file.WriteLine(string.Format(Inv, "# summary,{0},{1} readings,mean {2:F0} BPM,range {3:F0}-{4:F0}",
        Csv(st.Source), st.Readings, st.MeanBpm, st.MinBpm, st.MaxBpm));
    if (s.Compared != null)
      file.WriteLine(string.Format(Inv, "# comparison,{0} paired seconds,mean difference {1:F1} BPM,median absolute {2:F1},worst {3:F1},within 5 BPM {4:F0}%",
        s.Compared.Paired, s.Compared.MeanDifference, s.Compared.MedianAbsolute, s.Compared.WorstAbsolute, s.Compared.WithinFive));
    file.Close();
    AppendToIndex(s);
    try { s.ReportPath = WritePdf(s); } catch (Exception ex) { s.ReportPath = null; s.ReportError = ex.Message; }
    return s;
  }

  Comparison Compare(string a, string b) {
    var diffs = new List<double>();
    foreach (var sec in bySecond.Values)
      if (sec.ContainsKey(a) && sec.ContainsKey(b)) diffs.Add(sec[a] - sec[b]);
    var c = new Comparison { A = a, B = b, Paired = diffs.Count };
    if (diffs.Count == 0) return c;
    c.MeanDifference = diffs.Average();
    var abs = diffs.Select(Math.Abs).OrderBy(v => v).ToList();
    c.MedianAbsolute = abs[abs.Count / 2];
    c.WorstAbsolute = abs[abs.Count - 1];
    c.WithinFive = 100.0 * abs.Count(v => v <= 5) / abs.Count;
    return c;
  }

  // A one-file report that opens on any machine: summary, a chart per sensor, the comparison, then the readings.
  string WritePdf(Summary s) {
    var pdf = new Pdf();
    pdf.NewPage();
    double top = Pdf.PageHeight - 60;
    pdf.Text(50, top, 18, s.PerSource.Count > 1 ? "Heart rate session - two sensors" : "Heart rate session", true);
    pdf.Text(50, top - 26, 12, s.Subject, true, 0.15);
    pdf.Text(50, top - 44, 10, Started.ToString("dddd d MMMM yyyy, HH:mm"), false, 0.35);
    pdf.Line(50, top - 56, Pdf.PageWidth - 50, top - 56, 0.8, 0.75);

    double y = top - 84;
    Action<string, string> row = (k, v) => {
      pdf.Text(50, y, 10, k, false, 0.4);
      pdf.Text(230, y, 11, v, true, 0.1);
      y -= 20;
    };
    row("Duration", s.Seconds + " s");
    foreach (var st in s.PerSource) {
      row(st.Source, st.Readings > 0
        ? string.Format(Inv, "{0:F0} BPM mean, {1:F0}-{2:F0}, {3} readings", st.MeanBpm, st.MinBpm, st.MaxBpm, st.Readings)
        : "no readings captured");
      if (st.MeanSpO2 > 0) row("   SpO2 (uncalibrated)", string.Format(Inv, "{0:F0} %", st.MeanSpO2));
    }
    if (s.Compared != null && s.Compared.Paired > 0) {
      y -= 6;
      pdf.Text(50, y, 10, "AGREEMENT BETWEEN THE TWO SENSORS", true, 0.4); y -= 20;
      row("Seconds both produced a reading", s.Compared.Paired.ToString());
      row("Mean difference (first minus second)",
          string.Format(Inv, "{0:+0.0;-0.0;0.0} BPM", s.Compared.MeanDifference));
      row("Typical disagreement", string.Format(Inv, "{0:F1} BPM (worst {1:F1})", s.Compared.MedianAbsolute, s.Compared.WorstAbsolute));
      row("Within 5 BPM of each other", string.Format(Inv, "{0:F0} % of the time", s.Compared.WithinFive));
    }

    // Chart: every sensor's rate against time, gaps left as gaps.
    double cx = 50, cw = Pdf.PageWidth - 100, ch = 160, cy = y - ch - 34;
    pdf.Text(cx, cy + ch + 12, 10, "HEART RATE OVER THE SESSION", true, 0.4);
    pdf.FillRect(cx, cy, cw, ch, 0.97);
    var withData = s.PerSource.Where(p => p.Readings > 0).ToList();
    if (withData.Count > 0) {
      double lo = Math.Max(30, withData.Min(p => p.MinBpm) - 10), hi = withData.Max(p => p.MaxBpm) + 10;
      for (int i = 0; i <= 4; i++) {
        double gy = cy + ch * i / 4.0;
        pdf.Line(cx, gy, cx + cw, gy, 0.4, 0.85);
        pdf.Text(cx - 28, gy - 3, 8, (lo + (hi - lo) * i / 4.0).ToString("F0", Inv), false, 0.45);
      }
      var colours = new[] { new[] { 0.13, 0.42, 0.85 }, new[] { 0.85, 0.25, 0.30 } };
      for (int si = 0; si < s.PerSource.Count; si++) {
        string src = s.PerSource[si].Source;
        var col = colours[si % colours.Length];
        var run = new List<double[]>();
        for (int sec = 1; sec <= s.Seconds; sec++) {
          double px = cx + cw * (s.Seconds > 1 ? (sec - 1.0) / (s.Seconds - 1) : 0);
          if (!bySecond.ContainsKey(sec) || !bySecond[sec].ContainsKey(src)) {
            if (run.Count > 1) pdf.Polyline(run, 1.6, col[0], col[1], col[2]);
            run = new List<double[]>();
            continue;
          }
          run.Add(new[] { px, cy + ch * (bySecond[sec][src] - lo) / Math.Max(hi - lo, 1) });
        }
        if (run.Count > 1) pdf.Polyline(run, 1.6, col[0], col[1], col[2]);
        // key
        double ky = cy + ch - 12 - si * 14;
        pdf.Polyline(new List<double[]> { new[] { cx + cw - 150, ky }, new[] { cx + cw - 130, ky } }, 2.0, col[0], col[1], col[2]);
        pdf.Text(cx + cw - 124, ky - 3, 8, src, false, 0.3);
      }
      pdf.Text(cx, cy - 14, 8, "0 s", false, 0.45);
      pdf.Text(cx + cw - 24, cy - 14, 8, s.Seconds + " s", false, 0.45);
    } else {
      pdf.Text(cx + 16, cy + ch / 2, 10, "No readings were captured during this session.", false, 0.45);
    }

    // Readings table, continued over as many pages as it needs.
    double ty = cy - 70;
    Action header = () => {
      pdf.Text(50, ty, 9, "TIME", true, 0.4); pdf.Text(100, ty, 9, "SENSOR", true, 0.4);
      pdf.Text(230, ty, 9, "BPM", true, 0.4); pdf.Text(275, ty, 9, "QUALITY", true, 0.4);
      pdf.Text(335, ty, 9, "SPO2", true, 0.4); pdf.Text(380, ty, 9, "DEVICE BPM", true, 0.4);
      pdf.Text(450, ty, 9, "STATE", true, 0.4);
      ty -= 6;
      pdf.Line(50, ty, Pdf.PageWidth - 50, ty, 0.6, 0.8);
      ty -= 13;
    };
    pdf.Text(50, ty + 22, 10, "READINGS", true, 0.4);
    header();
    foreach (var r in rows) {
      if (ty < 60) { pdf.NewPage(); ty = Pdf.PageHeight - 60; header(); }
      pdf.Text(50, ty, 9, Convert.ToString(r[0]), false, 0.2);
      pdf.Text(100, ty, 9, Convert.ToString(r[2]), false, 0.35);
      pdf.Text(230, ty, 9, r[3] == null ? "-" : Convert.ToString(r[3], Inv), false, 0.1);
      pdf.Text(275, ty, 9, r[4] == null ? "" : Convert.ToString(r[4], Inv), false, 0.35);
      pdf.Text(335, ty, 9, r[6] == null ? "" : Convert.ToString(r[6], Inv), false, 0.35);
      pdf.Text(380, ty, 9, r[10] == null ? "" : Convert.ToString(r[10], Inv), false, 0.35);
      pdf.Text(450, ty, 9, Convert.ToString(r[13]), false, 0.35);
      ty -= 13;
    }
    if (ty < 80) { pdf.NewPage(); ty = Pdf.PageHeight - 60; }
    pdf.Text(50, ty - 16, 8, "SpO2 is an uncalibrated estimate from development sensors, not a medical measurement.", false, 0.45);
    pdf.Text(50, ty - 28, 8, "Blank rows are seconds when movement or poor contact left no clear pulse.", false, 0.45);
    if (s.Compared != null)
      pdf.Text(50, ty - 40, 8, "DEVICE BPM is the sensor's own on-board reading, where it has one; the BPM column is this app's.", false, 0.45);

    string path = System.IO.Path.ChangeExtension(Path, ".pdf");
    pdf.Save(path);
    return path;
  }

  // One row per session per sensor, so every recording ever made can be read from a single file.
  static void AppendToIndex(Summary s) {
    string index = System.IO.Path.Combine(Folder, "sessions.csv");
    bool fresh = !File.Exists(index);
    using (var w = new StreamWriter(index, true)) {
      if (fresh) w.WriteLine("date,time,subject,source,seconds,readings,mean_bpm,min_bpm,max_bpm,mean_spo2,paired_seconds,mean_difference,within_5bpm_percent,file");
      foreach (var st in s.PerSource) {
        var c = s.Compared;
        w.WriteLine(string.Join(",",
          DateTime.Now.ToString("yyyy-MM-dd"), DateTime.Now.ToString("HH:mm:ss"), Csv(s.Subject), Csv(st.Source),
          s.Seconds.ToString(), st.Readings.ToString(),
          st.MeanBpm > 0 ? st.MeanBpm.ToString("F0", Inv) : "",
          st.MinBpm > 0 ? st.MinBpm.ToString("F0", Inv) : "",
          st.MaxBpm > 0 ? st.MaxBpm.ToString("F0", Inv) : "",
          st.MeanSpO2 > 0 ? st.MeanSpO2.ToString("F0", Inv) : "",
          c != null ? c.Paired.ToString() : "",
          c != null ? c.MeanDifference.ToString("F1", Inv) : "",
          c != null ? c.WithinFive.ToString("F0", Inv) : "",
          Csv(System.IO.Path.GetFileName(s.Path))));
      }
    }
  }

  static string Csv(string v) {
    return v.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
  }
}
