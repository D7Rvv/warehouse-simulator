# 3D asset review — 2026-10-27

These are original, editable/generated warehouse props for the station slice.
They are a production improvement over plain primitive blockout, **not a signed
off claim of official-game-equivalent artwork**. The three original user images
are not available as image files in this workspace, and the official Steam page
was blocked by the environment network policy. No blocked source was bypassed.

## Actual checks and corrections

- Blender 4.3.2 generated and exported all eight GLBs successfully. Total unique
  geometry is 23,812 triangles; the runtime model files total about 4.9MB.
- Model/file hashes and actual geometry/material counts are recorded in
  `../assets/ART_MANIFEST.json`. Original albedo and roughness maps are 512px.
- Materials are split into kraft paper, dark rubber, blue paint, galvanized
  sheet, brushed steel and safety paint. Geometric edge bevels provide highlights
  without requiring a high-frequency normal texture on every object.
- Conveyor legs reach the frame, tyre bases meet the ground, bench feet have
  shoes, and gate/rack posts have baseplates. Small opaque contact details avoid
  many layered transparent blob shadows.
- Truck side braces, cargo floor, rear threshold and tail lamps, metal conveyor
  guard rails, fasteners, drive cover, operator button, rack braces, pallet
  boards and safety guards are actual geometry. They are not image backdrops.
- A real dimensional defect was found: the conveyor's motor/control extended
  outside its one-cell placement footprint. They were moved inside the cell,
  re-exported and re-measured. Current X bounds are -0.49..0.50m and Z bounds are
  -0.50..0.50m. The generator now rejects an oversized conveyor footprint.
- Three offline views were rendered and visually inspected: inspection bench,
  inbound truck interior and export gate/conveyor. Initial framing cut off the
  truck roof and gate title, so the cameras were moved back and the views were
  rendered again. The complete structures and export sign are now in frame.
- The installed Blender build has no OpenImageDenoise support. The failed
  denoising attempt was corrected to CPU Cycles, 32 samples, denoising disabled.
  The preview grain is offline sampling noise and is not a real-time Godot effect.

- Integration review of actual Godot overview/build captures found that the
  foreground right wall obscured the work area. The shell was re-exported with
  a separate `CutawayRightWall` node and `CUTAWAY_RIGHT_` material-mesh children.
  The world presenter can hide that wall, plinth and supports in overview/build
  while retaining the enclosure for close station views. This adds four small
  mesh groups, without increasing triangle count; final live captures verify
  the visibility policy separately.

## Evidence and remaining checks

`builds/mobile-art/{inspection_review,inbound_review,export_review}.png` and
`export-review.blend` are the actual offline review outputs. These images use
area-lighting from the review scene; **they are not Godot render captures and do
not establish iPhone/Android frame rate, heat, memory, or final visual quality**.

Godot must separately check GLB import, material sharing, texture interpretation,
camera framing, label overlays and live interaction. Real phone rendering,
30-minute thermal testing, LOD decisions, final baked lighting, finer surface
wear and comparison against the original reference images remain open. The
truck's dimensions and engineering details are authored prototype estimates,
not measurements of a specific real truck.
