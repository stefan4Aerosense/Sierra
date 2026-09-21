using System; using System.IO; using System.Linq;
// filttest <fs> <file...> : what each band-pass low edge does to the reported rate.
class FiltTest {
  static void Main(string[] a) {
    double fs = double.Parse(a[0], System.Globalization.CultureInfo.InvariantCulture);
    var ch = a.Skip(1).Select(f => File.ReadAllLines(f).Select(double.Parse).ToArray()).ToArray();
    int n = ch.Min(c => c.Length), step = (int)Math.Round(fs);
    foreach (double hp in new[] { 0.5, 0.7, 0.8, 0.9 }) {
      HeartRateEstimator.HighPassHz = hp;
      var est = ch.Select(c => new HeartRateEstimator(fs)).ToArray();
      int reads = 0, low = 0; double last = 0, lastQ = 0;
      for (int i = 0; i < n; i++) {
        for (int c = 0; c < ch.Length; c++) est[c].Add(-ch[c][i]);
        if (i == 0 || i % step != 0) continue;
        double bb = 0, bq = 0;
        for (int c = 0; c < ch.Length; c++) { double b, q; if (est[c].Estimate(out b, out q) && q > bq) { bq = q; bb = b; } }
        if (bb > 0) { reads++; last = bb; lastQ = bq; if (bb < 55) low++; }
      }
      Console.WriteLine("  low edge {0:F1} Hz ({1,2:F0} BPM) -> last {2,3:F0} BPM (quality {3:F2}), {4,2} readings, {5} under 55",
        hp, hp * 60, last, lastQ, reads, low);
    }
  }
}
