// Offline comparison of heart-rate methods on recordings. Usage: hreval <fs> <green file> [<green file> ...]
// Prints, per second, the current app method (autocorrelation, best channel) vs the spectral harmonic-sum method.
using System; using System.IO; using System.Linq; using System.Collections.Generic;
class HrEval {
  static void Main(string[] a) {
    double fs = double.Parse(a[0], System.Globalization.CultureInfo.InvariantCulture);
    var ch = a.Skip(1).Select(f => File.ReadAllLines(f).Select(double.Parse).ToArray()).ToArray();
    int n = ch.Min(c => c.Length), step = (int)Math.Round(fs);
    var ac = ch.Select(c => new HeartRateEstimator(fs)).ToArray();
    var sp = new SpectralHeartRate(fs, ch.Length);
    var agree = new List<double>(); int acOk = 0, spOk = 0, acLow = 0, spLow = 0;
    for (int i = 0; i < n; i++) {
      for (int k = 0; k < ch.Length; k++) { ac[k].Add(-ch[k][i]); sp.Add(k, ch[k][i]); }
      if (i > 0 && i % step == 0 && i > fs * 10) {
        double bestQ = 0, bestB = 0;
        for (int k = 0; k < ch.Length; k++) { double b, q; if (ac[k].Estimate(out b, out q) && q > bestQ) { bestQ = q; bestB = b; } }
        double sb, sq; bool sOk = sp.Estimate(out sb, out sq);
        if (bestB > 0) { acOk++; if (bestB < 50) acLow++; }
        if (sOk) { spOk++; if (sb < 50) spLow++; }
        Console.WriteLine("t={0,3}s  autocorr {1,5}  spectral {2,5} (score {3:F1})", i / step, bestB > 0 ? bestB.ToString("F0") : "--", sOk ? sb.ToString("F0") : "--", sq);
      }
    }
    Console.WriteLine("SUMMARY autocorr: reading {0} s ({1} below 50 BPM) | spectral: reading {2} s ({3} below 50 BPM)", acOk, acLow, spOk, spLow);
  }
}
