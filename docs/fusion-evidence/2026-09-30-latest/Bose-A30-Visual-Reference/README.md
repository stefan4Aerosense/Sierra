# Bose A30 visual reference

Created 2026-09-30 in the separate Fusion document `Bose A30 - Visual Reference - Approximate`.

Use `Bose-A30-Visual-Reference-FINAL.f3d`. The earlier archive is a preserved intermediate before material corrections.

This is an approximate external visual model, not Bose manufacturing CAD or an internal electronic model. It has 10 components and 40 positive-volume solid bodies: earcups, removable cushions, pivots/sliders, headband, crown pads, microphone boom/windscreen, and a shortened display cable with control module and dual GA plugs. No linked dependencies. Original H1 assembly was not edited.

Scale reference: Bose owner guide lists the retracted headset as 214 mm high x 161 mm wide x 80 mm deep. The model uses these as approximate anchors; pivot projections and the deployed microphone are outside the nominal shell envelope. Individual dimensions, contours, cushion openings, control positions, cable lengths and mechanical interfaces are estimates. Do not use these cushion dimensions to manufacture a fitted sensor insert without measuring the physical A30.

Sources:
- https://assets.bose.com/content/dam/Bose_DAM/Web/consumer_electronics/global/products/headphones/a30-aviation-headset/pdf/876056_og_a30-aviation-headset_en.pdf
- https://www.bose.com/p/aviation-headsets/bose-a30-aviation-headset/A30-HEADSETAVIA.html

Available aircraft wiring schematics do not define mechanical geometry. Internal circuit schematics are confidential and were not recreated.

Validation: all 40 bodies are solids with positive volume; preview inspected. No mechanical fit, joints, movement, manufacturing, or electronic functionality has been validated. Cloud save and native Fusion archive export succeeded. Fusion STEP export returned false twice; the incomplete STEP header is retained as `STEP-Export-Failed.txt`, not a usable STEP model.
