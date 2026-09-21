# Where to put the PPG sensor around the ear — measured study

**Date:** 2026-09-21 · **Agent:** KILO · **Hardware:** MAX86171EVSYS# (see `max86171-evsys-bringup.md`)
**Subject:** Stefan, one person, one morning, right ear, sensor hand-held against the skin.
**Raw data:** `tools/max86171-hr-spo2/recordings/rec_EAR1..4_*`, `rec_TRG1..3_*`, `rec_SWEEP*`, `rec_QUIET_*`,
`rec_TALK_*` · **Scorer:** `tools/max86171-hr-spo2/bench/score.cs`

## Why this exists

The H1 carries the sensor inside the ear cushion, low down beside the ear. This study answers, with
measurements rather than opinion: which site, which optical settings, and what breaks it.

## Method `[measured]`

Each recording is 20 s at 25 Hz with eight simultaneous measurements, so every variant sees identical skin
contact. Per channel the scorer reports DC (% of the ADC full scale), pulse amplitude as % of DC (the
perfusion index), and an SNR = pulse amplitude ÷ median amplitude at nearby non-pulse, non-harmonic
frequencies. **That SNR is our own definition**, consistent across all runs here but not a published metric.

## 1 · Site survey — app settings, four sites

| Site | Best pulse | Best SNR | Channel agreement |
|---|---:|---:|---|
| **Earlobe / just below the ear** | **4.35 %** | 28 | all channels 73–74 BPM |
| Concha (bowl of the ear) | 1.94 % | **43** | good, 74 BPM |
| In front of the tragus | 2.76 % | 25 | all channels 78 BPM |
| Behind the ear (mastoid) | 0.52 % | 17 | **poor** — 88 vs 50 BPM |

Fingertip reference the day before: pulse 0.5–1.1 %, SNR 25–33. **The lobe beats a fingertip.** The concha is
the cleanest of all but unreachable by a cushion — note it for any in-ear variant.

## 2 · Optical settings do not matter; contact does `[measured]`

At the lobe, LED current swept while everything else stayed fixed:

| LED current | DC | Pulse | SNR |
|---|---:|---:|---:|
| 0x0A (present setting) | 32 % | 1.42 % | 12.0 |
| 0x05 | 16 % | 1.44 % | 12.1 |
| **0x02** | **6 %** | **1.46 %** | **12.3** |
| 0x0A, ADC range one step less sensitive | 34 % | 1.43 % | 12.0 |

**→ the LEDs can run at one fifth of current for identical signal quality** — less battery, less heat on skin.
Raising current instead (0x20 with all three green LEDs) **saturated** the detectors (60–87 % DC, ALC overflow
flag set, data dropped). Same conclusion as the fingertip work on 17 Sep: placement dominates settings.

Contact repeatability is the real variance: **the same site gave SNR 28 and then 12–19 on the next recording.**

## 3 · Stray light is the enemy `[measured]`

In every run the winning channel was one where **little** light returned (DC 3–10 % of full scale); channels at
35–88 % DC were the worst each time. That is light crossing from LED to detector over the surface instead of
through tissue. **Cushion design consequence:** an opaque barrier between emitters and detector, matte black
pocket, sealed against daylight.

## 4 · Tragus specifics `[measured]`

- Single LEDs at 0x0A are too dim there (DC 0–10 %, channels disagree 53 vs 75 BPM). The three-LED group works.
- Group vs single LED, two currents: all four variants landed on **68 BPM**, SNR 16–18. Detector pair 1/2 best.
- **Red/IR at the tragus are not usable:** pulse 0.04–0.2 % (green 0.8 % at the same moment), SNR 5–14, several
  channels locking onto 133 BPM = double the true rate. Higher current only saturated (88–95 % DC).
- **Red/IR at the lobe are far better:** SNR 21–24 while still. SpO2, if wanted, belongs at the lobe, not the tragus.

## 5 · Talking — the pilot problem `[measured]`

Same lobe site, still vs talking normally:

| | Best green SNR | Red/IR SNR | HR available | Readings wrong |
|---|---:|---:|---:|---:|
| Quiet | **31** | 21–24 | 10 s of 10 | 0 |
| Talking | **7.5** | 3.6–4.2 | 5 s of 10 | 0 |

**Talking costs availability, not accuracy**: the readings it still produces are right (66–70 vs 71–72 quiet),
but half the seconds produce nothing, and **red/IR collapse entirely**. So heart rate at the lobe survives
speech with gaps — acceptable with a short hold-and-smooth in software. Blood oxygen must be computed only in
quiet, still windows.

## 6 · Recommendation

1. **Primary site: the lower front quadrant of the cushion — lobe and just below the tragus.** Strongest pulse,
   good red/IR, no bone beneath.
2. **Second choice / redundancy: in front of the tragus.** Consistent, fed by a different artery.
3. **Avoid behind the ear (mastoid).** Weakest and least consistent.
4. Opaque LED/detector barrier, matte black sealed pocket, light contact, LEDs at ~1/5 current.
5. Software: score every channel separately and pick per second (landed `48b3c31`); hold the last good value
   through speech gaps; gate SpO2 on quiet windows.

## 7 · What this does NOT establish

- **One person, one ear, one morning, hand-held.** Ear anatomy varies; a cushion applies different pressure than
  fingers. Repeat on 3–5 heads with the real cushion before cutting tooling.
- **No SpO2 accuracy claim anywhere.** The R→SpO2 curve is Maxim's, for a different sensor, uncalibrated here.
- **Chewing, head movement, walking, cold hands/ears, skin tone and hair were not tested.** Only speech was.
- The front-of-tragus site sits over the jaw joint and was **not** speech-tested; it may fare worse than the lobe.
- Long-term wear, sweat and sensor heating were not tested.

---

## Appendix — calibrating SpO2, for later (2026-09-21)

Parked on Stefan's call; recorded so it need not be re-derived. The R -> SpO2 curve in the tester is Maxim's,
for a different sensor: every reading is uncalibrated and the app says so.

- **Route A, reference comparison** `[assumed cost: days]` — our sensor and a certified fingertip oximeter on
  the same person at once, many people, fit our R to the reference. **Limit: healthy people at sea level sit at
  96-100 %**, so the fit is anchored only there and says nothing about 85 %; the reference is itself +-2 %.
- **Route B, controlled desaturation study** — a lab lowers inspired oxygen in stages with arterial blood drawn
  and measured by CO-oximetry. FDA's 2025 draft guidance recommends **>= 24 participants across the full Monk
  skin-tone scale, 480 paired data points, ARMS < 3 %** `[fetched]` (fda.gov/media/175828/download; Healio,
  13 Jan 2025). Specialist labs, months, real money. Required for any clinical or safety claim.
- **Middle option:** a hypobaric chamber / altitude training facility gives genuine low-saturation points under
  supervision, and tests the cold-ear case. Not arterial ground truth, but far better than resting-healthy data.
- **The H1-specific risk:** hypoxia awareness matters at 80-90 %, exactly the range Route A cannot validate, and
  altitude brings cold ears and weak perfusion - our worst measured condition. A sensor fitted only on warm
  resting subjects could read normal on a pilot who is going hypoxic.
- **Build when resumed:** a reference-reading field in the session recorder (pairs collected automatically), a
  fitting tool, and curve coefficients in a settings file rather than compiled in.
