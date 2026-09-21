using System;
using System.Collections.Generic;
using System.Linq;

// 2nd-order IIR section (RBJ cookbook).
class Biquad {
  double b0, b1, b2, a1, a2, z1, z2;
  public static Biquad LowPass(double fs, double fc) { return Make(fs, fc, false); }
  public static Biquad HighPass(double fs, double fc) { return Make(fs, fc, true); }
  static Biquad Make(double fs, double fc, bool high) {
    double w = 2 * Math.PI * fc / fs, cos = Math.Cos(w), alpha = Math.Sin(w) / (2 * Math.Sqrt(0.5));
    double a0 = 1 + alpha;
    var q = new Biquad();
    if (high) { q.b0 = (1 + cos) / 2; q.b1 = -(1 + cos); q.b2 = (1 + cos) / 2; }
    else { q.b0 = (1 - cos) / 2; q.b1 = 1 - cos; q.b2 = (1 - cos) / 2; }
    q.b0 /= a0; q.b1 /= a0; q.b2 /= a0; q.a1 = -2 * cos / a0; q.a2 = (1 - alpha) / a0;
    return q;
  }
  public double Step(double x) {
    double y = b0 * x + z1;
    z1 = b1 * x - a1 * y + z2;
    z2 = b2 * x - a2 * y;
    return y;
  }
}

// Heart rate from a raw PPG stream: band-pass 0.5-4 Hz, then autocorrelation over an 8 s window.
class HeartRateEstimator {
  public const double MinBpm = 35, MaxBpm = 220;
  readonly double fs;
  readonly Biquad hp1, hp2, lp1, lp2;
  readonly List<double> filtered = new List<double>();
  readonly List<double> recent = new List<double>();
  int settle;

  public HeartRateEstimator(double sampleRate) {
    fs = sampleRate;
    hp1 = Biquad.HighPass(fs, 0.5); hp2 = Biquad.HighPass(fs, 0.5);
    lp1 = Biquad.LowPass(fs, Math.Min(4.0, fs * 0.45)); lp2 = Biquad.LowPass(fs, Math.Min(4.0, fs * 0.45));
    settle = (int)(fs * 2); // let the filters settle before using samples
  }

  public double SampleRate { get { return fs; } }
  public int Count { get { return filtered.Count; } }

  public double Add(double raw) {
    double y = lp2.Step(lp1.Step(hp2.Step(hp1.Step(raw))));
    if (settle > 0) { settle--; return 0; }
    filtered.Add(y);
    int keep = (int)(fs * 12);
    if (filtered.Count > keep * 2) filtered.RemoveRange(0, filtered.Count - keep);
    return y;
  }

  public double[] Tail(int n) {
    n = Math.Min(n, filtered.Count);
    return filtered.GetRange(filtered.Count - n, n).ToArray();
  }

  // Returns true with bpm/quality when the last 8 s contain a clear periodic pulse.
  public bool Estimate(out double bpm, out double quality) { return Estimate(out bpm, out quality, 8.0); }

  // windowSeconds: shorter windows give a first reading sooner, at the cost of a noisier correlation.
  public bool Estimate(out double bpm, out double quality, double windowSeconds) {
    bpm = 0; quality = 0;
    int n = (int)(fs * windowSeconds);
    if (filtered.Count < n) return false;
    var x = Tail(n);
    double mean = x.Average();
    for (int i = 0; i < n; i++) x[i] -= mean;

    int minLag = Math.Max(2, (int)Math.Floor(fs * 60 / MaxBpm));
    int maxLag = Math.Min(n / 2, (int)Math.Ceiling(fs * 60 / MinBpm));
    var r = new double[maxLag + 2];
    for (int lag = minLag - 1; lag <= maxLag + 1 && lag < n; lag++) {
      double sxy = 0, sxx = 0, syy = 0;
      for (int i = 0; i + lag < n; i++) { sxy += x[i] * x[i + lag]; sxx += x[i] * x[i]; syy += x[i + lag] * x[i + lag]; }
      r[lag] = (sxx > 0 && syy > 0) ? sxy / Math.Sqrt(sxx * syy) : 0;
    }

    double best = 0;
    for (int lag = minLag; lag <= maxLag; lag++) best = Math.Max(best, r[lag]);
    if (best < 0.3) return false;

    // Pick the shortest-lag local peak close to the global max, so harmonics (2T, 3T) are not chosen.
    int pick = -1;
    for (int lag = minLag; lag <= maxLag; lag++) {
      if (r[lag] >= r[lag - 1] && r[lag] >= r[lag + 1] && r[lag] >= 0.85 * best) { pick = lag; break; }
    }
    if (pick < 0) return false;

    double y0 = r[pick - 1], y1 = r[pick], y2 = r[pick + 1];
    double denom = y0 - 2 * y1 + y2;
    double offset = Math.Abs(denom) > 1e-12 ? 0.5 * (y0 - y2) / denom : 0;
    double period = pick + Math.Max(-0.5, Math.Min(0.5, offset));

    double hr = 60.0 * fs / period;
    quality = y1;
    if (y1 < 0.5 || hr < MinBpm || hr > MaxBpm) return false;

    recent.Add(hr);
    if (recent.Count > 5) recent.RemoveAt(0);
    var sorted = recent.OrderBy(v => v).ToList();
    bpm = sorted[sorted.Count / 2];
    return true;
  }

  public void ClearHistory() { recent.Clear(); }
}

// Heart rate from several green channels (one per photodiode); reports the channel with the clearest pulse.
// Channels that recently saturated are skipped.
class MultiHeartRate {
  const int FullScale = 524287;
  readonly HeartRateEstimator[] ch;
  readonly string[] names;
  readonly int[] saturatedFor;
  int best;

  public MultiHeartRate(double sampleRate, params string[] channelNames) {
    names = channelNames;
    ch = channelNames.Select(n => new HeartRateEstimator(sampleRate)).ToArray();
    saturatedFor = new int[ch.Length];
  }

  public double SampleRate { get { return ch[0].SampleRate; } }
  public int Count { get { return ch[best].Count; } }
  public string BestChannel { get { return names[best]; } }
  public double[] Tail(int n) { return ch[best].Tail(n); }

  public void Add(int i, double raw) { Add(i, raw, Math.Abs(raw) >= FullScale - 1000); }

  // saturated: set when any ADC reading that makes up this sample hit full scale.
  public void Add(int i, double raw, bool saturated) {
    if (saturated) saturatedFor[i] = (int)(SampleRate * 10);
    else if (saturatedFor[i] > 0) saturatedFor[i]--;
    ch[i].Add(-raw); // invert so pulse peaks point up
  }

  public bool Estimate(out double bpm, out double quality) { return Estimate(out bpm, out quality, 8.0); }

  public bool Estimate(out double bpm, out double quality, double windowSeconds) {
    bpm = 0; quality = 0;
    bool any = false;
    for (int i = 0; i < ch.Length; i++) {
      if (saturatedFor[i] > 0) continue;
      double b, q;
      if (ch[i].Estimate(out b, out q, windowSeconds) && q > quality) { bpm = b; quality = q; best = i; any = true; }
    }
    return any;
  }
}
