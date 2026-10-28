extends Node3D
## Godot native station slice. The fixture is immutable; UI/3D never dispatch logistics.

const ModelScript = preload("res://scripts/station_model.gd")
const WorldScript = preload("res://scripts/station_world.gd")
const HudScript = preload("res://scripts/station_hud.gd")
const RouterScript = preload("res://scripts/station_pointer_router.gd")
const QualityScript = preload("res://scripts/mobile_quality.gd")

var model
var world
var hud
var router
var quality
var is_ready_state := false
var demo_elapsed_seconds := 0.0
var diagnostics_visible := false
var _header_elapsed := 0.0
var _suspended := false
var _thermal_suspended := false
var _last_stat_target := ""
var _local_web_state: Object
var _local_web_cancel_epoch := 0

func _ready() -> void:
	_initialize()

func _initialize() -> void:
	get_tree().quit_on_go_back = false
	var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string("res://data/prototype_station_fixture_v0_1.json"))
	model = ModelScript.new()
	if not parsed is Dictionary or not model.load_fixture(parsed):
		push_error("Invalid isolated prototype fixture; initialization rejected.")
		return
	world = WorldScript.new()
	world.name = "WarehouseWorld"
	add_child(world)
	world.build(parsed.duplicate(true))
	model.set_preview_blocker(world.preview_blocking_reason)
	model.set_anchor_validator(world.has_anchor)
	for binding: Dictionary in world.entity_bindings():
		model.register_target(str(binding.entity_id), str(binding.station_id))
	hud = HudScript.new()
	add_child(hud)
	hud.initialize()
	router = RouterScript.new()
	add_child(router)
	router.configure(hud.is_ui_position, world.raycast_entity, world.has_entity)
	hud.configure_callbacks(router.can_invoke_ui_action, func() -> int: return router.cancellation_version, router.cancel_all)
	hud.station_requested.connect(navigate)
	hud.action_requested.connect(handle_action)
	hud.back_requested.connect(go_back)
	router.entity_tapped.connect(_on_entity_tapped)
	router.dragged.connect(_on_dragged)
	router.pinched.connect(func(delta: float) -> void:
		if not hud.is_modal_open(): world.zoom(delta))
	router.back_requested.connect(go_back)
	quality = QualityScript.new()
	quality.name = "MobileQuality"
	add_child(quality)
	quality.quality_changed.connect(_on_quality_changed)
	quality.safety_pause_requested.connect(_on_safety_pause)
	quality.activity_changed.connect(func(backgrounded: bool) -> void:
		_suspended = backgrounded or _thermal_suspended
		if backgrounded: router.cancel_all())
	if not quality.initialize(get_viewport(), world.stats):
		push_error(quality.last_error)
		return
	is_ready_state = true
	if OS.has_feature("iphone_local_review") and Engine.has_singleton("JavaScriptBridge"):
		var bridge: Object = Engine.get_singleton("JavaScriptBridge")
		_local_web_state = bridge.call("get_interface", "warehouseLocal")
		if _local_web_state != null: _local_web_cancel_epoch = int(_local_web_state.cancelEpoch)
	navigate(str(model.world().initialStation))
	hud.show_feedback("試作データ [P]：タップで作業場所を切替。物流・購入・収益は未実装です。")
	_update_header()

func _input(event: InputEvent) -> void:
	if not is_ready_state: return
	_sync_local_web_cancellation()
	if _suspended: return
	# World/UI capture is resolved before a UI release can invoke an action.
	router.feed_event(event)
	hud.feed_event(event)

func _sync_local_web_cancellation() -> void:
	if _local_web_state == null: return
	var epoch: int = int(_local_web_state.cancelEpoch)
	if epoch == _local_web_cancel_epoch: return
	_local_web_cancel_epoch = epoch
	router.cancel_all()
	hud.cancel_input_captures()

func navigate(station_id: String, target_id: String = "") -> bool:
	if not is_ready_state: return false
	var started := Time.get_ticks_usec()
	if not model.select_station(station_id, target_id):
		hud.show_feedback(model.last_error)
		return false
	router.cancel_all()
	hud.close_overlay()
	var definition: Dictionary = model.current_station()
	if not world.switch_station(station_id, str(definition.get("cameraAnchor", ""))):
		hud.show_feedback(world.last_error)
		return false
	var category := str(definition.get("category", ""))
	var mode := "fixed"
	if category == "Inspection": mode = "inspection"
	elif category == "Warehouse": mode = "overview"
	elif category == "Build": mode = "build"
	router.set_mode(mode)
	hud.show_feedback("")
	if not target_id.is_empty() and not model.get_parcel(target_id).is_empty(): model.select_parcel(target_id)
	world.rotate_inspection(model.inspection_rotation_degrees)
	world.update_inspection_label(model.get_parcel(model.selected_parcel_id))
	_refresh_ghost()
	_refresh_station()
	if category == "Inspection" and model.label_expanded: _show_label()
	quality.record_station_switch_ms((Time.get_ticks_usec() - started) / 1000.0)
	return true

func _on_entity_tapped(entity_id: String, station_id: String) -> void:
	if hud.is_modal_open(): return
	if not world.has_entity(entity_id):
		model.remove_target(entity_id)
		hud.show_feedback("対象は現在利用できません。")
		return
	if navigate(station_id, entity_id): world.focus_entity(entity_id)

func _on_dragged(delta: Vector2, position: Vector2) -> void:
	if hud.is_modal_open(): return
	match str(model.current_station().get("category", "")):
		"Inspection":
			model.inspection_rotation_degrees += delta.x * 0.4
			world.rotate_inspection(model.inspection_rotation_degrees)
		"Warehouse": world.pan(delta)
		"Build":
			var cell: Vector2i = world.screen_to_grid(position)
			var previous: Dictionary = model.preview()
			if previous.x != cell.x or previous.z != cell.y or not previous.active:
				model.move_preview(cell.x, cell.y)
				_refresh_ghost()
				_refresh_station()

func handle_action(id: String) -> void:
	if not is_ready_state: return
	match id:
		"label": _show_label()
		"close-details":
			router.cancel_all()
			hud.close_overlay()
			model.label_expanded = false
		"rotate-parcel":
			model.inspection_rotation_degrees += 90.0
			world.rotate_inspection(model.inspection_rotation_degrees)
		"focus-cargo":
			var runs: Array = model.truck_runs()
			if not runs.is_empty() and not runs[0].cargoParcelIds.is_empty():
				var parcel_id := str(runs[0].cargoParcelIds[0])
				navigate("LOC-INBOUND-01", parcel_id)
				world.focus_entity(parcel_id)
		"bay": _details("搬入ベイ・試作データ", "接車トラックと3箱はFixture表示です。\n荷卸し・手動移送は未実装。箱の所属を変更しません。")
		"gate": _details("現在バッファと前回明細", _gate_details())
		"calendar": _details("出荷日・未確定 [U]", "AIRの曜日表は未確定です。\n本Sprintでは出荷・料金・自動提出を処理しません。")
		"preview-reset": model.reset_preview(); _refresh_ghost(); _refresh_station()
		"preview-rotate": model.rotate_preview(); _refresh_ghost(); _refresh_station()
		"preview-flip": model.flip_preview(); _refresh_ghost(); _refresh_station()
		"preview-cancel": model.cancel_preview(); _refresh_ghost(); _refresh_station()
		"preview-confirm": hud.show_feedback("表示プレビューの確認のみ。本設置・購入・在庫の変更は未実装です。")
		"zoom-in": world.zoom(40.0)
		"zoom-out": world.zoom(-40.0)
		"sorting": _details("仕分け・情報枠", "既設ベルトは表示用です。\n搬送グラフ・自動仕分けは未実装です。")
		"storage": _details("保管・情報枠", "ラック・パレットの表示用見本。\n実容量・入出庫は未実装です。")
		"yard": _details("道路とトラック", "DEV-RUN-001：接車済みの表示見本。\n道路運行・荷役スケジュールは未実装です。")
		"shop": _details("設備カテゴリ・情報枠", "本番の価格・購入・注文・納品は未実装です。")
		"economy": _details("経営・試作データ", "資金・TierはFixture表示のみ。\n収益、購入、License、Billsは未実装です。")
		"diagnostics":
			diagnostics_visible = not diagnostics_visible
			if diagnostics_visible: _details("実行環境の計測・実機判定ではありません", _diagnostic_text())
			else: hud.close_overlay()
		"diagnostics-export":
			var path: String = quality.write_csv()
			hud.show_feedback("計測CSV：" + path if not path.is_empty() else "CSV保存に失敗しました。")
		"quality-battery_saver", "quality-balanced", "quality-high":
			quality.set_preset(id.trim_prefix("quality-"))
			router.cancel_all()
			hud.close_overlay()
			hud.show_feedback("画質設定：" + str(quality.profile_id) + " ／ 30fps上限。実機性能・熱は未検証です。")
		_: hud.show_feedback("この処理は後のSprintで実装します。")

func _action(id: String, label: String) -> Dictionary:
	return {"id": id, "label": label}

func _refresh_station() -> void:
	var category := str(model.current_station().get("category", ""))
	var title := "作業場所"
	var info := "試作データ [P]"
	var actions: Array = []
	match category:
		"Inbound":
			title = "搬入・トラック荷台"
			info = "接車ベイ DEV-DOCK-01\n3箱は試作データ。タップで選択。\n移送・荷卸しは未実装です。"
			actions = [_action("bay", "ベイ情報"), _action("focus-cargo", "積載箱を見る")]
		"Inspection":
			title = "検査台"
			info = model.selected_parcel_id + "\n箱の回転と伝票の表示デモ。\n検査合否・移送は未実装です。"
			actions = [_action("label", "伝票を拡大"), _action("rotate-parcel", "箱を90°回転")]
		"Export":
			var gate: Dictionary = model.gate()
			title = "出荷・AIR"
			info = "現在 %d/%d（例示）\n前回バッチ：正%d・純額 $%d\n提出・収益は未実装。" % [gate.currentBufferedCount, gate.maxCapacity, gate.visualLastBatchSummary.correctCount, gate.visualLastBatchSummary.netUSD]
			actions = [_action("gate", "進捗・前回明細"), _action("calendar", "出荷日パネル")]
		"Build":
			var preview: Dictionary = model.preview()
			title = "建設・表示プレビュー"
			info = "格子 (%d, %d) ／ %d°\n%s" % [preview.x, preview.z, int(preview.rotation_quarter_turns) * 90, preview.reason]
			actions = [_action("preview-reset", "直線ベルト"), _action("preview-rotate", "90°回転"), _action("preview-flip", "IN/OUT反転"), _action("preview-cancel", "取り消す"), _action("preview-confirm", "確認（デモ）")]
		"Warehouse":
			title = "倉庫・俯瞰"
			info = "ドラッグでパン・2本指でズーム。\n設備をタップして作業場所を選択。"
			actions = [_action("zoom-in", "拡大＋"), _action("zoom-out", "縮小−")]
		"Sorting": title = "仕分け"; info = "青色ベルトは表示用です。"; actions = [_action("sorting", "ライン情報")]
		"Storage": title = "保管ラック"; info = "ラック・パレットは表示用です。"; actions = [_action("storage", "保管情報")]
		"Yard": title = "トラックヤード"; info = "道路・接車状態の表示見本。"; actions = [_action("yard", "ベイ・予定")]
		"Shop": title = "ショップ"; info = "設備カテゴリの情報枠です。"; actions = [_action("shop", "設備カテゴリ")]
		"Economy": title = "経営"; info = "収益・購入・進行は未実装。"; actions = [_action("economy", "運営概要")]
	if not model.active_target_id.is_empty(): info += "\n選択：" + model.active_target_id
	hud.show_station(model.active_station_id, title, info, actions)

func _refresh_ghost() -> void:
	if str(model.current_station().get("category", "")) == "Build": world.update_ghost(model.preview())
	else: world.hide_ghost()

func _details(title: String, text: String) -> void:
	router.cancel_all()
	hud.show_detail(title, text)

func _show_label() -> void:
	var parcel: Dictionary = model.get_parcel(model.selected_parcel_id)
	if parcel.is_empty(): hud.show_feedback("荷物が到着していません。"); return
	model.label_expanded = true
	var label: Dictionary = parcel.visibleLabel
	_details("伝票拡大・試作データ", "%s\n区分：%s　国：%s\n旗ID：%s　重量：%dg\n所有：%s / %s\n表示見本。移送・検査判定は未実装です。" % [parcel.id, label.cargo, label.countryName, label.flagId, parcel.massGrams, parcel.owner.kind, parcel.owner.entityId])

func _gate_details() -> String:
	var gate: Dictionary = model.gate()
	var previous: Dictionary = gate.visualLastBatchSummary
	return "現在のバッファ：%d/%d（例示）\n前回：正%d ／ 誤%d\n正箱価値 $%d ／ 手数料 $%d\n前回純額 $%d（現在個数とは別データ）\n提出・収益は未実装です。" % [gate.currentBufferedCount, gate.maxCapacity, previous.correctCount, previous.incorrectCount, previous.correctParcelValueUSD, previous.handlingFeeUSD, previous.netUSD]

func _diagnostic_text() -> String:
	return JSON.stringify(quality.snapshot(), "  ")

func go_back() -> void:
	if not is_ready_state: return
	router.cancel_all()
	if hud.close_overlay(): model.label_expanded = false; return
	var previous: String = model.previous_station_id
	navigate(previous if not previous.is_empty() else str(model.world().initialStation))

func _process(delta: float) -> void:
	if not is_ready_state or _suspended: return
	demo_elapsed_seconds += delta
	_header_elapsed += delta
	if _header_elapsed < 0.5: return
	_header_elapsed = 0.0
	if not model.active_target_id.is_empty() and not world.has_entity(model.active_target_id):
		model.remove_target(model.active_target_id)
		_refresh_station()
		hud.show_feedback("対象がなくなったため、作業場所の表示に戻りました。")
	_update_header()

func _update_header() -> void:
	var values: Dictionary = model.world()
	hud.update_header("試作データ [P]　Day %d %s %s（固定）\n$%d　Tier %d　正/誤：未集計　次便：未実装　表示経過 %02d:%02d" % [values.day, values.weekday, values.time, values.currencyUSD, values.tier, int(demo_elapsed_seconds) / 60, int(demo_elapsed_seconds) % 60])

func _on_quality_changed(profile: Dictionary) -> void:
	if world != null: world.set_shadow_enabled(bool(profile.get("realtime_shadows", false)))
	if quality != null:
		_thermal_suspended = quality.thermal_state == "CRITICAL"
		_suspended = quality.backgrounded or _thermal_suspended

func _on_safety_pause(reason: String) -> void:
	_thermal_suspended = true
	_suspended = true
	if router != null: router.cancel_all()
	if hud != null: hud.show_feedback(reason + " ／ 熱情報の実機連携は未検証です。")

func _notification(what: int) -> void:
	if not is_ready_state: return
	if what == NOTIFICATION_WM_GO_BACK_REQUEST:
		go_back()
	elif what == NOTIFICATION_APPLICATION_FOCUS_OUT or what == NOTIFICATION_APPLICATION_PAUSED:
		router.cancel_all()
	# MobileQuality combines focus and pause state. Its activity signal is the
	# sole authority for resume, so out-of-order OS notifications cannot unpause.
