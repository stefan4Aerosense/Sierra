# Fusion 360 → GLB

Fusion cannot export GLB. It exports OBJ, FBX, STL and others; this turns any of those into a GLB in one
command, using **Blender**, which is already installed on this machine. Nothing else to install, nothing
uploaded anywhere.

## Use it

```
tools\to-glb\to-glb.cmd "C:\path\H1 - Assembly.obj"
```

writes `H1 - Assembly.glb` beside the input. An optional second argument sets the output path.

## Exporting from Fusion first

In Fusion, right-click the component or body → **Save As Mesh**, or **File → Export**:

| Format | Use it when |
|---|---|
| **OBJ** | **Default choice.** Keeps per-body colours through its `.mtl` file — keep the `.mtl` next to the `.obj` |
| **FBX** | Also carries materials; use if OBJ gives trouble |
| **STL** | Geometry only, **no colour** — fine for printing, poor for a viewer |

**Set the mesh refinement to High** in the export dialog, or curved surfaces arrive faceted. GLB is a mesh
format: the smoothness is baked in at export, and no converter can add it back later.

## What it does

Imports the file into an empty Blender scene, applies modifiers, and exports GLB with Y up — the convention
glTF viewers expect, so the model is not lying on its side. Materials and colours carry across.

## Verified `[measured 2026-10-02]`

Converted an OBJ with a named material; the resulting GLB has the correct `glTF` magic, version 2, a declared
length matching the file, 1 mesh, 1 material named `AeroBlue`, and the base colour preserved
(0.12, 0.45, 0.62). Written by Khronos glTF Blender I/O v5.2.40.

If Blender ever moves, edit the `BLENDER=` line at the top of `to-glb.cmd`.
