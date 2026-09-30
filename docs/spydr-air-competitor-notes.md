# SPYDR Air — what Spotlight Technologies actually publishes about it

**Compiled 30 September 2026 by KILO** from spotlighttechnologies.com (every page), their peer-reviewed study,
and the US SBIR record. Fetched 30 Sep 2026.

---

## 1 · The short version

**SPYDR Air is barely documented.** On their Technologies page it gets one paragraph, one photograph and three
headings — *Key Features*, *Mission Focus*, *Performance Intelligence* — **all three of which are empty**
`[measured: the page's own DOM contains no body text under them]`. There is no specification, no price, no
supported-headset list, no battery figure, no availability date.

Everything concrete they publish is about the **helmet** product, SPYDR, which is a mature, flight-proven
system. SPYDR Air is the same sensing idea repackaged for commercial headsets, and the US government's SBIR
record describes that commercial work as **"in development (unfunded) with other commercial aviation
entities"** `[fetched: sbir.gov/portfolio/15370]`.

The full published text on Air is:

> "SPYDR Air transforms commercial aviation headsets into intelligent biometric and environmental monitoring
> systems for non-helmeted aircrew. SPYDR Air enhances existing headset architecture with real-time
> physiological awareness, environmental sensing, and predictive risk monitoring — without compromising
> comfort or operational performance."

---

## 2 · What the underlying platform does (the helmet product, documented)

| Item | What they publish |
|---|---|
| Form | A **replacement earcup** — self-contained, holding microprocessors, I/O and solid-state memory |
| Sensor site | Reflectance PPG **behind the ear**, between pinna and hairline, on the mastoid, above the sternocleidomastoid, near the posterior auricular artery |
| Measures | Heart rate and heart-rate variability · SpO2 · breathing rate and depth · skin and sensor temperature · head movement (6-axis) |
| Environment | Cabin pressure, G-force, mass airflow, oxygen quantity, air pressure, humidity, take-off/landing detection |
| Alerting | Graduated audible warning of impending incapacitation, by **bone conduction** — no aircraft audio integration |
| Power/integration | Fully standalone: no aircraft power or data. **20+ hours** in flight, a year on the shelf, triple-redundant battery safety |
| Weight | **70 g**, "in most cases adds zero weight to the helmet" |
| Fitting | Under 60 seconds, backwards compatible with existing helmets |
| Track record | **3,803 sorties**; operational in F-15, F-16, F-22, A-10; tested in T-6, T-45, F-18, EA-18; ground test F-35; safe-to-fly certified after centrifuge, altitude chamber and ROBD testing |
| Data | Downloaded after each sortie, pilot identity masked, aggregated in a cloud analytics platform; integrates Whoop, Garmin and Oura data alongside |

---

## 3 · Their validation study — the part worth reading properly

Published in *Biosensors* 2020, 10(4), 34 `[fetched: mdpi.com/2079-6374/10/4/34]`. This is the most useful
document they have, because it is the study we have parked as "Route B".

- **Where:** the Hypoxia Research Laboratory at UC San Francisco — an independent university facility with no
  ties to oximeter manufacturers — under the FDA's recommended protocol.
- **Who:** 12 healthy adults, 7 female and 5 male, ages 21–49, **skin tone light to dark**, mixed ethnicity.
- **How:** an arterial cannula in the wrist; oxygen driven down in plateaus at 100 %, room air, 93, 90, 87,
  85, 82, 80, 77, 75 and 70 %; **348 arterial blood samples**; gold-standard co-oximetry on a Radiometer
  ABL-90.
- **Result:** root-mean-square error **2.61 % across 70–100 %**, inside the FDA's requirement.
- **Against other sensors:** a second test with five subjects in an F/A-18 ejection-seat mock-up, wearing an
  oxygen mask fed by a reduced-oxygen breathing device, compared SPYDR against **Nonin** (finger) and
  **Masimo** (forehead). Correlation above 93 % for both pulse rate and SpO2.
- **The finding that matters most for aviation:** SPYDR **responded to changes in oxygen up to 50 seconds
  sooner** than the finger and forehead sensors. A behind-the-ear site is centrally perfused, so it desaturates
  before the extremities do.

They also had the earcups acoustically assessed by AFRL against the legacy HGU-55/P earcups and judged "an
acceptable replacement" — i.e. they proved the hearing protection was not degraded, which any headset
modification has to.

---

## 4 · What this means for the H1

1. **The commercial product does not exist yet.** There is nothing to buy, no published price, no headset
   compatibility list. We are not late.
2. **But the sensing question is settled by someone else, publicly.** Their study is exactly the work we
   parked, done properly: independent lab, arterial blood, 348 samples, mixed skin tones. It is also the
   benchmark anyone will measure us against.
3. **The 50-second earlier warning is their real product claim**, not accuracy alone. If the H1 ever makes a
   hypoxia claim, that is the bar: not "we read SpO2" but "we warn sooner than a finger clip".
4. **Their site is thin on Air because it is a concept, and that is an opportunity and a warning.** An
   opportunity because the commercial market is open; a warning because they have the validation, the patents
   (see `patents-earcup-ppg-2026-09-30.md`) and the military credibility to move quickly when funded.
5. **Their mastoid result still contradicts our measurement**, where behind-the-ear was the worst of four
   sites. Their study is far stronger evidence than our hand-held test — but theirs is under a helmet earcup
   with real clamping pressure. **That difference, pressure and coupling, is the most likely explanation and
   the thing to test next with a cushion-mounted sensor.**

---

## 5 · Sources

Spotlight Technologies: [Technologies](https://www.spotlighttechnologies.com/technologies) ·
[Validation and Verification](https://www.spotlighttechnologies.com/validation-and-verification) ·
[AI & Wearables Integration](https://www.spotlighttechnologies.com/ai-wearables-integration) ·
[Research & News](https://www.spotlighttechnologies.com/research-news).
Peer-reviewed study: Bradke B, Everman B, *Investigation of Photoplethysmography Behind the Ear for Pulse
Oximetry in Hypoxic Conditions with a Novel Device (SPYDR)*, **Biosensors 2020, 10(4), 34**,
[doi:10.3390/bios10040034](https://www.mdpi.com/2079-6374/10/4/34).
Commercial status: [SBIR portfolio entry](https://www.sbir.gov/portfolio/15370).
AFRL acoustic assessment: linked from their Validation page as a PowerPoint (June 2022).
