extends SceneTree
## Additional real-render views for inspecting truck cab/wheels, storage and belt detail.
## This visual tour does not replace the interaction suite or phone performance tests.

var sources_at_start: Dictionary = {}
var captures: Array[Dictionary] = []
var app
var failed := false
var output := ""

func _initialize() -> void:
	call_deferred("run")

func run() -> void:
	var args: PackedStringArray = OS.get_cmdline_user_args()
	output = ProjectSettings.globalize_path("res://../builds/godot-mobile/visual-tour")
	for i: int in range(args.size()):
		if args[i] == "--output" and i + 1 < args.size(): output = args[i + 1]
	DirAccess.make_dir_recursive_absolute(output)
	if DisplayServer.get_name() == "headless":
		push_error("Visual tour needs an actual renderer.")
		quit(1)
		return
	sources_at_start = source_hashes()
	root.size = Vector2i(1280, 720)
	root.content_scale_size = Vector2i(1280, 720)
	app = load("res://main.tscn").instantiate()
	root.add_child(app)
	current_scene = app
	await frames(5)
	if not app.is_ready_state or not app.world.missing_assets.is_empty():
		push_error("Visual tour cannot substitute missing assets.")
		quit(1)
		return
	var authority: String = app.model.fingerprint()
	for route: Array in [["LOC-YARD-01", "yard"], ["LOC-STORAGE-01", "storage"], ["LOC-SORTING-01", "sorting"]]:
		if not app.navigate(str(route[0])): failed = true
		await frames(4)
		await RenderingServer.frame_post_draw
		var image: Image = root.get_texture().get_image()
		var path: String = output.path_join(str(route[1]) + ".png")
		if image.save_png(path) != OK: failed = true
		captures.append({"name": route[1], "file": str(route[1]) + ".png", "width": image.get_width(), "height": image.get_height(), "sha256": FileAccess.get_sha256(path), "world_stats": app.world.stats(), "runtime_stats": app.quality.snapshot()})
	if app.model.fingerprint() != authority: failed = true
	var at_end: Dictionary = source_hashes()
	if sources_at_start != at_end: failed = true
	var report: Dictionary = {"status": "PASS" if not failed else "FAIL", "scope": "desktop_actual_render_visual_tour_not_phone_or_reference_acceptance", "captures": captures, "source_sha256": sources_at_start, "source_sha256_at_end": at_end, "source_cohort_unchanged": sources_at_start == at_end, "renderer": RenderingServer.get_video_adapter_name(), "reference_parity": "NOT_RUN_REFERENCE_UNAVAILABLE", "phone_fps": "NOT_RUN", "phone_thermal": "NOT_RUN"}
	var file := FileAccess.open(output.path_join("report.json"), FileAccess.WRITE)
	if file != null: file.store_string(JSON.stringify(report, "\t") + "\n")
	app.queue_free()
	await frames(2)
	print("GODOT VISUAL TOUR %s: %d additional views; phone/reference NOT_RUN" % [report.status, captures.size()])
	quit(1 if failed else 0)

func frames(count: int) -> void:
	for i: int in range(count): await process_frame

func source_hashes() -> Dictionary:
	var result: Dictionary = {}
	for path: String in ["res://main.gd", "res://main.tscn", "res://project.godot", "res://scripts/station_world.gd", "res://scripts/station_hud.gd", "res://scripts/mobile_quality.gd", "res://scripts/station_model.gd", "res://scripts/station_pointer_router.gd", "res://tests/visual_tour.gd", "res://assets/ART_MANIFEST.json", "res://assets/fonts/warehouse_jp.otf", "res://data/mobile_quality_profiles.json", "res://data/prototype_station_fixture_v0_1.json"]:
		if FileAccess.file_exists(path): result[path] = FileAccess.get_sha256(path)
	for name: String in ["warehouse_shell", "truck", "inspection_bench", "conveyor", "export_gate", "rack", "pallet", "parcel"]:
		var path: String = "res://assets/models/" + name + ".glb"
		result[path] = FileAccess.get_sha256(path)
	return result
