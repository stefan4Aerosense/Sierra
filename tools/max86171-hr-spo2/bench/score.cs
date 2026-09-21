using System; using System.IO; using System.Linq; using System.Collections.Generic;
// score <prefix> [fs] - ranks every recorded channel by pulse strength and signal-to-noise.
// Reports, per channel: DC level (% of full scale), pulse amplitude as % of DC, SNR, and the rate it implies.
class Score {
  static double fs = 25.0;
  static double[] Tone(double[] v, double f) {
    int n = v.Length; double sx=0, sy=0, sxx=0, sxy=0;
    for (int i=0;i<n;i++){ sx+=i; sy+=v[i]; sxx+=(double)i*i; sxy+=i*v[i]; }
    double sl=(n*sxy-sx*sy)/(n*sxx-sx*sx), ic=(sy-sl*sx)/n;
    double re=0, im=0, ws=0;
    for (int i=0;i<n;i++){ double w=0.5-0.5*Math.Cos(2*Math.PI*i/(n-1)); double d=(v[i]-(ic+sl*i))*w, ph=2*Math.PI*f*i/fs; re+=d*Math.Cos(ph); im+=d*Math.Sin(ph); ws+=w; }
    return new[]{ 2*re/ws, 2*im/ws };
  }
  static double Mag(double[] c){ return Math.Sqrt(c[0]*c[0]+c[1]*c[1]); }
  static void Main(string[] a) {
    string pre = a[0];
    if (a.Length > 1) fs = double.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture);
    var files = Directory.GetFiles(".", "rec_" + pre + "_*.txt").OrderBy(f => f).ToArray();
    if (files.Length == 0) { Console.WriteLine("no files for prefix " + pre); return; }
    Console.WriteLine("{0,-10} {1,8} {2,8} {3,8} {4,7} {5,6}", "channel", "n", "DC %FS", "pulse%", "SNR", "BPM");
    foreach (var f in files) {
      var v = File.ReadAllLines(f).Skip(50).Select(double.Parse).ToArray();
      if (v.Length < fs * 8) continue;
      double dc = v.Average();
      double bestF = 0, bestM = -1;
      var mags = new List<double>();
      for (double fr = 0.7; fr <= 3.0; fr += 0.01) { double m = Mag(Tone(v, fr)); if (m > bestM) { bestM = m; bestF = fr; } }
      for (double fr = 0.7; fr <= Math.Min(4.0, fs/2-0.3); fr += 0.05)
        if (Math.Abs(fr-bestF) > 0.15 && Math.Abs(fr-2*bestF) > 0.15) mags.Add(Mag(Tone(v, fr)));
      mags.Sort();
      double floor = Math.Max(mags[mags.Count/2], 1e-9);
      string name = Path.GetFileNameWithoutExtension(f).Substring(("rec_"+pre+"_").Length);
      Console.WriteLine("{0,-10} {1,8} {2,8:F1} {3,8:F3} {4,7:F1} {5,6:F0}", name, v.Length, 100*dc/524287.0, 100*bestM/Math.Abs(dc), bestM/floor, bestF*60);
    }
  }
}
