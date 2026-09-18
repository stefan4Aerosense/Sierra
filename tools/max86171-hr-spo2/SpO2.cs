using System;
using System.Collections.Generic;
using System.Linq;

// Blood-oxygen estimate from red and IR PPG using the "ratio of ratios":
//   R = (AC_red / DC_red) / (AC_ir / DC_ir),  SpO2 = f(R)
// AC is measured at the heart-rate frequency only (a lock-in style measurement), which rejects most noise.
// When a green reference is given, the pulse frequency AND phase come from green (the strongest signal), and
// red/IR are measured as their projection onto that phase: noise that is out of step with the heartbeat is
// discarded instead of being counted as pulse.
// The R -> SpO2 curve is a generic empirical one and is NOT calibrated for this sensor.
class SpO2Estimator {
  public const int FullScale = 524287;
  public const double MinSnr = 3, GoodSnr = 5, MinPi = 0.05, GoodPi = 0.08, GoodSpread = 2, MaxSpread = 5;
  readonly double fs;
  readonly List<double> red = new List<double>(), ir = new List<double>(), green = new List<double>();
  readonly List<double> recent = new List<double>();

  public SpO2Estimator(double sampleRate) { fs = sampleRate; }

  public int WindowLength { get { return (int)(fs * 16); } } // long window: red/IR pulses are small on this sensor

  public void Add(double redRaw, double irRaw) { Add(redRaw, irRaw, double.NaN); }

  public void Add(double redRaw, double irRaw, double greenRaw) {
    red.Add(redRaw); ir.Add(irRaw); green.Add(greenRaw);
    int keep = (int)(fs * 20);
    if (red.Count > keep * 2) {
      int drop = red.Count - keep;
      red.RemoveRange(0, drop); ir.RemoveRange(0, drop); green.RemoveRange(0, drop);
    }
  }

  // Latest n raw green samples, or null if not enough / not recorded.
  public double[] GreenWindow(int n) {
    if (green.Count < n) return null;
    var g = green.GetRange(green.Count - n, n).ToArray();
    return g.Any(double.IsNaN) ? null : g;
  }

  public class Result {
    public double SpO2, Spread, R, PerfusionIr, PhaseDeg, Snr;
    public string Problem, Path;
    public bool LowConfidence, GreenLocked;
  }

  public Result Estimate(double bpm) { return Estimate(bpm, null); }

  // bpm must come from a valid heart-rate reading. greenRef: raw green window aligned with this path's
  // latest WindowLength samples (any photodiode), or null to lock onto IR alone.
  public Result Estimate(double bpm, double[] greenRef) {
    var res = new Result();
    int n = WindowLength;
    if (ir.Count < n) { res.Problem = "collecting data"; return res; }
    var r = red.GetRange(red.Count - n, n).ToArray();
    var x = ir.GetRange(ir.Count - n, n).ToArray();

    double dcR = r.Average(), dcI = x.Average();
    if (r.Max() >= FullScale - 1000 || x.Max() >= FullScale - 1000) { res.Problem = "sensor saturated - cover the sensor fully"; return res; }
    if (dcR < FullScale * 0.02 || dcI < FullScale * 0.02) { res.Problem = "too little light - cover the sensor fully"; return res; }

    // Pulse frequency near the heart rate: from green if available, else from IR.
    double[] refSig = (greenRef != null && greenRef.Length == n) ? greenRef : x;
    res.GreenLocked = refSig != x;
    double f0 = bpm / 60.0, bestF = f0, bestMag = -1;
    for (double f = f0 - 0.08; f <= f0 + 0.0801; f += 0.005) {
      double m = Mag(Tone(refSig, f));
      if (m > bestMag) { bestMag = m; bestF = f; }
    }
    var cI = Tone(x, bestF); var cR = Tone(r, bestF);
    double acI, acR;
    if (res.GreenLocked) {
      var cG = Tone(refSig, bestF); double gm = Mag(cG);
      if (gm <= 0) { res.Problem = "no green reference"; return res; }
      // Signed projection onto the green pulse phase. Blood volume lowers all three wavelengths together,
      // so red and IR must project positively; a negative projection means the "pulse" is something else.
      acI = (cI[0] * cG[0] + cI[1] * cG[1]) / gm;
      acR = (cR[0] * cG[0] + cR[1] * cG[1]) / gm;
      if (acI <= 0 || acR <= 0) { recent.Clear(); res.Problem = "red/IR not in step with the heartbeat - keep still"; return res; }
    } else {
      acI = Mag(cI); acR = Mag(cR);
    }
    res.PerfusionIr = 100 * acI / dcI;
    res.R = (acR / dcR) / (acI / dcI);
    // Signal-to-noise: pulse amplitude vs the typical amplitude at nearby non-harmonic frequencies (worse of red/IR).
    res.Snr = Math.Min(acI / NoiseFloor(x, bestF), acR / NoiseFloor(r, bestF));
    if (res.Snr < MinSnr) { recent.Clear(); res.Problem = "red/IR pulse too weak (SNR " + res.Snr.ToString("F1") + ") - rest finger lightly, keep warm"; return res; }

    // Red and IR pulses come from the same blood volume change, so they should be in phase.
    double phase = Math.Atan2(cR[1] * cI[0] - cR[0] * cI[1], cR[0] * cI[0] + cR[1] * cI[1]) * 180 / Math.PI;
    res.PhaseDeg = phase;
    if (Math.Abs(phase) > 25) { recent.Clear(); res.Problem = "red/IR pulses out of step - keep still"; return res; }
    if (res.PerfusionIr < MinPi) { recent.Clear(); res.Problem = "red/IR pulse too weak - rest finger lightly, keep warm and still"; return res; }
    if (res.R < 0.2 || res.R > 2.0) { recent.Clear(); res.Problem = "ratio out of range (R=" + res.R.ToString("F2") + ")"; return res; }

    double s = CurveSpO2(res.R);
    recent.Add(s);
    if (recent.Count > 10) recent.RemoveAt(0);
    var sorted = recent.OrderBy(v => v).ToList();
    res.SpO2 = sorted[sorted.Count / 2];
    // Spread of recent readings (ignoring the single highest and lowest once there are enough).
    int trim = sorted.Count >= 5 ? 1 : 0;
    res.Spread = (sorted[sorted.Count - 1 - trim] - sorted[trim]) / 2;
    // Only report a number once several recent readings agree. Marginal signals are shown, flagged low-confidence.
    if (recent.Count < 5) res.Problem = "checking stability (" + recent.Count + "/5)";
    else if (res.Spread > MaxSpread) res.Problem = "unstable, readings vary +/-" + res.Spread.ToString("F0") + "% - keep still";
    else res.LowConfidence = res.Spread > GoodSpread || res.Snr < GoodSnr || res.PerfusionIr < GoodPi;
    return res;
  }

  // Widely used empirical curve (Maxim reference design); clamp to a plausible range.
  public static double CurveSpO2(double r) {
    double s = -45.060 * r * r + 30.354 * r + 94.845;
    return Math.Max(70, Math.Min(100, s));
  }

  // Complex amplitude of a sinusoid at frequency f (Hann window, linear trend removed).
  public double[] Tone(double[] v, double f) {
    int n = v.Length;
    double sx = 0, sy = 0, sxx = 0, sxy = 0;
    for (int i = 0; i < n; i++) { sx += i; sy += v[i]; sxx += (double)i * i; sxy += i * v[i]; }
    double slope = (n * sxy - sx * sy) / (n * sxx - sx * sx), icpt = (sy - slope * sx) / n;
    double re = 0, im = 0, wsum = 0;
    for (int i = 0; i < n; i++) {
      double w = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (n - 1));
      double d = (v[i] - (icpt + slope * i)) * w;
      double ph = 2 * Math.PI * f * i / fs;
      re += d * Math.Cos(ph); im += d * Math.Sin(ph); wsum += w;
    }
    return new[] { 2 * re / wsum, 2 * im / wsum };
  }

  public double NoiseFloor(double[] v, double pulseF) {
    var mags = new List<double>();
    for (double f = 0.6; f <= Math.Min(4.0, fs / 2 - 0.3); f += 0.1) {
      if (Math.Abs(f - pulseF) < 0.15 || Math.Abs(f - 2 * pulseF) < 0.15 || Math.Abs(f - 3 * pulseF) < 0.15) continue;
      mags.Add(Mag(Tone(v, f)));
    }
    if (mags.Count == 0) return 1e-9;
    mags.Sort();
    return Math.Max(mags[mags.Count / 2], 1e-9);
  }

  public static double Mag(double[] c) { return Math.Sqrt(c[0] * c[0] + c[1] * c[1]); }
}

// Several independent red/IR measurement paths (one per photodiode). Each keeps its own history;
// the reading comes from the cleanest valid path, because averaging a poor path in only adds error.
// All paths are locked to the cleanest green pulse on any photodiode.
class MultiPathOximeter {
  readonly SpO2Estimator[] paths;
  readonly string[] names;
  public MultiPathOximeter(double sampleRate, params string[] pathNames) {
    names = pathNames;
    paths = pathNames.Select(n => new SpO2Estimator(sampleRate)).ToArray();
  }
  public void Add(int path, double red, double ir) { paths[path].Add(red, ir); }
  public void Add(int path, double red, double ir, double green) { paths[path].Add(red, ir, green); }

  // The green window (any path) with the clearest pulse at the heart rate, or null.
  double[] BestGreen(double bpm) {
    double[] best = null; double bestSnr = 0;
    foreach (var p in paths) {
      var g = p.GreenWindow(p.WindowLength);
      if (g == null) continue;
      double f = bpm / 60.0, snr = SpO2Estimator.Mag(p.Tone(g, f)) / p.NoiseFloor(g, f);
      if (snr > bestSnr) { bestSnr = snr; best = g; }
    }
    return bestSnr >= 2 ? best : null;
  }

  public SpO2Estimator.Result Estimate(double bpm) {
    var gRef = BestGreen(bpm);
    var results = new List<SpO2Estimator.Result>();
    for (int i = 0; i < paths.Length; i++) { var r = paths[i].Estimate(bpm, gRef); r.Path = names[i]; results.Add(r); }
    var valid = results.Where(r => r.Problem == null)
                       .OrderBy(r => r.LowConfidence).ThenByDescending(r => r.Snr).ToList();
    if (valid.Count > 0) return valid[0];
    // Nothing usable: report the path that is closest to usable.
    return results.OrderByDescending(r => r.Snr).First();
  }
}
