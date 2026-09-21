// Heart rate and SpO2 from the MAX86171's PPG stream.
// Direct port of tools/max86171-hr-spo2/{HeartRate,SpO2,PpgStream}.cs - keep the two in step.

// 2nd-order IIR section (RBJ cookbook).
export class Biquad {
  constructor(fs, fc, high) {
    const w = 2 * Math.PI * fc / fs, cos = Math.cos(w), alpha = Math.sin(w) / (2 * Math.sqrt(0.5));
    const a0 = 1 + alpha;
    if (high) { this.b0 = (1 + cos) / 2; this.b1 = -(1 + cos); this.b2 = (1 + cos) / 2; }
    else { this.b0 = (1 - cos) / 2; this.b1 = 1 - cos; this.b2 = (1 - cos) / 2; }
    this.b0 /= a0; this.b1 /= a0; this.b2 /= a0;
    this.a1 = -2 * cos / a0; this.a2 = (1 - alpha) / a0;
    this.z1 = 0; this.z2 = 0;
  }
  step(x) {
    const y = this.b0 * x + this.z1;
    this.z1 = this.b1 * x - this.a1 * y + this.z2;
    this.z2 = this.b2 * x - this.a2 * y;
    return y;
  }
}

// Band-pass 0.5-4 Hz, then autocorrelation over an 8 s window (5 s for the first reading).
export class HeartRateEstimator {
  constructor(fs) {
    this.fs = fs;
    this.hp1 = new Biquad(fs, 0.5, true); this.hp2 = new Biquad(fs, 0.5, true);
    const fc = Math.min(4.0, fs * 0.45);
    this.lp1 = new Biquad(fs, fc, false); this.lp2 = new Biquad(fs, fc, false);
    this.settle = Math.round(fs * 2);
    this.filtered = [];
    this.recent = [];
  }
  get count() { return this.filtered.length; }
  add(raw) {
    const y = this.lp2.step(this.lp1.step(this.hp2.step(this.hp1.step(raw))));
    if (this.settle > 0) { this.settle--; return 0; }
    this.filtered.push(y);
    const keep = Math.round(this.fs * 12);
    if (this.filtered.length > keep * 2) this.filtered.splice(0, this.filtered.length - keep);
    return y;
  }
  tail(n) { n = Math.min(n, this.filtered.length); return this.filtered.slice(this.filtered.length - n); }

  // Returns {bpm, quality} or null.
  estimate(windowSeconds = 8) {
    const n = Math.round(this.fs * windowSeconds);
    if (this.filtered.length < n) return null;
    const x = this.tail(n);
    let mean = 0;
    for (const v of x) mean += v;
    mean /= n;
    for (let i = 0; i < n; i++) x[i] -= mean;

    const minBpm = 35, maxBpm = 220;
    const minLag = Math.max(2, Math.floor(this.fs * 60 / maxBpm));
    const maxLag = Math.min(Math.floor(n / 2), Math.ceil(this.fs * 60 / minBpm));
    const r = new Float64Array(maxLag + 2);
    for (let lag = minLag - 1; lag <= maxLag + 1 && lag < n; lag++) {
      let sxy = 0, sxx = 0, syy = 0;
      for (let i = 0; i + lag < n; i++) { sxy += x[i] * x[i + lag]; sxx += x[i] * x[i]; syy += x[i + lag] * x[i + lag]; }
      r[lag] = (sxx > 0 && syy > 0) ? sxy / Math.sqrt(sxx * syy) : 0;
    }
    let best = 0;
    for (let lag = minLag; lag <= maxLag; lag++) best = Math.max(best, r[lag]);
    if (best < 0.3) return null;

    // Shortest-lag local peak near the global max, so harmonics are not chosen.
    let pick = -1;
    for (let lag = minLag; lag <= maxLag; lag++) {
      if (r[lag] >= r[lag - 1] && r[lag] >= r[lag + 1] && r[lag] >= 0.85 * best) { pick = lag; break; }
    }
    if (pick < 0) return null;

    const y0 = r[pick - 1], y1 = r[pick], y2 = r[pick + 1];
    const denom = y0 - 2 * y1 + y2;
    const offset = Math.abs(denom) > 1e-12 ? 0.5 * (y0 - y2) / denom : 0;
    const period = pick + Math.max(-0.5, Math.min(0.5, offset));
    const hr = 60.0 * this.fs / period;
    if (y1 < 0.5 || hr < minBpm || hr > maxBpm) return null;

    this.recent.push(hr);
    if (this.recent.length > 5) this.recent.shift();
    const sorted = [...this.recent].sort((a, b) => a - b);
    return { bpm: sorted[Math.floor(sorted.length / 2)], quality: y1 };
  }
}

const FULL_SCALE = 524287;

// One estimator per green channel; the reading comes from the channel with the clearest pulse.
export class MultiHeartRate {
  constructor(fs, names) {
    this.names = names;
    this.ch = names.map(() => new HeartRateEstimator(fs));
    this.saturatedFor = names.map(() => 0);
    this.best = 0;
    this.fs = fs;
  }
  get bestChannel() { return this.names[this.best]; }
  get count() { return this.ch[this.best].count; }
  tail(n) { return this.ch[this.best].tail(n); }
  add(i, raw, saturated) {
    if (saturated === undefined) saturated = Math.abs(raw) >= FULL_SCALE - 1000;
    if (saturated) this.saturatedFor[i] = Math.round(this.fs * 10);
    else if (this.saturatedFor[i] > 0) this.saturatedFor[i]--;
    this.ch[i].add(-raw); // invert so pulse peaks point up
  }
  estimate(windowSeconds = 8) {
    let out = null;
    for (let i = 0; i < this.ch.length; i++) {
      if (this.saturatedFor[i] > 0) continue;
      const r = this.ch[i].estimate(windowSeconds);
      if (r && (!out || r.quality > out.quality)) { out = r; this.best = i; }
    }
    return out;
  }
}

// Blood oxygen from the ratio of ratios, amplitudes measured at the pulse frequency only.
export class SpO2Estimator {
  constructor(fs) { this.fs = fs; this.red = []; this.ir = []; this.recent = []; }
  add(red, ir) {
    this.red.push(red); this.ir.push(ir);
    const keep = Math.round(this.fs * 20);
    if (this.red.length > keep * 2) { this.red.splice(0, this.red.length - keep); this.ir.splice(0, this.ir.length - keep); }
  }
  static curve(r) { return Math.max(70, Math.min(100, -45.060 * r * r + 30.354 * r + 94.845)); }

  tone(v, f) {
    const n = v.length;
    let sx = 0, sy = 0, sxx = 0, sxy = 0;
    for (let i = 0; i < n; i++) { sx += i; sy += v[i]; sxx += i * i; sxy += i * v[i]; }
    const slope = (n * sxy - sx * sy) / (n * sxx - sx * sx), icpt = (sy - slope * sx) / n;
    let re = 0, im = 0, wsum = 0;
    for (let i = 0; i < n; i++) {
      const w = 0.5 - 0.5 * Math.cos(2 * Math.PI * i / (n - 1));
      const d = (v[i] - (icpt + slope * i)) * w;
      const ph = 2 * Math.PI * f * i / this.fs;
      re += d * Math.cos(ph); im += d * Math.sin(ph); wsum += w;
    }
    return [2 * re / wsum, 2 * im / wsum];
  }
  static mag(c) { return Math.sqrt(c[0] * c[0] + c[1] * c[1]); }

  noiseFloor(v, pulseF) {
    const mags = [];
    for (let f = 0.6; f <= Math.min(4.0, this.fs / 2 - 0.3); f += 0.1) {
      if (Math.abs(f - pulseF) < 0.15 || Math.abs(f - 2 * pulseF) < 0.15 || Math.abs(f - 3 * pulseF) < 0.15) continue;
      mags.push(SpO2Estimator.mag(this.tone(v, f)));
    }
    if (!mags.length) return 1e-9;
    mags.sort((a, b) => a - b);
    return Math.max(mags[Math.floor(mags.length / 2)], 1e-9);
  }

  estimate(bpm) {
    const res = { spo2: 0, spread: 0, r: 0, perfusionIr: 0, phaseDeg: 0, snr: 0, problem: null, path: null };
    const n = Math.round(this.fs * 16);
    if (this.ir.length < n) { res.problem = "collecting data"; return res; }
    const r = this.red.slice(this.red.length - n);
    const x = this.ir.slice(this.ir.length - n);
    const avg = a => a.reduce((s, v) => s + v, 0) / a.length;
    const dcR = avg(r), dcI = avg(x);
    if (Math.max(...r) >= FULL_SCALE - 1000 || Math.max(...x) >= FULL_SCALE - 1000) { res.problem = "sensor saturated - press more lightly"; return res; }
    if (dcR < FULL_SCALE * 0.02 || dcI < FULL_SCALE * 0.02) { res.problem = "too little light - cover the sensor fully"; return res; }

    const f0 = bpm / 60.0;
    let bestF = f0, bestMag = -1;
    for (let f = f0 - 0.08; f <= f0 + 0.0801; f += 0.005) {
      const m = SpO2Estimator.mag(this.tone(x, f));
      if (m > bestMag) { bestMag = m; bestF = f; }
    }
    const cI = this.tone(x, bestF), cR = this.tone(r, bestF);
    const acI = SpO2Estimator.mag(cI), acR = SpO2Estimator.mag(cR);
    res.perfusionIr = 100 * acI / dcI;
    res.r = (acR / dcR) / (acI / dcI);
    res.snr = Math.min(acI / this.noiseFloor(x, bestF), acR / this.noiseFloor(r, bestF));
    if (res.snr < 5) { this.recent = []; res.problem = "red/IR pulse buried in noise (SNR " + res.snr.toFixed(1) + ")"; return res; }

    const phase = Math.atan2(cR[1] * cI[0] - cR[0] * cI[1], cR[0] * cI[0] + cR[1] * cI[1]) * 180 / Math.PI;
    res.phaseDeg = phase;
    if (Math.abs(phase) > 25) { this.recent = []; res.problem = "red/IR pulses out of step - keep still"; return res; }
    if (res.perfusionIr < 0.08) { this.recent = []; res.problem = "red/IR pulse too weak - keep still and warm"; return res; }
    if (res.r < 0.2 || res.r > 2.0) { this.recent = []; res.problem = "ratio out of range (R=" + res.r.toFixed(2) + ")"; return res; }

    this.recent.push(SpO2Estimator.curve(res.r));
    if (this.recent.length > 10) this.recent.shift();
    const sorted = [...this.recent].sort((a, b) => a - b);
    res.spo2 = sorted[Math.floor(sorted.length / 2)];
    const trim = sorted.length >= 5 ? 1 : 0;
    res.spread = (sorted[sorted.length - 1 - trim] - sorted[trim]) / 2;
    if (this.recent.length < 5) res.problem = "checking stability (" + this.recent.length + "/5)";
    else if (res.spread > 2) res.problem = "unstable, readings vary +/-" + res.spread.toFixed(0) + "% - keep still";
    return res;
  }
}

// One red/IR path per photodiode; the reading comes from the cleanest valid path.
export class MultiPathOximeter {
  constructor(fs, names) { this.names = names; this.paths = names.map(() => new SpO2Estimator(fs)); }
  add(i, red, ir) { this.paths[i].add(red, ir); }
  estimate(bpm) {
    const results = this.paths.map((p, i) => { const r = p.estimate(bpm); r.path = this.names[i]; return r; });
    const valid = results.filter(r => !r.problem).sort((a, b) => b.snr - a.snr);
    if (valid.length) return valid[0];
    return results.sort((a, b) => b.snr - a.snr)[0];
  }
}
