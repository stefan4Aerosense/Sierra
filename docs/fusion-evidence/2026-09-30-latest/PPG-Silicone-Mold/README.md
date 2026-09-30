# Curved cushion silicone casting mold - nominal 1 mm

## Files and print orientation
Print only these four STLs, in millimetres at 100% scale. They are already oriented with their intended bed face at Z=0:

- 01-Core-Plate.stl: 112 x 132 x 30 mm; flat base down, core and integral alignment pins up.
- 02-Lower-Left.stl: 56 x 132 x 9 mm; flat underside down.
- 03-Lower-Right.stl: 56 x 132 x 9 mm; flat underside down.
- 04-Upper-Cavity.stl: 112 x 132 x 15 mm; flat outside face down, cavity up.

Individual STEP exports use the same print orientations. Mold-Assembly.step uses assembled coordinates and also contains hidden reference/casting components; do not print the entire assembly. Silicone-Casting-1mm.step is the intended flexible result, not a printed mold part.

Silicone-Mold-1mm-FINAL.f3d is the completed self-contained Fusion archive. Before-Cushion.f3d preserves the user's original project. Silicone-Cushion-Mold-1mm.f3d is an earlier intermediate study, not the final print source. Fusion cloud document: Headset PPG - Silicone Cushion Mold - 1mm.

## Geometry and scope
The original curved cushion profile is retained. A Fusion inward shell operation of 1.00 mm supplied the skin/core surfaces; this is a normal offset, not a scaled-down cushion. The sensor recess was cut after shelling. The underside is open for inserting foam. The user approved replacing the integral TPU sleeve with an open recess for a separate sensor-retaining insert. The recess also opens through the outer edge to remove a trapped core. A retaining insert, foam, bottom closure, and optical window are NOT included in these print files.

Nominal silicone volume is 10.237 mL, excluding the gate, vents, flash and handling waste. Select and mix material using its supplier's instructions; do not equate millilitres with grams without its density. No shrinkage compensation is applied. Actual cured thickness depends on print accuracy, mold finishing, clamping, material and cure. Cast and measure a first sample before treating 1 mm as achieved.

## Printing and preparation
Use a fine layer height on the cavity and core, approximately 0.12-0.16 mm as a starting point for a 0.4 mm nozzle. Keep all mold surfaces watertight, and preview for unsupported areas before printing. Do not put support scars on the forming surfaces if avoidable. Layer lines will transfer to silicone. Any sanding or coating alters the 1 mm gap; finish lightly and verify thickness afterward.

Pins are nominal diameter 5 mm (three) and 4 mm (one asymmetric key). Upper sockets provide 0.20 mm radial clearance. Lower pieces have open guide slots so they can slide off sideways. Dry-fit first and clean any elephant-foot or burrs; do not force the pins. Parting lands contact directly and may need careful flattening to prevent leakage. External clamps or straps are needed; no clamp hardware is included.

Choose a castable silicone with suitable tear strength, viscosity and documented suitability for the intended skin-contact use. Test a small cure/release sample against the actual printed and finished material. Follow the silicone and release-agent manufacturers' instructions. No material compatibility, skin-contact qualification or casting trial has yet been performed.

## Assembly, filling and removal
1. Place the core plate (01) flat, core facing up.
2. Slide lower left (02) and lower right (03) into position along their open guide slots. Bring their outer edges flush with the base. Check all parting lands are seated.
3. Fit upper cavity (04) over the keyed alignment pins. Clamp evenly without bending the plates.
4. The large 5 mm side port at the split line is the fill gate. The five 0.8 mm channels are vents. Keep vents unobstructed. For a trial fill, orient the gate low and the opposite side high; inject compatible low-viscosity silicone gently with suitable dispensing equipment. Do not use high pressure to overcome blocked flow. Thin-gap filling and air evacuation need a physical trial; CAD does not establish bubble-free filling. Follow supplier guidance on working time and degassing.
5. After full cure, release the clamps and lift upper cavity (04) straight up off the pins.
6. Slide lower left (02) to the left and lower right (03) to the right. Do not try to lift them over the curved core.
7. Carefully peel the flexible silicone off the remaining core, using the open underside and sensor recess. Trim gate/vent flash and inspect for tears, bubbles, thin spots and thickness variation.
8. Insert foam through the open underside. Sensor retention and any final underside attachment/closure need the separate insert/backing design.

## Verification and limitations
All four printable parts are single connected CAD solids. Exported STLs have one connected component, closed manifold edges and positive signed volume. CAD pairwise assembled intersections were zero. Rigid opening paths were sampled at 1, 3, 8, 20 and 60 mm (upper upward, lower halves sideways) with zero reported intersections. This is a sampled check, not continuous collision analysis or a physical demolding test. Silicone peeling forces, durability, printing accuracy and flow have not been validated.

The mold is mechanical tooling only. It does not establish heart-rate sensor accuracy or suitability for behind-ear measurements.
