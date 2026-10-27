class_name StationPointerRouter
extends Node
## Per-pointer capture. Rendering and camera navigation never own parcel data.
## Call feed_event before StationHud.feed_event from the composition root _input.

signal entity_tapped(entity_id: String, station_id: String)
signal dragged(delta: Vector2, position: Vector2)
signal pinched(delta: float)
signal back_requested

var cancellation_version: int:
	get:
		return _cancellation_version
var mode: String = "fixed"
var drag_threshold: float = 12.0
var _pointers: Dictionary = {}
var _group_suppressed: bool = false
var _cancellation_version: int = 0
var _is_ui: Callable
var _hit_test: Callable
var _entity_valid: Callable
var _mouse_held: bool = false

func configure(is_ui: Callable, hit_test: Callable, entity_valid: Callable = Callable()) -> void:
	_is_ui = is_ui
	_hit_test = hit_test
	_entity_valid = entity_valid

func set_mode(next_mode: String) -> void:
	cancel_all()
	mode = next_mode.to_lower()

func count() -> int:
	return _pointers.size()

func world_count() -> int:
	var total: int = 0
	for pointer_id: int in _pointers:
		var pointer: Dictionary = _pointers[pointer_id]
		if not bool(pointer.ui):
			total += 1
	return total

func is_pinching() -> bool:
	return mode != "fixed" and world_count() == 2

func can_invoke_ui_action() -> bool:
	return not _group_suppressed and world_count() < 2

func capture_ui_press() -> Dictionary:
	return {"eligible": can_invoke_ui_action(), "version": _cancellation_version}

func ui_press_allowed(token: Dictionary) -> bool:
	return bool(token.get("eligible", false)) and int(token.get("version", -1)) == _cancellation_version and can_invoke_ui_action()

func cancel_all() -> void:
	_pointers.clear()
	_group_suppressed = false
	_mouse_held = false
	_cancellation_version += 1

func process_pointer(pointer_id: int, phase: String, position: Vector2, ui_blocked: bool = false, hit: Dictionary = {}) -> void:
	var event_phase: String = phase.to_lower()
	if not position.is_finite():
		if _pointers.has(pointer_id):
			_cancellation_version += 1
			_remove(pointer_id)
		return
	if event_phase == "down":
		_remove(pointer_id)
		var pointer: Dictionary = {
			"start": position, "position": position, "ui": ui_blocked,
			"dragged": false, "grouped": not ui_blocked and _group_suppressed,
			"entity_id": str(hit.get("entity_id", "")), "station_id": str(hit.get("station_id", ""))
		}
		if not ui_blocked and world_count() > 0:
			if not _group_suppressed:
				_cancellation_version += 1
			_group_suppressed = true
			pointer.grouped = true
		_pointers[pointer_id] = pointer
		return
	if not _pointers.has(pointer_id):
		return
	if event_phase == "cancel":
		# A single OS-canceled UI finger invalidates queued UI clicks while the
		# other independent fingers keep their original captures.
		_cancellation_version += 1
		_remove(pointer_id)
		return
	var pointer: Dictionary = _pointers[pointer_id]
	var previous_distance: float = _pair_distance()
	var delta: Vector2 = position - Vector2(pointer.position)
	pointer.position = position
	if position.distance_squared_to(Vector2(pointer.start)) >= drag_threshold * drag_threshold:
		pointer.dragged = true
	if not bool(pointer.ui) and _group_suppressed:
		pointer.grouped = true
	_pointers[pointer_id] = pointer
	if event_phase == "up":
		var should_tap: bool = not bool(pointer.ui) and not bool(pointer.dragged) and not bool(pointer.grouped) and not _group_suppressed and not str(pointer.entity_id).is_empty()
		_remove(pointer_id)
		if should_tap and (not _entity_valid.is_valid() or bool(_entity_valid.call(str(pointer.entity_id)))):
			entity_tapped.emit(str(pointer.entity_id), str(pointer.station_id))
		return
	if event_phase != "move" or bool(pointer.ui):
		return
	if _group_suppressed:
		if mode != "fixed" and world_count() == 2:
			var distance_delta: float = _pair_distance() - previous_distance
			if absf(distance_delta) > 0.001:
				pinched.emit(distance_delta)
		return
	if bool(pointer.dragged) and mode != "fixed":
		dragged.emit(delta, position)

func feed_event(event: InputEvent) -> void:
	if event is InputEventScreenTouch:
		var touch: InputEventScreenTouch = event as InputEventScreenTouch
		if touch.canceled:
			process_pointer(touch.index, "cancel", touch.position)
		elif touch.pressed:
			_begin(touch.index, touch.position)
		else:
			process_pointer(touch.index, "up", touch.position)
	elif event is InputEventScreenDrag:
		var drag: InputEventScreenDrag = event as InputEventScreenDrag
		process_pointer(drag.index, "move", drag.position)
	elif event is InputEventMouseButton:
		var mouse: InputEventMouseButton = event as InputEventMouseButton
		if mouse.button_index == MOUSE_BUTTON_LEFT:
			_mouse_held = mouse.pressed
			if mouse.pressed:
				_begin(-1, mouse.position)
			else:
				process_pointer(-1, "up", mouse.position)
		elif mouse.pressed and mode != "fixed" and not _blocked(mouse.position):
			if mouse.button_index == MOUSE_BUTTON_WHEEL_UP:
				pinched.emit(24.0)
			elif mouse.button_index == MOUSE_BUTTON_WHEEL_DOWN:
				pinched.emit(-24.0)
	elif event is InputEventMouseMotion and _mouse_held:
		process_pointer(-1, "move", (event as InputEventMouseMotion).position)
	elif event is InputEventKey:
		var key: InputEventKey = event as InputEventKey
		if key.pressed and not key.echo and (key.physical_keycode == KEY_ESCAPE or key.keycode == KEY_ESCAPE):
			cancel_all()
			back_requested.emit()

func _begin(pointer_id: int, position: Vector2) -> void:
	var blocked: bool = _blocked(position)
	var hit: Dictionary = {}
	if not blocked and _hit_test.is_valid():
		var value: Variant = _hit_test.call(position)
		if value is Dictionary:
			hit = value
	process_pointer(pointer_id, "down", position, blocked, hit)

func _blocked(position: Vector2) -> bool:
	return _is_ui.is_valid() and bool(_is_ui.call(position))

func _remove(pointer_id: int) -> void:
	_pointers.erase(pointer_id)
	if world_count() == 0:
		_group_suppressed = false

func _pair_distance() -> float:
	if world_count() != 2:
		return 0.0
	var first: Vector2 = Vector2.ZERO
	var found: bool = false
	for pointer_id: int in _pointers:
		var pointer: Dictionary = _pointers[pointer_id]
		if bool(pointer.ui):
			continue
		if not found:
			first = Vector2(pointer.position)
			found = true
		else:
			return first.distance_to(Vector2(pointer.position))
	return 0.0

func _notification(what: int) -> void:
	if what == NOTIFICATION_APPLICATION_FOCUS_OUT or what == NOTIFICATION_APPLICATION_PAUSED or what == NOTIFICATION_WM_WINDOW_FOCUS_OUT:
		cancel_all()
