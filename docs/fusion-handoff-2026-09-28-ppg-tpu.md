# Fusion handoff — 28 September 2026

## Current headset PPG cushion
Current Fusion document: **Headset PPG - Curved Cushion Prototype**. The user's actual rounded cushion was copied from Earcup Seal with Heart Rate Sensor. Current visible structural component: **11 ONE PIECE TPU cushion - integral push-in sleeve**. Earlier rigid sleeve, carrier, retaining plate and screws are hidden historical alternatives, NOT the intended print design.

User correction: the entire cushion is printed TPU. Sensor must push in from the flat underside into an integral sleeve. Current design is one connected TPU solid with two flexible keeper lips and eight optical apertures. Clear optical windows remain separate; opaque TPU must not cover the light paths. Latest edit rounds the outer shoulder R1.2 mm and optical-face edge R0.4 mm. A visible step remains: this is NOT a fully tangent/sculpted continuation of the cushion.

Provisional cavity 15.6 x 21 mm for provisional 15 x 20.4 mm sensor PCB; 0.3 mm nominal clearance per side, keeper lips 0.7 mm thick. Retention deliberately requires TPU deformation. TPU hardness/grade unspecified. No elastic simulation or physical fit testing performed. Sensor-carrier hard plate and screws are not needed in active design. Cable routing/strain relief, seal and optical window attachment still require development. Slicer supports and practical assembly need review before printing the complete cushion.

## Sensor identification and mechanical reference
Photos match MAXSENSORBLE + MAX86171_OSB, Molex 150150225 flex and PKCELL LP401230 3.7 V 105 mAh battery (MAX86171EVSYS / MAXREFDES280 family). Official schematics: https://www.analog.com/media/en/technical-documentation/data-sheets/MAX86171EVSYS.pdf pp21-25; board layouts pp26-30. Exact hardware revision not confirmed.

Fusion document MAX86171 Complete Sensor System - Provisional contains six components and 244 solids. Main PCB 35 x 18 x 0.8 mm estimated; optical PCB 15 x 20.4 x 0.8 mm from prior PDF reconstruction. Passives and connector geometry simplified. Not a routed PCB design or manufacturing CAD.

## Evidence and qualification limits
The latest smoothing operation succeeded and left a single solid. F3D, cushion-only STEP and STL exported. Earlier rigid sleeve withdrawal checks do NOT validate the new interference-retained TPU design. No optical/heart-rate performance validation follows from CAD. Existing KILO ear-placement measurements and unresolved signal-quality issues remain in force; this CAD trial position is not evidence of an optimal anatomical location. Do not use behind-ear placement or claim reliable pilot monitoring based on this model.

[Evidence files](fusion-evidence/2026-09-28-ppg-tpu/) include screenshots, validation logs, notes and SHA256 provenance.

Latest printable geometry: C:/Users/Stefan/Documents/Codex/2026-09-13/so-x20/outputs/PPG-Smoothed-TPU/Smoothed-TPU.stl and Smoothed-TPU.step. Fusion archive Smoothed-TPU.f3d includes hidden history. Earlier construction outputs: PPG-One-Piece-TPU, PPG-Bottom-Loading, Curved-Curved-PPG-Prototype. Large CAD binaries remain in Fusion backup workspace, not duplicated into KILO.

Other 28 September Fusion outputs: two-board flat-print clip enclosures in Clip-Case-Outside-Numbers and ADAU1860-Pi5-Port-Walls; M2.5 nut pockets, outward smooth panel numbers and port access. Fun-Cheese-Enclosure is a separate four-board yellow case with removable lid. These are prototype geometries with physical clip/port fit unverified. Modular building/bathroom work remains excluded.

## Backup status
Fusion cloud saves and local exports completed. FusionChatGPT Git staging failed with index.lock permission denied; remote publication remains pending. Updating this handoff does not publish those CAD binaries. KILO changes are prepared locally for the supplied PowerShell commit/push script.
