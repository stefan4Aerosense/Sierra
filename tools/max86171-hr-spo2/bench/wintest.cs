using System; using System.IO; using System.Linq;
// wintest <fs> <file...> : how soon, and how reliably, a reading appears with different window lengths.
class WinTest {
  static void Main(string[] a) {
    double fs = double.Parse(a[0], System.Globalization.CultureInfo.InvariantCulture);
    var ch = a.Skip(1).Select(f => File.ReadAllLines(f).Select(double.Parse).ToArray()).ToArray();
    int n = ch.Min(c => c.Length);
    foreach (double win in new[] { 4.0, 5.0, 6.0, 8.0 }) {
      var est = ch.Select(c => new HeartRateEstimator(fs)).ToArray();
      int first = -1, reads = 0, wrong = 0; double lastBpm = 0;
      for (int i = 0; i < n; i++) {
        for (int c = 0; c < ch.Length; c++) est[c].Add(-ch[c][i]);
        if (i % (int)Math.Round(fs) != 0 || i == 0) continue;
        double bestBpm = 0, bestQ = 0;
        for (int c = 0; c < ch.Length; c++) {
          double b, q;
          if (est[c].Estimate(out b, out q, win) && q > bestQ) { bestQ = q; bestBpm = b; }
        }
        if (bestBpm > 0) {
          if (first < 0) first = i / (int)Math.Round(fs);
          reads++; lastBpm = bestBpm;
          if (bestBpm < 50) wrong++;
        }
      }
      Console.WriteLine("  window {0,3:F0}s -> first reading at {1,3}s, {2,3} readings, {3} below 50 BPM, last {4:F0}",
        win, first < 0 ? -1 : first, reads, wrong, lastBpm);
    }
  }
}
