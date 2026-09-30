# H1 assembly animation - revision 2

This revision replaces the crossing exploded-view movements in the first animation. The original video and scene remain preserved in H1-Assembly-Cinematic-2026-09-30.

## Files
- H1-Assembly-360-Revision2-20s-1080p.mp4: silent 20-second, 1920 x 1080, 24 fps animation.
- H1-Assembly-Cinematic-Revision2.blend: editable Blender scene with embedded geometry and procedural materials; self-contained.
- Fusion-Source-Current.f3d, geometry.json and Export-Report.json: unchanged source capture from H1 - Assembly - Updated.
- Assembly-Plan.json, Collision-Validation.json, Scene-Validation.json and Video-Validation.json: validation and choreography records.

## Changed choreography
The headband, strap, pivots, outer shells and microphone mounting housing remain a preassembled outer module. Each internal carrier travels as a rigid subassembly with its internal plate and existing screws. The right and left carriers install sequentially through the open inner faces. Each cushion's three related bodies remain together. Cushions install sequentially after the carriers.

Every moving group first aligns laterally in open space, pauses, then seats axially along the approximately 12.5-degree earcup mounting direction. No shell moves through the headband, no cushion follows the former diagonal through-shell path, and no loose fasteners fly through other components. No CAD mesh has been deformed or redesigned. All groups finish at the original exported positions. The same cinematic lighting and complete 360-degree turn are retained.

## Validation and limits
All 480 rendered frames were checked with triangle-mesh BVH surface-intersection tests between groups with relative motion. No new intersecting body pairs were detected. Existing source-CAD contact pairs remain near final seating; the maximum relative displacement at such contact is 3.751 mm. The carrier/fabric-strap pair at frame 104 is the only event beyond the initial 3.5 mm review threshold, and remains explicitly recorded rather than erased from the report.

The checks are sampled surface-intersection checks, not continuous solid-volume collision detection. Rigid subassembly interiors and already-intersecting source CAD are not repaired here. This is a more plausible assembly presentation, not a verified factory assembly procedure or tolerance study. Fasteners are retained with their carriers rather than demonstrating unverified screw/hole insertion or torque.

Fusion reports the same eight unavailable washer/screw references documented in Export-Report.json. All 65 exportable visible bodies are included. The original Fusion design was not changed. The Blender scene is self-contained; restoration of the Fusion archive's unavailable linked references is not guaranteed.
