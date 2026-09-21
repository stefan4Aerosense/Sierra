# H1 Fusion handoff — 21 September 2026

This handoff consolidates completed headset work from the FusionChatGPT conversation. It does not report new Fusion edits or a fresh measurement on 21 September.

## Provenance

Source workspace: `C:/Users/Stefan/Documents/Codex/2026-09-13/so-x20`.
Source repository: https://github.com/stefan4Aerosense/FusionChatGPT
Local source HEAD: `203ddb2c886542bec54ac67ba4495d177e84ff89` (remote containment not checked during this handoff).
Supporting reports are copied into [h1-fusion-evidence/2026-09-21](h1-fusion-evidence/2026-09-21/). Copies retain original dates and historical backup-status statements; those statements are not current push confirmations. Update evidence by recopying from the source, preserving provenance.

## Master skeleton

[recorded] Separate Fusion document **H1 - Master Skeleton - DEV-B** preserves the original H1 assembly. Initial skeleton includes 22 parameters, eight datum planes, cup layouts and pivot references, with target band radius 73.8 mm.

This is an initial controlling layout, **not a completed conversion of all downstream components to skeleton-driven geometry**. Linked dependencies remain; archive self-containment is not verified. See the copied creation report for scope and limitations.

Source CAD artifacts in FusionChatGPT `outputs/`: `H1-Master-Skeleton-DEV-B.f3d`, `H1-Master-Skeleton-Only-DEV-B.f3d`. CAD binaries remain in the designated Fusion backup repository; this Kilo update provides documentation and evidence links.

## Mass/materials audit — 18 September

[measured in prior API audit] DEV-B had 154 occurrences, 251 enumerated BRep body instances and one mesh body. Root mass 2,682.16 g; all-body sum 2,997.05 g; visibility-filtered subtotal 537.39 g. **None is a validated finished-headset mass.** Root/body discrepancy 314.89 g remains unresolved; visibility is not a released BOM.

- Two earcup bases assigned steel: combined 248.113 g.
- Four cushion-like bodies assigned solid leather: combined 143.191 g, not validated layered foam/gel/cover mass.
- Generic and historical bodies require inclusion/material review.
- Root CG is only a model-coordinate diagnostic; wearing reference and complete mass budget are not established.
- Historical 304 g / CG 1.1 mm cannot be reproduced or used to claim compliance. The documented below-400-g prototype criterion remains unverified.

No material or geometry changes were made in that audit. Full methodology and limitations: [audit report](h1-fusion-evidence/2026-09-21/H1-Mass-Materials-Audit-2026-09-18.md).

## Spring band — unresolved, retain current geometry

[measured in audit] DEV-B skeleton thickness is 1.0 mm; visible Tension Band V3 uses generic Steel and reports 37.255 g. [source record] The materials list records 0.8 mm and unresolved 17-7 PH versus 301 spring temper. These are conflicting revision states, not an approved geometry change.

[conversation] Supplier offered stainless and manganese steel, describing corrosion/cost tradeoffs. The screenshot did **not** identify the exact manganese grade. Kilo's separate mention of 65Mn must not be attributed to this screenshot without additional evidence.

[advice, not a decision] Request exact grade, temper, hardness, heat treatment, coating, thickness tolerance, finished mass, clamping-force and fatigue/corrosion evidence; obtain samples before selecting. 301 spring-temper stainless remains a candidate alongside 17-7 PH. No alloy was approved in this conversation.

Kilo separately records a band-force estimate (~203 gf at current width/0.8 mm, with larger thickness or width suggested). This handoff does not independently validate that calculation or adopt its dimensions. Reconcile geometry, boundary conditions and target force, then confirm experimentally before revising the band.

## Sensor board and cushion fit

[recorded] **MAX86171 OSB - Mechanical Reference - Provisional**: 37 components, 126 solid bodies, simplified editable BRep representation reconstructed from the supplied EVSYS PDF. Outline approximately 15 × 20.4 mm; thickness 0.8 mm assumed. It is not routed electronics or a fabrication-ready board. Source archive/STEP: `outputs/MAX86171-OSB-Mechanical-Reference.f3d` and `.step` in FusionChatGPT. See copied reference notes for dimensional uncertainties.

[recorded and prior geometric checks] **Earcup Seal - Integrated Sensor Fit V2** uses the user's revised oval cushion. Sensor and surround shifted together -3 mm X, +1 mm Y, -0.4 mm Z. Pocket cross-section checked within cushion outline; no positive-volume sensor/surround/cushion intersections; resulting cushion solid. Original geometry preserved. See copied validation JSON and notes.

This verifies a concept fit, **not optical performance, skin-contact comfort, retention, cable routing or tooling readiness**. Sensor source remains linked; self-contained assembly export not verified. Source: `outputs/Earcup-Integrated-Sensor-Fit-V2.f3d` in FusionChatGPT.

## Immediate next work

1. Reconcile included assembly parts and linked revisions; correct earcup-base and cushion mass representations.
2. Account for electronics, sensor, drivers, wiring, microphone, connectors, damping, adhesives and soft goods using supplier/measured masses.
3. Resolve band revision and validate force/fit with samples before changing it.
4. Recalculate mass and CG in a defined wearing pose; corroborate with a physical prototype.
5. Validate actual sensor-board dimensions and optical/contact behaviour before final cushion tooling.

The heart-rate tester software in Kilo was not changed by this handoff. No other repository/team inbox was updated.
