using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;

// Records a measurement session to disk: one CSV per session, plus a single index of every session.
// Everything is plain text on this machine - nothing is sent anywhere.
class SessionRecorder {
  public readonly string Subject;
  public readonly DateTime Started;
  public readonly string Path;
  readonly StreamWriter file;
  readonly List<double> rates = new List<double>(), oxygen = new List<double>();
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
    public string Subject, Path;
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
    return s;
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
