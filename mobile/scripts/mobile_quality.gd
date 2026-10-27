class_name MobileQuality
extends Node
## Presentation-only budgets and measured frame intervals. No gameplay clock or model writes.
## Render scales are requested settings; this controller cannot certify GPU resolution or phone heat.

signal quality_changed(profile: Dictionary)
signal safety_pause_requested(reason: String)
signal activity_changed(backgrounded: bool)

const PROFILE_PATH := "res://data/mobile_quality_profiles.json"
const FRAME_CAPACITY := 1800
const HISTORY_CAPACITY := 720
const SUPPORTED_THERMAL_STATES := ["UNKNOWN", "NOMINAL", "WARM", "HOT", "CRITICAL"]

var profile_id := "balanced"
var adaptive_ratio := 1.0
var applied_scale := 1.0
var frame_limit := 30
var thermal_state := "UNKNOWN"
var thermal_source := "unavailable"
var backgrounded := false
var last_error := ""
var switch_count := 0
var last_switch_ms := 0.0
var max_switch_ms := 0.0

var _target_viewport: Viewport
var _stats_provider: Callable
var _config: Dictionary = {}
var _profiles: Dictionary = {}
var _adaptive: Dictionary = {}
var _frames := PackedFloat64Array()
var _sort_buffer := PackedFloat64Array()
var _frame_count := 0
var _frame_cursor := 0
var _history: Array[Dictionary] = []
var _history_cursor := 0
var _evaluation_elapsed := 0.0
var _history_elapsed := 0.0
var _overload_seconds := 0.0
var _recovery_seconds := 0.0
var _cooldown_seconds := 0.0
var _last_process_usec := 0
var _initialized := false
var _requested_60fps := false
var _render_loop_was_enabled := true
var _clock_seconds := 0.0
var _scale_ceiling := 1.0
var _last_render_target_size := Vector2i.ZERO
var _frame_sample_source := "none"
var _os_paused := false
var _os_unfocused := false
var _previous_frame_limit := 0


func initialize(target_viewport: Viewport, stats_provider: Callable = Callable()) -> bool:
	last_error = ""
	var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(PROFILE_PATH))
	if not parsed is Dictionary or parsed.get("schema_version", "") != "warehouse.mobile_quality.v1":
		last_error = "Quality profile data is missing or unsupported."
		return false
	_config = parsed.duplicate(true)
	_profiles = _config.get("profiles", {}).duplicate(true)
	_adaptive = _config.get("adaptive", {}).duplicate(true)
	if not _profiles.has("balanced") or not _profiles.has("battery_saver") or not _profiles.has("high"):
		last_error = "Required quality profiles are missing."
		return false
	_previous_frame_limit = Engine.max_fps
	_target_viewport = target_viewport
	_stats_provider = stats_provider
	_frames.resize(FRAME_CAPACITY)
	_sort_buffer.resize(FRAME_CAPACITY)
	_reset_frame_window()
	_initialized = true
	profile_id = str(_config.get("initial_profile", "balanced"))
	if _target_viewport != null and not _target_viewport.size_changed.is_connected(_on_viewport_size_changed):
		_target_viewport.size_changed.connect(_on_viewport_size_changed)
	_apply_profile()
	return true


func set_preset(id: String) -> bool:
	if not _profiles.has(id):
		last_error = "Unknown quality profile: " + id
		return false
	profile_id = id
	adaptive_ratio = minf(1.0, _thermal_ratio_limit())
	_requested_60fps = false
	_reset_controller_window()
	_reset_frame_window()
	_apply_profile()
	return true


func set_optional_60fps(enabled: bool, device_verified: bool = false) -> bool:
	if enabled and (profile_id != "high" or not device_verified):
		last_error = "60fps requires an explicit high-profile selection and representative-device verification."
		return false
	_requested_60fps = enabled
	_reset_controller_window()
	_reset_frame_window()
	_apply_profile()
	return true


func record_frame_ms(milliseconds: float, sample_source: String = "injected_or_external_unverified") -> void:
	if backgrounded or not is_finite(milliseconds) or milliseconds <= 0.0:
		return
	if _frame_sample_source == "none":
		_frame_sample_source = sample_source
	elif _frame_sample_source != sample_source:
		_frame_sample_source = "mixed_sources"
	_frames[_frame_cursor] = milliseconds
	_frame_cursor = (_frame_cursor + 1) % FRAME_CAPACITY
	_frame_count = mini(_frame_count + 1, FRAME_CAPACITY)


func record_station_switch_ms(milliseconds: float) -> void:
	if not is_finite(milliseconds) or milliseconds < 0.0:
		return
	switch_count += 1
	last_switch_ms = milliseconds
	max_switch_ms = maxf(max_switch_ms, milliseconds)


func tick_controller(elapsed_seconds: float) -> void:
	if not _initialized or backgrounded or not is_finite(elapsed_seconds) or elapsed_seconds <= 0.0:
		return
	_clock_seconds += elapsed_seconds
	_cooldown_seconds = maxf(0.0, _cooldown_seconds - elapsed_seconds)
	_evaluation_elapsed += elapsed_seconds
	_history_elapsed += elapsed_seconds
	var interval := float(_adaptive.get("evaluation_interval_seconds", 5.0))
	if _evaluation_elapsed < interval:
		return
	var window_seconds := _evaluation_elapsed
	_evaluation_elapsed = 0.0
	var sample := _frame_statistics()
	if int(sample["sample_count"]) >= int(_adaptive.get("minimum_frame_samples", 90)):
		_assess(float(sample["p95_frame_ms"]), window_seconds)
	if _history_elapsed >= 5.0:
		_history_elapsed = 0.0
		_store_history(snapshot())


func _assess(p95_ms: float, elapsed_seconds: float) -> void:
	var target_budget_ms := 1000.0 / float(maxi(frame_limit, 1))
	var overload_ms := target_budget_ms * float(_adaptive.get("overload_p95_ms", 36.5)) / (1000.0 / 30.0)
	var recovery_ms := target_budget_ms * float(_adaptive.get("recovery_p95_ms", 34.5)) / (1000.0 / 30.0)
	if p95_ms > overload_ms:
		_overload_seconds += elapsed_seconds
		_recovery_seconds = 0.0
	elif p95_ms < recovery_ms and thermal_state in ["UNKNOWN", "NOMINAL"]:
		_recovery_seconds += elapsed_seconds
		_overload_seconds = 0.0
	else:
		_overload_seconds = 0.0
		_recovery_seconds = 0.0
	if _cooldown_seconds > 0.0:
		return
	var old_ratio := adaptive_ratio
	var step := float(_adaptive.get("scale_step", 0.08))
	var minimum := float(_adaptive.get("minimum_relative_scale", 0.66))
	if _overload_seconds >= float(_adaptive.get("overload_hold_seconds", 15.0)):
		adaptive_ratio = maxf(minimum, adaptive_ratio - step)
	elif _recovery_seconds >= float(_adaptive.get("recovery_hold_seconds", 60.0)):
		adaptive_ratio = minf(1.0, adaptive_ratio + step)
	if not is_equal_approx(old_ratio, adaptive_ratio):
		_cooldown_seconds = float(_adaptive.get("change_cooldown_seconds", 20.0))
		_overload_seconds = 0.0
		_recovery_seconds = 0.0
		_apply_profile()


func set_thermal_hint(state: String, source: String = "injected_not_hardware") -> bool:
	## Future OS bridge/test injection entry. There is no sensor backend in this build.
	var normalized := state.to_upper()
	if not normalized in SUPPORTED_THERMAL_STATES:
		last_error = "Unknown thermal hint; previous state was preserved."
		return false
	var next_source := "unavailable" if normalized == "UNKNOWN" else source
	if thermal_state == normalized and thermal_source == next_source:
		return true
	thermal_state = normalized
	thermal_source = next_source
	# Cooling never instantly restores pixels: stable frames must satisfy the recovery hold.
	adaptive_ratio = minf(adaptive_ratio, _thermal_ratio_limit())
	_reset_controller_window()
	_apply_profile()
	if normalized == "CRITICAL":
		safety_pause_requested.emit("Thermal hint is CRITICAL; suspend heavy presentation. Sensor backend is not implemented.")
	return true


func set_backgrounded(value: bool) -> void:
	if value == backgrounded:
		return
	backgrounded = value
	_last_process_usec = 0
	_reset_controller_window()
	_reset_frame_window()
	if backgrounded:
		_render_loop_was_enabled = RenderingServer.render_loop_enabled
		RenderingServer.render_loop_enabled = false
		frame_limit = 5
		Engine.max_fps = frame_limit
		set_process(false)
	else:
		RenderingServer.render_loop_enabled = _render_loop_was_enabled
		set_process(true)
		_apply_profile()
	activity_changed.emit(backgrounded)


func snapshot() -> Dictionary:
	var sample := _frame_statistics()
	var profile := _profile_snapshot()
	var extra: Dictionary = {}
	if _stats_provider.is_valid():
		var supplied: Variant = _stats_provider.call()
		if supplied is Dictionary:
			extra = supplied.duplicate(true)
	return {
		"scope": "runtime_diagnostic_not_device_acceptance",
		"sample_time_seconds": _clock_seconds,
		"utc": Time.get_datetime_string_from_system(true),
		"platform": OS.get_name(),
		"device_model": OS.get_model_name(),
		"godot": Engine.get_version_info().get("string", "unknown"),
		"rendering_method": RenderingServer.get_current_rendering_method(),
		"profile_id": profile_id,
		"frame_limit": Engine.max_fps,
		"requested_frame_limit": frame_limit,
		"sample_count": sample["sample_count"],
		"mean_frame_ms": sample["mean_frame_ms"],
		"p95_frame_ms": sample["p95_frame_ms"],
		"p99_frame_ms": sample["p99_frame_ms"],
		"frame_metric": "wall_clock_callback_interval_including_frame_limit_not_gpu_or_cpu_execution",
		"frame_sample_source": _frame_sample_source,
		"camera_switch_count": switch_count,
		"last_switch_cpu_ms": last_switch_ms,
		"max_switch_cpu_ms": max_switch_ms,
		"switch_metric": "cpu_handler_duration_not_tap_to_photon_latency",
		"requested_3d_scale": applied_scale,
		"adaptive_relative_scale": adaptive_ratio,
		"viewport_render_target_width": _last_render_target_size.x,
		"viewport_render_target_height": _last_render_target_size.y,
		"requested_3d_height": profile["requested_3d_height"],
		"gpu_framebuffer_size_verified": false,
		"total_draw_calls": int(Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME)),
		"total_objects": int(Performance.get_monitor(Performance.RENDER_TOTAL_OBJECTS_IN_FRAME)),
		"total_primitives": int(Performance.get_monitor(Performance.RENDER_TOTAL_PRIMITIVES_IN_FRAME)),
		"render_video_memory_bytes_estimate": int(Performance.get_monitor(Performance.RENDER_VIDEO_MEM_USED)),
		"render_texture_memory_bytes_estimate": int(Performance.get_monitor(Performance.RENDER_TEXTURE_MEM_USED)),
		"tracked_static_memory_bytes": int(Performance.get_monitor(Performance.MEMORY_STATIC)),
		"renderer_monitor_scope": "engine_total_including_ui_not_3d_only",
		"gpu_time_ms": null,
		"gpu_time_status": "UNAVAILABLE",
		"thermal_state": thermal_state,
		"thermal_source": thermal_source,
		"foreground_safety_suspended": thermal_state == "CRITICAL",
		"thermal_sensor_available": false,
		"hardware_thermal_observed": false,
		"thermal_30_minutes": "NOT_RUN",
		"physical_phone_acceptance": "NOT_RUN",
		"backgrounded": backgrounded,
		"world_stats": extra
	}


func write_csv(path: String = "") -> String:
	if path.is_empty():
		path = "user://diagnostics/station-" + Time.get_datetime_string_from_system(true).replace(":", "").replace("-", "") + ".csv"
	var absolute := ProjectSettings.globalize_path(path)
	if DirAccess.make_dir_recursive_absolute(absolute.get_base_dir()) != OK:
		last_error = "Could not create the diagnostic directory."
		return ""
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		last_error = "Could not open the diagnostic CSV."
		return ""
	var columns := PackedStringArray([
		"scope", "utc", "sample_time_seconds", "platform", "device_model", "godot", "rendering_method", "profile_id", "frame_limit", "requested_frame_limit",
		"sample_count", "mean_frame_ms", "p95_frame_ms", "p99_frame_ms", "frame_sample_source", "camera_switch_count", "last_switch_cpu_ms", "max_switch_cpu_ms",
		"requested_3d_scale", "adaptive_relative_scale", "requested_3d_height", "gpu_framebuffer_size_verified", "total_draw_calls",
		"total_objects", "total_primitives", "render_video_memory_bytes_estimate", "render_texture_memory_bytes_estimate", "tracked_static_memory_bytes",
		"gpu_time_status", "thermal_state", "thermal_source", "thermal_sensor_available", "hardware_thermal_observed", "thermal_30_minutes", "physical_phone_acceptance"
	])
	file.store_csv_line(columns)
	var rows := history()
	rows.append(snapshot())
	for row in rows:
		var values := PackedStringArray()
		for key in columns:
			values.append(str(row.get(key, "")))
		file.store_csv_line(values)
	file.flush()
	file.close()
	return path


func history() -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	for index in range(_history.size()):
		var position := (_history_cursor + index) % _history.size() if _history.size() == HISTORY_CAPACITY else index
		result.append(_history[position].duplicate(true))
	return result


func _store_history(row: Dictionary) -> void:
	if _history.size() < HISTORY_CAPACITY:
		_history.append(row)
	else:
		_history[_history_cursor] = row
		_history_cursor = (_history_cursor + 1) % HISTORY_CAPACITY


func _profile_snapshot() -> Dictionary:
	var raw: Dictionary = _profiles.get(profile_id, {})
	var height := maxi(_last_render_target_size.y, 1)
	return {
		"id": profile_id,
		"display_name": raw.get("display_name", profile_id),
		"frame_limit": frame_limit,
		"applied_3d_scale": applied_scale,
		"adaptive_relative_scale": adaptive_ratio,
		"requested_3d_height": int(ceil(float(height) * applied_scale)),
		"detail_density": minf(float(raw.get("detail_density", 0.85)), 0.65) if thermal_state in ["HOT", "CRITICAL"] else float(raw.get("detail_density", 0.85)),
		"realtime_shadows": false,
		"thermal_state": thermal_state,
		"thermal_source": thermal_source,
		"foreground_safety_suspended": thermal_state == "CRITICAL",
		"source_status": "P_TECHNICAL_BUDGET_NOT_DEVICE_MEASUREMENT"
	}


func _apply_profile() -> void:
	if _profiles.is_empty():
		return
	var raw: Dictionary = _profiles[profile_id]
	var size := Vector2i(1280, 720)
	if _target_viewport != null:
		var texture := _target_viewport.get_texture()
		if texture != null and texture.get_size().x > 0 and texture.get_size().y > 0:
			size = texture.get_size()
		else:
			size = Vector2i(_target_viewport.get_visible_rect().size)
	_last_render_target_size = size
	_scale_ceiling = minf(float(raw.get("base_scale", 0.85)), float(raw.get("max_3d_height", 900)) / float(maxi(size.y, 1)))
	var thermal_ratio := _thermal_ratio_limit()
	applied_scale = clampf(_scale_ceiling * minf(adaptive_ratio, thermal_ratio), float(_adaptive.get("absolute_scale_floor", 0.33)), 1.0)
	if backgrounded or thermal_state == "CRITICAL":
		frame_limit = 5
	elif _requested_60fps and profile_id == "high" and thermal_state in ["UNKNOWN", "NOMINAL"]:
		frame_limit = 60
	else:
		frame_limit = 30
	if not backgrounded:
		Engine.max_fps = frame_limit
	if _target_viewport != null:
		_target_viewport.scaling_3d_mode = Viewport.SCALING_3D_MODE_BILINEAR
		_target_viewport.scaling_3d_scale = applied_scale
		_target_viewport.msaa_3d = Viewport.MSAA_DISABLED
	quality_changed.emit(_profile_snapshot())


func _thermal_ratio_limit() -> float:
	if thermal_state == "WARM":
		return 0.84
	if thermal_state in ["HOT", "CRITICAL"]:
		return float(_adaptive.get("minimum_relative_scale", 0.66))
	return 1.0


func _frame_statistics() -> Dictionary:
	if _frame_count == 0:
		return {"sample_count": 0, "mean_frame_ms": 0.0, "p95_frame_ms": 0.0, "p99_frame_ms": 0.0}
	var sum := 0.0
	for index in range(_frame_count):
		var value := _frames[index]
		_sort_buffer[index] = value
		sum += value
	# Sorting happens only for the infrequent controller assessment or explicit snapshot/CSV.
	var sorted := _sort_buffer.slice(0, _frame_count)
	sorted.sort()
	return {
		"sample_count": _frame_count,
		"mean_frame_ms": sum / float(_frame_count),
		"p95_frame_ms": sorted[maxi(0, int(ceil(float(_frame_count) * 0.95)) - 1)],
		"p99_frame_ms": sorted[maxi(0, int(ceil(float(_frame_count) * 0.99)) - 1)]
	}


func _reset_controller_window() -> void:
	_evaluation_elapsed = 0.0
	_overload_seconds = 0.0
	_recovery_seconds = 0.0
	_cooldown_seconds = 0.0


func _reset_frame_window() -> void:
	_frame_count = 0
	_frame_cursor = 0
	_frame_sample_source = "none"
	_last_process_usec = 0


func _on_viewport_size_changed() -> void:
	_apply_profile()


func _process(_delta: float) -> void:
	if not _initialized or backgrounded:
		return
	var now := Time.get_ticks_usec()
	if _last_process_usec != 0:
		var milliseconds := float(now - _last_process_usec) / 1000.0
		record_frame_ms(milliseconds, "process_wall_clock")
		tick_controller(milliseconds / 1000.0)
	_last_process_usec = now


func _notification(what: int) -> void:
	if not _initialized:
		return
	if what == NOTIFICATION_APPLICATION_PAUSED:
		_os_paused = true
	elif what == NOTIFICATION_APPLICATION_RESUMED:
		_os_paused = false
	elif what == NOTIFICATION_APPLICATION_FOCUS_OUT:
		_os_unfocused = true
	elif what == NOTIFICATION_APPLICATION_FOCUS_IN:
		_os_unfocused = false
	else:
		return
	set_backgrounded(_os_paused or _os_unfocused)


func _exit_tree() -> void:
	# Never leave the global renderer disabled when a backgrounded controller is removed.
	if backgrounded:
		RenderingServer.render_loop_enabled = _render_loop_was_enabled
	if _initialized:
		Engine.max_fps = _previous_frame_limit
