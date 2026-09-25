# Spec sheet — 3D-printed base plate for Chris's four audio dev units

**Prepared by KILO, 25 September 2026, from INDIA's brief `2026-09-25-001` and GOLF's package
`chris/golf/baseplate-2026-09-25/` (19 files, 56 MB, in the Aerosense-Dev-Team-Sync repo).**
Written to be handed to another engineer or agent cold: everything needed is below.

---

## 0 · How to read this document

**Every dimension carries a grade. Check it before cutting metal or starting a print.**

| Grade | Meaning |
|---|---|
| `OFFICIAL` | From the manufacturer's own drawing or 3D model. Use freely. |
| `SCALED ±0.5` | Derived from a vector drawing by a known ruler. Good enough to start CAD, not to print. |
| `SCALED ±2` | Derived from a picture. **Not good enough for anything.** Must be calipered first. |
| `[measured]` | Someone put calipers on the real hardware. Record who and when. |
| `UNKNOWN` | Nobody has a number yet. Listed in §7. |

The fleet rule on this project is *we don't guess*. If a number is not in this document, do not invent it —
it belongs in §7 as a question for Chris.

---

## 1 · The job

Chris wants a 3D-printed base plate that holds his audio development rig on one bench, so four loose boards
stop sliding around and their wiring stays fixed. The plate carries **four boards in two independent pairs**.
Each pair is a host computer plus its own audio codec board.

The deliverable is **the plate design**, not the boards: an outline with mounting-hole positions, standoffs,
clearance for connectors that hang over board edges, and routes for the cabling described in §4.

---

## 2 · What goes on the plate

| # | Unit | Model | Role | Paired with |
|---|---|---|---|---|
| 1 | Raspberry Pi 5 | Pi 5 Model B Rev 1.1, 8 GB, 512 GB NVMe, Active Cooler | host **DEV-A** | #2 |
| 2 | ADAU1860 eval board | **EVAL-ADAU1860EBZ** | DEV-A's audio codec | #1 |
| 3 | Raspberry Pi CM5 (8 GB / 32 GB eMMC) on a Waveshare carrier | **Waveshare CM5-IO-BASE-A** | host **DEV-B** | #4 |
| 4 | ADAU1372 eval board | **EVAL-ADAU1372Z** | DEV-B's audio codec | #3 |

The CM5 module sits on the Waveshare carrier, inside its outline — it needs no mounting of its own.

---

## 3 · Dimensions

All in millimetres, measured from the board's top-left corner viewed from the component side.

| Unit | Outline | Mounting holes | Grade |
|---|---|---|---|
| **Raspberry Pi 5** | **85 × 56** | 4 × **Ø2.7**, centres **3.5** from the left and top/bottom edges, pattern **58 × 49** | `OFFICIAL` (the drawing calls itself "approximate, for reference") |
| **Waveshare CM5-IO-BASE-A** | **85 × 56** | 4 holes, **3.5** from left/top/bottom; right-hand pair **23.5** from the right edge → pattern **58 × 49** (the Pi HAT pattern) | `OFFICIAL` + a STEP model |
| **EVAL-ADAU1372Z** | **≈ 61.2 × 89.2** (portrait) | 4 × **≈ Ø2.5**, centres **≈ 5.1** from every edge, pattern **≈ 50.9 × 79.0** | `SCALED ±0.5` |
| **EVAL-ADAU1860EBZ** | **≈ 136 × 115** (landscape) | 4 × **≈ Ø2.9**, centres ≈ 3.6–4.5 from the edges, pattern **≈ 128 × 107** | `SCALED ±2` — **do not print from this** |

**Both Raspberry-Pi-family boards share the 58 × 49 HAT hole pattern, so their mounts are interchangeable.**
**M2.5 standoffs fit the Ø2.7 holes** on the Pi 5 and the Waveshare carrier.

### How the two uncertain numbers were obtained

- **ADAU1372:** scaled from the vector layout in user guide UG-807, Figure 40, using the 0.1-inch header pitch
  as the ruler (12.9 pt = 2.54 mm, repeated ~50 times). The four holes came out symmetric to 0.1 mm, which
  cross-checks the scale. Hence ±0.5 mm.
- **ADAU1860:** scaled from a **raster image** in user guide UG-2017, Figure 16, at 7.70 px/mm taken from a
  2×10 header pitch. Hence ±2 mm, which is worse than the clearance any print needs.

**Analog Devices publishes no drawing, design files, Gerbers or 3D model for either eval board** (both product
pages checked 25 Sep 2026; DigiKey lists no size either). These numbers cannot be improved from documents —
only calipers will fix them.

---

## 4 · Things that stick out past the board outline

A plate sized to the outlines alone will foul. Allow for:

| Unit | What overhangs, and where |
|---|---|
| Pi 5 | USB and Ethernet overhang the **right edge by 3 mm** |
| CM5-IO-BASE-A | Ethernet, USB-A and HDMI overhang the **right edge** — take the exact figure from the STEP model |
| EVAL-ADAU1372Z | the **J2 barrel jack** and **J1 header** stand above the **top edge** |
| EVAL-ADAU1860EBZ | its **three input jacks (AIN0–AIN2)** sit on the **top edge** |

Also unmeasured: the tallest component on each board, which sets standoff height and any cover. `UNKNOWN` — see §7.

---

## 5 · Wiring: this is what decides the layout

The plate exists to hold wiring stable, so the layout follows the wiring, not the other way round.

### DEV-A — Pi 5 ↔ ADAU1860
- **A USB cable** from a Pi USB-A port to the 1860's **P22**. It carries the 1860's power *and* control, so
  leave room for the plug at both ends. **The 1860's P47 power header stays empty.**
- **Six jumper wires** from the Pi's 40-pin header (pins **12, 14, 35, 38, 39, 40**) to the 1860's **P2**.
- **Pin 40 passes through a 1.2 kΩ / 1.0 kΩ resistor divider** on the way to P2. **That divider needs a home on
  the plate** — a small perfboard pad or a holder.
- **ICM-45686 IMU #2** wires to header pins **17, 19, 21, 23, 24, 25**, and sits "on the mic mount".

### DEV-B — CM5-IO-BASE-A ↔ ADAU1372
- **Four wires** from header pins **3, 4, 5, 6** to the 1372's **J1**: control plus its 5 V power.
- **Seven wires** from header pins **12, 14, 15, 35, 38, 39, 40** to the 1372's **J4**.
- **The ADI USBi adapter (EVAL-ADUSB2EBZ) is NOT used** — the CM5 drives J1 directly. No space needed for it.
- **An ADXL354 accelerometer eval board** feeds the 1372's **AIN0–AIN2**, and **ICM-45686 IMU #1** is mounted
  **rigidly next to it**. They are a matched pair and **must not move relative to each other** — they need a
  single common mount, not two separate ones.

### Hard constraints

1. **Microphone leads are ≤ 15 cm** (Infineon's tested limit). Mics plug into the 1860's **P9/P10/P11/P44** and
   the 1372's **AIN3**, via small "red adapter" boards. Anything the mics reach must be within 15 cm of them.
2. **Each codec sits next to its own host.** The header-to-codec wires carry audio clocks; shorter is better.
3. **Keep IMU wiring away from codec wiring.** `[measured, 22 Sep]` On DEV-B, the IMU bus running beside the
   audio clocks corrupted **1,253 audio words in 3 seconds**. Moved away, it corrupted **0**. This is a measured
   failure, not a precaution — treat physical separation of those two runs as a requirement.
4. **Both hosts need airflow.** The Pi 5 has an Active Cooler; DEV-B has a fan `[measured, 22 Sep]`. Do not box
   either in, and do not block the underside if the standoffs are the only air gap.

---

## 6 · Size, and why the plate probably cannot be one piece

Using the §3 outlines with a 15 mm gap between boards and a 15 mm margin:

| Arrangement | Overall size |
|---|---|
| DEV-A pair alone (Pi 5 + 1860) | ≈ 236 × 115 mm |
| DEV-B pair alone (CM5 carrier + 1372) | ≈ 161 × 89 mm |
| All four side by side | **≈ 442 × 145 mm** |
| All four in two rows | **≈ 266 × 249 mm** |

`[calculated]` from the §3 numbers, two of which are provisional.

**A typical desktop printer bed is 220–256 mm square.** Both whole-plate arrangements exceed that, and the
side-by-side one exceeds it by a long way. So the design should assume **either** a large-format printer
**or**, more likely, **a plate split into sections** — the natural split being one plate per dev unit, since
each pair is independent. Sections should locate to each other positively (dovetails, or dowel pins plus M3
bolts) so the rig can be moved without losing alignment.

**Ask Chris for his printer's bed size before choosing** — it changes the whole design. See §7.

---

## 7 · Open questions — answer these before producing anything

| # | Question | Why it blocks | Ask |
|---|---|---|---|
| 1 | **Calipered outline, hole centres, hole Ø for both ADI eval boards** | The 1860 figure is ±2 mm; holes would not line up | Chris (boards are on his bench) |
| 2 | **Tallest component on each board** | Sets standoff height and any cover | Chris |
| 3 | **Printer bed size** | Decides one plate or several — see §6 | Chris |
| 4 | **Does the mIDAS-Link emulator stay on the bench?** (ADAU1860 kit, no published size) | Needs a place and a measurement if yes | Chris |
| 5 | **Do the ADXL354 eval board, both IMUs and the mic adapter boards mount on the plate?** | They need positions and a shared rigid mount (§5) | Chris |
| 6 | **Output format: dimensioned drawing, OpenSCAD, or STEP?** | Decides the tool | Chris |
| 7 | **Is the plate ever moved or transported?** | Decides section joints and whether boards need retention beyond standoffs | Chris |

---

## 8 · Suggested approach

1. **Build it parametrically**, with every board's outline, hole pattern and position as named variables at the
   top of the file. Two of the four boards will change once calipered, and a parametric model absorbs that in
   one line each. OpenSCAD suits this well; so does any CAD with a parameter table.
2. **Start from the known-good boards.** The Pi 5 and the Waveshare carrier are official and share the 58 × 49
   pattern, so their mounts can be a single reusable module.
3. **Import the Waveshare STEP model** (`CM5-IO-BASE-A-20250225.stp`) rather than modelling the carrier — it
   gives exact connector overhang and heights.
4. **Leave the ADI boards' mounts as parameters with generous slots** (or print those two mounts separately)
   until §7.1 is answered.
5. **Route the wiring first, then place the boards.** The 15 cm mic limit and the IMU-separation rule in §5 are
   the binding constraints; hole positions follow from them.
6. **Print one board's mount alone first** as a fit check before committing to a full plate.

---

## 9 · Acceptance checks

- Every dimension on the drawing traces to a source file and page, or to `[measured]` with a name and date.
- No number graded `SCALED ±2` survives into the final design.
- Each codec is beside its own host; mic runs ≤ 15 cm; IMU runs physically separated from codec runs.
- Right-edge port overhangs (Pi 5, Waveshare) are clear of the plate edge or have cut-outs.
- Cooler and fan intakes are unobstructed.
- The plate, or each section of it, fits Chris's printer bed.
- A place exists for the DEV-A resistor divider, and a single rigid mount carries the ADXL354 with IMU #1.

---

## 10 · Source files

All in `chris/golf/baseplate-2026-09-25/` of the Aerosense-Dev-Team-Sync repo:

| File | What |
|---|---|
| `HANDOVER.md`, `MECHANICAL.md` | GOLF's brief and dimension sheet, with grades |
| `CM5-IO-BASE-A-20250225.stp` | Waveshare carrier, **STEP 3D model** (24.8 MB) |
| `raspberry-pi-5-mechanical-drawing.pdf` / `.png` | Pi 5 official drawing, 1:1 at A4 |
| `waveshare-CM5-IO-BASE-A-size.jpg`, `…_Sch.pdf` | Waveshare size drawing and schematic |
| `EVAL-ADAU1372Z_top_assembly_fig40.png` | the figure the 1372 numbers were scaled from |
| `EVAL-ADAU1860EBZ_layer_xref338.png` / `xref344.png` | the images the 1860 numbers were scaled from |
| `EVAL-ADAU1372Z_UG-807.pdf` (layout p.17), `eval-adau1860-ug-2017.pdf` (layout p.22) | ADI user guides |
| `ADAU1372.pdf`, `adau1860-1.pdf`, `adau186x-hardware-reference-manual-ug-2257.pdf` | codec datasheets |
| `raspberry-pi-cm5.pdf`, `…-product-brief.pdf`, `raspberry-pi-5-product-brief.pdf` | host datasheets |
| `adxl354_adxl355.pdf` | the accelerometer feeding the 1372 |

Originals, if they need re-fetching:
- Pi 5 drawing — `datasheets.raspberrypi.com/rpi5/raspberry-pi-5-mechanical-drawing.pdf`
- Waveshare carrier (drawing, schematic, STEP) — `waveshare.com/wiki/CM5-IO-BASE-A`
- ADI user guides — `analog.com` product pages for EVAL-ADAU1372 and EVAL-ADAU1860.
  **analog.com blocks plain HTTP clients; use a real browser.**

---

## 11 · What this document does not establish

- **No ADI eval board dimension here is verified against hardware.** Two of the four boards are provisional.
- **No layout is proposed yet** — only the constraints one must satisfy. Placement waits on §7.
- Nothing is known about the plate's material, thickness, infill or heat tolerance. A bench plate under a Pi 5
  with an active cooler is not a thermal problem, but nobody has checked it.
- The §6 sizes are arithmetic from provisional outlines, not a layout study.
