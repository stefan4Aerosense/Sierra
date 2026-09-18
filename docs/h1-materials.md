# H1 headset — materials discussed, and where each one stands

**Compiled 2026-09-18 by KILO** from four sources on Stefan's machine. This is a consolidation, not a new
decision — every status below is quoted from its source. Where sources disagree, both are shown.

| Tag | Source |
|---|---|
| `[S]` | **Component Materials Summary**, Stefan, 15 Jul 2026 — `D:\Downloads\Aerosense_H1_Materials_Summary.docx` (posted #cad-design 17 Jul, "subject to change") |
| `[R]` | **Materials Research** companion, 8 Jul 2026 — `D:\MrClaude\aerosense-headset\MATERIALS.md` (identical copy in `D:\Documents\MrClaude\…`) |
| `[ops]` | `aerosense-ops@6c7fdb4` `dev-docs/h1-headset.md` + `ledger/LEDGER.md` — the company record, newest state |
| `[CAD]` | Fusion agent, `D:\Github\Fusion\docs\H1_weight_cg_materials_2026-07-29.md`; FusionChatGPT `@5c34fea` |

---

## Current list

| Component | Material | Status (latest) | Notes |
|---|---|---|---|
| **Tension / spring band** ("Alice band") | **17-7 PH stainless spring steel** | Base spec 9 Jul, confirmed 4 Aug `[ops]` — **but alloy NOT settled**: supplier talk is **301 spring temper** `[ops]` | One-piece laser-cut flat pattern, R ≈ 73.8 mm, **t = 0.8 mm** (was 1.0; corrected 14 Aug) `[ops]`. Form annealed (Condition A), harden to CH900/RH950 after forming `[S]`. Target 350–450 gf/side vs Bose A20/A30; ~9× stress margin at t = 1.0 `[S]`. CAD carries 17-4 PH as a library proxy `[CAD]`. Unsourced: JP Steel doesn't do spring steel; **Capewell** approached `[ops]` |
| **Earcup shell** | **PA12 GF (outer) + PA12 or PA11 (inner)**, SLS/MJF, vapour-smoothed | **Production = PA12 / PA12 GF**; ID/materials lane closed 7 Aug `[ops]` | **ABS = fit-check prototypes only** `[ops]`. Avoid glass fill at the thin rim `[S]` |
| **CLD damping core** (between shell layers) | **Silicone gel, Shore 00-10 to 00-30** | Recommended `[S]` | Considered: 3M ISD-112/110, Sorbothane `[R]` |
| **Transparent cup / window** | **Polycarbonate vs acrylic (PMMA)** | **OPEN** — board card *"Decide polycarbonate or acrylic for the transparent window"*, **no owner** `[ops]` | Summary applied **acrylic PMMA, cast, tinted blue** (PC→PMMA for clarity) `[S]`; the 29 Jul CAD pass set **PC** on 32 of 33 bodies `[CAD]`; Duran leaned acrylic (9 Jul) `[ops]`. Not printable either way. Spread attachment loads — crack risk at small fixings `[ops]` |
| **Ear seals / cushions** | **Memory-foam core + gel ring, microfiber / PU synthetic-leather cover** | Concept: **dual-seal** (firm inner acoustic rim + soft outer cushion + damped moat), in CAD on both cups 3 Aug `[ops]` | **Attachment = lip/flap wrap** over a lip on the cup (4 Aug) `[ops]` — supersedes the snap-ring recommended in `[S]`. **Outer cover material still open**, must be strong enough to hold the lip `[ops]`. Height/durometer to be tuned on the EARS Pro + 711 coupler, not in CAD `[ops]`. Fusion has no gel/memory-foam material — needs a custom one `[ops]` |
| **Head-top cushion** | **Open-cell PU foam**, same leather-family cover | Recommended `[S]` | |
| **Outer cosmetic strap** | **Polyester webbing + brushed loop-knit lining** | Applied in CAD `[S]`; **sourcing open** `[ops]` | Non-load-bearing. **Weavell** best match (high-tenacity polyester, 20/25/38/50 mm, black, 200 m rolls); Exotic Ribbons never replied; Webbing Products quote; board also names "Weavetech" — **one supplier or two is unresolved** `[ops]`. Card `680030c0` |
| **Band → earcup joint** | **Recessed compliant rubber dampener / grommet**, ~5–10 mm, 8 screws | Direction approved 21 Jul `[ops]` | CAD grommet material still wrong (steel), target ABS vs TPU/nylon unconfirmed `[CAD]` |
| **Mic swivel** | **Brass insert** on a spindle pivot (Swivel Mic V1) | In CAD `[ops]` | |
| **Microphone cap** | **Aluminium 6063-T5, anodised red** (Type II) | Applied `[S]` | 6063 chosen over 6061 for anodising colour uniformity `[S]` |
| **Boom mic arm** | **Silicone-jacketed stainless gooseneck** | Recommended `[S]` — **but CAD shows a rigid arm**; design intent open `[S]` | Housing aluminium (EMI) or PA12 `[S]` |
| **Cable jacket** | **TPU** (avoid PVC) | Recommended `[S]` | Tether = wired, quick-release **Lemo** `[ops]`; Lemo size/position is an unowned card `[ops]`. `[R]` suggested dual GA plugs — superseded by the Lemo tether |
| **First-prototype assembly** | **Glued**, shelled parts, no snap fits | Decided 9 Jul `[ops]` | |
| **Certification grade** | **FR-PA12 / FR-PA11** if TSO / FAR 25.853 | **OPEN — blocks final polymer grades** `[S]` | Aftermarket/PMA (the GA norm) allows standard grades `[S]` |

## Considered and set aside
- **Band:** titanium spring alloy (Ti-3Al-2.5V / beta-Ti) — **premium-tier upsell only**; **Nitinol** — shelved;
  **composites / GFRP** — rejected for the band; **carbon spring steel (music wire A228)** — option in `[R]`, needs a
  coating. `[ops]` `[R]`
- **Band prototypes:** ABS ≈ 4.7 mm or PLA ≈ 4.0 mm thick for fit-check feel only — both creep; never production `[S]`
- **Shell:** **magnesium casting** — deferred to V2/launch `[ops]`
- **Seal cover:** PVC leather (cracks), genuine lambskin (cost/supply) `[R]`

## Still open, in one place
1. **Window: PC or acrylic** — unowned card.
2. **Band alloy: 17-7 PH vs 301 spring temper**, and a supplier for it.
3. **Ear-cushion cover material** (must hold the lip).
4. **Strap supplier** — Weavell / Weavetech ambiguity.
5. **Grommet material** (ABS vs TPU/nylon) and its CAD fix.
6. **Boom arm**: gooseneck or rigid.
7. **Certification target** — decides every polymer grade.

*Mass context: H1 Assembly **304 g in CAD** (12 Aug), CG 1.1 mm — materials-dependent, 3 bodies still generic
"Steel"; V2 criterion (4) is < 400 g in prototype form — CAD evidence, not a weighed prototype. `[ops]`*

---

## Clamping force — how hard the band should squeeze (added 2026-09-18)

| Reference | Force | Source |
|---|---|---|
| **H1 design target** | **350–450 gf (3.4–4.4 N)** | `[S]` §3 + appendix; matched to Bose A20/A30 |
| Bose A20 | **450 g ≈ 4.4 N** | `[fetched]` Skies Mag review, figure given by Bose senior product manager Matt Ruwe |
| Bose A30 | ~20 % less than A20 ≈ 360 g ≈ 3.5 N | `[S]` (community figure); widely repeated, no primary source found |
| EN 352-1 (industrial earmuffs, safety PPE) | **≤ 14 N (~1 430 g) maximum** — a ceiling, not a comfort target | `[fetched]` Health & Safety International, "EN 352: ear plugs, ear muffs" (test method EN 13819-1) |
| Earmuff comfort research | Earmuffs tested at 6, 8, 10, 12 and 14 N | `[fetched]` search abstract only, *Comfort evaluation of hearing protection*, Int. J. Industrial Ergonomics (ScienceDirect S0169814104000150) — paywalled, result not read |

**Reading it:** a headband force is one number — the band presses each cup against the head with the same
force F; "per side" does not double it. Aviation headsets are worn for hours, so they sit far below the
earmuff ceiling: ~3.5–4.5 N is the Bose class the H1 targets.

**Discarded:** web "comfort thresholds" such as *">3.8 N causes fatigue per IEEE Std 1789-2022"* — IEEE 1789 is
an LED-flicker standard; the claim comes from product blogs. Not usable.

### ⚠ The 0.8 mm change probably halved the clamping force `[calc]`

The summary's own model (`F = 2·E·I·δ / (π·R³)`, `I = w·t³/12`, E = 204.2 GPa, R = 73.8 mm, spread δ = 15 mm,
width w = 9.6 mm — geometry `[assumed]` from `[S]`):

| Thickness | Force |
|---|---|
| 1.0 mm (as sized on 15 Jul) | 3.88 N ≈ **396 gf** — on target |
| **0.8 mm (current, since 14 Aug)** | 1.99 N ≈ **203 gf** — about **half** the target |

To get back to ~400 gf at 0.8 mm: width ≈ **18.8 mm**, or a pre-spread of ≈ **29 mm**, or a mix. Force scales
with E, so 301 vs 17-7 PH barely changes it (similar modulus); the alloy matters for yield and stress
relaxation. **Not verified:** whether width or spread were changed alongside the thickness — no record of a
force recheck at 0.8 mm was found. Needs a measurement on a real band or a CAD/FEA check.

### Required band thickness for 350–450 gf — stainless steel `[calc]` (2026-09-18)

Same model as above, R = 73.8 mm, spread δ = 15 mm (`[assumed]` from `[S]`); model reproduces `[S]` exactly
(396 gf → t = 1.000 mm, 179 MPa). Stainless 301/304 E = 193 GPa; 17-7 PH E = 204.2 GPa.

| Width | 301/304 spring temper, 400 gf | 17-7 PH, 400 gf | Bending stress |
|---|---|---|---|
| 8 mm | 1.09 mm | 1.07 mm | ~185–190 MPa |
| **9.6 mm (current)** | **1.02 mm** (0.98–1.06 for 350–450 gf) | **1.00 mm** | ~175–180 MPa |
| 12 mm | 0.95 mm | 0.93 mm | ~160–165 MPa |
| 15 mm | 0.88 mm | 0.87 mm | ~150–155 MPa |
| **20 mm** | **0.80 mm** (0.77–0.83) | 0.79 mm | ~135–140 MPa |

- **Temper matters more than grade:** stresses are ~135–190 MPa. Annealed 304 yields at roughly 200 MPa — too
  close; a spring temper (301 full-hard, or 17-7 PH hardened after forming) sits far above. `[assumed]` typical
  values — take real yield from the supplier's mill certificate.
- **Tolerance:** force ∝ t³, so ±5 % on thickness is ~±15 % on force.
- **Head size:** force ∝ spread. At 9.6 × 1.0 mm (301): 10 mm spread → ~250 gf, 15 → ~375, 20 → ~500,
  25 → ~620 gf. 5th–95th %ile bitragion breadth spans ~25 mm `[S]`, so the relaxed opening sets who gets what.
