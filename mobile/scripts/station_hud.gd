class_name StationHud
extends CanvasLayer
## One reusable touch HUD. Buttons are visual Controls; real pointer ownership
## and clicks are handled explicitly to preserve native touch IDs and Cancel.
## Call router.feed_event(event) BEFORE this feed_event(event).

signal station_requested(station_id: String)
signal action_requested(action_id: String)
signal back_requested

const NAVIGATION: Array = [
	["搬入", "LOC-INBOUND-01"], ["検査", "LOC-INSPECTION-01"],
	["出荷", "LOC-EXPORT-AIR-01"], ["倉庫", "LOC-WAREHOUSE-OVERVIEW"],
	["建設", "LOC-BUILD-01"]
]
const MORE_STATIONS: Array = [
	["仕分け", "LOC-SORTING-01"], ["保管", "LOC-STORAGE-01"],
	["トラック", "LOC-YARD-01"], ["ショップ", "LOC-SHOP"],
	["経営", "LOC-ECONOMY"], ["契約・未解放", "LOC-CONTRACT"]
]

var _initialized: bool = false
var _root: Control
var _safe: Control
var _top: Panel
var _navigation: Panel
var _action_panel: Panel
var _title: Label
var _header: Label
var _badge: Label
var _feedback: Label
var _info_scroll: ScrollContainer
var _info: Label
var _overlay: Panel
var _overlay_title: Label
var _detail_scroll: ScrollContainer
var _detail_body: Label
var _portrait: Panel
var _portrait_label: Label
var _buttons: Dictionary = {}
var _button_order: Array[Button] = []
var _nav_buttons: Array[Button] = []
var _action_buttons: Array[Button] = []
var _menu_buttons: Array[Button] = []
var _close_button: Button
var _back_button: Button
var _menu_button: Button
var _quality_button: Button
var _presses: Dictionary = {}
var _allow_ui: Callable
var _cancel_version: Callable
var _cancel_all: Callable
var _safe_viewport_rect: Rect2
var _ui_scale: float = 1.0
var _logical_size: Vector2 = Vector2(1280, 720)
var _overlay_mode: String = ""
var _station_id: String = ""
var _description: String = ""
var _feedback_text: String = ""
var _compact: bool = false
var _action_count: int = 0
var _font: Font
var _last_layout_rect: Rect2
var _last_layout_scale: float = 0.0

func _ready() -> void:
	initialize()

func initialize() -> void:
	if _initialized:
		return
	_initialized = true
	layer = 20
	_root = Control.new()
	_root.name = "StationHUD"
	_root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_root)
	if ResourceLoader.exists("res://assets/fonts/warehouse_jp.otf"):
		_font = load("res://assets/fonts/warehouse_jp.otf") as Font
	if _font == null:
		_font = ThemeDB.fallback_font
	var theme: Theme = Theme.new()
	theme.default_font = _font
	theme.default_font_size = 15
	_root.theme = theme
	_safe = Control.new()
	_safe.name = "SafeArea"
	_safe.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_root.add_child(_safe)
	_top = _panel("TopHud", _safe, Color(0.035, 0.055, 0.071, 0.95))
	_navigation = _panel("BottomNavigation", _safe, Color(0.035, 0.055, 0.071, 0.97))
	_action_panel = _panel("ActionPanel", _safe, Color(0.045, 0.072, 0.087, 0.94))
	_back_button = _button("back", "戻る", _top)
	_quality_button = _button("quality-menu", "画質", _top)
	_badge = _label("PrototypeBadge", _top, 12, Color(0.95, 0.76, 0.37))
	_badge.text = "STATION LAB  /  試作データ [P]"
	_badge.hide() # Main header already includes the prototype marker; never add a third row.
	_header = _label("WorldStatus", _top, 16, Color(0.92, 0.96, 0.97))
	_header.autowrap_mode = TextServer.AUTOWRAP_OFF
	_header.clip_text = true
	_header.max_lines_visible = 2
	_header.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_header.add_theme_constant_override("line_spacing", 0)
	for item: Array in NAVIGATION:
		var button: Button = _button("station:" + str(item[1]), str(item[0]), _navigation)
		_nav_buttons.append(button)
	_menu_button = _button("menu", "メニュー", _navigation)
	_title = _label("StationTitle", _action_panel, 20, Color(0.95, 0.98, 0.99))
	_info_scroll = _scroll("StationInfoScroll", _action_panel)
	_info = _label("StationInformation", _info_scroll, 14, Color(0.76, 0.84, 0.86))
	_info.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_info.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	for index: int in range(6):
		var button: Button = _button("action-pool-" + str(index), "", _action_panel)
		button.hide()
		_action_buttons.append(button)
	_feedback = _label("Feedback", _safe, 14, Color(1.0, 0.86, 0.53))
	_feedback.vertical_alignment = VERTICAL_ALIGNMENT_BOTTOM
	_build_overlay()
	_portrait = _panel("LandscapePrompt", _root, Color(0.025, 0.045, 0.06, 0.99))
	_portrait_label = _label("RotateMessage", _portrait, 24, Color(0.93, 0.97, 0.98))
	_portrait_label.text = "端末を横向きにしてください\n\n画面回転ロックを解除すると\n倉庫と操作画面を広く表示できます。"
	_portrait_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_portrait_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	get_viewport().size_changed.connect(_layout)
	_layout()

func configure_callbacks(allow_ui: Callable, cancel_version: Callable, cancel_all: Callable = Callable()) -> void:
	_allow_ui = allow_ui
	_cancel_version = cancel_version
	_cancel_all = cancel_all

func update_header(value: Variant) -> void:
	initialize()
	if value is Dictionary:
		var data: Dictionary = value
		_header.text = "試作データ [P]　Day %s %s %s（固定）\n$%s　Tier %s　正/誤：未集計　次便：未実装" % [
			data.get("day", "—"), data.get("weekday", "—"), data.get("time", "—"),
			data.get("currencyUSD", "—"), data.get("tier", "—")]
	else:
		_header.text = str(value)

func show_station(station_id: String, title: String, information: String, actions: Array = []) -> void:
	initialize()
	_station_id = station_id
	_title.text = title
	_description = information
	_info.text = information
	_info_scroll.scroll_vertical = 0
	_action_count = mini(actions.size(), _action_buttons.size())
	for index: int in range(_action_buttons.size()):
		var button: Button = _action_buttons[index]
		var old_key: String = str(button.get_meta("action_key", ""))
		if _buttons.get(old_key) == button:
			_buttons.erase(old_key)
		button.visible = index < _action_count
		if index >= _action_count:
			continue
		var action: Dictionary = actions[index] if actions[index] is Dictionary else {}
		var key: String = str(action.get("id", ""))
		button.text = str(action.get("label", key))
		button.set_meta("action_key", key)
		_buttons[key] = button
	for button: Button in _nav_buttons:
		button.self_modulate = Color(0.52, 0.95, 0.83) if str(button.get_meta("action_key", "")) == "station:" + station_id else Color.WHITE
	_refresh_feedback()
	_layout()

func show_detail(title: String, text: String) -> void:
	initialize()
	_cancel_captures()
	_overlay_mode = "detail"
	_overlay_title.text = title
	_detail_body.text = text
	_detail_scroll.scroll_vertical = 0
	_overlay.show()
	_detail_scroll.show()
	for button: Button in _menu_buttons:
		button.hide()
	_close_button.show()
	_layout()

func is_modal_open() -> bool:
	return _initialized and _overlay.visible

func close_overlay() -> bool:
	if not _initialized:
		return false
	var had_overlay: bool = _overlay.visible
	if had_overlay:
		_cancel_captures()
	_overlay.hide()
	_overlay_mode = ""
	return had_overlay

func show_feedback(text: String) -> void:
	initialize()
	_feedback_text = text
	_refresh_feedback()

func is_ui_position(position: Vector2) -> bool:
	if not _initialized:
		return false
	if not _safe_viewport_rect.has_point(position) or _portrait.visible or is_modal_open():
		return true
	return _top.get_global_rect().has_point(position) or _navigation.get_global_rect().has_point(position) or _action_panel.get_global_rect().has_point(position)

func button_by_id(key: String) -> Button:
	var cached: Button = _buttons.get(key) as Button
	if is_instance_valid(cached) and cached.is_visible_in_tree():
		return cached
	for index: int in range(_button_order.size() - 1, -1, -1):
		var button: Button = _button_order[index]
		if button.is_visible_in_tree() and str(button.get_meta("action_key", "")) == key:
			return button
	return cached

func button_rect(key: String) -> Rect2:
	var button: Button = button_by_id(key)
	if button == null or not button.is_visible_in_tree():
		return Rect2()
	return button.get_global_rect()

func modal_text() -> String:
	return "" if _detail_body == null else _detail_body.text

func information_text() -> String:
	return _description

func feed_event(event: InputEvent) -> void:
	if not _initialized:
		return
	if event is InputEventScreenTouch:
		var touch: InputEventScreenTouch = event as InputEventScreenTouch
		_pointer(touch.index, "cancel" if touch.canceled else ("down" if touch.pressed else "up"), touch.position)
	elif event is InputEventScreenDrag:
		var drag: InputEventScreenDrag = event as InputEventScreenDrag
		_pointer(drag.index, "move", drag.position)
	elif event is InputEventMouseButton:
		var mouse: InputEventMouseButton = event as InputEventMouseButton
		if mouse.button_index == MOUSE_BUTTON_LEFT:
			_pointer(-1, "down" if mouse.pressed else "up", mouse.position)
		elif mouse.pressed and mouse.button_index in [MOUSE_BUTTON_WHEEL_UP, MOUSE_BUTTON_WHEEL_DOWN]:
			var scroll: ScrollContainer = _scroll_at(mouse.position)
			if scroll != null:
				scroll.scroll_vertical += -42 if mouse.button_index == MOUSE_BUTTON_WHEEL_UP else 42
				get_viewport().set_input_as_handled()
	elif event is InputEventMouseMotion and _presses.has(-1):
		_pointer(-1, "move", (event as InputEventMouseMotion).position)

func _pointer(pointer_id: int, phase: String, position: Vector2) -> void:
	if not position.is_finite():
		_release_visual(pointer_id)
		return
	if phase == "down":
		_release_visual(pointer_id)
		if _portrait.visible:
			return
		var button: Button = _button_at(position)
		if button != null:
			var accepted: bool = _allowed()
			_presses[pointer_id] = {"button": button, "key": str(button.get_meta("action_key", "")), "eligible": accepted, "version": _version(), "start": position, "dragged": false}
			button.modulate = Color(0.65, 0.9, 0.8) if accepted else Color(0.6, 0.6, 0.6)
			get_viewport().set_input_as_handled()
			return
		var scroll: ScrollContainer = _scroll_at(position)
		if scroll != null:
			_presses[pointer_id] = {"scroll": scroll, "scroll_start": scroll.scroll_vertical, "start": position}
			get_viewport().set_input_as_handled()
		return
	if not _presses.has(pointer_id):
		return
	var press: Dictionary = _presses[pointer_id]
	if phase == "cancel":
		_release_visual(pointer_id)
		get_viewport().set_input_as_handled()
		return
	if press.has("scroll"):
		if phase == "move":
			var scroll: ScrollContainer = press.scroll as ScrollContainer
			scroll.scroll_vertical = int(press.scroll_start) - roundi((position.y - Vector2(press.start).y) / _ui_scale)
		elif phase == "up":
			_presses.erase(pointer_id)
		get_viewport().set_input_as_handled()
		return
	if phase == "move":
		if position.distance_squared_to(Vector2(press.start)) > 144.0 * _ui_scale * _ui_scale:
			press.dragged = true
			_presses[pointer_id] = press
		get_viewport().set_input_as_handled()
		return
	if phase == "up":
		var button: Button = press.button as Button
		var accepted: bool = bool(press.eligible) and not bool(press.dragged) and int(press.version) == _version() and _allowed() and is_instance_valid(button) and button.is_visible_in_tree() and button.get_global_rect().has_point(position)
		var key: String = str(press.key)
		_release_visual(pointer_id)
		get_viewport().set_input_as_handled()
		if accepted:
			_activate(key)

func _activate(key: String) -> void:
	if key.begins_with("station:"):
		station_requested.emit(key.trim_prefix("station:"))
	elif key == "menu":
		_show_menu()
	elif key == "quality-menu":
		_show_quality()
	elif key == "close-overlay":
		close_overlay()
		action_requested.emit("close-details")
	elif key == "back":
		back_requested.emit()
	else:
		action_requested.emit(key)

func _allowed() -> bool:
	return not _allow_ui.is_valid() or bool(_allow_ui.call())

func _version() -> int:
	return int(_cancel_version.call()) if _cancel_version.is_valid() else 0

func _release_visual(pointer_id: int) -> void:
	if not _presses.has(pointer_id):
		return
	var press: Dictionary = _presses[pointer_id]
	if press.has("button") and is_instance_valid(press.button):
		(press.button as Button).modulate = Color.WHITE
	_presses.erase(pointer_id)

func _cancel_captures() -> void:
	for pointer_id: int in _presses.keys():
		_release_visual(pointer_id)
	if _cancel_all.is_valid():
		_cancel_all.call()

func _button_at(position: Vector2) -> Button:
	for index: int in range(_button_order.size() - 1, -1, -1):
		var button: Button = _button_order[index]
		if not button.is_visible_in_tree() or button.disabled:
			continue
		if is_modal_open() and not _overlay.is_ancestor_of(button):
			continue
		if button.get_global_rect().has_point(position):
			return button
	return null

func _scroll_at(position: Vector2) -> ScrollContainer:
	if is_modal_open():
		return _detail_scroll if _detail_scroll.visible and _detail_scroll.get_global_rect().has_point(position) else null
	return _info_scroll if _info_scroll.visible and _info_scroll.get_global_rect().has_point(position) else null

func _build_overlay() -> void:
	_overlay = _panel("StationOverlay", _safe, Color(0.035, 0.058, 0.073, 0.985))
	_overlay_title = _label("OverlayTitle", _overlay, 22, Color(0.95, 0.98, 0.99))
	_detail_scroll = _scroll("DetailScroll", _overlay)
	_detail_body = _label("DetailBody", _detail_scroll, 18, Color(0.86, 0.92, 0.94))
	_detail_body.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_detail_body.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	for index: int in range(9):
		var button: Button = _button("menu-pool-" + str(index), "", _overlay)
		_menu_buttons.append(button)
	_close_button = _button("close-overlay", "閉じる", _overlay)
	_overlay.hide()

func _show_menu() -> void:
	_cancel_captures()
	_overlay_mode = "menu"
	_overlay_title.text = "作業場所と設定"
	_overlay.show()
	_detail_scroll.hide()
	_close_button.hide()
	var entries: Array = []
	for item: Array in MORE_STATIONS:
		entries.append({"id": "station:" + str(item[1]), "label": str(item[0])})
	entries.append({"id": "quality-menu", "label": "表示と省電力"})
	entries.append({"id": "diagnostics", "label": "計測表示"})
	entries.append({"id": "close-overlay", "label": "閉じる"})
	_set_menu_entries(entries)
	_layout()

func _show_quality() -> void:
	_cancel_captures()
	_overlay_mode = "quality"
	_overlay_title.text = "画質と消費電力"
	_overlay.show()
	_detail_scroll.hide()
	_close_button.hide()
	_set_menu_entries([
		{"id": "quality-battery_saver", "label": "省電力・30fps"},
		{"id": "quality-balanced", "label": "バランス・30fps"},
		{"id": "quality-high", "label": "高品質・30fps"},
		{"id": "close-overlay", "label": "閉じる"}
	])
	_layout()

func _set_menu_entries(entries: Array) -> void:
	for index: int in range(_menu_buttons.size()):
		var button: Button = _menu_buttons[index]
		var old_key: String = str(button.get_meta("action_key", ""))
		if _buttons.get(old_key) == button:
			_buttons.erase(old_key)
		button.visible = index < entries.size()
		if not button.visible:
			continue
		var item: Dictionary = entries[index]
		button.text = str(item.label)
		button.set_meta("action_key", str(item.id))
		_buttons[str(item.id)] = button

func _refresh_feedback() -> void:
	if _feedback == null:
		return
	_feedback.text = (_description + "\n" if _compact else "") + _feedback_text

func _layout() -> void:
	if not _initialized or not is_inside_tree():
		return
	var viewport_size: Vector2 = get_viewport().get_visible_rect().size
	if viewport_size.x <= 0 or viewport_size.y <= 0:
		return
	_root.size = viewport_size
	var headless: bool = DisplayServer.get_name() == "headless"
	var window_size: Vector2 = viewport_size if headless else Vector2(get_window().size)
	if window_size.x <= 0 or window_size.y <= 0:
		window_size = viewport_size
	var physical_to_viewport: Vector2 = viewport_size / window_size
	var safe_pixels: Rect2i = Rect2i() if headless else DisplayServer.get_display_safe_area()
	var window_position: Vector2 = Vector2.ZERO if headless else Vector2(DisplayServer.window_get_position())
	var safe_rect: Rect2 = Rect2(Vector2.ZERO, window_size)
	if OS.has_feature("mobile") and safe_pixels.size.x > 0 and safe_pixels.size.y > 0:
		safe_rect = Rect2(Vector2(safe_pixels.position) - window_position, Vector2(safe_pixels.size)).intersection(safe_rect)
	_safe_viewport_rect = Rect2(safe_rect.position * physical_to_viewport, safe_rect.size * physical_to_viewport)
	var dpi: float = 160.0 if headless else float(DisplayServer.screen_get_dpi())
	var density: float = dpi / 160.0 if OS.has_feature("mobile") and dpi >= 100 else 1.0
	_ui_scale = maxf(0.75, density * physical_to_viewport.y)
	if _last_layout_rect.size.x > 0 and (_last_layout_rect != _safe_viewport_rect or not is_equal_approx(_last_layout_scale, _ui_scale)):
		_cancel_captures()
	_last_layout_rect = _safe_viewport_rect
	_last_layout_scale = _ui_scale
	_safe.position = _safe_viewport_rect.position
	_safe.scale = Vector2.ONE * _ui_scale
	_logical_size = _safe_viewport_rect.size / _ui_scale
	_safe.size = _logical_size
	var width: float = _logical_size.x
	var height: float = _logical_size.y
	_compact = height < 344
	_portrait.position = Vector2.ZERO
	_portrait.size = viewport_size
	_portrait.visible = viewport_size.y > viewport_size.x
	_portrait_label.add_theme_font_size_override("font_size", roundi(22.0 * _ui_scale))
	_portrait_label.position = Vector2(24, 24)
	_portrait_label.size = viewport_size - Vector2(48, 48)
	_top.position = Vector2.ZERO
	_top.size = Vector2(width, 58)
	_back_button.position = Vector2(7, 5)
	_back_button.size = Vector2(58, 48)
	_quality_button.position = Vector2(width - 65, 5)
	_quality_button.size = Vector2(58, 48)
	_badge.position = Vector2(78, 5)
	_badge.size = Vector2(maxf(0, width - 152), 20)
	_header.position = Vector2(78, 4)
	_header.size = Vector2(maxf(0, width - 152), 50)
	_navigation.position = Vector2(0, height - 60)
	_navigation.size = Vector2(width, 60)
	var nav_width: float = (width - 30) / 6.0
	for index: int in range(_nav_buttons.size()):
		_nav_buttons[index].position = Vector2(5 + index * (nav_width + 4), 6)
		_nav_buttons[index].size = Vector2(nav_width, 48)
	_menu_button.position = Vector2(5 + 5 * (nav_width + 4), 6)
	_menu_button.size = Vector2(nav_width, 48)
	var panel_width: float = clampf(width * 0.31, 248, 286)
	var panel_height: float = maxf(1, height - 134)
	_action_panel.position = Vector2(width - panel_width - 8, 66)
	_action_panel.size = Vector2(panel_width, panel_height)
	_title.position = Vector2(12, 8)
	_title.size = Vector2(panel_width - 24, 29)
	var columns: int = 3 if _action_count > 4 else 2
	var rows: int = maxi(1, ceili(float(_action_count) / columns))
	var action_height: float = rows * 48 + (rows - 1) * 6
	var action_width: float = (panel_width - 24 - (columns - 1) * 6) / columns
	for index: int in range(_action_buttons.size()):
		_action_buttons[index].position = Vector2(12 + (index % columns) * (action_width + 6), panel_height - 12 - action_height + floori(float(index) / columns) * 54)
		_action_buttons[index].size = Vector2(action_width, 48)
	_info_scroll.position = Vector2(12, 44)
	_info_scroll.size = Vector2(panel_width - 24, maxf(24, panel_height - action_height - 68))
	_info_scroll.visible = not _compact
	_info.custom_minimum_size.x = panel_width - 38
	_feedback.position = Vector2(14, 74)
	_feedback.size = Vector2(maxf(60, width - panel_width - 42), maxf(48, height - 148))
	var overlay_width: float = minf(780, width - 24)
	var overlay_height: float = minf(474, height - 24)
	_overlay.position = Vector2((width - overlay_width) * 0.5, (height - overlay_height) * 0.5)
	_overlay.size = Vector2(overlay_width, overlay_height)
	_overlay_title.position = Vector2(18, 12)
	_overlay_title.size = Vector2(overlay_width - 36, 32)
	_detail_scroll.position = Vector2(18, 54)
	_detail_scroll.size = Vector2(overlay_width - 36, maxf(24, overlay_height - 122))
	_detail_body.custom_minimum_size.x = overlay_width - 50
	_close_button.position = Vector2(18, overlay_height - 60)
	_close_button.size = Vector2(overlay_width - 36, 48)
	var menu_cell_width: float = (overlay_width - 48) / 3.0
	var menu_cell_height: float = maxf(48, minf(74, (overlay_height - 70) / 3.0))
	for index: int in range(_menu_buttons.size()):
		_menu_buttons[index].position = Vector2(18 + (index % 3) * (menu_cell_width + 6), 54 + floori(float(index) / 3.0) * (menu_cell_height + 6))
		_menu_buttons[index].size = Vector2(menu_cell_width, menu_cell_height)
	_refresh_feedback()

func _panel(node_name: String, parent: Node, color: Color) -> Panel:
	var panel: Panel = Panel.new()
	panel.name = node_name
	panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = color
	style.border_color = Color(0.25, 0.35, 0.38, 0.4)
	style.set_border_width_all(1)
	style.set_corner_radius_all(9)
	panel.add_theme_stylebox_override("panel", style)
	parent.add_child(panel)
	return panel

func _label(node_name: String, parent: Node, font_size: int, color: Color) -> Label:
	var label: Label = Label.new()
	label.name = node_name
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	label.add_theme_font_size_override("font_size", font_size)
	label.add_theme_color_override("font_color", color)
	label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	parent.add_child(label)
	return label

func _button(key: String, caption: String, parent: Node) -> Button:
	var button: Button = Button.new()
	button.name = key.replace(":", "-")
	button.text = caption
	button.mouse_filter = Control.MOUSE_FILTER_IGNORE
	button.focus_mode = Control.FOCUS_NONE
	button.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	button.add_theme_font_size_override("font_size", 16)
	var normal: StyleBoxFlat = StyleBoxFlat.new()
	normal.bg_color = Color(0.095, 0.15, 0.18, 1.0)
	normal.border_color = Color(0.29, 0.4, 0.43, 1.0)
	normal.set_border_width_all(1)
	normal.set_corner_radius_all(7)
	button.add_theme_stylebox_override("normal", normal)
	button.add_theme_stylebox_override("hover", normal)
	button.add_theme_stylebox_override("pressed", normal)
	button.set_meta("action_key", key)
	parent.add_child(button)
	_buttons[key] = button
	_button_order.append(button)
	return button

func _scroll(node_name: String, parent: Node) -> ScrollContainer:
	var scroll: ScrollContainer = ScrollContainer.new()
	scroll.name = node_name
	scroll.mouse_filter = Control.MOUSE_FILTER_IGNORE
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	scroll.follow_focus = false
	parent.add_child(scroll)
	return scroll

func _notification(what: int) -> void:
	if what == NOTIFICATION_APPLICATION_FOCUS_OUT or what == NOTIFICATION_APPLICATION_PAUSED or what == NOTIFICATION_WM_WINDOW_FOCUS_OUT:
		for pointer_id: int in _presses.keys():
			_release_visual(pointer_id)
