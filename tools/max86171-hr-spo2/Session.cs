using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;

// Records a measurement session to disk: a PDF report and a CSV per session, plus one index of all sessions.
// Everything is plain text on this machine - nothing is sent anywhere.
class SessionRecorder {
  public readonly string Subject;
  public readonly DateTime Started;
  public readonly string Path;
  readonly StreamWriter file;
  readonly List<double> rates = new List<double>(), oxygen = new List<double>();
  readonly List<object[]> rows = new List<object[]>();
  int seconds;
  // This machine formats decimals with a comma, which would split CSV columns - always write invariant.
  static readonly IFormatProvider Inv = CultureInfo.InvariantCulture;

  public static string Folder {
    get { return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessions"); }
  }

  public int Seconds { get { return seconds; } }
  public int WithReading { get { return rates.Count; } }

  public SessionRecorder(string subject) {
    Subject = string.IsNullOrWhiteSpace(subject) ? "unnamed" : subject.Trim();
    Started = DateTime.Now;
    Directory.CreateDirectory(Folder);
    string safe = new string(Subject.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());
    Path = System.IO.Path.Combine(Folder, Started.ToString("yyyy-MM-dd_HHmmss") + "_" + safe + ".csv");
    file = new StreamWriter(Path);
    file.WriteLine("# subject," + Csv(Subject));
    file.WriteLine("# started," + Started.ToString("yyyy-MM-dd HH:mm:ss"));
    file.WriteLine("# note,SpO2 is an uncalibrated estimate and is not a medical measurement");
    file.WriteLine("time,seconds,bpm,pulse_quality,channel,spo2,spo2_spread,ratio_R,signal_noise,state");
    file.Flush();
  }

  // Called once a second. bpm/spo2 of 0 mean "no reading this second", which is recorded as a gap.
  public void Add(double bpm, double quality, string channel, double spo2, double spread, double r, double snr, string state) {
    seconds++;
    if (bpm > 0) rates.Add(bpm);
    if (spo2 > 0) oxygen.Add(spo2);
    // Kept in memory as typed values too, for the PDF report written when the session stops.
    rows.Add(new object[] {
      DateTime.Now.ToString("HH:mm:ss"), (double)seconds,
      bpm > 0 ? (object)Math.Round(bpm) : null,
      bpm > 0 ? (object)Math.Round(quality, 2) : null,
      channel ?? "",
      spo2 > 0 ? (object)Math.Round(spo2) : null,
      spo2 > 0 ? (object)Math.Round(spread, 1) : null,
      spo2 > 0 ? (object)Math.Round(r, 3) : null,
      snr > 0 ? (object)Math.Round(snr, 1) : null,
      state });
    file.WriteLine(string.Join(",",
      DateTime.Now.ToString("HH:mm:ss"),
      seconds.ToString(),
      bpm > 0 ? bpm.ToString("F0", Inv) : "",
      bpm > 0 ? quality.ToString("F2", Inv) : "",
      Csv(channel ?? ""),
      spo2 > 0 ? spo2.ToString("F0", Inv) : "",
      spo2 > 0 ? spread.ToString("F1", Inv) : "",
      spo2 > 0 ? r.ToString("F3", Inv) : "",
      snr > 0 ? snr.ToString("F1", Inv) : "",
      Csv(state)));
    file.Flush(); // flush every second: a crash or a flat battery must not cost the session
  }

  public class Summary {
    public string Subject, Path, ReportPath, ReportError;
    public int Seconds, Readings;
    public double MeanBpm, MinBpm, MaxBpm, MeanSpO2;
    public double Coverage { get { return Seconds > 0 ? 100.0 * Readings / Seconds : 0; } }
  }

  public Summary Stop() {
    var s = new Summary { Subject = Subject, Path = Path, Seconds = seconds, Readings = rates.Count };
    if (rates.Count > 0) { s.MeanBpm = rates.Average(); s.MinBpm = rates.Min(); s.MaxBpm = rates.Max(); }
    if (oxygen.Count > 0) s.MeanSpO2 = oxygen.Average();
    file.WriteLine("# ended," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    file.WriteLine(string.Format(Inv, "# summary,{0} s,{1} readings ({2:F0}% of the time),mean {3:F0} BPM,range {4:F0}-{5:F0}",
      s.Seconds, s.Readings, s.Coverage, s.MeanBpm, s.MinBpm, s.MaxBpm));
    file.Close();
    AppendToIndex(s);
    try { s.ReportPath = WritePdf(s); } catch (Exception ex) { s.ReportPath = null; s.ReportError = ex.Message; }
    return s;
  }

  // A one-file report that opens on any machine: summary, a chart of the session, then the readings.
  string WritePdf(Summary s) {
    var pdf = new Pdf();
    pdf.NewPage();
    double top = Pdf.PageHeight - 60;
    pdf.Text(50, top, 18, "Heart rate session", true);
    pdf.Text(50, top - 26, 12, s.Subject, true, 0.15);
    pdf.Text(50, top - 44, 10, Started.ToString("dddd d MMMM yyyy, HH:mm"), false, 0.35);
    pdf.Line(50, top - 56, Pdf.PageWidth - 50, top - 56, 0.8, 0.75);

    double y = top - 84;
    Action<string, string> row = (k, v) => {
      pdf.Text(50, y, 10, k, false, 0.4);
      pdf.Text(210, y, 11, v, true, 0.1);
      y -= 20;
    };
    row("Duration", s.Seconds + " s");
    row("Readings", string.Format(Inv, "{0} of {1} seconds ({2:F0}% of the time)", s.Readings, s.Seconds, s.Coverage));
    if (s.Readings > 0) {
      row("Mean heart rate", string.Format(Inv, "{0:F0} BPM", s.MeanBpm));
      row("Range", string.Format(Inv, "{0:F0} - {1:F0} BPM", s.MinBpm, s.MaxBpm));
    }
    if (s.MeanSpO2 > 0) row("Mean SpO2", string.Format(Inv, "{0:F0} %  (uncalibrated)", s.MeanSpO2));

    // Chart: heart rate against time. Gaps where no reading was available are left as gaps, not bridged.
    double cx = 50, cw = Pdf.PageWidth - 100, ch = 180, cy = y - ch - 30;
    pdf.Text(cx, cy + ch + 12, 10, "HEART RATE OVER THE SESSION", true, 0.4);
    pdf.FillRect(cx, cy, cw, ch, 0.97);
    if (s.Readings > 0) {
      double lo = Math.Max(30, s.MinBpm - 10), hi = s.MaxBpm + 10;
      for (int i = 0; i <= 4; i++) {
        double gy = cy + ch * i / 4.0;
        pdf.Line(cx, gy, cx + cw, gy, 0.4, 0.85);
        pdf.Text(cx - 28, gy - 3, 8, (lo + (hi - lo) * i / 4.0).ToString("F0", Inv), false, 0.45);
      }
      var run = new List<double[]>();
      foreach (var r in rows) {
        double t = Convert.ToDouble(r[1], Inv);
        double px = cx + cw * (s.Seconds > 1 ? (t - 1) / (s.Seconds - 1) : 0);
        if (r[2] == null) { // gap: end the current line
          if (run.Count > 1) pdf.Polyline(run, 1.6, 0.13, 0.42, 0.85);
          run = new List<double[]>();
          continue;
        }
        double bpm = Convert.ToDouble(r[2], Inv);
        run.Add(new[] { px, cy + ch * (bpm - lo) / Math.Max(hi - lo, 1) });
      }
      if (run.Count > 1) pdf.Polyline(run, 1.6, 0.13, 0.42, 0.85);
      pdf.Text(cx, cy - 14, 8, "0 s", false, 0.45);
      pdf.Text(cx + cw - 24, cy - 14, 8, s.Seconds + " s", false, 0.45);
    } else {
      pdf.Text(cx + 16, cy + ch / 2, 10, "No readings were captured during this session.", false, 0.45);
    }

    // Readings table, continued over as many pages as it needs.
    double ty = cy - 70;
    Action header = () => {
      pdf.Text(50, ty, 9, "TIME", true, 0.4); pdf.Text(110, ty, 9, "BPM", true, 0.4);
      pdf.Text(160, ty, 9, "QUALITY", true, 0.4); pdf.Text(230, ty, 9, "SPO2", true, 0.4);
      pdf.Text(285, ty, 9, "SIGNAL", true, 0.4); pdf.Text(345, ty, 9, "STATE", true, 0.4);
      ty -= 6;
      pdf.Line(50, ty, Pdf.PageWidth - 50, ty, 0.6, 0.8);
      ty -= 13;
    };
    pdf.Text(50, ty + 22, 10, "READINGS", true, 0.4);
    header();
    foreach (var r in rows) {
      if (ty < 60) { pdf.NewPage(); ty = Pdf.PageHeight - 60; header(); }
      pdf.Text(50, ty, 9, Convert.ToString(r[0]), false, 0.2);
      pdf.Text(110, ty, 9, r[2] == null ? "-" : Convert.ToString(r[2], Inv), false, 0.1);
      pdf.Text(160, ty, 9, r[3] == null ? "" : Convert.ToString(r[3], Inv), false, 0.35);
      pdf.Text(230, ty, 9, r[5] == null ? "" : Convert.ToString(r[5], Inv), false, 0.35);
      pdf.Text(285, ty, 9, r[8] == null ? "" : Convert.ToString(r[8], Inv), false, 0.35);
      pdf.Text(345, ty, 9, Convert.ToString(r[9]), false, 0.35);
      ty -= 13;
    }
    if (ty < 80) { pdf.NewPage(); ty = Pdf.PageHeight - 60; }
    pdf.Text(50, ty - 16, 8, "SpO2 is an uncalibrated estimate from a development sensor, not a medical measurement.", false, 0.45);
    pdf.Text(50, ty - 28, 8, "Blank rows are seconds when movement or poor contact left no clear pulse.", false, 0.45);

    string path = System.IO.Path.ChangeExtension(Path, ".pdf");
    pdf.Save(path);
    return path;
  }


  // One row per session, so every recording ever made can be read from a single file.
  static void AppendToIndex(Summary s) {
    string index = System.IO.Path.Combine(Folder, "sessions.csv");
    bool fresh = !File.Exists(index);
    using (var w = new StreamWriter(index, true)) {
      if (fresh) w.WriteLine("date,time,subject,seconds,readings,coverage_percent,mean_bpm,min_bpm,max_bpm,mean_spo2,file");
      w.WriteLine(string.Join(",",
        DateTime.Now.ToString("yyyy-MM-dd"), DateTime.Now.ToString("HH:mm:ss"), Csv(s.Subject),
        s.Seconds.ToString(), s.Readings.ToString(), s.Coverage.ToString("F0", Inv),
        s.MeanBpm > 0 ? s.MeanBpm.ToString("F0", Inv) : "",
        s.MinBpm > 0 ? s.MinBpm.ToString("F0", Inv) : "",
        s.MaxBpm > 0 ? s.MaxBpm.ToString("F0", Inv) : "",
        s.MeanSpO2 > 0 ? s.MeanSpO2.ToString("F0", Inv) : "",
        Csv(System.IO.Path.GetFileName(s.Path))));
    }
  }

  static string Csv(string v) {
    return v.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
  }
}
