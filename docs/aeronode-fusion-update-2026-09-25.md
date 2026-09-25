# AeroNode Fusion handoff - 25 September 2026

## Scope and provenance
Latest AeroNode presentation and RV-8 protective-packaging concept work from the Fusion workspace `C:/Users/Stefan/Documents/Codex/2026-09-13/so-x20`. This handoff does not claim a fresh Fusion inspection on 25 September. Modular building/bathroom work is explicitly excluded.

Evidence is in [the dated evidence folder](aeronode-fusion-evidence/2026-09-25/). SHA-256 hashes identify copied source reports and screenshots. CAD archives remain in the designated Fusion backup workspace/repository, https://github.com/stefan4Aerosense/FusionChatGPT; remote publication of these new artifacts remains unconfirmed.

## Latest protective concept: V3
User intent: eventual approved aircraft flight recorder for an RV-8, compact packaging, initially protecting the whole device. Electronics-engineer discussion and qualification basis remain pending.

A separate concept preserves the original AeroNode. V2 replaced the faceted shell with the AeroNode-like rounded rectangular silhouette: 105 x 132 x 44 mm body envelope, excluding raised logo and mounting tabs; 7 mm plan corner radius and 1.5 mm upper-edge fillet. Teal exterior, pearl base, original logo. Dimensions are provisional, not a demonstrated minimum.

V3 refines all four mounting tabs with 2 mm plan corner fillets, 0.4 mm edge/bore-entry fillets and pearl finish. Original hole centres and nominal 4.5 mm bores retained. Four tab bodies checked solid; screenshot inspected. Tabs visible again after being hidden in V2 styling views. No strength assessment performed.

Latest local archives in Fusion workspace outputs:
- `AeroNode-RV8-Rounded-Mounts-V3-CONCEPT.f3d`
- `AeroNode-RV8-Rounded-Mounts-V3-CONCEPT.step`
- `AeroNode-RV8-Before-Mount-Refinement.f3d` (historical snapshot)

Use F3D for the complete mesh-electronics assembly; STEP may omit PCB meshes.

## Material discussion - recommendation, not selection
Investigate titanium for the protective enclosure where weight matters, with stainless steel as an alternative. Commercial flight recorder memory enclosures use both; this is not an alloy or thickness specification. Pair any shell with engineered thermal protection and mechanically retained supports/closures.

A dedicated protected memory capsule inside a lighter electronics enclosure is recommended for investigation. It has NOT been implemented or accepted as the final architecture. Existing CAD appearances do not establish physical materials.

Primary references:
- https://www.l3harris.com/sites/default/files/2023-07/as-cas-brochure-avionics-fa2100.pdf
- https://aerospace2.honeywell.com/onlineordering/system-config/systemconfigDisplay?catalogId=10251&categoryId=35

## Unresolved engineering
No demonstrated crash survivability or approval. No impact, fire, crush, penetration, immersion, pressure, fatigue or comprehensive interference verification. Thermal allocation contains an existing overlap and is only a space claim. USB/Ethernet access remains enclosed/unresolved. Normal cooling, thermal insulation, memory architecture, seals, base/cover retention, mounting load paths, positive board retention and aircraft installation require engineering development. Current dimensions and materials must not be released for manufacture as a qualified recorder.

## Presentation animation
Original AeroNode REV B (not the RV-8 concept) has a locally rendered 12-second 1920x1080, 24 fps H.264 animation: exploded view, gradual assembly and slow rotation. Created in headless Blender from visible Fusion geometry; 94 meshes, electronics grouped. Presentation only, not a validated assembly simulation.

Local files under `outputs/AeroNode-Blender-Animation/`: `AeroNode-Assembly-1080p.mp4`, `AeroNode-Assembly.blend`, `AeroNode-Source.f3d`, `README.md` and verification records. Large binaries are not duplicated into KILO.
