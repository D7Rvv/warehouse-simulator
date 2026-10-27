extends SceneTree
## Real engine, real 3D renderer and frame-spanning InputEventScreenTouch checks.
## Desktop software rendering is not evidence of iPhone/Android performance or heat.

var app
var checks: Array[Dictionary] = []
var captures: Array[Dictionary] = []
var _failures: int = 0
var _output: String
var _baseline_authority: String
var _stats: Dictionary = {}
var _loaded_source_sha256: Dictionary = {}

func _initialize() -> void:
	call_deferred("_run")

func _run() -> void:
	_output = _output_directory()
	DirAccess.make_dir_recursive_absolute(_output)
	_loaded_source_sha256 = _snapshot_sources()
	_check(DisplayServer.get_name() != "headless", "RENDER-01", "A real graphics display is active")
	if DisplayServer.get_name() == "headless":
		_finish()
		return
	root.size = Vector2i(1280, 720)
	root.content_scale_size = Vector2i(1280, 720)
	var packed = load("res://main.tscn")
	app = packed.instantiate()
	root.add_child(app)
	current_scene = app
	await _frames(6)
	_check(app.is_ready_state, "RENDER-02", "Committed Godot main scene initializes")
	if not app.is_ready_state:
		_finish()
		return
	_baseline_authority = _authority()
	var initial_world_id: int = app.world.get_instance_id()
	var initial_nodes: int = _count_nodes(app.world)
	_check(_count_type(app, "Camera3D") == 1 and _count_type(app, "RigidBody3D") == 0 and _count_type(app, "CharacterBody3D") == 0, "RENDER-03", "One station camera, no walking character or rigid-body parcel simulation")
	_check(app.world.assets_loaded.size() == 8 and app.world.missing_assets.is_empty(), "RENDER-04", "All eight authored GLB assets load without primitive fallbacks", app.world.assets_loaded)
	_check(app.world.cargo_visual_count == 3 and app.world.placed_equipment_count == 1, "RENDER-05", "Visual cargo/equipment counts match isolated fixture")
	await _capture("overview")
	var routes: Array = [
		["LOC-INBOUND-01", "inbound"], ["LOC-INSPECTION-01", "inspection"],
		["LOC-EXPORT-AIR-01", "export"], ["LOC-WAREHOUSE-OVERVIEW", "overview_after_navigation"],
		["LOC-BUILD-01", "build_valid"]
	]
	for route: Array in routes:
		await _tap_button("station:" + str(route[0]))
		_check(app.model.active_station_id == route[0] and app.world.active_station_id == route[0], "NAV-" + str(route[1]), "Held native touch switches to " + str(route[0]))
		await _capture(str(route[1]))
	_check(app.world.get_instance_id() == initial_world_id and _count_nodes(app.world) == initial_nodes and _authority() == _baseline_authority, "RENDER-06", "Normal station navigation reuses world geometry and preserves authority")
	_check(app.world.ghost_visible and app.model.preview().valid, "RENDER-07", "Build shows a valid green visual-only conveyor preview")
	var initial_preview: Dictionary = app.model.preview()
	var drag_start: Vector2 = app.world.camera.unproject_position(Vector3(float(initial_preview.x), 0, float(initial_preview.z)))
	var valid_cell_screen: Vector2 = app.world.camera.unproject_position(Vector3(3, 0, 2))
	_check(not app.hud.is_ui_position(drag_start) and not app.hud.is_ui_position(valid_cell_screen), "DRAG-RENDER-01", "Preview movement uses an exposed 3D viewport region")
	await _drag_screen(71, drag_start, valid_cell_screen)
	_check(int(app.model.preview().x) == 3 and int(app.model.preview().z) == 2 and app.model.preview().valid and app.model.active_station_id == "LOC-BUILD-01", "DRAG-RENDER-02", "Actual frame-spanning world drag snaps the preview to a valid grid cell")
	var original_direction: Vector3 = app.world.ghost_arrow_direction
	await _tap_button("preview-flip")
	_check(original_direction.dot(app.world.ghost_arrow_direction) < -0.99, "RENDER-08", "Flip reverses the displayed IN-to-OUT arrow")
	await _tap_button("preview-rotate")
	_check(int(app.model.preview().rotation_quarter_turns) == 1, "RENDER-09", "Visible rotate button changes preview by a quarter turn")
	var occupied_cell_screen: Vector2 = app.world.camera.unproject_position(Vector3.ZERO)
	await _drag_screen(72, valid_cell_screen, occupied_cell_screen)
	_check(int(app.model.preview().x) == 0 and int(app.model.preview().z) == 0 and app.model.active_station_id == "LOC-BUILD-01", "DRAG-RENDER-03", "Dragging over an existing belt preserves gesture capture instead of opening Sorting")
	await _frames(3)
	_check(not app.model.preview().valid and not str(app.model.preview().reason).is_empty(), "RENDER-10", "Occupied cell produces an invalid preview and explicit reason")
	await _capture("build_invalid")
	for cell: Vector2i in [Vector2i(-4, 2), Vector2i(5, 3), Vector2i(3, 3)]:
		app.model.move_preview(cell.x, cell.y)
		app.handle_action("preview-rotate")
		_check(not app.model.preview().valid and not str(app.model.preview().reason).is_empty() and _authority() == _baseline_authority, "STRUCTURE-%d-%d" % [cell.x, cell.y], "Visible bench, rack or pallet obstruction marks preview red without mutating authority")
	await _tap_button("preview-cancel")
	_check(not app.world.ghost_visible and _authority() == _baseline_authority, "RENDER-11", "Cancel hides the preview without placing or charging")
	for i: int in range(30):
		app.handle_action("preview-reset")
		app.model.move_preview(1 + i % 5, 1 + i % 4)
		app.handle_action("preview-rotate")
		app.handle_action("preview-flip")
		app.handle_action("preview-confirm")
		app.handle_action("preview-cancel")
		await process_frame
	_check(app.world.placed_equipment_count == 1 and _count_nodes(app.world) == initial_nodes and _authority() == _baseline_authority, "RENDER-12", "Thirty engine preview cycles keep installed objects, money and ownership unchanged")
	await _test_input_guards()
	await _test_quality_controls()
	await _test_3d_cargo_taps()
	await _test_inspection_draft()
	await _test_navigation_reuse(initial_world_id, initial_nodes)
	await _test_compact_layout()
	_stats["final_quality_snapshot"] = app.quality.snapshot()
	_stats["world"] = app.world.stats()
	app.queue_free()
	await _frames(3)
	_finish()

func _test_input_guards() -> void:
	app.navigate("LOC-WAREHOUSE-OVERVIEW")
	await _frames(3)
	var target: Vector2 = app.hud.button_rect("station:LOC-BUILD-01").get_center()
	_send_touch(40, target, true)
	await _frames(3)
	_send_touch(40, target, false, true)
	_send_touch(40, target, false)
	await _frames(3)
	_check(app.model.active_station_id == "LOC-WAREHOUSE-OVERVIEW", "INPUT-RENDER-01", "Canceled held UI touch never executes on release")
	_send_touch(41, target, true)
	await _frames(2)
	_send_touch(42, Vector2(180, 240), true)
	_send_touch(43, Vector2(320, 240), true)
	await _frames(3)
	_check(not app.router.can_invoke_ui_action(), "INPUT-RENDER-02", "Two real world touches block a held UI navigation action")
	var camera_before_pinch: Vector3 = app.world.camera.position
	_send_drag(43, Vector2(360, 240), Vector2(40, 0))
	await _frames(3)
	_check(not app.world.camera.position.is_equal_approx(camera_before_pinch), "INPUT-RENDER-09", "Actual native pinch distance changes the limited overview camera zoom")
	_send_touch(42, Vector2(180, 240), false)
	_send_touch(43, Vector2(360, 240), false)
	await _frames(2)
	_send_touch(41, target, false)
	await _frames(3)
	_check(app.model.active_station_id == "LOC-WAREHOUSE-OVERVIEW" and app.router.count() == 0, "INPUT-RENDER-03", "UI press begun before a pinch cannot fire after the pair ends")
	_send_touch(44, target, true)
	await _frames(3)
	app.notification(Node.NOTIFICATION_APPLICATION_FOCUS_OUT)
	await _frames(2)
	_send_touch(44, target, false)
	await _frames(2)
	_check(app.model.active_station_id == "LOC-WAREHOUSE-OVERVIEW" and app.router.count() == 0, "INPUT-RENDER-04", "Focus interruption invalidates an already-held navigation press")
	app.notification(Node.NOTIFICATION_APPLICATION_FOCUS_IN)
	await _tap_button("station:LOC-BUILD-01")
	_check(app.model.active_station_id == "LOC-BUILD-01", "INPUT-RENDER-05", "Fresh input works after canceled gesture and focus recovery")
	app.navigate("LOC-WAREHOUSE-OVERVIEW")
	await _tap_button("menu")
	_check(app.hud.is_modal_open() and app.hud.button_rect("station:LOC-CONTRACT").size.y >= 48, "INPUT-RENDER-06", "Less frequent stations are reachable through a second tap")
	var before: Vector3 = app.world.camera.position
	await _tap_button("station:LOC-CONTRACT")
	_check(app.model.active_station_id == "LOC-WAREHOUSE-OVERVIEW" and app.world.camera.position.is_equal_approx(before), "INPUT-RENDER-07", "Locked contract selection keeps the current camera")
	app.go_back()
	await _frames(2)
	_check(not app.hud.is_modal_open(), "INPUT-RENDER-08", "Back dismisses the active menu before changing station")

func _test_quality_controls() -> void:
	app.navigate("LOC-WAREHOUSE-OVERVIEW")
	await _frames(2)
	await _tap_button("quality-menu")
	_check(app.hud.is_modal_open(), "QUALITY-RENDER-01", "Actual held touch opens the quality menu")
	await _tap_button("quality-battery_saver")
	_check(app.quality.profile_id == "battery_saver" and Engine.max_fps == 30 and not app.hud.is_modal_open(), "QUALITY-RENDER-02", "Battery preset is applied by real UI input and closes its menu")
	_check(app.hud.button_rect("station:LOC-INBOUND-01").has_area(), "QUALITY-RENDER-03", "Quality changes retain sharp, active 2D navigation controls")
	await _tap_button("quality-menu")
	await _tap_button("quality-balanced")
	_check(app.quality.profile_id == "balanced" and Engine.max_fps == 30, "QUALITY-RENDER-04", "Balanced mode is restored without silently enabling sixty FPS")
	var station_before: String = app.model.active_station_id
	var pressed: Vector2 = app.hud.button_rect("station:LOC-INBOUND-01").get_center()
	_send_touch(61, pressed, true)
	await _frames(2)
	app.quality.set_thermal_hint("CRITICAL", "unit_test_injection_not_phone_measurement")
	await _frames(1)
	var critical: Dictionary = app.quality.snapshot()
	_stats["critical_injection_probe"] = critical
	_check(app.get("_suspended") == true and Engine.max_fps <= 10 and app.router.count() == 0, "QUALITY-RENDER-05", "Critical test hint suspends main input, cancels captures and reduces render frequency")
	_send_touch(61, pressed, false)
	_send_touch(62, pressed, true)
	await _frames(1)
	_send_touch(62, pressed, false)
	await _frames(1)
	_check(app.model.active_station_id == station_before and not bool(critical.hardware_thermal_observed) and not bool(critical.thermal_sensor_available), "QUALITY-RENDER-06", "Suspended main ignores UI touches and injected thermal data stays marked unobserved")
	app.quality.set_thermal_hint("NOMINAL", "unit_test_injection_not_phone_measurement")
	await _frames(2)
	_check(app.get("_suspended") == false and Engine.max_fps == 30, "QUALITY-RENDER-07", "Noncritical hint resumes main input at the normal thirty-FPS limit")
	_send_touch(61, pressed, false)
	await _frames(2)
	_check(app.model.active_station_id == station_before, "QUALITY-RENDER-08", "Pre-critical held UI press cannot execute after cooling")
	await _tap_button("station:LOC-INBOUND-01")
	_check(app.model.active_station_id == "LOC-INBOUND-01", "QUALITY-RENDER-09", "Fresh native UI input works after safety recovery")
	app.quality.set_thermal_hint("UNKNOWN")
	app.handle_action("quality-balanced")
	_check(_authority() == _baseline_authority, "QUALITY-RENDER-10", "Rendering safety policy never changes parcel ownership, fixture clock or wallet")

func _test_3d_cargo_taps() -> void:
	app.navigate("LOC-INBOUND-01")
	await _frames(3)
	for id: String in ["DEV-PARCEL-001", "DEV-PARCEL-002", "DEV-PARCEL-003"]:
		app.navigate("LOC-INBOUND-01")
		await physics_frame
		await _frames(2)
		var world_position: Vector3 = app.world.entity_position(id)
		var screen: Vector2 = app.world.camera.unproject_position(world_position)
		var hit: Dictionary = app.world.raycast_entity(screen)
		_check(str(hit.get("entity_id", "")) == id and not app.hud.is_ui_position(screen), "PICK-" + id, "Unoccluded 3D cargo ray resolves its unique parcel ID", {"screen": [screen.x, screen.y], "hit": hit})
		_send_touch(51, screen, true)
		await _frames(3)
		_send_touch(51, screen, false)
		await _frames(3)
		_check(app.model.active_target_id == id and app.model.selected_parcel_id == id, "TAP-" + id, "Actual frame-spanning 3D touch retains selected parcel ID")
	_check(_authority() == _baseline_authority, "PICK-04", "Selecting visual cargo does not transfer parcels out of the truck")

func _test_inspection_draft() -> void:
	await _tap_button("station:LOC-INSPECTION-01")
	var before_drag_angle: float = app.model.inspection_rotation_degrees
	var box_position: Vector2 = app.world.camera.unproject_position(app.world.entity_position("DEV-INSPECTION-DISPLAY"))
	await _drag_screen(73, box_position, box_position + Vector2(80, 12))
	_check(not is_equal_approx(app.model.inspection_rotation_degrees, before_drag_angle) and app.model.active_station_id == "LOC-INSPECTION-01", "DRAFT-05", "Actual inspection-model drag rotates the view draft without changing stations")
	var previous_angle: float = app.model.inspection_rotation_degrees
	var draft: String = app.model.inspection_draft_id
	await _tap_button("rotate-parcel")
	_check(is_equal_approx(app.model.inspection_rotation_degrees, fposmod(previous_angle + 90.0, 360.0)), "DRAFT-01", "Inspection rotation button changes only the view draft")
	await _tap_button("label")
	_check(app.hud.is_modal_open() and app.hud.modal_text().contains(app.model.selected_parcel_id) and app.hud.modal_text().contains("TRUCK_CARGO"), "DRAFT-02", "Label modal uses selected fixture data and keeps actual ownership visible")
	await _capture("inspection_label")
	app.navigate("LOC-BUILD-01")
	app.navigate("LOC-INSPECTION-01")
	await _frames(3)
	_check(app.model.inspection_draft_id == draft and app.model.label_expanded and app.hud.is_modal_open(), "DRAFT-03", "Inspection draft and expanded label survive station changes")
	await _tap_button("close-overlay")
	_check(not app.hud.is_modal_open(), "DRAFT-04", "A held touch can close a reused label modal")
	await _capture("inspection_rotated")

func _test_navigation_reuse(world_id: int, node_count: int) -> void:
	app.model.label_expanded = false
	var stations: Array[String] = ["LOC-INBOUND-01", "LOC-INSPECTION-01", "LOC-EXPORT-AIR-01", "LOC-WAREHOUSE-OVERVIEW", "LOC-BUILD-01"]
	var elapsed: float = app.demo_elapsed_seconds
	var all_switched: bool = true
	for i: int in range(50):
		var station: String = stations[i % stations.size()]
		await _tap_button("station:" + station, 2)
		all_switched = all_switched and app.model.active_station_id == station
	_check(all_switched and app.demo_elapsed_seconds > elapsed, "STRESS-01", "Fifty actual held-button switches work while demo elapsed time continues")
	var before_memory: int = int(Performance.get_monitor(Performance.MEMORY_STATIC))
	var started: int = Time.get_ticks_usec()
	for i: int in range(1000): app.navigate(stations[i % stations.size()])
	var elapsed_ms: float = (Time.get_ticks_usec() - started) / 1000.0
	await _frames(5)
	var after_memory: int = int(Performance.get_monitor(Performance.MEMORY_STATIC))
	_stats["thousand_engine_switches"] = {"count": 1000, "total_cpu_handler_ms": elapsed_ms, "tracked_static_memory_before_bytes": before_memory, "tracked_static_memory_after_bytes": after_memory, "tracked_static_memory_delta_bytes": after_memory - before_memory, "scope": "single_desktop_engine_run_not_phone_leak_or_latency_acceptance"}
	_check(app.world.get_instance_id() == world_id and _count_nodes(app.world) == node_count and _count_type(app, "Camera3D") == 1, "STRESS-02", "One thousand engine station switches do not create cameras or world nodes")
	_check(_authority() == _baseline_authority, "STRESS-03", "Engine stress navigation preserves fixture IDs, owner slots, money and fixed clock")
	var camera_before: Transform3D = app.world.camera.transform
	var selected_before: String = app.model.active_station_id
	_check(not app.navigate("NO-STATION") and app.world.camera.transform.is_equal_approx(camera_before) and app.model.active_station_id == selected_before, "STRESS-04", "Invalid navigation cannot move the real camera")

func _test_compact_layout() -> void:
	app.hud.close_overlay()
	root.size = Vector2i(844, 300)
	root.content_scale_size = Vector2i(844, 300)
	await _frames(6)
	app.navigate("LOC-BUILD-01")
	app.handle_action("preview-reset")
	await _frames(3)
	var viewport_rect: Rect2 = root.get_visible_rect()
	var contained: bool = true
	var touch_targets: bool = true
	for key: String in ["back", "quality-menu", "menu", "station:LOC-INBOUND-01", "station:LOC-INSPECTION-01", "station:LOC-EXPORT-AIR-01", "station:LOC-WAREHOUSE-OVERVIEW", "station:LOC-BUILD-01", "preview-reset", "preview-rotate", "preview-flip", "preview-cancel", "preview-confirm"]:
		var bounds: Rect2 = app.hud.button_rect(key)
		contained = contained and bounds.has_area() and viewport_rect.encloses(bounds)
		touch_targets = touch_targets and bounds.size.y >= 47.5
	_check(contained and touch_targets, "LAYOUT-01", "Primary navigation and build actions remain visible at 844x300 with 48-unit heights")
	await _capture("compact_build_844x300")
	await _tap_button("menu")
	var menu_inside: bool = true
	for key: String in ["station:LOC-SORTING-01", "station:LOC-STORAGE-01", "station:LOC-YARD-01", "station:LOC-SHOP", "station:LOC-ECONOMY", "station:LOC-CONTRACT", "close-overlay"]:
		menu_inside = menu_inside and viewport_rect.encloses(app.hud.button_rect(key))
	_check(menu_inside, "LAYOUT-02", "All secondary station targets remain inside the short landscape viewport")
	await _capture("compact_menu_844x300")
	app.go_back()
	await _tap_button("station:LOC-INSPECTION-01")
	await _tap_button("label")
	_check(app.hud.is_modal_open() and viewport_rect.encloses(app.hud.button_rect("close-overlay")), "LAYOUT-03", "Short landscape label modal retains a usable close action")
	await _capture("compact_label_844x300")
	app.go_back()
	root.size = Vector2i(844, 390)
	root.content_scale_size = Vector2i(844, 390)
	await _frames(5)
	app.navigate("LOC-INBOUND-01")
	await _capture("inbound_844x390")
	_check(_authority() == _baseline_authority, "LAYOUT-04", "Viewport changes never mutate the fixture")

func _capture(name: String) -> void:
	await _frames(3)
	await RenderingServer.frame_post_draw
	var image: Image = root.get_texture().get_image()
	var path: String = _output.path_join(name + ".png")
	var saved: Error = image.save_png(path)
	var minimum: Color = Color(1, 1, 1)
	var maximum: Color = Color(0, 0, 0)
	for y: int in range(80, image.get_height() - 64, 23):
		for x: int in range(20, maxi(21, int(image.get_width() * .64)), 29):
			var pixel: Color = image.get_pixel(x, y)
			minimum = Color(minf(minimum.r, pixel.r), minf(minimum.g, pixel.g), minf(minimum.b, pixel.b))
			maximum = Color(maxf(maximum.r, pixel.r), maxf(maximum.g, pixel.g), maxf(maximum.b, pixel.b))
	var variation: float = (Vector3(maximum.r, maximum.g, maximum.b) - Vector3(minimum.r, minimum.g, minimum.b)).length()
	_check(saved == OK and variation > .05, "IMAGE-" + name, "Rendered capture contains non-flat scene pixels", {"pixel_range": variation})
	captures.append({"name": name, "file": name + ".png", "width": image.get_width(), "height": image.get_height(), "sha256": FileAccess.get_sha256(path), "station_id": app.model.active_station_id, "world_stats": app.world.stats(), "runtime_stats": app.quality.snapshot()})

func _tap_button(key: String, held_frames: int = 3) -> void:
	var bounds: Rect2 = app.hud.button_rect(key)
	if not bounds.has_area():
		_check(false, "MISSING-BUTTON-" + key, "Expected visible button does not exist")
		return
	var center: Vector2 = bounds.get_center()
	_send_touch(20, center, true)
	await _frames(held_frames)
	_send_touch(20, center, false)
	await _frames(2)

func _drag_screen(id: int, start: Vector2, finish: Vector2, steps: int = 5) -> void:
	_send_touch(id, start, true)
	await _frames(2)
	var previous: Vector2 = start
	for step: int in range(1, steps + 1):
		var position: Vector2 = start.lerp(finish, float(step) / steps)
		_send_drag(id, position, position - previous)
		previous = position
		await process_frame
	_send_touch(id, finish, false)
	await _frames(2)

func _send_touch(id: int, position: Vector2, pressed: bool, canceled: bool = false) -> void:
	var event := InputEventScreenTouch.new()
	event.index = id
	event.position = position
	event.pressed = pressed
	event.canceled = canceled
	Input.parse_input_event(event)
	Input.flush_buffered_events()

func _send_drag(id: int, position: Vector2, delta: Vector2) -> void:
	var event := InputEventScreenDrag.new()
	event.index = id
	event.position = position
	event.relative = delta
	Input.parse_input_event(event)
	Input.flush_buffered_events()

func _frames(count: int) -> void:
	for i: int in range(count): await process_frame

func _authority() -> String:
	return JSON.stringify({"world": app.model.world(), "parcels": app.model.parcels(), "runs": app.model.truck_runs(), "gate": app.model.gate()})

func _count_nodes(node: Node) -> int:
	var count: int = 1
	for child: Node in node.get_children(): count += _count_nodes(child)
	return count

func _count_type(node: Node, class_name_value: String) -> int:
	var count: int = 1 if node.is_class(class_name_value) else 0
	for child: Node in node.get_children(): count += _count_type(child, class_name_value)
	return count

func _check(condition: bool, id: String, description: String, detail: Variant = null) -> void:
	var record: Dictionary = {"id": id, "description": description, "status": "PASS" if condition else "FAIL"}
	if detail != null: record["detail"] = detail
	checks.append(record)
	if not condition: _failures += 1
	print("%s %s %s" % [record.status, id, description])

func _output_directory() -> String:
	var args: PackedStringArray = OS.get_cmdline_user_args()
	for i: int in range(args.size()):
		if args[i] == "--output" and i + 1 < args.size(): return args[i + 1]
	return ProjectSettings.globalize_path("res://../builds/godot-mobile/render")

func _snapshot_sources() -> Dictionary:
	var sources: Dictionary = {}
	for path: String in ["res://main.gd", "res://main.tscn", "res://project.godot", "res://data/prototype_station_fixture_v0_1.json", "res://data/mobile_quality_profiles.json", "res://assets/fonts/warehouse_jp.otf", "res://scripts/station_model.gd", "res://scripts/station_world.gd", "res://scripts/station_hud.gd", "res://scripts/station_pointer_router.gd", "res://scripts/mobile_quality.gd", "res://tests/render_smoke.gd", "res://assets/ART_MANIFEST.json"]:
		if FileAccess.file_exists(path): sources[path] = FileAccess.get_sha256(path)
	for asset: String in ["warehouse_shell", "truck", "inspection_bench", "conveyor", "export_gate", "rack", "pallet", "parcel"]:
		var path: String = "res://assets/models/" + asset + ".glb"
		if FileAccess.file_exists(path): sources[path] = FileAccess.get_sha256(path)
		var import_path: String = path + ".import"
		if FileAccess.file_exists(import_path):
			sources[import_path] = FileAccess.get_sha256(import_path)
			var config := ConfigFile.new()
			if config.load(import_path) == OK:
				var imported: String = str(config.get_value("remap", "path", ""))
				if not imported.is_empty() and FileAccess.file_exists(imported): sources[imported] = FileAccess.get_sha256(imported)
	return sources

func _finish() -> void:
	var sources: Dictionary = _snapshot_sources()
	_check(sources == _loaded_source_sha256, "COHORT-01", "Source, GLBs and imported resources stayed unchanged during execution")
	var report: Dictionary = {"kind": "actual_godot_render_and_input_smoke", "status": "PASS" if _failures == 0 else "FAIL", "passed": checks.size() - _failures, "failed": _failures, "checks": checks, "captures": captures, "stats": _stats, "display": DisplayServer.get_name(), "rendering_method": RenderingServer.get_current_rendering_method(), "rendering_device": RenderingServer.get_video_adapter_name(), "godot": Engine.get_version_info().get("string", ""), "actual_phone_fps": "NOT_RUN", "physical_thermal_30_minutes": "NOT_RUN", "reference_comparison": "NOT_RUN_REFERENCE_UNAVAILABLE", "scope": "desktop_engine_renderer_and_synthetic_native_events_not_physical_phone", "source_sha256": _loaded_source_sha256, "source_sha256_at_end": sources, "source_cohort_unchanged": sources == _loaded_source_sha256}
	var file := FileAccess.open(_output.path_join("report.json"), FileAccess.WRITE)
	if file != null: file.store_string(JSON.stringify(report, "\t") + "\n")
	print("GODOT RENDER %s: %d checks passed, %d failed; %d captures; phone/thermal/reference NOT_RUN" % [report.status, report.passed, _failures, captures.size()])
	quit(0 if _failures == 0 else 1)
