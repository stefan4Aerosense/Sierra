using System; using System.IO; using System.Linq; using System.Collections.Generic;
// spec <file> <fs> - prints the strongest frequencies in a recorded channel, so a half-rate lock is visible.
class Spec {
  static double fs;
  static double[] Tone(double[] v, double f) {
    int n = v.Length; double sx=0, sy=0, sxx=0, sxy=0;
    for (int i=0;i<n;i++){ sx+=i; sy+=v[i]; sxx+=(double)i*i; sxy+=i*v[i]; }
    double sl=(n*sxy-sx*sy)/(n*sxx-sx*sx), ic=(sy-sl*sx)/n;
    double re=0, im=0, ws=0;
    for (int i=0;i<n;i++){ double w=0.5-0.5*Math.Cos(2*Math.PI*i/(n-1)); double d=(v[i]-(ic+sl*i))*w, ph=2*Math.PI*f*i/fs; re+=d*Math.Cos(ph); im+=d*Math.Sin(ph); ws+=w; }
    return new[]{2*re/ws, 2*im/ws};
  }
  static double Mag(double[] c){ return Math.Sqrt(c[0]*c[0]+c[1]*c[1]); }
  static void Main(string[] a) {
    var v = File.ReadAllLines(a[0]).Skip(50).Select(double.Parse).ToArray();
    fs = double.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture);
    double dc = v.Average();
    var peaks = new List<double[]>();
    for (double f = 0.5; f <= 3.6; f += 0.01) peaks.Add(new[]{ f, Mag(Tone(v, f)) });
    // local maxima only
    var tops = new List<double[]>();
    for (int i = 1; i < peaks.Count - 1; i++)
      if (peaks[i][1] >= peaks[i-1][1] && peaks[i][1] >= peaks[i+1][1]) tops.Add(peaks[i]);
    foreach (var p in tops.OrderByDescending(p => p[1]).Take(8))
      Console.WriteLine("  {0,5:F2} Hz = {1,3:F0} BPM   amplitude {2,8:F0}  ({3:F3}% of DC)", p[0], p[0]*60, p[1], 100*p[1]/Math.Abs(dc));
  }
}
