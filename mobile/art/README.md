# Original warehouse art production

Runtime assets are eight meter-scale, Y-up GLB files under `../assets/models/`.
Godot loads the exported GLBs and does not need Blender or Unity installed.

All meshes, signs, barcode linework and textures were authored procedurally for
this project. No reference-game artwork, photograph, model or extracted texture
is used. Dimensions and physical construction details are prototype estimates,
not surveyed dimensions of a real vehicle or claims of exact reference parity.

Rebuild with Python 3 + Pillow/NumPy and Blender 4.3.2:

```sh
python3 mobile/art/generate_textures.py
blender --background --factory-startup --python mobile/art/generate_assets.py
```

For an optional offline material review:

```sh
blender --background --factory-startup --python mobile/art/render_previews.py
```

That writes three CPU-rendered images and an editable preview `.blend` under
ignored `builds/mobile-art/`. These images use offline area lighting and are
**not Godot screenshots or mobile performance evidence**. Godot captures and
real iPhone/Android profiling are separate acceptance steps.

The JSON manifest records actual exported triangle counts, mesh/material groups,
file hashes, bounds and texture hashes. All texture maps are original 512px
albedo/roughness maps. Props are merged by material to limit draw submissions;
applications can reuse equally named materials between imported props. Safety
fences use sparse opaque bars to avoid layered transparent overdraw. Construction
ghost transparency is a separate runtime presentation.

The coordinate convention is ground-centered, with props facing +Z and belts
flowing from +Z to -Z in Godot. The truck origin is the rear opening at Z=0, with
its body extending toward -Z; its cargo deck is Y=1.15. The inspection bench top
is Y=0.95. The warehouse shell is an intentional roof/front cutaway for fixed
station cameras, with separate loading and export openings in the rear wall.

The shell exposes `CutawayRightWall` / `CUTAWAY_RIGHT_` children for camera-driven
cutaways. Preserve this grouping when editing/re-exporting: globally merging all
wall material surfaces would prevent hiding the foreground wall in overview.
