"""Convert a mesh file to GLB, run by Blender in the background.

Usage (via to-glb.cmd):  blender --background --python to_glb.py -- <input> [output.glb]

Fusion 360 cannot export GLB. It can export OBJ, FBX, STL and others, and Blender turns any of those into
a GLB in one step - keeping materials and colours when the source has them (OBJ with its .mtl, or FBX).
"""
import os
import sys

import bpy

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
if not argv:
    print("ERROR: no input file given")
    sys.exit(2)

src = os.path.abspath(argv[0])
dst = os.path.abspath(argv[1]) if len(argv) > 1 else os.path.splitext(src)[0] + ".glb"
ext = os.path.splitext(src)[1].lower()

if not os.path.isfile(src):
    print("ERROR: no such file: " + src)
    sys.exit(2)

# Start from an empty scene: the default cube would otherwise end up in the export.
bpy.ops.wm.read_factory_settings(use_empty=True)

def _import(path, extension):
    """Import, preferring Blender 4/5's current operators and falling back to the older names."""
    if extension == ".obj":
        try:
            bpy.ops.wm.obj_import(filepath=path)
        except AttributeError:
            bpy.ops.import_scene.obj(filepath=path)
    elif extension == ".fbx":
        bpy.ops.import_scene.fbx(filepath=path)
    elif extension == ".stl":
        try:
            bpy.ops.wm.stl_import(filepath=path)
        except AttributeError:
            bpy.ops.import_mesh.stl(filepath=path)
    elif extension == ".ply":
        try:
            bpy.ops.wm.ply_import(filepath=path)
        except AttributeError:
            bpy.ops.import_mesh.ply(filepath=path)
    elif extension in (".gltf", ".glb"):
        bpy.ops.import_scene.gltf(filepath=path)
    elif extension == ".dae":
        bpy.ops.wm.collada_import(filepath=path)
    elif extension == ".usd" or extension == ".usdz" or extension == ".usdc":
        bpy.ops.wm.usd_import(filepath=path)
    else:
        print("ERROR: cannot import " + extension + " - export OBJ or FBX from Fusion instead")
        sys.exit(3)

_import(src, ext)

objects = [o for o in bpy.context.scene.objects if o.type == "MESH"]
if not objects:
    print("ERROR: the file contained no mesh - check the export settings in Fusion")
    sys.exit(4)

tris = sum(len(o.data.loop_triangles) if o.data.loop_triangles else len(o.data.polygons) for o in objects)
print("imported %d mesh object(s), ~%d faces" % (len(objects), tris))

bpy.ops.export_scene.gltf(
    filepath=dst,
    export_format="GLB",
    export_apply=True,      # bake modifiers
    export_yup=True,        # glTF convention: Y up, which is what viewers expect
)

print("WROTE %s (%d bytes)" % (dst, os.path.getsize(dst)))
