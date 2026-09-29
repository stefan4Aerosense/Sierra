using System; using System.IO; using System.Linq;
// spo2test <fs> <red file> <ir file> <bpm> - runs our SpO2 estimator over a recorded red/IR pair.
class SpO2Test {
  static void Main(string[] a) {
    double fs = double.Parse(a[0], System.Globalization.CultureInfo.InvariantCulture);
    double bpm = double.Parse(a[3], System.Globalization.CultureInfo.InvariantCulture);
    var red = File.ReadAllLines(a[1]).Select(double.Parse).ToArray();
    var ir = File.ReadAllLines(a[2]).Select(double.Parse).ToArray();
    int n = Math.Min(red.Length, ir.Length), step = (int)Math.Round(fs);
    var ox = new SpO2Estimator(fs);
    for (int i = 0; i < n; i++) {
      ox.Add(red[i], ir[i]);
      if (i > 0 && i % step == 0) {
        var r = ox.Estimate(bpm);
        if (i / step >= 17 && (i / step) % 4 == 0)
          Console.WriteLine("  t={0,3}s  {1}", i / step,
            r.Problem == null
              ? string.Format("SpO2 {0:F1}% +-{1:F1}  R={2:F3}  PI_ir={3:F3}%  SNR={4:F0}", r.SpO2, r.Spread, r.R, r.PerfusionIr, r.Snr)
              : r.Problem + string.Format("   (R={0:F2}, PI_ir={1:F3}%, SNR={2:F1})", r.R, r.PerfusionIr, r.Snr));
      }
    }
  }
}
