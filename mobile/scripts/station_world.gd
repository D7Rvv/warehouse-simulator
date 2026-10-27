class_name StationWorld
extends Node3D
## Persistent, presentation-only warehouse. Cameras and ghosts never own gameplay data.

const MODEL_ROOT := "res://assets/models/"
const STATION_ANCHORS := {
	"LOC-WAREHOUSE-OVERVIEW": "CAM_OVERVIEW", "LOC-BUILD-01": "CAM_OVERVIEW",
	"LOC-SHOP": "CAM_OVERVIEW", "LOC-ECONOMY": "CAM_OVERVIEW",
	"LOC-INBOUND-01": "CAM_INBOUND", "LOC-INSPECTION-01": "CAM_INSPECTION",
	"LOC-EXPORT-AIR-01": "CAM_EXPORT_AIR", "LOC-SORTING-01": "CAM_SORTING",
	"LOC-STORAGE-01": "CAM_STORAGE", "LOC-YARD-01": "CAM_YARD", "LOC-CONTRACT": "CAM_CONTRACT"
}
const ANCHORS := {
	"CAM_OVERVIEW": {"position": Vector3(11.5, 12.5, 14.5), "focus": Vector3(0, 0, -.5), "min": 11.0, "max": 26.0, "pan": true, "zoom": true, "offset": 2.0},
	"CAM_INBOUND": {"position": Vector3(-4, 3.3, -1.9), "focus": Vector3(-4, 1.95, -8.0), "min": 4.0, "max": 8.0, "pan": false, "zoom": false, "offset": .55},
	"CAM_INSPECTION": {"position": Vector3(-2.95, 2.35, 4.6), "focus": Vector3(-4.3, 1.1, 2.2), "min": 1.7, "max": 4.7, "pan": false, "zoom": true, "offset": .36},
	"CAM_EXPORT_AIR": {"position": Vector3(5.65, 2.7, .3), "focus": Vector3(5, 1.5, -5.1), "min": 3.0, "max": 8.0, "pan": false, "zoom": false, "offset": .7},
	"CAM_SORTING": {"position": Vector3(1.5, 1.8, 2.4), "focus": Vector3(0, .55, 0), "min": 1.7, "max": 5.0, "pan": false, "zoom": false, "offset": .36},
	"CAM_STORAGE": {"position": Vector3(1.4, 2.8, 7.1), "focus": Vector3(5, 1.5, 3.0), "min": 3.0, "max": 8.0, "pan": false, "zoom": false, "offset": .6},
	"CAM_YARD": {"position": Vector3(5, 8.0, -15), "focus": Vector3(-4, 1, -9.0), "min": 8.0, "max": 18.0, "pan": false, "zoom": false, "offset": .9},
	"CAM_CONTRACT": {"position": Vector3(5, 8.0, -15), "focus": Vector3(-4, 1, -9.0), "min": 8.0, "max": 18.0, "pan": false, "zoom": false, "offset": .9}
}

var camera: Camera3D
var last_error := ""
var active_station_id := ""
var camera_switch_count := 0
var assets_loaded: Dictionary = {}
var missing_assets: Array[String] = []
var cargo_visual_count := 0
var placed_equipment_count := 1
var ghost_visible := false
var ghost_arrow_direction := Vector3.FORWARD
var last_ghost_reason := ""
var _built := false
var _sections: Dictionary = {}
var _entities: Dictionary = {}
var _poses: Dictionary = {}
var _shared_materials: Dictionary = {}
var _asset_scenes: Dictionary = {}
var _mesh_cache: Dictionary = {}
var _mesh_instances: Array[MeshInstance3D] = []
var _colliders: Dictionary = {}
var _inspection_proxy: Node3D
var _inspection_label: Label3D
var _ghost: Node3D
var _ghost_direction: Node3D
var _ghost_surfaces: Array[MeshInstance3D] = []
var _ghost_coord: Label3D
var _grid: MeshInstance3D
var _valid_ghost: StandardMaterial3D
var _invalid_ghost: StandardMaterial3D
var _focus := Vector3.ZERO
var _profile: Dictionary = {}
var _shell_cutaway_meshes: Array[Node3D] = []
var _preview_blockers: Array[Rect2] = []
var _preview_blocker_nodes: Array[Node3D] = []
var _contact_material: StandardMaterial3D
var _key_light: DirectionalLight3D
var _environment: WorldEnvironment

func build(fixture: Dictionary) -> void:
	if _built:
		return
	_built = true
	name = "WarehouseWorld"
	for key in ["Shell", "Inbound", "Inspection", "Export", "Sorting", "Storage", "Yard"]:
		var section := Node3D.new()
		section.name = key
		add_child(section)
		_sections[key] = section
	_setup_lighting()
	_setup_shell()
	_setup_inbound(fixture)
	_setup_inspection()
	_setup_export(fixture)
	_setup_sorting()
	_setup_storage()
	_setup_yard()
	_setup_grid_and_ghost()
	for section in _sections.values():
		_merge_contact_patches(section)
	camera = Camera3D.new()
	camera.name = "StationCamera"
	camera.near = .08
	camera.far = 85.0
	camera.fov = 48.0
	camera.current = true
	add_child(camera)
	_cache_meshes(self)
	switch_station("LOC-WAREHOUSE-OVERVIEW")

func _setup_lighting() -> void:
	_environment = WorldEnvironment.new()
	_environment.name = "StaticWarehouseEnvironment"
	var environment := Environment.new()
	var sky := Sky.new()
	var sky_material := ProceduralSkyMaterial.new()
	sky_material.sky_top_color = Color(.2, .3, .43)
	sky_material.sky_horizon_color = Color(.75, .79, .8)
	sky_material.ground_bottom_color = Color(.12, .15, .17)
	sky_material.ground_horizon_color = Color(.48, .51, .54)
	sky.sky_material = sky_material
	sky.radiance_size = Sky.RADIANCE_SIZE_128
	sky.process_mode = Sky.PROCESS_MODE_QUALITY
	environment.sky = sky
	environment.background_mode = Environment.BG_COLOR
	environment.background_color = Color(.17, .22, .27)
	environment.ambient_light_source = Environment.AMBIENT_SOURCE_SKY
	environment.ambient_light_energy = .42
	environment.reflected_light_source = Environment.REFLECTION_SOURCE_SKY
	environment.tonemap_mode = Environment.TONE_MAPPER_LINEAR
	_environment.environment = environment
	add_child(_environment)
	_key_light = DirectionalLight3D.new()
	_key_light.name = "SingleStaticSun"
	_key_light.rotation_degrees = Vector3(-50, -35, 0)
	_key_light.light_color = Color(1.0, .94, .85)
	_key_light.light_energy = 1.28
	_key_light.shadow_enabled = false
	add_child(_key_light)

func _setup_shell() -> void:
	var shell := _add_asset("warehouse_shell", _sections.Shell, Vector3.ZERO, Vector3(16, .18, 14), Color(.48, .5, .49))
	_find_cutaway_meshes(shell)
	_sign(_sections.Shell, "WAREHOUSE  /  STATION DEMO", Vector3(0, 4.7, -6.82), .006, 50)

func _setup_inbound(fixture: Dictionary) -> void:
	var truck := _add_asset("truck", _sections.Inbound, Vector3(-4, 0, -6.3), Vector3(2.6, 3.6, 7), Color(.74, .76, .75))
	# Pick only the cab, never a closed hull across the open cargo bay: parcels remain tappable.
	_register_entity(truck, "DEV-RUN-001", "LOC-INBOUND-01", Vector3(2.7, 2.4, 1.8), Vector3(0, 1.35, -5.6))
	# The cargo floor is 1.15m high: a real dock/bridge and access steps support it.
	var dock := Node3D.new()
	dock.name = "RaisedDockAndAccess"
	_sections.Inbound.add_child(dock)
	var dock_concrete := _material("dock_concrete", Color(.29, .31, .3), .96)
	var dock_steel := _material("dock_checker_plate", Color(.24, .27, .28), .65)
	var safety_yellow := _material("dock_safety_edge", Color(.88, .66, .12), .73)
	_box(dock, "RaisedDockBase", Vector3(-4, .55, -5.3), Vector3(3, 1.1, 2), dock_concrete)
	_box(dock, "DockSteelDeck", Vector3(-4, 1.125, -5.3), Vector3(3, .05, 2), dock_steel)
	_box(dock, "BridgePlateToTruck", Vector3(-4, 1.154, -6.3), Vector3(2.5, .028, .34), dock_steel)
	_box(dock, "SafetyEdgeLeft", Vector3(-5.48, 1.157, -5.3), Vector3(.055, .015, 2), safety_yellow)
	_box(dock, "SafetyEdgeRight", Vector3(-2.52, 1.157, -5.3), Vector3(.055, .015, 2), safety_yellow)
	for step in range(5):
		var height := 1.15 * float(5 - step) / 6.0
		_box(dock, "DockAccessStep", Vector3(-2.36 + step * .28, height * .5, -5.1), Vector3(.28, height, .92), dock_concrete)
		_box(dock, "StepSafetyNosing", Vector3(-2.23 + step * .28, height + .007, -5.1), Vector3(.035, .014, .92), safety_yellow)
	_merge_static_boxes(dock)
	_cache_preview_footprint(truck)
	_cache_preview_footprint(dock)
	var runs: Array = fixture.get("mockTruckRuns", [])
	var cargo: Array = runs[0].get("cargoParcelIds", []) if not runs.is_empty() else []
	for i in cargo.size():
		var id := str(cargo[i])
		var x := -.63 if i % 2 == 0 else .5
		var z := -.7 - float(i / 2) * 1.15
		var parcel := _add_asset("parcel", _sections.Inbound, Vector3(-4 + x, 1.15, -6.3 + z), Vector3(.62, .45, .48), Color(.62, .44, .26))
		parcel.rotation.y = .045 * (i - 1)
		_register_entity(parcel, id, "LOC-INBOUND-01", Vector3(.64, .47, .5), Vector3(0, .225, 0))
		# Replace the SAMPLE caption on its physical card, leaving the barcode unobscured.
		_box(parcel, "ParcelIdPaperCaption", Vector3(.13, .28, .253), Vector3(.18, .045, .002), _material("fixture_paper", Color(.91, .9, .83), .94))
		var id_text := _sign(parcel, id.right(3), Vector3(.13, .28, .256), .0013, 22)
		id_text.modulate = Color(.035, .04, .045)
		id_text.outline_size = 0
		_contact_shadow(_sections.Inbound, Vector3(-4 + x, 1.151, -6.3 + z), Vector2(.8, .65))
		cargo_visual_count += 1
	_sign(_sections.Inbound, "DOCK 01   /   INBOUND", Vector3(-4, 4.35, -6.0), .005, 44)

func _setup_inspection() -> void:
	var bench := _add_asset("inspection_bench", _sections.Inspection, Vector3(-4, 0, 2), Vector3(2.8, .95, 1.25), Color(.15, .31, .36))
	_register_entity(bench, "DEV-INSPECTION-01", "LOC-INSPECTION-01", Vector3(2.9, 1.0, 1.35), Vector3(0, .5, 0))
	_cache_preview_footprint(bench)
	_inspection_proxy = Node3D.new()
	_inspection_proxy.name = "InspectionDisplayOnly_NotParcelOwner"
	_sections.Inspection.add_child(_inspection_proxy)
	_inspection_proxy.position = Vector3(-4.4, .955, 2.29)
	var parcel := _add_asset("parcel", _inspection_proxy, Vector3.ZERO, Vector3(.62, .45, .48), Color(.65, .47, .29))
	_register_entity(parcel, "DEV-INSPECTION-DISPLAY", "LOC-INSPECTION-01", Vector3(.64, .47, .5), Vector3(0, .225, 0))
	# A physical front-face paper card shares the exact selected fixture text with the HUD.
	_box(_inspection_proxy, "VisibleFixtureShippingPaper", Vector3(0, .25, .254), Vector3(.52, .26, .004), _material("fixture_paper", Color(.91, .9, .83), .94))
	_inspection_label = _sign(_inspection_proxy, "DISPLAY ONLY\nROAD / ALDEN\n4,000 g", Vector3(0, .25, .257), .0016, 28)
	_inspection_label.modulate = Color(.035, .04, .045)
	_inspection_label.outline_size = 0
	_contact_shadow(_sections.Inspection, Vector3(-4, .012, 2), Vector2(3.0, 1.5))
	_contact_shadow(_sections.Inspection, Vector3(-4.4, .956, 2.29), Vector2(.82, .66))

func _setup_export(fixture: Dictionary) -> void:
	var gate := _add_asset("export_gate", _sections.Export, Vector3(5, 0, -5.6), Vector3(3.2, 3.5, 2.2), Color(.18, .35, .44))
	_register_entity(gate, "DEV-GATE-AIR-01", "LOC-EXPORT-AIR-01", Vector3(3.2, 3.5, 2.2), Vector3(0, 1.75, 0))
	_cache_preview_footprint(gate)
	# The GLB already has its original AIR / EXPORT 01 header. Do not double-label it.
	_contact_shadow(_sections.Export, Vector3(5, .012, -5.6), Vector2(3.5, 2.8))
	var fixture_gate: Dictionary = fixture.get("fakeExportGate", {})
	var buffered := int(fixture_gate.get("currentBufferedCount", 0))
	for i in buffered:
		var parcel := _add_asset("parcel", _sections.Export, Vector3(5 + .16 * (i % 2), .83, -4.7 - .6 * (i / 2)), Vector3(.62, .45, .48), Color(.63, .45, .28))
		parcel.name = "GateDisplaySample_NotAnOwnedParcel_%d" % i
	_box(_sections.Export, "CurrentBufferDisplay", Vector3(5, 2.5, -4.43), Vector3(2.35, .3, .035), _material("buffer_display", Color(.025, .055, .065), .86))
	_sign(_sections.Export, "BUFFER %d / %d  [DEMO]" % [buffered, int(fixture_gate.get("maxCapacity", 0))], Vector3(5, 2.5, -4.405), .0027, 34)

func _setup_sorting() -> void:
	var belt := _add_asset("conveyor", _sections.Sorting, Vector3.ZERO, Vector3(.88, .82, 1), Color(.08, .3, .46))
	_register_entity(belt, "DEV-BELT-0", "LOC-SORTING-01", Vector3(.9, .86, 1), Vector3(0, .43, 0))
	_contact_shadow(_sections.Sorting, Vector3(0, .012, 0), Vector2(1.2, 1.4))
	_sign(_sections.Sorting, "IN", Vector3(0, .88, .38), .0026, 32).rotation_degrees.x = -90
	_sign(_sections.Sorting, "OUT", Vector3(0, .88, -.38), .0026, 32).rotation_degrees.x = -90

func _setup_storage() -> void:
	var rack := _add_asset("rack", _sections.Storage, Vector3(5, 0, 3), Vector3(2.8, 3.2, 1.1), Color(.14, .3, .42))
	_register_entity(rack, "DEV-RACK-01", "LOC-STORAGE-01", Vector3(2.8, 3.2, 1.1), Vector3(0, 1.6, 0))
	_cache_preview_footprint(rack)
	var pallet := _add_asset("pallet", _sections.Storage, Vector3(3.1, 0, 3.2), Vector3(1.2, .16, .8), Color(.48, .34, .2))
	_cache_preview_footprint(pallet)
	_contact_shadow(_sections.Storage, Vector3(5, .012, 3), Vector2(3.1, 1.5))
	_contact_shadow(_sections.Storage, Vector3(3.1, .012, 3.2), Vector2(1.1, 1.4))

func _cache_preview_footprint(prop: Node3D) -> void:
	# Presentation context only: conservative stationary geometry AABB, cached once.
	# This neither occupies the fixture grid nor implements production GridService.
	var meshes: Array[MeshInstance3D] = []
	_collect_meshes(prop, meshes)
	var combined := AABB()
	var has_bounds := false
	for mesh in meshes:
		if mesh.mesh == null:
			continue
		var transform_to_world := Transform3D.IDENTITY
		var ancestor: Node = mesh
		while ancestor != self and ancestor != null:
			if ancestor is Node3D:
				transform_to_world = (ancestor as Node3D).transform * transform_to_world
			ancestor = ancestor.get_parent()
		var bounds: AABB = transform_to_world * mesh.get_aabb()
		combined = combined.merge(bounds) if has_bounds else bounds
		has_bounds = true
	if has_bounds:
		_preview_blockers.append(Rect2(combined.position.x, combined.position.z, combined.size.x, combined.size.z))
		_preview_blocker_nodes.append(prop)

func preview_blocking_reason(x: int, z: int, floor: int = 0) -> String:
	if floor != 0:
		return "表示用の設備・構造物と干渉しています"
	# A tiny inset excludes exact edge contact and floating-point GLB epsilon.
	var footprint := Rect2(float(x) - .4999, float(z) - .4999, .9998, .9998)
	for index in _preview_blockers.size():
		var prop := _preview_blocker_nodes[index]
		if is_instance_valid(prop) and not prop.is_queued_for_deletion() and footprint.intersects(_preview_blockers[index]):
			return "表示用の設備・構造物と干渉しています"
	return ""

func _setup_yard() -> void:
	var asphalt := _material("yard_asphalt", Color(.16, .18, .18), .95)
	_box(_sections.Yard, "YardRoad", Vector3(0, -.16, -13), Vector3(23, .16, 14), asphalt)
	var roadpaint := _material("road_paint", Color(.76, .71, .46), .85)
	for x in [-8, -1, 6]:
		_box(_sections.Yard, "RoadMark", Vector3(x, -.011, -13), Vector3(.1, .01, 9), roadpaint)
	_sign(_sections.Yard, "YARD / SCHEDULE NOT IMPLEMENTED", Vector3(-4, .08, -16), .005, 48).rotation_degrees.x = -90

func _contact_shadow(parent: Node3D, at: Vector3, footprint: Vector2) -> void:
	# Static local contact approximation, not real-time shadows or a baked lightmap.
	# One 64px original radial texture is shared by all bounded small floor quads.
	if _contact_material == null:
		var image := Image.create(64, 64, false, Image.FORMAT_RGBA8)
		for y in 64:
			for x in 64:
				var u := (float(x) + .5) / 32.0 - 1.0
				var v := (float(y) + .5) / 32.0 - 1.0
				var fade := maxf(0, 1.0 - u * u - v * v)
				image.set_pixel(x, y, Color(0, 0, 0, .34 * fade * fade))
		_contact_material = StandardMaterial3D.new()
		_contact_material.resource_name = "OriginalStaticContact_64px"
		_contact_material.albedo_texture = ImageTexture.create_from_image(image)
		_contact_material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
		_contact_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
		_contact_material.depth_draw_mode = BaseMaterial3D.DEPTH_DRAW_DISABLED
		_contact_material.texture_filter = BaseMaterial3D.TEXTURE_FILTER_LINEAR
	var patch := MeshInstance3D.new()
	patch.name = "StaticContactApproximation"
	patch.set_meta("static_contact", true)
	var plane := PlaneMesh.new()
	plane.size = footprint
	patch.mesh = plane
	patch.material_override = _contact_material
	patch.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	parent.add_child(patch)
	patch.position = at

func _merge_contact_patches(parent: Node3D) -> void:
	var patches: Array[MeshInstance3D] = []
	for child in parent.get_children():
		if child is MeshInstance3D and child.has_meta("static_contact"):
			patches.append(child as MeshInstance3D)
	if patches.size() < 2:
		return
	var tool := SurfaceTool.new()
	tool.begin(Mesh.PRIMITIVE_TRIANGLES)
	for patch in patches:
		tool.append_from(patch.mesh, 0, patch.transform)
		patch.free()
	var merged := MeshInstance3D.new()
	merged.name = "SharedStaticContactBatch"
	merged.mesh = tool.commit()
	merged.material_override = _contact_material
	merged.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	parent.add_child(merged)

func _find_cutaway_meshes(node: Node) -> void:
	if node is Node3D and str(node.name).begins_with("CUTAWAY_RIGHT"):
		_shell_cutaway_meshes.append(node as Node3D)
	for child in node.get_children():
		_find_cutaway_meshes(child)

func _setup_grid_and_ghost() -> void:
	_grid = MeshInstance3D.new()
	_grid.name = "PreviewGrid_OneDrawSurface"
	_grid.mesh = _make_grid()
	_grid.material_override = _material("grid", Color(.38, .45, .44), 1.0, true)
	_grid.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(_grid)
	_ghost = Node3D.new()
	_ghost.name = "ConstructionPreview_NoPlacementTransaction"
	add_child(_ghost)
	var proxy := _add_asset("conveyor", _ghost, Vector3.ZERO, Vector3(.88, .82, 1), Color(.12, .9, .5))
	_collect_meshes(proxy, _ghost_surfaces)
	_valid_ghost = _ghost_material(Color(.1, .95, .48, .42))
	_invalid_ghost = _ghost_material(Color(.95, .13, .1, .48))
	_ghost_direction = Node3D.new()
	_ghost_direction.name = "IN_to_OUT"
	_ghost.add_child(_ghost_direction)
	var arrow_mat := _material("preview_direction", Color(.83, 1, .88), 1, true)
	_box(_ghost_direction, "ArrowShaft", Vector3(0, .91, 0), Vector3(.045, .025, .56), arrow_mat)
	var left := _box(_ghost_direction, "ArrowHeadL", Vector3(-.065, .91, -.19), Vector3(.045, .025, .2), arrow_mat)
	left.rotation_degrees.y = 42
	var right := _box(_ghost_direction, "ArrowHeadR", Vector3(.065, .91, -.19), Vector3(.045, .025, .2), arrow_mat)
	right.rotation_degrees.y = -42
	_ghost_coord = _sign(_ghost, "[2, 3]  IN > OUT", Vector3(0, .04, .7), .0025, 36)
	_ghost_coord.rotation_degrees.x = -90
	_ghost.visible = false
	_grid.visible = false

func _make_grid() -> ArrayMesh:
	var tool := SurfaceTool.new()
	tool.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i in range(-6, 8):
		var n := float(i) - .5
		_add_quad(tool, Vector3(n - .009, .006, -6.5), Vector3(n + .009, .006, -6.5), Vector3(n + .009, .006, 6.5), Vector3(n - .009, .006, 6.5))
		_add_quad(tool, Vector3(-6.5, .006, n - .009), Vector3(6.5, .006, n - .009), Vector3(6.5, .006, n + .009), Vector3(-6.5, .006, n + .009))
	tool.generate_normals()
	return tool.commit()

func _add_quad(tool: SurfaceTool, a: Vector3, b: Vector3, c: Vector3, d: Vector3) -> void:
	for v in [a, c, b, a, d, c]:
		tool.add_vertex(v)

func _ghost_material(color: Color) -> StandardMaterial3D:
	var material := StandardMaterial3D.new()
	material.albedo_color = color
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.cull_mode = BaseMaterial3D.CULL_BACK
	material.roughness = 1
	return material

func _add_asset(id: String, parent: Node3D, at: Vector3, fallback_size: Vector3, color: Color) -> Node3D:
	var path := MODEL_ROOT + id + ".glb"
	var node: Node3D
	if not _asset_scenes.has(id) and ResourceLoader.exists(path):
		_asset_scenes[id] = load(path)
	if _asset_scenes.get(id) is PackedScene:
		node = (_asset_scenes[id] as PackedScene).instantiate() as Node3D
		assets_loaded[id] = true
		_share_materials(node)
	else:
		node = Node3D.new()
		_box(node, "TemporaryMissingAssetProxy", Vector3(0, fallback_size.y * .5, 0), fallback_size, _material("fallback_" + id, color, .75))
		if not missing_assets.has(id):
			missing_assets.append(id)
	node.name = id
	parent.add_child(node)
	node.position = at
	return node

func _share_materials(node: Node) -> void:
	if node is MeshInstance3D:
		var mesh_node := node as MeshInstance3D
		mesh_node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		if mesh_node.mesh:
			for i in mesh_node.mesh.get_surface_count():
				var material := mesh_node.get_active_material(i)
				if material is StandardMaterial3D:
					var m := material as StandardMaterial3D
					var texture_name := m.albedo_texture.resource_name if m.albedo_texture else "none"
					var key := "%s/%s/%s/%s/%s" % [m.resource_name, m.albedo_color, m.roughness, m.metallic, texture_name]
					if not _shared_materials.has(key):
						_shared_materials[key] = m
					# Replace the imported mesh binding too: an unused GLB material must
					# not keep a duplicate embedded texture alive behind an override.
					if mesh_node.mesh is ArrayMesh:
						(mesh_node.mesh as ArrayMesh).surface_set_material(i, _shared_materials[key])
					mesh_node.set_surface_override_material(i, _shared_materials[key])
	for child in node.get_children():
		_share_materials(child)

func _box(parent: Node3D, node_name: String, at: Vector3, size: Vector3, material: Material) -> MeshInstance3D:
	var node := MeshInstance3D.new()
	node.name = node_name
	var mesh := BoxMesh.new()
	mesh.size = size
	node.mesh = mesh
	node.material_override = material
	node.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	parent.add_child(node)
	node.position = at
	return node

func _merge_static_boxes(parent: Node3D) -> void:
	# Startup-only batching: preserve hard normals/UVs, reduce dock draws by material.
	var groups: Dictionary = {}
	for child in parent.get_children():
		if not child is MeshInstance3D:
			continue
		var mesh_node := child as MeshInstance3D
		var material := mesh_node.material_override
		var id := material.get_instance_id()
		if not groups.has(id):
			var tool := SurfaceTool.new()
			tool.begin(Mesh.PRIMITIVE_TRIANGLES)
			groups[id] = {"tool": tool, "material": material}
		(groups[id].tool as SurfaceTool).append_from(mesh_node.mesh, 0, mesh_node.transform)
		mesh_node.free()
	for group in groups.values():
		var merged := MeshInstance3D.new()
		merged.name = "DockSharedMaterialBatch"
		merged.mesh = (group.tool as SurfaceTool).commit()
		merged.material_override = group.material
		merged.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
		parent.add_child(merged)

func _sign(parent: Node3D, text: String, at: Vector3, pixel: float, size: int) -> Label3D:
	var label := Label3D.new()
	label.name = "OriginalInformationSign"
	label.text = text
	label.font_size = size
	label.pixel_size = pixel
	label.modulate = Color(.93, .96, .93)
	label.outline_size = 4
	label.outline_modulate = Color(.04, .07, .08)
	label.no_depth_test = false
	parent.add_child(label)
	label.position = at
	return label

func _material(id: String, color: Color, rough: float, unshaded := false) -> StandardMaterial3D:
	if _shared_materials.has(id):
		return _shared_materials[id]
	var material := StandardMaterial3D.new()
	material.resource_name = id
	material.albedo_color = color
	material.roughness = rough
	if unshaded:
		material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	_shared_materials[id] = material
	return material

func _register_entity(visual: Node3D, id: String, station_id: String, size: Vector3, center: Vector3) -> void:
	var body := StaticBody3D.new()
	body.name = "PickTarget_" + id
	body.set_meta("entity_id", id)
	body.set_meta("station_id", station_id)
	body.collision_layer = 1
	body.collision_mask = 0
	var collision := CollisionShape3D.new()
	var shape := BoxShape3D.new()
	shape.size = size
	collision.shape = shape
	body.add_child(collision)
	collision.position = center
	visual.add_child(body)
	_entities[id] = {"entity_id": id, "station_id": station_id, "node": visual, "body": body}
	_colliders[id] = body

func entity_bindings() -> Array:
	var result: Array = []
	for item in _entities.values():
		result.append({"entity_id": item.entity_id, "station_id": item.station_id})
	return result

func has_entity(id: String) -> bool:
	return _entities.has(id) and is_instance_valid(_entities[id].node) and _entities[id].node.is_inside_tree()

func entity_position(id: String) -> Vector3:
	if not has_entity(id):
		return Vector3.ZERO
	var body: StaticBody3D = _entities[id].body
	if is_instance_valid(body) and body.get_child_count() > 0:
		return (body.get_child(0) as CollisionShape3D).global_position
	return (_entities[id].node as Node3D).global_position

func entity_node(id: String) -> Node3D:
	return _entities[id].node as Node3D if has_entity(id) else null

func has_anchor(id: String) -> bool:
	return ANCHORS.has(id)

func switch_station(station_id: String, anchor_id := "") -> bool:
	var resolved := str(STATION_ANCHORS.get(station_id, "")) if anchor_id.is_empty() else anchor_id
	if camera == null or not ANCHORS.has(resolved):
		last_error = "作業場所のカメラがありません。現在の画面を維持します。"
		return false
	if not active_station_id.is_empty():
		_save_pose()
	_profile = ANCHORS[resolved].duplicate(true)
	active_station_id = station_id
	if _poses.has(station_id):
		var pose: Dictionary = _poses[station_id]
		camera.position = pose.position
		camera.rotation = pose.rotation
		_focus = pose.focus
	else:
		camera.position = _profile.position
		_focus = _profile.focus
		camera.basis = Basis.looking_at(_focus - camera.position, Vector3.UP)
	camera.h_offset = float(_profile.offset)
	_apply_visibility(station_id)
	camera_switch_count += 1
	last_error = ""
	return true

func _save_pose() -> void:
	_poses[active_station_id] = {"position": camera.position, "rotation": camera.rotation, "focus": _focus}

func pan(delta: Vector2) -> void:
	if camera == null or not bool(_profile.get("pan", false)):
		return
	var right := camera.basis.x
	var forward := Vector3(camera.basis.z.x, 0, camera.basis.z.z).normalized()
	var movement := (-right * delta.x + forward * delta.y) * .014
	movement.y = 0
	var next := _focus + movement
	next.x = clampf(next.x, -5, 5)
	next.z = clampf(next.z, -4.5, 4.5)
	camera.position += next - _focus
	_focus = next
	_save_pose()

func zoom(delta: float) -> void:
	if camera == null or not bool(_profile.get("zoom", false)):
		return
	var offset := camera.position - _focus
	var distance := clampf(offset.length() - delta * .025, float(_profile.min), float(_profile.max))
	camera.position = _focus + offset.normalized() * distance
	_save_pose()

func focus_entity(id: String) -> bool:
	if not has_entity(id):
		return false
	var node: Node3D = _entities[id].node
	_focus = node.global_position + Vector3.UP * .3
	camera.basis = Basis.looking_at(_focus - camera.position, Vector3.UP)
	_save_pose()
	return true

func raycast_entity(screen_position: Vector2) -> Dictionary:
	if camera == null or not is_inside_tree():
		return {}
	var origin := camera.project_ray_origin(screen_position)
	var end := origin + camera.project_ray_normal(screen_position) * camera.far
	var query := PhysicsRayQueryParameters3D.create(origin, end, 1)
	var hit := get_world_3d().direct_space_state.intersect_ray(query)
	if hit.is_empty():
		return {}
	var body: Object = hit.collider
	if body.has_meta("entity_id"):
		var id := str(body.get_meta("entity_id"))
		if has_entity(id):
			return {"entity_id": id, "station_id": str(body.get_meta("station_id"))}
	return {}

func screen_to_grid(screen_position: Vector2) -> Vector2i:
	if camera == null:
		return Vector2i.ZERO
	var point: Variant = Plane(Vector3.UP, 0).intersects_ray(camera.project_ray_origin(screen_position), camera.project_ray_normal(screen_position))
	if point == null:
		return Vector2i.ZERO
	var p: Vector3 = point
	return Vector2i(roundi(p.x), roundi(p.z))

func rotate_inspection(degrees: float) -> void:
	if _inspection_proxy:
		_inspection_proxy.rotation_degrees.y = degrees

func update_inspection_label(parcel: Dictionary) -> void:
	if _inspection_label == null or parcel.is_empty():
		return
	var label: Dictionary = parcel.get("visibleLabel", {})
	_inspection_label.text = "%s\n%s / %s\n%s g" % [parcel.get("id", "DISPLAY"), label.get("cargo", "?"), label.get("countryName", "?"), parcel.get("massGrams", "?")]

func update_ghost(preview: Dictionary) -> void:
	if _ghost == null:
		return
	ghost_visible = active_station_id == "LOC-BUILD-01" and bool(preview.get("active", false))
	_ghost.visible = ghost_visible
	_grid.visible = active_station_id == "LOC-BUILD-01"
	var x := int(preview.get("x", 0))
	var z := int(preview.get("z", 0))
	_ghost.position = Vector3(x, 0, z)
	var turn := int(preview.get("rotation_quarter_turns", 0))
	_ghost.rotation_degrees.y = float(posmod(turn, 4)) * 90
	_ghost_direction.rotation_degrees.y = 180 if bool(preview.get("flipped", false)) else 0
	ghost_arrow_direction = -_ghost_direction.global_basis.z
	var valid := bool(preview.get("valid", false))
	for mesh in _ghost_surfaces:
		mesh.material_override = _valid_ghost if valid else _invalid_ghost
	_ghost_coord.text = "[%d, %d]  IN > OUT" % [x, z]
	_ghost_coord.modulate = Color(.3, 1, .5) if valid else Color(1, .25, .2)
	last_ghost_reason = str(preview.get("reason", ""))

func hide_ghost() -> void:
	ghost_visible = false
	if _ghost:
		_ghost.visible = false

func _apply_visibility(station_id: String) -> void:
	var shown: Array = ["Shell"]
	match station_id:
		"LOC-INBOUND-01": shown.append_array(["Inbound", "Sorting", "Yard"])
		"LOC-INSPECTION-01": shown.append("Inspection")
		"LOC-EXPORT-AIR-01": shown.append_array(["Export", "Sorting"])
		"LOC-SORTING-01": shown.append_array(["Sorting", "Export", "Storage"])
		"LOC-STORAGE-01": shown.append_array(["Storage", "Sorting"])
		"LOC-YARD-01", "LOC-CONTRACT": shown.append_array(["Yard", "Inbound"])
		_: shown.append_array(["Inbound", "Inspection", "Export", "Sorting", "Storage", "Yard"])
	for key in _sections:
		(_sections[key] as Node3D).visible = shown.has(key)
	var cutaway := str(STATION_ANCHORS.get(station_id, "")) == "CAM_OVERVIEW"
	for wall in _shell_cutaway_meshes:
		wall.visible = not cutaway
	for id in _entities:
		var item: Dictionary = _entities[id]
		if is_instance_valid(item.body) and is_instance_valid(item.node):
			(item.body as StaticBody3D).collision_layer = 1 if (item.node as Node3D).is_visible_in_tree() else 0
	if _grid:
		_grid.visible = station_id == "LOC-BUILD-01"
	if _ghost and station_id != "LOC-BUILD-01":
		_ghost.visible = false
		ghost_visible = false

func set_shadow_enabled(value: bool) -> void:
	if _key_light:
		_key_light.shadow_enabled = value

func _collect_meshes(node: Node, result: Array[MeshInstance3D]) -> void:
	if node is MeshInstance3D:
		result.append(node as MeshInstance3D)
	for child in node.get_children():
		_collect_meshes(child, result)

func _cache_meshes(node: Node) -> void:
	_collect_meshes(node, _mesh_instances)

func stats() -> Dictionary:
	var visible := 0
	var triangles := 0
	var surfaces := 0
	for node in _mesh_instances:
		if not is_instance_valid(node) or not node.is_visible_in_tree() or node.mesh == null:
			continue
		visible += 1
		surfaces += node.mesh.get_surface_count()
		var id := node.mesh.get_instance_id()
		if not _mesh_cache.has(id):
			_mesh_cache[id] = int(node.mesh.get_faces().size() / 3)
		triangles += int(_mesh_cache[id])
	return {"visible_mesh_instances": visible, "visible_triangles": triangles, "visible_surfaces": surfaces, "shared_materials": _shared_materials.size(), "camera_switches": camera_switch_count, "cargo_visual_count": cargo_visual_count, "placed_equipment_count": placed_equipment_count, "assets_loaded": assets_loaded.duplicate(), "missing_assets": missing_assets.duplicate(), "node_count": _count_nodes(self), "rigid_bodies": 0, "shadow_lights": 1 if _key_light and _key_light.shadow_enabled else 0}

func _count_nodes(node: Node) -> int:
	var count := 1
	for child in node.get_children():
		count += _count_nodes(child)
	return count
