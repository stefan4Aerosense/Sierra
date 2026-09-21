# H1 mass, materials and centre-of-gravity audit

**Date:** 18 September 2026  
**Audited document:** H1 - Master Skeleton - DEV-B  
**Status:** Initial audit complete; finished-product mass and balance NOT validated.

## Executive finding

The current CAD material assignments and assembly membership are not sufficiently reliable to support a finished-headset weight claim. Two visible earcup-base bodies are assigned steel; four cushion-like bodies are assigned solid leather. These are major sources of misleading mass. The spring band remains provisional, as requested. No geometry or materials were changed during this audit.

The supplied materials compilation records a prototype criterion below 400 g. This audit neither demonstrates compliance nor establishes failure: the current CAD figures are not a validated product bill of materials. The historical 304 g value cannot be reproduced from this model without reconciling configuration and materials.

## Evidence and scope

- Live Fusion API inspection of 154 occurrences and 251 BRep body instances; one mesh body also exists in Tension Band V3.
- High-accuracy physical properties requested for BRep bodies and the root component.
- Material intent compared against `C:/Users/Stefan/Documents/GitHub/kilo/docs/h1-materials.md`, compiled 18 September 2026. Its underlying source documents were not independently revalidated in this audit.
- Evidence: `H1-Mass-Audit-2026-09-18-raw.json` and `H1-Mass-Audit-2026-09-18-View.png` alongside this report.
- This is the DEV-B skeleton assembly, not confirmation of every later change in separately saved component designs. Linked-component revisions and manufacturing BOM membership remain to be verified.

## Mass results: diagnostic values only

| Calculation | Mass | Interpretation |
|---|---:|---|
| Fusion root physical properties | 2,682.16 g | Assembly-level result; not accepted as finished product mass |
| Sum of all enumerated BRep body instances | 2,997.05 g | Includes hidden geometry and historical alternatives |
| Sum passing body/occurrence visibility filter | 537.39 g | Screening subtotal only; visibility is not a manufacturing BOM |

Root and enumerated-body totals differ by 314.89 g. The cause has not been resolved. Mesh treatment, assembly-context membership and historical geometry must be reconciled before accepting an aggregate. The visibility filter used native-body and occurrence visibility; it is not a substitute for validating each assembly-context body and released part quantity.

## Priority discrepancies

| Priority | Item | Observed CAD state | Required resolution |
|---|---|---|---|
| High | Left and right earcup bases | Steel; 123.925 g and 124.188 g | Confirm part mapping to the intended polymer structure, then assign actual supplier grade/density |
| High | Four cushion-like bodies | Leather, weathered; combined 143.191 g | Identify ear/top cushions and replace homogeneous leather mass with layered foam/gel/cover masses or weighed assemblies |
| High | Assembly membership | Hidden historical geometry and unexplained aggregate discrepancy | Establish one included/excluded BOM with quantities and approved component revisions |
| High | Spring band thickness | Skeleton parameter 1.0 mm; supplied specification 0.8 mm | Resolve revision before changing geometry; preserve current band for now |
| Medium | Generic steel bodies | Component4 (1):1 and Component7:1 contribute about 18.19 g each | Identify function and approved material; do not assume these are grommets |
| Medium | Two knob-related occurrences | Configuration 1:1 and Textured Knob - Left:1 each contribute 12.264 g | Confirm whether both belong in the assembly; equal mass alone does not prove duplication |
| Medium | Transparent/shell bodies | Visible acrylic bodies total 26.140 g, including an occurrence named OLD | Resolve active revision and distinguish transparent windows from polymer shell structure; PC versus PMMA remains open |
| Medium | Strap | Custom polyester material; 14.038 g | Validate against supplier linear mass or a weighed finished strap rather than solid-envelope density alone |
| Medium | Remaining small parts | PAEK, ABS, rubber, nylon and several steels present | Map each to the production materials list; some rubber joint parts already exist, so the older blanket steel-grommet finding is not assumed current |

### Ear-base sensitivity example

The two steel bases have approximately 31.607 cm³ combined volume. At their current steel assignment they contribute 248.113 g. Using 0.99 g/cm³ solely as an illustrative PA12 density already represented in this CAD file would give approximately 31.29 g, a reduction of 216.82 g for unchanged geometry.

This is not an approved production density, a prediction for glass-filled PA12, or a corrected headset weight. It shows why material reconciliation must precede weight-reduction redesign.

## Spring band: retain as provisional

The visible Tension Band V3 body is assigned generic Steel and reports 37.255 g. This differs from the older materials compilation's mention of a 17-4 PH library proxy. The skeleton thickness parameter is 1 mm, and observed cylindrical radii include 74.5 and 75.5 mm, consistent with a 1 mm separation on those surfaces; uniform thickness was not independently established.

The supplied list records 0.8 mm and an unresolved choice between 17-7 PH and 301 spring temper. Keep both alloy and thickness revision explicitly provisional. A thickness reduction cannot be approved from mass arithmetic alone: spring force, deflection, forming and fatigue requirements must also be checked. No previous strength-margin claim has been carried forward as validated evidence.

## Centre of gravity and comfort

Fusion reports root CG at **(3.104, -57.368, -41.961) mm** in the model coordinate system. The visibility-filtered body calculation gives **(1.254, -77.199, -50.180) mm**, using occurrence transforms. These coordinates are diagnostic only: the BOM, mass assignments and nested assembly transforms have not been fully reconciled.

No head-contact reference frame or validated wearing pose was established. Therefore neither coordinate set can substantiate a comfort offset, and the historical “CG 1.1 mm” cannot be compared without its axis, datum and configuration.

After correcting the BOM, define the headset centre plane, ear/pivot reference and head-contact datum in the wearing pose. Calculate total mass, left/right mass and front/back moment about the chosen support reference. Confirm with a weighed prototype and balance measurement. Comfort assessment should also include clamping force and cushion pressure; mass and CG alone do not establish multi-hour comfort.

## Completion checklist for a trustworthy mass budget

1. Confirm DEV-B is the intended audit baseline and reconcile linked component revisions.
2. Name and classify every physical part; exclude skeleton/reference geometry and superseded alternatives. Resolve the aggregate discrepancy and mesh membership.
3. Correct material assignments using approved supplier densities; leave unresolved materials clearly labelled as scenarios.
4. Account explicitly for drivers, PCBs, sensor hardware, microphone, wiring, connectors, fasteners, damping, adhesives, foam, gel and covers. This audit has not verified a complete mass allowance for those items; do not assume they are all absent or all included.
5. Record measured or supplier masses for bought-in and soft assemblies. Associate each with its installed position for CG calculations.
6. Recalculate mass and CG for the agreed worn configuration, defining whether the external cable and accessories are included.
7. Compare against the documented below-400-g prototype criterion, set a separate design margin, and corroborate against the physical prototype before making an ultra-light claim.

**Immediate next step:** reconcile the two earcup bases, cushion construction and included BOM. These are more consequential than selecting between the provisional spring-steel grades at this stage of the mass audit.
