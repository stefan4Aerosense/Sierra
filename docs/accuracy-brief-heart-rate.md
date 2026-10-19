# Brief: making the heart-rate and SpO2 readings more accurate

**For an engineer or agent joining to improve accuracy. Written 19 October 2026 by KILO.**
Companion to [`handover-heart-rate-2026-09-30.md`](handover-heart-rate-2026-09-30.md), which covers what the
system *is*. This one covers what is **known about its accuracy**, what is **unexplained**, and the specific
experiments that would settle each question.

Tags: **[measured]** = observed here, with the date · **[fetched]** = from the source named · **[assumed]** =
believed, unverified. Nothing in this project is accepted as working on reasoning alone.

---

## 1 · Start here: the three questions worth your time

1. **An unexplained 48–50 BPM reading at the ear** that the app reported with high confidence (§4.1). Open
   since 21 Sep. One reference measurement settles it. **Do this first — it is the only known case of the
   system being confidently wrong.**
2. **SpO2 is uncalibrated on both sensors** and cannot be fixed with what is on the bench (§4.2).
3. **Two sensors have never been run simultaneously on one person**, although the app supports it and reports
   their agreement automatically (§5.1). It is the cheapest unexploited check we have.

Everything else below is context for those.

---

## 2 · What accuracy we can actually evidence

| Claim | Evidence | Date |
|---|---|---|
| Our heart-rate maths agrees with an independent commercial algorithm | **76 vs 76.5 BPM** on identical raw data — ours vs the MAX32664 hub's own output; tracked together 66→85 BPM as the subject's rate rose | `[measured 29 Sep]` |
| Our maths is not sensor-specific | The same code produced those numbers on **two different vendors' optics** (ADI MAX86171, Maxim MAX30101) | `[measured 29 Sep]` |
| Channel selection matters more than filtering | Scoring 8 channels separately instead of summing them: ear recordings went from 5 readings (all wrong, sub-50) to 10 readings, none wrong; a weak finger recording from 35 readings with 4 wrong to 51 with 0 wrong | `[measured 21 Sep]` |
| The ear is a viable site | Lobe 4.35 % pulse amplitude — **better than a fingertip** (0.5–1.1 %) | `[measured 21 Sep]` |
| Optical settings are not the lever | LED current across a 5× range changed SNR **not at all**; the same site measured twice gave SNR 28 then 12 | `[measured 21 Sep]` |

**What none of this establishes:** absolute accuracy against a reference. Two algorithms agreeing proves
*parity*, not *correctness*. No reading in this project has ever been compared to a validated device.

---

## 3 · Exact algorithm, as implemented

So you can criticise the real thing rather than a description of it. Source: `HeartRate.cs`, `SpO2.cs`.

### Heart rate
- Band-pass: **two 2nd-order high-pass sections at 0.5 Hz**, two low-pass at **min(4 Hz, 0.45·fs)**; first
  **2 s of samples discarded** while the filters settle. `HeartRateEstimator.HighPassHz` is a field, so a
  sweep needs no code change (`bench/filttest.cs` does it).
- **Autocorrelation over an 8 s window** (5 s until the first reading appears; 4 s was tested and **rejected**
  — it produced wrong sub-50 values `[measured 21 Sep]`).
- Lag search spans **35–220 BPM**. Rejected if the best correlation is **< 0.3**.
- Then the **shortest-lag local peak that reaches 0.85 × the global maximum** is chosen — this is the
  anti-harmonic rule, and the prime suspect in §4.1.
- Parabolic interpolation around that peak; rejected if the peak's own correlation is **< 0.5**.
- Output is the **median of the last 5 accepted estimates**. Buffer holds 12 s.
- Multi-channel: one estimator **per channel**, the highest-quality channel wins. Never summed (§2).

### Blood oxygen
- 16 s window, 20 s buffer. Pulse frequency found by searching **±0.08 Hz around the heart rate**, in 0.005 Hz
  steps, maximising the IR amplitude; red and IR amplitudes then measured **at that one frequency**
  (Hann-windowed, linear trend removed).
- `R = (AC_red/DC_red)/(AC_ir/DC_ir)`, then `SpO2 = −45.060R² + 30.354R + 94.845` — **Maxim's curve for a
  different sensor**.
- Refused when: SNR **< 5** · red/IR phase differ by **> 25°** · IR perfusion **< 0.08 %** · R outside
  **0.3–1.2** · the curve output lands on its **70/100 clamp** (added after a clamped "70 %" displayed as a
  rock-steady reading `[measured 21 Sep]`).
- Reported value is the **median of up to 10** recent values; flagged unstable if the trimmed spread > 2 %.
- SNR is defined **by us**: pulse amplitude ÷ median amplitude at 0.6–4 Hz excluding the pulse and its first
  two harmonics. Consistent across our work, not a standard metric.

### Display timing (`App.cs`)
Numbers update **once a second**; a lost pulse holds the previous value for **8 s**, dimmed and labelled with
its age, then blanks.

---

## 4 · The open accuracy problems

### 4.1 The 48–50 BPM ear reading — **unexplained, highest priority**

`[measured 21 Sep]` At the ear, the app reported **48–50 BPM with quality 0.83–0.95** — i.e. very confident —
while the subject felt normal and had measured 62–78 BPM repeatedly that morning.

What was established:
- **The spectrum of the channel it used** (`bench/spec.cs` on `recordings/rec_LIVE_M1_PD1.txt`): peaks at
  **0.66 Hz (40 BPM, strongest), 0.84 (50), 0.57 (34)**, and a weak **1.70 Hz (102)**. A real pulse does not
  produce unrelated peaks at 34, 40 and 50.
- **Raising the band-pass low edge to 0.7, 0.8 and 0.9 Hz did not change the result** (`bench/filttest.cs`),
  so it is not simple drift leaking through.
- Two readings fit the data: a **true ~102 BPM with a half-rate lock**, or a **genuine ~50**.

**What would settle it:** one simultaneous reference — a chest strap, a watch, or a counted pulse — recorded
alongside. Ten minutes of work. Until then do not change the heart-rate maths: an earlier attempt to fix a
symptom without a reference made things worse, and the real cause turned out to be elsewhere.

**If it is a half-rate lock**, the suspect is the 0.85-of-maximum peak rule in §3: at 102 BPM the 50 BPM lag
is a true autocorrelation peak, and if it reaches 0.85 of the maximum it wins because it is reached first.
A harmonic-sum spectral method was written and tested (`bench/hreval.cs`, in git at `c2ce134`) and **withdrawn
on the user's instruction**; it is available if the evidence ends up supporting it.

### 4.2 SpO2 is uncalibrated, and the two sensors cannot fix each other

Both run the same generic Maxim curve, fitted for neither board, so they agree by construction. Anchoring
needs an independently validated device. Routes and their limits are in
[`h1-ear-ppg-placement.md`](h1-ear-ppg-placement.md) §Appendix. Short version: a certified fingertip oximeter
(~R300–600) fixes the **96–100 %** range only; the **80–90 %** range that matters for aviation needs a
controlled desaturation study. A competitor has done exactly that study — see
[`spydr-air-competitor-notes.md`](spydr-air-competitor-notes.md) — and published **2.61 % RMS over 70–100 %**,
with desaturation detected **up to 50 s before finger and forehead sensors**.

### 4.3 Conditions known to break readings

| Condition | Effect | Evidence |
|---|---|---|
| **Talking** | Heart-rate availability halves (10 s → 5 s of 10); **red/IR collapse** (SNR 21–24 → 4). Readings that do appear stay correct | `[measured 21 Sep]` |
| **Movement** | Readings drop out; the display holds for 8 s | `[measured]` |
| **Pressing harder** | Worse, not better — occludes the capillaries being measured | `[measured]` |
| **Behind-the-ear placement** | Worst of four sites: 0.5 % pulse, channels disagreeing | `[measured 21 Sep]` |
| **Cold** | Untested here, expected to reduce perfusion. **Relevant: altitude means cold ears** | `[assumed]` |

### 4.4 ⚠ A sample-rate hazard that is not yet a bug

At 25 samples/s, helicopter rotor harmonics **alias into the pulse band and look like a clean heartbeat** — a
27.5 Hz tail rotor folds to exactly 150 BPM `[fetched: fleet research, 18 Aug]`. **The Bluetooth sensor is
configured at 25 samples/s**; the USB one runs at 100. Harmless on a bench, dangerous in an aircraft. Anyone
working on accuracy should treat raising the Bluetooth rate as a design question, not a tuning knob.

---

## 5 · Experiments worth running, in order

Each has a stated acceptance criterion. Record everything; the app does this for you.

1. **Reference check for §4.1.** Both sensors plus a reference (watch/strap/counted pulse) on one person, 3 × 60 s,
   at rest and after light exercise. **Pass: within ±5 BPM of the reference on all runs.** This also
   establishes the first real accuracy number this project has.
2. **Both sensors simultaneously** — ear and finger — recorded in one session. The PDF reports paired
   seconds, mean difference, typical and worst disagreement, and % within 5 BPM. **Pass: mean difference
   under 2 BPM with no systematic drift.**
3. **Cushion-mounted test.** Every placement measurement so far was **hand-held**. A competitor's validated
   product uses the site we measured as worst, which suggests clamping pressure and optical sealing change the
   answer. Mount the sensor in a cushion with representative pressure and repeat the four-site study.
4. **Multiple subjects.** Everything here rests on **one person's ears**. Three to five, varied skin tone,
   before any geometry is committed.
5. **Then, and only then, tune the algorithm.** With a reference in hand, sweep: window length
   (`bench/wintest.cs`), band-pass edge (`bench/filttest.cs`), the 0.85 peak rule, autocorrelation vs the
   spectral method (`bench/hreval.cs`). Without a reference, tuning is guessing.

---

## 6 · What you have to work with

**Data.** `tools/max86171-hr-spo2/recordings/` — raw captures behind every claim above. One value per line,
one file per channel, named `rec_<session>_M<meas>_PD<detector>.txt`. Sessions: `EAR1`–`EAR4` (the four-site
study, 25 Hz), `QUIET`/`TALK` (the speech test), `TRG1`–`TRG3` (tragus settings sweeps), `SWEEP`/`SWEEP2` (LED
current), `XIAO` (the USB sensor, 100 Hz, with `xiao_finger_2026-09-29.ndjson` as the full device stream),
`rec_LIVE_M1_PD1.txt` (the unexplained 48–50 BPM capture), plus earlier finger sessions `A`–`C`, `F`.

**Tools** (`bench/`, each builds standalone with the C# compiler already in Windows):
`score.cs` ranks channels by pulse strength and SNR · `spec.cs` prints the strongest frequencies — this is how
a half-rate or drift lock is spotted · `filttest.cs` sweeps the filter edge · `wintest.cs` sweeps window
length · `hreval.cs` compares heart-rate methods · `spo2test.cs` runs the oxygen maths over a recorded red/IR
pair · `rec.cs` records raw channels under any sensor configuration · `pdfshot.cs` renders a PDF to images.

**Live modes:** `hrdiag.exe --diag <s>` (Bluetooth), `--xiao <s>` (USB, prints our reading beside the device's
own), `--selftest` (offline maths tests — run after any change).

**Hardware constraints to respect:** never write the MAX86171's OTP registers (≥ 0xD0); never touch the
board's firmware-update BLE service (`00001530-…`); the Bluetooth board accepts **one connection at a time**,
so a phone holding it will look like a dead board.

---

## 7 · The rules this project runs on

- **Every claim ships its evidence.** A measured number with a date, a file and page, or an explicit
  `[assumed]`. "Cannot measure" is recorded as unknown, never rounded up to working.
- **A display never shows a stale or invalid value as live.** Held readings are dimmed and aged; invalid ones
  blank with a reason. A clamped or saturated value must never be rendered as a reading — it looks steadier
  than a real one.
- **Verify at the point of use.** Render the PDF and look at it; replay the estimator over a real recording;
  read the firmware version back after flashing. A compile proves nothing.
- **Averaging a weak channel into a strong one destroys the strong one.** Score separately, pick the best.
- **Don't change the maths to chase a symptom without a reference.** That mistake has already been made here
  once, and the real cause was elsewhere.
