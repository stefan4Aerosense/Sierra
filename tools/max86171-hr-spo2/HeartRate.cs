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
  public bool Estimate(out double bpm, out double quality) {
    bpm = 0; quality = 0;
    int n = (int)(fs * 8);
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

// Heart rate from several channels at once, in the frequency domain, built for weak pulses.
// Per channel: 10 s window, linear trend removed, Hann window, amplitude spectrum normalised by its own median
// (so each channel is in "times the noise floor"). A heartbeat has energy at f AND 2f, slow wander does not, so
// the score is S(f) + 0.5 S(2f), summed over channels, searched only over 45-210 BPM. Motivated by 2026-09-18:
// with a weak finger contact the autocorrelation method locked onto slow wander and read 39-42 BPM.
class SpectralHeartRate {
  // Scores are in whitened "times the local noise" units: noise alone peaks ~2-3, real pulses 3.3-11 (measured on
  // recordings B, C, F, OLD). >= MinScore starts a reading; >= KeepScore continues one within KeepBpm of it.
  public const double MinHz = 0.75, MaxHz = 3.5, MinScore = 4.0, KeepScore = 3.0, KeepBpm = 8;
  int missStreak;
  readonly double fs;
  readonly List<double>[] buf;
  readonly List<bool>[] sat;
  readonly List<double> recent = new List<double>();
  public int BestChannel { get; private set; }

  public SpectralHeartRate(double sampleRate, int channels) {
    fs = sampleRate;
    buf = Enumerable.Range(0, channels).Select(i => new List<double>()).ToArray();
    sat = Enumerable.Range(0, channels).Select(i => new List<bool>()).ToArray();
  }

  int Window { get { return (int)(fs * 10); } }

  public void Add(int k, double raw) { Add(k, raw, false); }
  public void Add(int k, double raw, bool saturated) {
    buf[k].Add(raw); sat[k].Add(saturated);
    if (buf[k].Count > Window * 2) { int d = buf[k].Count - Window; buf[k].RemoveRange(0, d); sat[k].RemoveRange(0, d); }
  }

  // Amplitude spectrum on a 0.01 Hz grid from 0.5 Hz to 2*MaxHz, normalised by its median.
  double[] Spectrum(double[] v, out double f0) {
    f0 = 0.5; int n = v.Length;
    double sx = 0, sy = 0, sxx = 0, sxy = 0;
    for (int i = 0; i < n; i++) { sx += i; sy += v[i]; sxx += (double)i * i; sxy += i * v[i]; }
    double slope = (n * sxy - sx * sy) / (n * sxx - sx * sx), icpt = (sy - slope * sx) / n;
    var d = new double[n];
    for (int i = 0; i < n; i++) d[i] = (v[i] - (icpt + slope * i)) * (0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (n - 1)));
    int m = (int)Math.Round((2 * MaxHz - f0) / 0.01) + 1;
    var s = new double[m];
    for (int j = 0; j < m; j++) {
      double w = 2 * Math.PI * (f0 + 0.01 * j) / fs, re = 0, im = 0;
      for (int i = 0; i < n; i++) { re += d[i] * Math.Cos(w * i); im += d[i] * Math.Sin(w * i); }
      s[j] = Math.Sqrt(re * re + im * im);
    }
    // Local whitening: each bin relative to the median of its +/-0.4 Hz neighbourhood, so the rising floor of
    // slow wander at the low end cannot outscore a real pulse.
    var outp = new double[m];
    for (int j = 0; j < m; j++) {
      int a = Math.Max(0, j - 40), b = Math.Min(m - 1, j + 40);
      var nb = new double[b - a + 1]; Array.Copy(s, a, nb, 0, nb.Length); Array.Sort(nb);
      outp[j] = s[j] / Math.Max(nb[nb.Length / 2], 1e-12);
    }
    return outp;
  }

  // score = mean over usable channels of the harmonic-sum at the peak, in "times the noise floor".
  public bool Estimate(out double bpm, out double score) {
    bpm = 0; score = 0;
    int n = Window;
    var specs = new List<double[]>(); var ids = new List<int>(); double f0 = 0.5;
    for (int k = 0; k < buf.Length; k++) {
      if (buf[k].Count < n) continue;
      if (sat[k].Skip(sat[k].Count - n).Any(x => x)) continue;
      specs.Add(Spectrum(buf[k].GetRange(buf[k].Count - n, n).ToArray(), out f0)); ids.Add(k);
    }
    if (specs.Count == 0) return false;
    int lo = (int)Math.Round((MinHz - f0) / 0.01), hi = (int)Math.Round((MaxHz - f0) / 0.01);
    Func<double[], int, double> h = (s, j) => { int j2 = (int)Math.Round((2 * (f0 + 0.01 * j) - f0) / 0.01); return s[j] + (j2 < s.Length ? 0.5 * s[j2] : 0); };
    int bestJ = lo; double bestH = -1;
    for (int j = lo; j <= hi; j++) { double t = specs.Sum(s => h(s, j)); if (t > bestH) { bestH = t; bestJ = j; } }
    score = bestH / specs.Count;
    int bc = 0; double bs = -1;
    for (int c = 0; c < specs.Count; c++) if (specs[c][bestJ] > bs) { bs = specs[c][bestJ]; bc = ids[c]; }
    BestChannel = bc;
    double median = recent.Count > 0 ? recent.OrderBy(x => x).ToList()[recent.Count / 2] : 0;
    double cand = 60 * (f0 + 0.01 * bestJ);
    bool keep = score >= KeepScore && recent.Count > 0 && Math.Abs(cand - median) <= KeepBpm;
    if (score < MinScore && !keep) { if (++missStreak >= 10) recent.Clear(); return false; }
    missStreak = 0;
    // Parabolic refinement between grid points.
    double y0 = specs.Sum(s => h(s, Math.Max(lo, bestJ - 1))), y2 = specs.Sum(s => h(s, Math.Min(hi, bestJ + 1)));
    double den = y0 - 2 * bestH + y2, off = Math.Abs(den) > 1e-9 ? Math.Max(-0.5, Math.Min(0.5, 0.5 * (y0 - y2) / den)) : 0;
    double hr = 60 * (f0 + 0.01 * (bestJ + off));
    recent.Add(hr);
    if (recent.Count > 5) recent.RemoveAt(0);
    bpm = recent.OrderBy(x => x).ToList()[recent.Count / 2];
    return true;
  }
}

// Heart rate from several green channels (one per photodiode); reports the channel with the clearest pulse.
// Channels that recently saturated are skipped.
class MultiHeartRate {
  const int FullScale = 524287;
  readonly HeartRateEstimator[] ch;
  readonly string[] names;
  readonly int[] saturatedFor;
  readonly double[] dc; // running light level per channel
  readonly SpectralHeartRate spectral; // the heart-rate method (robust to weak pulses)
  int best;

  public MultiHeartRate(double sampleRate, params string[] channelNames) {
    names = channelNames;
    ch = channelNames.Select(n => new HeartRateEstimator(sampleRate)).ToArray();
    saturatedFor = new int[ch.Length];
    dc = new double[ch.Length];
    spectral = new SpectralHeartRate(sampleRate, ch.Length);
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
    dc[i] = dc[i] == 0 ? raw : dc[i] + (raw - dc[i]) * (1.0 / SampleRate); // ~1 s average light level
    ch[i].Add(-raw); // invert so pulse peaks point up
    spectral.Add(i, raw, saturated);
  }


  public enum Placement { NoFinger, Saturated, Moving, Weak, Fair, Good, Settling }

  // Pulse strength for placement feedback: amplitude of the band-passed green signal as % of the light level
  // (the perfusion index), best unsaturated channel over the last 6 s. Measured on this board: a light warm
  // finger gave ~2 %; a pressed or cold one 0.01-0.1 %.
  public double PerfusionIndex(out Placement p) {
    p = Placement.Settling;
    double best = -1; bool anyLight = false, allSat = true;
    int n = (int)(SampleRate * 6);
    for (int i = 0; i < ch.Length; i++) {
      if (dc[i] > FullScale * 0.02) anyLight = true;
      if (saturatedFor[i] > 0) continue;
      allSat = false;
      if (dc[i] <= FullScale * 0.02 || ch[i].Count < n) continue;
      var t = ch[i].Tail(n);
      double m = t.Average(), rms = Math.Sqrt(t.Select(v => (v - m) * (v - m)).Average());
      best = Math.Max(best, 100 * rms * Math.Sqrt(2) / dc[i]);
    }
    if (allSat) { p = Placement.Saturated; return 0; }
    if (!anyLight) { p = Placement.NoFinger; return 0; }
    if (best < 0) return 0;
    p = best > 6 ? Placement.Moving : best < 0.3 ? Placement.Weak : best < 1 ? Placement.Fair : Placement.Good;
    return best;
  }

  // Heart rate from all channels together (SpectralHeartRate). quality = spectral score / 10, capped at 1.
  // The per-channel autocorrelation estimators are kept only for the waveform and signal-strength display.
  public bool Estimate(out double bpm, out double quality) {
    double score;
    bool ok = spectral.Estimate(out bpm, out score);
    quality = Math.Min(1, score / 10);
    best = spectral.BestChannel;
    return ok;
  }
}
