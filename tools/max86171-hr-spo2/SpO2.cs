using System;
using System.Collections.Generic;
using System.Linq;

// Blood-oxygen estimate from red and IR PPG using the "ratio of ratios":
//   R = (AC_red / DC_red) / (AC_ir / DC_ir),  SpO2 = f(R)
// AC is measured at the heart-rate frequency only (a lock-in style measurement), which rejects most noise.
// The R -> SpO2 curve is a generic empirical one and is NOT calibrated for this sensor.
class SpO2Estimator {
  public const int FullScale = 524287;
  readonly double fs;
  readonly List<double> red = new List<double>(), ir = new List<double>();
  readonly List<double> recent = new List<double>();

  public SpO2Estimator(double sampleRate) { fs = sampleRate; }

  public void Add(double redRaw, double irRaw) {
    red.Add(redRaw); ir.Add(irRaw);
    int keep = (int)(fs * 20);
    if (red.Count > keep * 2) { red.RemoveRange(0, red.Count - keep); ir.RemoveRange(0, ir.Count - keep); }
  }

  public class Result { public double SpO2, Spread, R, PerfusionIr, PhaseDeg, Snr; public string Problem, Path; }

  // bpm must come from a valid heart-rate reading.
  public Result Estimate(double bpm) {
    var res = new Result();
    int n = (int)(fs * 16); // long window: red/IR pulses are small on this sensor
    if (ir.Count < n) { res.Problem = "collecting data"; return res; }
    var r = red.GetRange(red.Count - n, n).ToArray();
    var x = ir.GetRange(ir.Count - n, n).ToArray();

    double dcR = r.Average(), dcI = x.Average();
    if (r.Max() >= FullScale - 1000 || x.Max() >= FullScale - 1000) { res.Problem = "sensor saturated - press more lightly"; return res; }
    if (dcR < FullScale * 0.02 || dcI < FullScale * 0.02) { res.Problem = "too little light - cover the sensor fully"; return res; }

    // Find the exact pulse frequency near the heart rate using IR, then measure both colors there.
    double f0 = bpm / 60.0, bestF = f0, bestMag = -1;
    for (double f = f0 - 0.08; f <= f0 + 0.0801; f += 0.005) {
      var c = Tone(x, f); double m = Mag(c);
      if (m > bestMag) { bestMag = m; bestF = f; }
    }
    var cI = Tone(x, bestF); var cR = Tone(r, bestF);
    double acI = Mag(cI), acR = Mag(cR);
    res.PerfusionIr = 100 * acI / dcI;
    res.R = (acR / dcR) / (acI / dcI);
    // Signal-to-noise: pulse amplitude vs the typical amplitude at nearby non-harmonic frequencies (worse of red/IR).
    res.Snr = Math.Min(acI / NoiseFloor(x, bestF), acR / NoiseFloor(r, bestF));
    if (res.Snr < 5) { recent.Clear(); res.Problem = "red/IR pulse buried in noise (SNR " + res.Snr.ToString("F1") + ")"; return res; }

    // Red and IR pulses come from the same blood volume change, so they should be in phase.
    double phase = Math.Atan2(cR[1] * cI[0] - cR[0] * cI[1], cR[0] * cI[0] + cR[1] * cI[1]) * 180 / Math.PI;
    res.PhaseDeg = phase;
    if (Math.Abs(phase) > 25) { recent.Clear(); res.Problem = "red/IR pulses out of step - keep still"; return res; }
    if (res.PerfusionIr < 0.08) { recent.Clear(); res.Problem = "red/IR pulse too weak - rest finger lightly, keep warm and still"; return res; }
    if (res.R < 0.2 || res.R > 2.0) { recent.Clear(); res.Problem = "ratio out of range (R=" + res.R.ToString("F2") + ")"; return res; }

    double s = CurveSpO2(res.R);
    recent.Add(s);
    if (recent.Count > 10) recent.RemoveAt(0);
    var sorted = recent.OrderBy(v => v).ToList();
    res.SpO2 = sorted[sorted.Count / 2];
    // Spread of recent readings (ignoring the single highest and lowest once there are enough).
    int trim = sorted.Count >= 5 ? 1 : 0;
    res.Spread = (sorted[sorted.Count - 1 - trim] - sorted[trim]) / 2;
    // Only report a number once several recent readings agree; otherwise it is not trustworthy.
    if (recent.Count < 5) res.Problem = "checking stability (" + recent.Count + "/5)";
    else if (res.Spread > 2) res.Problem = "unstable, readings vary +/-" + res.Spread.ToString("F0") + "% - keep still";
    return res;
  }

  // Widely used empirical curve (Maxim reference design); clamp to a plausible range.
  public static double CurveSpO2(double r) {
    double s = -45.060 * r * r + 30.354 * r + 94.845;
    return Math.Max(70, Math.Min(100, s));
  }

  // Complex amplitude of a sinusoid at frequency f (Hann window, linear trend removed).
  double[] Tone(double[] v, double f) {
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

  double NoiseFloor(double[] v, double pulseF) {
    var mags = new List<double>();
    for (double f = 0.6; f <= Math.Min(4.0, fs / 2 - 0.3); f += 0.1) {
      if (Math.Abs(f - pulseF) < 0.15 || Math.Abs(f - 2 * pulseF) < 0.15 || Math.Abs(f - 3 * pulseF) < 0.15) continue;
      mags.Add(Mag(Tone(v, f)));
    }
    if (mags.Count == 0) return 1e-9;
    mags.Sort();
    return Math.Max(mags[mags.Count / 2], 1e-9);
  }

  static double Mag(double[] c) { return Math.Sqrt(c[0] * c[0] + c[1] * c[1]); }
}

// Several independent red/IR measurement paths (one per photodiode). Each keeps its own history;
// the reading comes from the cleanest valid path, because averaging a poor path in only adds error.
class MultiPathOximeter {
  readonly SpO2Estimator[] paths;
  readonly string[] names;
  public MultiPathOximeter(double sampleRate, params string[] pathNames) {
    names = pathNames;
    paths = pathNames.Select(n => new SpO2Estimator(sampleRate)).ToArray();
  }
  public void Add(int path, double red, double ir) { paths[path].Add(red, ir); }

  public SpO2Estimator.Result Estimate(double bpm) {
    var results = new List<SpO2Estimator.Result>();
    for (int i = 0; i < paths.Length; i++) { var r = paths[i].Estimate(bpm); r.Path = names[i]; results.Add(r); }
    var valid = results.Where(r => r.Problem == null).OrderByDescending(r => r.Snr).ToList();
    if (valid.Count > 0) return valid[0];
    // Nothing usable: report the path that is closest to usable.
    return results.OrderByDescending(r => r.Snr).First();
  }
}


