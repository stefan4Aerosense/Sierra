# HANDOVER — heart-rate and blood-oxygen sensing, and the tester app

**Written 30 September 2026 by KILO (Stefan's agent) for whoever picks this up next.**
Read this file top to bottom once. It is self-contained: what the hardware is, what the software does, what
has actually been measured, what has *not*, and what to do next.

Every factual line is tagged: **[measured]** = observed on this hardware, with the date · **[fetched]** = read
from the source named · **[assumed]** = believed but not verified. **Treat every [assumed] as an open
question.** Where a number has no tag, it is a design choice, not a finding.

---

## 1 · What exists, in one paragraph

Two different optical heart-rate sensors and one Windows app that reads either of them, plus a web page that
reads the first one from an Android phone. The app shows heart rate, blood oxygen and the live pulse waveform,
holds readings through movement, records named sessions, and writes a PDF report and a CSV per session. It can
run **both sensors at once** and report how far apart they are. All of it was built from scratch: the vendor
software for the first sensor has no heart-rate output at all.

---

## 2 · The two sensors

| | **Sensor A** | **Sensor B** |
|---|---|---|
| Name in the app | Bluetooth · MAX86171 | USB · XIAO |
| Hardware | Analog Devices **MAX86171EVSYS#** (MAXSensorBLE# data board + MAX86171_OSB# sensor board) | **Seeed XIAO RP2040** + **SparkFun SEN-15219** (MAX30101 optics + MAX32664 hub) |
| Link to the PC | **Bluetooth Low Energy only.** USB-C is power and firmware update; no sensor data crosses it `[measured]` | **USB serial**, 115200 baud, JSON one object per line `[measured]` |
| Light sources | red 660 nm, IR 880 nm, green 530/536 nm — **all usable** | red, IR usable; **green exists but cannot be read** `[measured by HOTEL]` |
| Photodiodes | **four**, independently selectable | one |
| On-board rate/SpO2 | none — the chip streams raw light only | **yes**, its MAX32664 hub computes HR, SpO2, confidence, finger state |
| Sample rate as configured | 25 /s | 100 /s with a finger, 25 /s without |
| Who built the firmware | vendor (protocol reverse-engineered here) | **HOTEL**, 28–29 Sep, `xiao_hr_usb 0.3.1` |

**Sensor A's protocol was reverse-engineered** from the vendor's .NET assemblies, because Analog Devices'
own GUI shows raw waveforms and **no heart rate** `[measured]`. Details in
[`max86171-evsys-bringup.md`](max86171-evsys-bringup.md): BLE service `6e400000-…`, config characteristic
`…0002`, notify characteristic `…0001`, and the register sequence that configures eight simultaneous
measurements across four photodiodes. **Never touch the `00001530-…` service — that is firmware update.**

**Sensor B** is documented in HOTEL's own handover, shipped with the package at
`D:\AeroSense\Heart Rate Sensors\xiao_hr_usb_KILO_package_2026-09-29\`. Its unit on this bench has serial
`4250304B38353817` — **the same unit HOTEL tested**, so their wiring history applies to it, including a note
that the I²C joints may not be soldered.

---

## 3 · Status — read this before trusting any number

### Working, measured

- **Heart rate on both sensors.** On 29 Sep, on the same person minutes apart: sensor B's own hub read 76.5 BPM
  and our maths read 76 on the identical raw stream `[measured]`. During a rising rate our estimate tracked the
  hub's from 66 to 85 BPM `[measured]`.
- **Two independent algorithms agree.** Ours (autocorrelation over a band-passed signal) and Maxim's (on their
  own chip) landed within 0.5 BPM on the same data `[measured 29 Sep]`. That is the strongest check the
  heart-rate code has had, and it holds across two different vendors' optics.
- **Sensor A at the ear.** Best site measured 4.35 % pulse amplitude — **better than a fingertip** `[measured
  21 Sep]`. Full study in [`h1-ear-ppg-placement.md`](h1-ear-ppg-placement.md).
- **Session recording**, PDF reports, dual-sensor comparison `[measured: reports generated and re-opened with
  the Windows PDF engine]`.

### Not working, or not known

- ❌ **Blood oxygen is uncalibrated on both sensors, and cannot be fixed by comparing them.** Both run the same
  generic Maxim R→SpO2 curve, fitted for neither board, so they agree by construction. Anchoring needs an
  independently validated device. See §8.
- ❌ **One unexplained heart-rate fault, open since 21 Sep.** At the ear, sensor A reported **48–50 BPM with
  high confidence (quality 0.83–0.95)** while Stefan felt fine `[measured]`. The captured channel's spectrum
  has unrelated peaks at 34, 40 and 50 BPM — not what a real pulse produces — plus a weak 102 BPM component.
  Raising the filter's low edge to 0.7/0.8/0.9 Hz **did not change it** `[measured]`, so it is not simple
  drift. Two explanations fit: a true ~102 with a half-rate lock, or a genuine ~50. **It needs one reference
  measurement to settle** — a watch or a counted pulse. The capture is kept as
  `tools/max86171-hr-spo2/recordings/rec_LIVE_M1_PD1.txt`; `bench/spec.cs` reproduces the spectrum.
- ❌ **The two sensors have never been run simultaneously** on one person. The app supports it; nobody has done
  it yet. That is the obvious next test (§9).
- ❌ **Nothing has been tested on more than one person.** Every measurement in every document here is Stefan.
- ⛔ **Green light on sensor B is impossible** — the optics sit behind the hub's private bus and its firmware
  outputs only IR and red `[measured by HOTEL]`. So on sensor B, heart rate runs off infrared.

---

## 4 · The app

**Location:** `tools/max86171-hr-spo2/` in the kilo repo. **Built binary:** `bin/HeartRateTester.exe`.

### Building it

`build.cmd` uses the C# compiler that ships with Windows — **no SDK, no NuGet, no project file**:

```
tools\max86171-hr-spo2\build.cmd [output folder]
```

It produces `HeartRateTester.exe` (the window) and `hrdiag.exe` (the same code as a console tool). The display
font and the logo are **embedded in the exe**, so the file is self-contained and can be copied anywhere.

### The files

| File | What it does |
|---|---|
| `App.cs` | The window: tabs, cards, waveform, session panel, and the once-a-second display loop |
| `Sources.cs` | `ISensorSource`, the interface both sensors implement; `XiaoStream` (USB serial); `FakeSource` (simulated, for rendering checks) |
| `OS61.cs` | Bluetooth client for sensor A: GATT, register read/write over the vendor protocol |
| `PpgStream.cs` | Sensor A's configuration and its FIFO parser (tag-based sample reassembly) |
| `HeartRate.cs` | Band-pass filters, autocorrelation heart rate, multi-channel selection |
| `SpO2.cs` | Ratio-of-ratios blood oxygen, measured at the pulse frequency only |
| `Session.cs` | Recording: CSV, the index, and the PDF report including the two-sensor agreement block |
| `Pdf.cs` | A minimal PDF writer — no library, standard fonts, opens anywhere |
| `SelfTest.cs` | Offline tests on synthetic signals: `hrdiag.exe --selftest` |
| `bench/*.cs` | Analysis tools — see below |
| `assets/` | The display font (SIL OFL licence included) and the logo |
| `recordings/` | Raw captures from every session that mattered, so any claim here can be re-checked |

### Command-line modes (all on `hrdiag.exe`, or the app's exe)

| Mode | Use |
|---|---|
| `--selftest` | Run the offline tests. **Do this after any change to the maths.** |
| `--diag <secs>` | Stream sensor A, printing a line a second |
| `--xiao <secs>` | Stream sensor B, printing **our reading beside the device's own** |
| `--shot <png> [demo\|compare]` | Render the window to an image with no hardware — how the layout is checked |

### Bench tools (`bench/`, each compiled standalone with `csc`)

`score.cs` ranks recorded channels by pulse strength and signal-to-noise · `spec.cs` prints the strongest
frequencies in a channel, which is how a half-rate or drift lock is spotted · `filttest.cs` sweeps the
band-pass low edge · `wintest.cs` sweeps the estimator window length · `hreval.cs` compares heart-rate methods
· `spo2test.cs` runs the oxygen maths over a recorded red/IR pair · `rec.cs` records raw channels with any
sensor configuration · `pdfshot.cs` renders a PDF to PNG using the Windows engine (**how generated reports are
verified**) · `imgconv.cs` converts images (including WebP) with the Windows imaging APIs.

---

## 5 · How the measurement works

**Heart rate.** Band-pass 0.5–4 Hz (two high-pass and two low-pass biquad sections), then autocorrelation over
an 8-second window; the shortest-lag peak near the maximum is chosen so harmonics are not mistaken for the
fundamental; a reading is refused unless the correlation exceeds 0.5, and the result is the median of the last
five. Before any reading exists, a 5-second window is also tried — measured to bring the first reading forward
2–3 seconds with no wrong readings, while a 4-second window produced wrong sub-50 values `[measured 21 Sep]`.

**Multiple channels.** Sensor A produces eight green channels (two LED groups × four photodiodes). Each is
estimated **separately** and the cleanest wins. **They must never be summed** — see §6.

**Blood oxygen.** `R = (AC_red/DC_red) / (AC_ir/DC_ir)`, with both AC amplitudes measured at the pulse
frequency only (a windowed Fourier component, which rejects most noise), then the Maxim curve
`SpO2 = −45.060R² + 30.354R + 94.845`. **That curve is Maxim's, for a different sensor, and is not calibrated
here.** A reading is refused when the signal-to-noise is below 5, when red and IR are more than 25° out of
phase, when perfusion is under 0.08 %, when R falls outside 0.3–1.2, or when the result lands on the curve's
clamp. The last two exist because a clamped value once displayed as a rock-steady "70 %" (§6).

---

## 6 · The five findings that cost the most to learn

1. **Never average a weak channel into a strong one.** The app summed each detector's two green measurements
   before estimating. At the ear that buried the good signal under the swamped one: **signal-to-noise 43 → 7**,
   and it produced confident sub-50 BPM readings. Split into eight separately scored channels: 0 wrong readings
   on the same data `[measured 21 Sep]`. The same lesson had already been learned for blood oxygen four days
   earlier and was not applied to heart rate — **when a rule is found in one signal path, check every other
   path the same day.**
2. **A clamped value looks more trustworthy than a real one.** R = 1.48 produced a displayed "70 %" with a
   spread of zero — the steadiest number on the screen, and meaningless. Any clamp, saturation or range limit
   must be detected and refused, never rendered `[measured 21 Sep]`.
3. **A generated file is not verified until something opens it.** The first PDF the app wrote was **invalid** —
   Windows refused it — because the page tree pointed at the wrong objects. Rendering it caught this in one
   run. The same habit caught a CSV bug: this machine writes decimals with a comma, which split values across
   columns `[measured 21 Sep]`.
4. **Placement beats settings, by a long way.** LED current across a 5× range changed the signal-to-noise not
   at all; the same spot measured twice gave 28 and then 12 `[measured 21 Sep]`. **Consequence: the LEDs can
   run at a fifth of the current** — less battery, less heat — and effort belongs in mechanical contact, not
   register tuning.
5. **Stray light is the enemy.** In every recording, the winning channel was one where *little* light came back
   (3–10 % of range); channels at 35–88 % were the worst every time `[measured]`. Any enclosure needs an opaque
   barrier between emitters and detector, and a matte black, daylight-sealed pocket.

---

## 7 · Findings that affect the product, not just the app

- **The ear works, and where matters.** Lobe and just below the tragus: 4.35 % pulse, the best of four sites.
  Concha: cleanest of all but unreachable by a cushion. In front of the tragus: good. **Behind the ear
  (mastoid): worst** — 0.5 %, channels disagreeing `[measured 21 Sep]`. Note this **contradicts the fleet's
  earlier research**, which recommended the mastoid on the strength of a competitor's product. Both cannot be
  right; it should be settled before a cushion design is committed.
- **Talking costs availability, not accuracy.** Heart rate readings halved in number but stayed correct; red
  and IR collapsed entirely (signal-to-noise 21–24 → 4) `[measured 21 Sep]`. So blood oxygen must be computed
  only in quiet, still windows.
- **⚠ Sample rate is a flight-safety issue.** The fleet's own research shows that at low sample rates,
  helicopter rotor harmonics **alias into the pulse band and look like a perfect heartbeat** — at 25 samples/s
  a 27.5 Hz tail rotor folds to exactly 150 BPM `[fetched: GOLF, 18 Aug]`. **Sensor A is currently configured
  at 25 samples/s.** Harmless on a bench, dangerous in an aircraft. Their table shows 100 Hz and above is
  clear. Sensor B already runs at 100.

---

## 8 · Blood-oxygen calibration — the honest position

Nothing here produces a trustworthy SpO2 number, and the app says so on screen and in every report.

- **Route A, comparison:** our sensor and a **certified fingertip oximeter** on the same person, many points,
  fit the curve. Cheap (a pharmacy oximeter is R300–600) and can start immediately. **Its limit is absolute:**
  healthy people at sea level sit at 96–100 %, so the fit is anchored only there and says nothing about 85 %.
- **Route B, controlled desaturation:** a lab lowers inspired oxygen in stages with arterial blood drawn and
  measured by CO-oximetry. FDA's 2025 draft guidance recommends **≥ 24 participants across the full skin-tone
  range, 480 paired data points, under 3 % ARMS** `[fetched]`. Specialist labs, months, real money. Required
  for any clinical or safety claim.
- **Not a route: calibrating the two sensors against each other.** Both run the same generic curve, so they
  agree by construction and the agreement proves nothing.
- **Why it matters for the H1:** hypoxia awareness matters at **80–90 %**, exactly the range Route A cannot
  validate, and altitude brings cold ears and weak perfusion — the worst measured condition. A sensor fitted
  only on warm, resting subjects could read comfortingly normal on a pilot going hypoxic.

---

## 9 · What to do next, in order

1. **Settle the 48–50 BPM question** (§3). One reference measurement — a watch, a chest strap, or a counted
   pulse — decides whether the maths needs fixing or the placement does. Everything else at the ear is on hold
   behind it.
2. **Run both sensors at once** on one person and record it. The app does this today and reports the agreement.
   It is the cheapest remaining check on both devices.
3. **Buy a certified fingertip oximeter** and do Route A. It also gives an independent check on heart rate.
4. **Repeat the placement study on three to five people** before any cushion geometry is committed. Everything
   known about placement rests on one pair of ears.
5. **Decide the sample rate for flight** (§7) before the sensor choice is frozen.
6. **Resolve the mastoid contradiction** between our measurements and the fleet's research.
7. If green light is ever needed at the ear, sensor B cannot provide it — that requires sensor A or different
   hardware.

---

## 10 · Where everything lives

| What | Where |
|---|---|
| App source, binaries, raw recordings | `tools/max86171-hr-spo2/` in the **kilo** repo (`github.com/stefan4Aerosense/kilo`) |
| Sensor A bring-up: protocol, registers, what it does *not* establish | `docs/max86171-evsys-bringup.md` |
| Ear placement study, and the calibration appendix | `docs/h1-ear-ppg-placement.md` |
| Phone version (sensor A only) | `web/` in kilo; published at **https://stefan4aerosense.github.io/h1-sensor-web/** (public repo `stefan4Aerosense/h1-sensor-web`) |
| Sensor B: firmware, its own handover, research, build history | `D:\AeroSense\Heart Rate Sensors\xiao_hr_usb_KILO_package_2026-09-29\` — **read `handover/HANDOVER.md`** |
| The fleet's PPG research (placement, vibration, buy lists) | `research/` inside that same package |
| Related work by another agent on the same question | `chris/golf/pilotsense-max86171/` in the Aerosense-Dev-Team-Sync repo |

**The phone page is public.** It contains the page, the maths and the sensor protocol — no company data, no
credentials, but public. It is deliberately Bluetooth-only: Chrome on Android cannot read USB serial devices.

---

## 11 · Conventions this work follows

- **Every claim ships its evidence**: a measured number with a date, a file and page, or an explicit
  `[assumed]`. A thing that cannot be measured is recorded as unknown, never rounded up to working.
- **A display never shows a stale or invalid value as live.** Held readings are dimmed and labelled with their
  age; invalid ones blank out with the reason.
- **Verify at the point of use.** Flash it and read the version back; render the PDF and look at it; run the
  estimator over a real recording rather than trusting that it compiles.
- **Raw recordings are committed** so that every claim can be re-checked by someone who doubts it. Session
  recordings of real people are **not** committed — they carry names.
- Commits are tagged `[KILO]`, staged with explicit paths, never `git add -A`.
