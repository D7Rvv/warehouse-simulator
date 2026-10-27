extends SceneTree
## Actual Godot execution of deterministic station, pointer and quality-policy checks.
## Synthetic frame intervals below are unit-test data, never phone performance evidence.

var checks: Array[Dictionary] = []
var _failures: int = 0
var _fixture_data: Dictionary = {}
var _source_at_start: Dictionary = {}

func _initialize() -> void:
	call_deferred("_run")

func _run() -> void:
	_source_at_start = _source_hashes()
	_fixture_data = JSON.parse_string(FileAccess.get_file_as_string("res://data/prototype_station_fixture_v0_1.json"))
	_check(not _fixture_data.is_empty(), "FIXTURE-01", "Canonical prototype fixture loads")
	_test_model()
	await _test_pointer()
	await _test_quality()
	var source_at_end: Dictionary = _source_hashes()
	if source_at_end != _source_at_start:
		_check(false, "COHORT-HEADLESS", "Source or fixture changed while headless checks were running")
	var evidence: Dictionary = {
		"kind": "godot_headless_logic_and_policy", "godot_version": Engine.get_version_info().get("string", ""),
		"status": "PASS" if _failures == 0 else "FAIL", "passed": checks.size() - _failures,
		"failed": _failures, "checks": checks, "rendering_executed": false,
		"synthetic_quality_policy_input": true, "actual_phone_fps": "NOT_RUN", "phone_thermal": "NOT_RUN",
		"fixture_sha256": FileAccess.get_sha256("res://data/prototype_station_fixture_v0_1.json"),
		"source_sha256": _source_at_start, "source_sha256_at_end": source_at_end, "source_cohort_unchanged": source_at_end == _source_at_start
	}
	var directory: String = _output_directory("headless")
	DirAccess.make_dir_recursive_absolute(directory)
	var file := FileAccess.open(directory.path_join("report.json"), FileAccess.WRITE)
	if file != null:
		file.store_string(JSON.stringify(evidence, "\t") + "\n")
	print("GODOT HEADLESS %s: %d checks passed, %d failed; phone/thermal NOT_RUN" % [evidence.status, evidence.passed, _failures])
	quit(0 if _failures == 0 else 1)

func _check(condition: bool, id: String, description: String, detail: Variant = null) -> void:
	var record: Dictionary = {"id": id, "description": description, "status": "PASS" if condition else "FAIL"}
	if detail != null:
		record["detail"] = detail
	checks.append(record)
	if not condition:
		_failures += 1
	print("%s %s %s" % [record.status, id, description])

func _new_model():
	var script = load("res://scripts/station_model.gd")
	var model = script.new()
	model.load_fixture(_fixture_data.duplicate(true))
	return model

func _authority(model) -> String:
	return JSON.stringify({"world": model.world(), "parcels": model.parcels(), "runs": model.truck_runs(), "gate": model.gate()})

func _test_model() -> void:
	var script = load("res://scripts/station_model.gd")
	_check(script != null, "MODEL-01", "Real station model script loads")
	if script == null:
		return
	var model = _new_model()
	_check(model.ready, "MODEL-02", "Valid canonical fixture is accepted")
	if not model.ready:
		return
	var before: String = _authority(model)
	var fingerprint: String = model.fingerprint()
	_check(model.world().currencyUSD == 2500 and model.world().time == "09:00", "MODEL-03", "Money/time remain canonical demo values")
	_check(model.gate().currentBufferedCount == 1 and model.gate().visualLastBatchSummary.correctCount == 16, "MODEL-04", "Current buffer and previous batch are distinct")
	_check(model.gate().visualLastBatchSummary.netUSD == 1003, "MODEL-05", "Previous net display is not multiplied by active buffer")
	_check(model.parcels().size() == 3 and model.truck_runs()[0].cargoParcelIds.size() == 3, "MODEL-06", "Truck manifest matches three unique sample parcels")
	var parcel_ids: Dictionary = {}
	var owners: Dictionary = {}
	for parcel: Dictionary in model.parcels():
		parcel_ids[parcel.id] = true
		owners[JSON.stringify(parcel.owner)] = true
	_check(parcel_ids.size() == 3 and owners.size() == 3, "MODEL-07", "Parcel IDs and owner slots are unique")
	_check(not model.get_parcel("DEV-PARCEL-001").has("hiddenTruth"), "MODEL-08", "Visible parcel data does not disclose inspection truth")
	var exposed: Dictionary = model.world()
	exposed["currencyUSD"] = -500
	var parcels: Array = model.parcels()
	parcels[0]["owner"]["slot"] = "changed-outside-model"
	_check(_authority(model) == before, "MODEL-09", "Public nested snapshots are isolated copies")
	var mutable_source: Dictionary = _fixture_data.duplicate(true)
	var another = script.new()
	another.load_fixture(mutable_source)
	mutable_source.world.currencyUSD = 0
	mutable_source.packages[0].owner.slot = "changed-after-load"
	_check(_authority(another) == before, "MODEL-10", "Mutating the source DTO cannot modify the loaded model")
	var invalid: Dictionary = _fixture_data.duplicate(true)
	invalid["promoteToProduction"] = true
	_check(not model.load_fixture(invalid) and model.ready and _authority(model) == before, "MODEL-11", "Unsafe promotion is rejected atomically")
	invalid = _fixture_data.duplicate(true)
	invalid["prototypeOnly"] = false
	_check(not model.load_fixture(invalid) and _authority(model) == before, "MODEL-12", "Production fixture cannot replace S1 mock")
	invalid = _fixture_data.duplicate(true)
	invalid.packages[1].id = invalid.packages[0].id
	_check(not model.load_fixture(invalid) and _authority(model) == before, "MODEL-13", "Duplicate parcel ID is rejected")
	invalid = _fixture_data.duplicate(true)
	invalid.packages[1].owner = invalid.packages[0].owner.duplicate(true)
	_check(not model.load_fixture(invalid) and _authority(model) == before, "MODEL-14", "Duplicate owner slot is rejected")
	invalid = _fixture_data.duplicate(true)
	invalid.packages[0].owner.entityId = "UNKNOWN-RUN"
	_check(not model.load_fixture(invalid) and _authority(model) == before, "MODEL-15", "Truck manifest and parcel ownership cannot disagree")
	var start: String = model.active_station_id
	_check(not model.select_station("MISSING-STATION") and model.active_station_id == start, "MODEL-16", "Unknown station preserves current selection")
	_check(not model.select_station("LOC-CONTRACT") and model.active_station_id == start, "MODEL-17", "Locked contract preserves selection and reports a reason")
	_check(not model.last_error.is_empty(), "MODEL-18", "Unavailable navigation supplies feedback")
	model.set_anchor_validator(func(anchor: String) -> bool: return anchor != "CAM_INBOUND")
	_check(not model.select_station("LOC-INBOUND-01") and model.active_station_id == start, "MODEL-19", "Missing camera anchor cannot change active station")
	model.set_anchor_validator(func(_anchor: String) -> bool: return true)
	_check(model.register_target("DEV-PARCEL-001", "LOC-INBOUND-01"), "MODEL-20", "Real entity target registers against its station")
	_check(model.select_station("LOC-INBOUND-01", "DEV-PARCEL-001") and model.active_target_id == "DEV-PARCEL-001", "MODEL-21", "3D target selection retains entity ID")
	_check(not model.select_station("LOC-INSPECTION-01", "DEV-PARCEL-001"), "MODEL-22", "Target cannot be selected through an unrelated station")
	model.select_station("LOC-WAREHOUSE-OVERVIEW")
	_check(model.select_station("LOC-INBOUND-01") and model.active_target_id == "DEV-PARCEL-001", "MODEL-23", "Menu selection restores the target for that station")
	model.remove_target("DEV-PARCEL-001")
	_check(model.active_target_id.is_empty() and model.active_station_id == "LOC-INBOUND-01", "MODEL-24", "Deleted visual target falls back without changing station")
	model.select_parcel("DEV-PARCEL-001")
	model.inspection_rotation_degrees = 90.0
	model.label_expanded = true
	model.hand_slot_parcel_id = "DEV-PARCEL-003"
	var draft: String = model.inspection_draft_id
	model.select_parcel("DEV-PARCEL-002")
	model.inspection_rotation_degrees = 180.0
	model.select_parcel("DEV-PARCEL-001")
	_check(is_equal_approx(model.inspection_rotation_degrees, 90.0) and model.label_expanded and model.inspection_draft_id == draft, "MODEL-25", "Per-parcel inspection draft survives selection changes")
	var nav: Array[String] = ["LOC-INBOUND-01", "LOC-INSPECTION-01", "LOC-EXPORT-AIR-01", "LOC-WAREHOUSE-OVERVIEW", "LOC-BUILD-01"]
	var all_selected: bool = true
	for i: int in range(50):
		all_selected = model.select_station(nav[i % nav.size()]) and all_selected
	_check(all_selected and _authority(model) == before and model.fingerprint() == fingerprint, "MODEL-26", "Fifty station changes preserve authoritative state")
	_check(model.inspection_draft_id == draft and model.hand_slot_parcel_id == "DEV-PARCEL-003", "MODEL-27", "Navigation does not empty HandSlot or inspection draft")
	model.register_target("DEV-PARCEL-001", "LOC-INBOUND-01")
	model.select_station("LOC-INBOUND-01", "DEV-PARCEL-001")
	model.remove_target("DEV-PARCEL-001")
	_check(model.selected_parcel_id.is_empty() and model.inspection_draft_id == draft and is_equal_approx(model.inspection_rotation_degrees, 90.0) and model.label_expanded and model.hand_slot_parcel_id == "DEV-PARCEL-003", "MODEL-37", "Deleting a selected visual preserves its active draft and reserved HandSlot")
	all_selected = true
	for i: int in range(1000):
		all_selected = model.select_station(nav[i % nav.size()]) and all_selected
	_check(all_selected and _authority(model) == before, "MODEL-28", "One thousand logical switches cannot reset the world")
	model.move_preview(0, 0)
	_check(not model.preview().valid and not str(model.preview().reason).is_empty(), "MODEL-29", "Occupied grid cell is invalid with a reason")
	model.move_preview(7, 2)
	_check(not model.preview().valid, "MODEL-30", "Out-of-bounds preview is rejected")
	model.move_preview(2, 2, 1)
	_check(not model.preview().valid, "MODEL-31", "Preview on unsupported floor is invalid")
	model.move_preview(2, 2)
	_check(model.preview().valid, "MODEL-32", "Unoccupied in-range cell permits a visual preview")
	for i: int in range(4): model.rotate_preview()
	_check(model.preview().rotation_quarter_turns == 0, "MODEL-33", "Four visible rotations return to original direction")
	model.flip_preview()
	model.flip_preview()
	_check(not model.preview().flipped, "MODEL-34", "Two flips restore flow direction")
	for i: int in range(30):
		model.reset_preview()
		model.move_preview(1 + i % 5, 1 + i % 4)
		model.rotate_preview()
		model.flip_preview()
		model.cancel_preview()
	_check(not model.preview().active and _authority(model) == before, "MODEL-35", "Thirty preview sequences never install equipment or charge money")
	model.reset_preview()
	_check(model.preview().active and model.preview().x == _fixture_data.ghostConstruction.gridPosition.x, "MODEL-36", "Reset restores the fixture preview without rebuilding world state")
	model.set_preview_blocker(func(x: int, z: int, floor_index: int) -> String: return "試作の構造物と重なっています" if x == 2 and z == 3 and floor_index == 0 else "")
	_check(not model.preview().valid and _authority(model) == before and model.fingerprint() == fingerprint, "MODEL-38", "Read-only presentation obstruction can invalidate a preview without changing authority")
	model.set_preview_blocker(Callable())
	_check(model.preview().valid and _authority(model) == before, "MODEL-39", "Removing a presentation-only obstruction context revalidates without changing world data")

func _test_pointer() -> void:
	var script = load("res://scripts/station_pointer_router.gd")
	_check(script != null, "INPUT-01", "Actual pointer router script loads")
	if script == null: return
	var router = script.new()
	root.add_child(router)
	router.set_process(false)
	var counters: Dictionary = {"taps": 0, "drags": 0, "pinches": 0}
	router.entity_tapped.connect(func(_entity: String, _station: String) -> void: counters.taps += 1)
	router.dragged.connect(func(_delta: Vector2, _position: Vector2) -> void: counters.drags += 1)
	router.pinched.connect(func(_delta: float) -> void: counters.pinches += 1)
	router.configure(func(_position: Vector2) -> bool: return false, func(_position: Vector2) -> Dictionary: return {}, func(_entity: String) -> bool: return true)
	router.set_mode("overview")
	var hit: Dictionary = {"entity_id": "TEST-ENTITY", "station_id": "LOC-WAREHOUSE-OVERVIEW"}
	router.process_pointer(1, "down", Vector2(100, 100), false, hit)
	router.process_pointer(1, "up", Vector2(100, 100))
	_check(counters.taps == 1 and router.count() == 0, "INPUT-02", "Ordinary target tap fires once and releases capture")
	router.process_pointer(2, "down", Vector2(100, 100), true, hit)
	router.process_pointer(2, "move", Vector2(180, 120))
	router.process_pointer(2, "up", Vector2(180, 120))
	_check(counters.taps == 1 and counters.drags == 0, "INPUT-03", "UI-origin pointer cannot fall through into world commands")
	router.process_pointer(3, "down", Vector2(100, 100), false, hit)
	router.process_pointer(3, "move", Vector2(140, 100))
	router.process_pointer(3, "up", Vector2(140, 100))
	_check(counters.drags > 0 and counters.taps == 1, "INPUT-04", "Captured world drag does not become a target tap")
	router.process_pointer(4, "down", Vector2(100, 100), false, hit)
	var pre_pair: Dictionary = router.capture_ui_press()
	router.process_pointer(5, "down", Vector2(200, 100), false, hit)
	var during_pair: Dictionary = router.capture_ui_press()
	_check(router.is_pinching() and not router.can_invoke_ui_action(), "INPUT-05", "Two world fingers own pinch and block UI actions")
	router.process_pointer(5, "move", Vector2(250, 100))
	_check(counters.pinches > 0, "INPUT-06", "Distance change emits bounded zoom intent")
	router.process_pointer(4, "up", Vector2(100, 100))
	router.process_pointer(5, "move", Vector2(280, 100))
	router.process_pointer(5, "up", Vector2(280, 100))
	_check(counters.taps == 1, "INPUT-07", "Pinch remainder cannot become an accidental tap")
	_check(not router.ui_press_allowed(pre_pair) and not router.ui_press_allowed(during_pair), "INPUT-08", "UI press before or during pinch stays invalid after fingers lift")
	var fresh: Dictionary = router.capture_ui_press()
	_check(router.ui_press_allowed(fresh), "INPUT-09", "Fresh UI press is accepted after completed gesture")
	router.process_pointer(6, "down", Vector2(90, 90), false, hit)
	router.process_pointer(7, "down", Vector2(190, 90), true)
	var canceled: Dictionary = router.capture_ui_press()
	router.process_pointer(7, "cancel", Vector2(190, 90))
	_check(not router.ui_press_allowed(canceled) and router.count() == 1 and router.world_count() == 1, "INPUT-10", "Individual UI cancellation rejects queued click and preserves other capture")
	router.process_pointer(6, "up", Vector2(90, 90))
	_check(counters.taps == 2, "INPUT-11", "Independent remaining world finger still finishes its tap")
	router.process_pointer(8, "down", Vector2(100, 100), false, hit)
	var before_cancel: Dictionary = router.capture_ui_press()
	router.cancel_all()
	_check(router.count() == 0 and not router.ui_press_allowed(before_cancel), "INPUT-12", "Global interruption empties owners and invalidates held UI")
	router.process_pointer(9, "down", Vector2(100, 100), false, hit)
	router.set_mode("inspection")
	_check(router.count() == 0, "INPUT-13", "Station mode change releases all held pointers")
	router.set_mode("fixed")
	var old_drag_count: int = counters.drags
	router.process_pointer(10, "down", Vector2(100, 100), false, hit)
	router.process_pointer(10, "move", Vector2(180, 100))
	router.process_pointer(10, "up", Vector2(180, 100))
	_check(counters.drags == old_drag_count, "INPUT-14", "Fixed station does not accept free camera drag")
	router.configure(func(_position: Vector2) -> bool: return false, func(_position: Vector2) -> Dictionary: return {}, func(_entity: String) -> bool: return false)
	router.process_pointer(11, "down", Vector2(100, 100), false, hit)
	router.process_pointer(11, "up", Vector2(100, 100))
	_check(counters.taps == 2, "INPUT-15", "Removed entity cannot receive delayed target tap")
	router.queue_free()
	await process_frame

func _test_quality() -> void:
	var script = load("res://scripts/mobile_quality.gd")
	_check(script != null, "QUALITY-01", "Actual quality controller script loads")
	if script == null: return
	var quality = script.new()
	root.add_child(quality)
	quality.set_process(false)
	_check(quality.initialize(root), "QUALITY-02", "Quality profiles initialize on a real Godot viewport")
	var sample: Dictionary = quality.snapshot()
	_check(str(sample.get("thermal_state", "UNKNOWN")) == "UNKNOWN", "QUALITY-03", "Default thermal state is unknown, never inferred from FPS")
	_check(Engine.max_fps == 30, "QUALITY-04", "Balanced mobile default caps rendering at 30 FPS")
	_check(not quality.set_optional_60fps(true, false), "QUALITY-05", "Unverified 60 FPS is denied")
	_check(not quality.set_preset("not-a-profile"), "QUALITY-06", "Unknown quality choice cannot silently apply")
	_check(quality.set_preset("battery_saver") and Engine.max_fps == 30, "QUALITY-07", "Battery saver retains conservative 30 FPS target")
	_check(quality.set_preset("high") and Engine.max_fps == 30, "QUALITY-08", "High visual mode does not automatically unlock 60 FPS")
	quality.set_preset("balanced")
	for i: int in range(100): quality.record_frame_ms(float(i + 1))
	sample = quality.snapshot()
	_check(is_equal_approx(float(sample.get("p95_frame_ms", sample.get("p95_ms", -1))), 95.0) and is_equal_approx(float(sample.get("p99_frame_ms", sample.get("p99_ms", -1))), 99.0), "QUALITY-09", "Measured interval p95/p99 use nearest-rank statistics")
	quality.record_station_switch_ms(1.25)
	sample = quality.snapshot()
	_check(int(sample.get("camera_switch_count", 0)) == 1, "QUALITY-10", "Switch telemetry records actual submitted durations")
	_check(str(sample.get("thermal_state", "UNKNOWN")) == "UNKNOWN", "QUALITY-11", "Poor synthetic frame times do not fabricate a thermal reading")
	quality.set_preset("balanced")
	for i: int in range(100): quality.record_frame_ms(40.0)
	quality.tick_controller(5.0)
	quality.tick_controller(5.0)
	_check(is_equal_approx(float(quality.snapshot().adaptive_relative_scale), 1.0), "QUALITY-13", "Transient overload below fifteen seconds does not change quality")
	quality.tick_controller(5.0)
	_check(is_equal_approx(float(quality.snapshot().adaptive_relative_scale), 0.92), "QUALITY-14", "Sustained overload steps quality down after the hold interval")
	quality.tick_controller(5.0)
	quality.tick_controller(5.0)
	quality.tick_controller(5.0)
	_check(is_equal_approx(float(quality.snapshot().adaptive_relative_scale), 0.92), "QUALITY-15", "Cooldown prevents rapid repeated quality changes")
	quality.tick_controller(5.0)
	_check(is_equal_approx(float(quality.snapshot().adaptive_relative_scale), 0.84), "QUALITY-16", "A further sustained overload waits for cooldown before another step")
	for i: int in range(1800): quality.record_frame_ms(33.333)
	for i: int in range(11): quality.tick_controller(5.0)
	_check(is_equal_approx(float(quality.snapshot().adaptive_relative_scale), 0.84), "QUALITY-17", "Recovery does not jump early during its sixty-second hold")
	quality.tick_controller(5.0)
	_check(is_equal_approx(float(quality.snapshot().adaptive_relative_scale), 0.92), "QUALITY-18", "A normal capped thirty-FPS interval can recover one quality step")
	for i: int in range(2000): quality.record_frame_ms(33.333)
	_check(int(quality.snapshot().sample_count) == 1800, "QUALITY-19", "Frame telemetry remains bounded instead of growing per frame")
	quality.record_frame_ms(NAN)
	quality.record_frame_ms(-1.0)
	quality.record_frame_ms(0.0)
	_check(int(quality.snapshot().sample_count) == 1800, "QUALITY-20", "Invalid frame intervals cannot pollute telemetry")
	quality.set_thermal_hint("WARM", "unit_test_injection")
	var warm_scale: float = float(quality.snapshot().requested_3d_scale)
	quality.set_thermal_hint("HOT", "unit_test_injection")
	_check(float(quality.snapshot().requested_3d_scale) <= warm_scale and Engine.max_fps == 30, "QUALITY-21", "Injected hot hint caps render scale and frame limit")
	_check(not bool(quality.snapshot().hardware_thermal_observed) and not bool(quality.snapshot().thermal_sensor_available), "QUALITY-22", "Injected thermal hint is never presented as observed phone temperature")
	var critical: Dictionary = {"count": 0}
	quality.safety_pause_requested.connect(func(_reason: String) -> void: critical.count += 1)
	quality.set_thermal_hint("CRITICAL", "unit_test_injection")
	_check(critical.count == 1, "QUALITY-23", "Critical hint requests a safety pause instead of altering gameplay state")
	_check(Engine.max_fps <= 10 and int(quality.snapshot().frame_limit) == Engine.max_fps, "QUALITY-28", "Critical safety state actually reduces foreground rendering frequency")
	quality.set_thermal_hint("UNKNOWN")
	_check(not quality.set_thermal_hint("INVENTED-STATE") and quality.snapshot().thermal_state == "UNKNOWN", "QUALITY-24", "Unknown thermal hint preserves the last legitimate state")
	quality.set_preset("high")
	_check(quality.set_optional_60fps(true, true) and Engine.max_fps == 60, "QUALITY-25", "Explicit test verification gate and high preset are both required for optional sixty FPS")
	quality.set_thermal_hint("HOT", "unit_test_injection")
	_check(Engine.max_fps == 30, "QUALITY-26", "A hot hint returns even an explicitly enabled sixty-FPS profile to thirty")
	quality.set_backgrounded(true)
	_check(not quality.is_processing(), "QUALITY-12", "Background state suspends diagnostic processing")
	quality.record_frame_ms(9999.0)
	_check(int(quality.snapshot().sample_count) == 0, "QUALITY-27", "Background duration is excluded from the rendering frame window")
	quality.set_backgrounded(false)
	quality.set_process(false)
	quality.queue_free()
	await process_frame
	var phone_size := SubViewport.new()
	phone_size.size = Vector2i(2778, 1284)
	phone_size.render_target_update_mode = SubViewport.UPDATE_DISABLED
	root.add_child(phone_size)
	var capped = script.new()
	root.add_child(capped)
	capped.set_process(false)
	capped.initialize(phone_size)
	var capped_sample: Dictionary = capped.snapshot()
	_check(int(capped_sample.requested_3d_height) <= 901 and phone_size.size == Vector2i(2778, 1284), "QUALITY-29", "Phone-size viewport requests a capped 3D height while retaining full-resolution 2D viewport")
	_check(not bool(capped_sample.gpu_framebuffer_size_verified), "QUALITY-30", "Requested render-scale settings do not claim verified GPU dimensions")
	capped.set_preset("battery_saver")
	_check(int(capped.snapshot().requested_3d_height) <= 721 and Engine.max_fps == 30, "QUALITY-31", "Battery mode lowers requested 3D height without changing the thirty-FPS cap")
	capped.queue_free()
	phone_size.queue_free()
	await process_frame

func _source_hashes() -> Dictionary:
	var hashes: Dictionary = {}
	for path: String in ["res://scripts/station_model.gd", "res://scripts/station_pointer_router.gd", "res://scripts/mobile_quality.gd", "res://data/mobile_quality_profiles.json", "res://data/prototype_station_fixture_v0_1.json", "res://tests/headless_suite.gd"]:
		if FileAccess.file_exists(path): hashes[path] = FileAccess.get_sha256(path)
	return hashes

func _output_directory(kind: String) -> String:
	var args: PackedStringArray = OS.get_cmdline_user_args()
	for i: int in range(args.size()):
		if args[i] == "--output" and i + 1 < args.size(): return args[i + 1]
	return ProjectSettings.globalize_path("res://../builds/godot-mobile/" + kind)
