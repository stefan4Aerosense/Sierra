using System;
using System.Linq;

// Offline check of the heart-rate maths on synthetic PPG (no hardware needed).
static class SelfTest {
  public static int Run() {
    var rnd = new Random(1);
    int failures = 0;
    foreach (double fs in new[] { 25.0, 50.0, 100.0 }) {
      foreach (double trueBpm in new[] { 45.0, 60.0, 72.0, 95.0, 130.0, 180.0 }) {
        var est = new HeartRateEstimator(fs);
        double f = trueBpm / 60.0, bpm = 0, q = 0, phase = 0;
        bool ok = false;
        for (int i = 0; i < fs * 30; i++) {
          double t = i / fs;
          phase += 2 * Math.PI * f * (1 + 0.03 * Math.Sin(2 * Math.PI * 0.1 * t)) / fs; // small HR variability
          // PPG-like: large DC, pulse with dicrotic harmonic, breathing wander, noise
          double pulse = -Math.Sin(phase) - 0.4 * Math.Sin(2 * phase + 0.8);
          double raw = 300000 + 2000 * pulse + 3000 * Math.Sin(2 * Math.PI * 0.25 * t) + 400 * (rnd.NextDouble() - 0.5);
          est.Add(raw);
          if (i % (int)fs == 0) ok = est.Estimate(out bpm, out q);
        }
        bool pass = ok && Math.Abs(bpm - trueBpm) <= Math.Max(3, trueBpm * 0.04);
        if (!pass) failures++;
        Console.WriteLine("fs={0,5} Hz  true={1,5:F0}  measured={2,6:F1}  quality={3:F2}  {4}", fs, trueBpm, bpm, q, pass ? "PASS" : "FAIL");
      }
    }
    // Pure noise must not produce a heart rate.
    var noise = new HeartRateEstimator(25);
    double nb = 0, nq = 0; bool falsePos = false;
    for (int i = 0; i < 25 * 20; i++) { noise.Add(300000 + 500 * (rnd.NextDouble() - 0.5)); if (i % 25 == 0 && noise.Estimate(out nb, out nq)) falsePos = true; }
    Console.WriteLine("noise only -> {0}", falsePos ? "FAIL (reported " + nb.ToString("F0") + " bpm)" : "PASS (no reading)");
    if (falsePos) failures++;
    // Several green channels: noise-only, saturated, and clean. The clean one must be used.
    var multiHr = new MultiHeartRate(25, "noise", "saturated", "clean");
    double mb = 0, mq = 0; bool mok = false;
    for (int i = 0; i < 25 * 20; i++) {
      double t = i / 25.0, pulse = Math.Sin(2 * Math.PI * 1.25 * t);
      multiHr.Add(0, 300000 + 800 * (rnd.NextDouble() - 0.5));
      multiHr.Add(1, 524287); // stuck at full scale
      multiHr.Add(2, 300000 - 1500 * pulse + 200 * (rnd.NextDouble() - 0.5));
      if (i % 25 == 0) mok = multiHr.Estimate(out mb, out mq);
    }
    bool mpass = mok && multiHr.BestChannel == "clean" && Math.Abs(mb - 75) <= 3;
    Console.WriteLine("multi-channel HR -> {0:F0} bpm via {1} {2}", mb, multiHr.BestChannel, mpass ? "PASS" : "FAIL");
    if (!mpass) failures++;

    failures += SpO2Tests(rnd);
    Console.WriteLine(failures == 0 ? "ALL TESTS PASSED" : failures + " TEST(S) FAILED");
    return failures;
  }

  // Synthetic red/IR with a known ratio R: the estimator must recover R and report the curve's SpO2.
  static int SpO2Tests(Random rnd) {
    int failures = 0;
    const double fs = 25;
    foreach (double trueR in new[] { 0.4, 0.6, 0.8, 1.0 }) {
      foreach (double noiseRms in new[] { 50.0, 220.0 }) {
        var ox = new SpO2Estimator(fs);
        double bpm = 70, f = bpm / 60, dcIr = 240000, dcRed = 300000, piIr = 0.0010; // 0.1% IR perfusion
        SpO2Estimator.Result res = null;
        for (int i = 0; i < fs * 30; i++) {
          double t = i / fs, pulse = Math.Sin(2 * Math.PI * f * t) + 0.3 * Math.Sin(4 * Math.PI * f * t + 0.5);
          double wander = Math.Sin(2 * Math.PI * 0.2 * t);
          double ir = dcIr * (1 + 0.004 * wander) - dcIr * piIr * pulse + noiseRms * Gauss(rnd);
          double red = dcRed * (1 + 0.004 * wander) - dcRed * piIr * trueR * pulse + noiseRms * Gauss(rnd);
          ox.Add(red, ir);
          if (i % (int)fs == 0) res = ox.Estimate(bpm);
        }
        double expected = SpO2Estimator.CurveSpO2(trueR);
        // Low noise: must be accurate. Realistic (high) noise: must be within 3%, or be withheld as unstable.
        bool accurate = res.Problem == null && Math.Abs(res.SpO2 - expected) <= (noiseRms < 100 ? 2 : 3);
        bool flagged = res.Problem != null;
        bool pass = noiseRms < 100 ? accurate : (accurate || flagged);
        if (!pass) failures++;
        Console.WriteLine("SpO2: true R={0:F2} noise={1,3:F0} -> R={2:F2} SpO2={3:F1}% +-{4:F1} (expect {5:F1}%) {6} {7}",
          trueR, noiseRms, res.R, res.SpO2, res.Spread, expected, res.Problem ?? "", pass ? (accurate ? "PASS" : "PASS (flagged unstable)") : "FAIL");
      }
    }
    // Red and IR pulses that are not in step (e.g. motion) must be rejected.
    var bad = new SpO2Estimator(fs);
    for (int i = 0; i < fs * 18; i++) {
      double t = i / fs;
      bad.Add(300000 + 400 * Math.Sin(2 * Math.PI * 70 / 60.0 * t + Math.PI / 2), 240000 + 400 * Math.Sin(2 * Math.PI * 70 / 60.0 * t));
    }
    var rb = bad.Estimate(70);
    bool rejected = rb.Problem != null;
    Console.WriteLine("SpO2: out-of-phase red/IR -> {0}", rejected ? "PASS (rejected: " + rb.Problem + ")" : "FAIL (reported " + rb.SpO2.ToString("F0") + "%)");
    if (!rejected) failures++;

    // Two paths: one buried in noise (wrong ratio), one clean. The clean path must be chosen.
    var multi = new MultiPathOximeter(fs, "noisy", "clean");
    SpO2Estimator.Result mr = null;
    for (int i = 0; i < fs * 30; i++) {
      double t = i / fs, pulse = Math.Sin(2 * Math.PI * 70 / 60.0 * t);
      multi.Add(0, 300000 - 300000 * 0.0003 * 1.4 * pulse + 400 * Gauss(rnd), 400000 - 400000 * 0.0003 * pulse + 400 * Gauss(rnd));
      multi.Add(1, 200000 - 200000 * 0.003 * 0.5 * pulse + 50 * Gauss(rnd), 300000 - 300000 * 0.003 * pulse + 50 * Gauss(rnd));
      if (i % (int)fs == 0) mr = multi.Estimate(70);
    }
    bool picked = mr.Problem == null && mr.Path == "clean" && Math.Abs(mr.SpO2 - SpO2Estimator.CurveSpO2(0.5)) <= 1;
    Console.WriteLine("SpO2: two paths -> chose {0}, {1:F1}% (expect {2:F1}%) {3}", mr.Path, mr.SpO2, SpO2Estimator.CurveSpO2(0.5), picked ? "PASS" : "FAIL " + (mr.Problem ?? ""));
    if (!picked) failures++;
    return failures;
  }

  static double Gauss(Random r) { return Math.Sqrt(-2 * Math.Log(1 - r.NextDouble())) * Math.Cos(2 * Math.PI * r.NextDouble()); }

  // Replays green + two red/IR paths (one value per line) through the heart-rate and multi-path oximeter.
  public static int Replay2(double fs, string green, string red1, string ir1, string red2, string ir2) {
    Func<string, double[]> load = p => System.IO.File.ReadAllLines(p).Select(double.Parse).ToArray();
    var g = load(green); var r1 = load(red1); var i1 = load(ir1); var r2 = load(red2); var i2 = load(ir2);
    int n = new[] { g.Length, r1.Length, i1.Length, r2.Length, i2.Length }.Min();
    var hr = new HeartRateEstimator(fs); var ox = new MultiPathOximeter(fs, "detector 1", "detector 2");
    int step = (int)Math.Round(fs);
    for (int i = 0; i < n; i++) {
      hr.Add(-g[i]); ox.Add(0, r1[i], i1[i]); ox.Add(1, r2[i], i2[i]);
      if (i > 0 && i % step == 0) {
        double bpm, q; bool ok = hr.Estimate(out bpm, out q);
        string s = "";
        if (ok) { var res = ox.Estimate(bpm); s = res.Problem != null ? res.Path + ": " + res.Problem : string.Format("SpO2 {0:F0}% +-{1:F0} via {2} R={3:F2} SNR={4:F0}", res.SpO2, res.Spread, res.Path, res.R, res.Snr); }
        Console.WriteLine("t={0,2}s  {1}  {2}", i / step, ok ? bpm.ToString("F0") + " BPM" : "no HR", s);
      }
    }
    return 0;
  }

  // Replays green/IR/red recordings (one value per line, 25 Hz) through both estimators.
  public static int Replay(string green, string ir, string red) {
    Func<string, double[]> load = p => System.IO.File.ReadAllLines(p).Select(double.Parse).ToArray();
    var g = load(green); var x = load(ir); var r = load(red);
    int n = Math.Min(g.Length, Math.Min(x.Length, r.Length));
    var hr = new HeartRateEstimator(25); var ox = new SpO2Estimator(25);
    for (int i = 0; i < n; i++) {
      hr.Add(-g[i]); ox.Add(r[i], x[i]);
      if (i > 0 && i % 25 == 0) {
        double bpm, q; bool ok = hr.Estimate(out bpm, out q);
        string s = "";
        if (ok) { var res = ox.Estimate(bpm); s = res.Problem ?? string.Format("SpO2 {0:F0}% +-{4:F0} R={1:F2} PI_ir={2:F3}% phase={3:F0}", res.SpO2, res.R, res.PerfusionIr, res.PhaseDeg, res.Spread); }
        Console.WriteLine("t={0,2}s  {1}  {2}", i / 25, ok ? bpm.ToString("F0") + " BPM" : "no HR", s);
      }
    }
    return 0;
  }
}


