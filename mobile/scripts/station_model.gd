class_name StationModel
extends RefCounted
## S0/S1 view model. Authority is a frozen prototype snapshot, never a ledger,
## transport simulator or a production catalog. Public getters return copies.

signal station_changed(station_id: String, target_id: String)
signal preview_changed(state: Dictionary)

const PREVIEW_MIN: int = -6
const PREVIEW_MAX: int = 6
const STATION_CATEGORIES: Array[String] = [
	"Warehouse", "Inbound", "Inspection", "Sorting", "Storage", "Export",
	"Yard", "Build", "Shop", "Economy", "Contract", "Settings",
]

var ready: bool = false
var last_error: String = ""

var active_station_id: String:
	get: return str(_selection.get("active_station_id", ""))
var active_target_id: String:
	get: return str(_selection.get("active_target_id", ""))
var previous_station_id: String:
	get: return str(_selection.get("previous_station_id", ""))
var selected_parcel_id: String:
	get: return str(_selection.get("selected_parcel_id", ""))
var inspection_draft_id: String:
	get: return str(_selection.get("inspection_draft_id", ""))
var construction_preview_id: String:
	get: return str(_selection.get("construction_preview_id", ""))
var inspection_rotation_degrees: float:
	get: return float(_current_draft().get("rotation_degrees", 0.0))
	set(value):
		if ready and is_finite(value):
			var draft: Dictionary = _current_draft()
			draft["rotation_degrees"] = fposmod(value, 360.0)
var label_expanded: bool:
	get: return bool(_current_draft().get("label_expanded", false))
	set(value):
		if ready:
			var draft: Dictionary = _current_draft()
			draft["label_expanded"] = value
## A reserved S1 view-state field only. Setting it does NOT transfer any parcel.
## A real hand-slot ownership command is intentionally absent until S2.
var hand_slot_parcel_id: String:
	get: return str(_selection.get("hand_slot_parcel_id", ""))
	set(value):
		if ready and (value.is_empty() or _parcels_by_id.has(value)):
			_selection["hand_slot_parcel_id"] = value

var _authority: Dictionary = {}
var _stations_by_id: Dictionary = {}
var _parcels_by_id: Dictionary = {}
var _targets: Dictionary = {}
var _remembered_targets: Dictionary = {}
var _selection: Dictionary = {}
var _drafts: Dictionary = {}
var _active_draft_parcel_id: String = ""
var _preview_state: Dictionary = {}
var _occupied: Dictionary = {}
var _authority_fingerprint: String = ""
var _anchor_validator: Callable = Callable()
var _preview_blocker: Callable = Callable()


func load_fixture(data: Dictionary) -> bool:
	# Validate before committing any state. A rejected load cannot damage a
	# previously loaded model, its selections, or its existing draft.
	if not _json_value(data, 0):
		last_error = "試作データにJSON以外の値または深すぎる構造があります。"
		return false
	var candidate: Dictionary = data.duplicate(true)
	var error: String = _validate(candidate)
	if not error.is_empty():
		last_error = error
		return false
	_authority = candidate
	_stations_by_id = {}
	_parcels_by_id = {}
	for station_value: Variant in _authority["stations"]:
		var definition: Dictionary = station_value
		_stations_by_id[str(definition["id"])] = definition
	for parcel_value: Variant in _authority["packages"]:
		var parcel: Dictionary = parcel_value
		_parcels_by_id[str(parcel["id"])] = parcel
	_freeze_json(_authority)
	_stations_by_id.make_read_only()
	_parcels_by_id.make_read_only()
	_authority_fingerprint = JSON.stringify(_authority, "", true).sha256_text()
	_targets.clear()
	_remembered_targets.clear()
	_drafts.clear()
	_active_draft_parcel_id = ""
	_selection = {
		"active_station_id": str(_authority["world"]["initialStation"]),
		"active_target_id": "", "previous_station_id": "",
		"selected_parcel_id": "", "hand_slot_parcel_id": "",
		"inspection_draft_id": "", "construction_preview_id": "VIEW-PREVIEW-001",
	}
	_occupied.clear()
	var ghost: Dictionary = _authority["ghostConstruction"]
	for cell_value: Variant in ghost["exampleOccupiedCells"]:
		var cell: Dictionary = cell_value
		_occupied[Vector3i(int(cell["x"]), int(cell["floor"]), int(cell["z"]))] = true
	ready = true
	if not _parcels_by_id.is_empty():
		select_parcel(str(_authority["packages"][0]["id"]))
	reset_preview()
	last_error = ""
	return true


func world() -> Dictionary:
	return _dictionary_copy("world")


func gate() -> Dictionary:
	return _dictionary_copy("fakeExportGate")


func stations() -> Array:
	return _array_copy("stations")


func truck_runs() -> Array:
	return _array_copy("mockTruckRuns")


func parcels() -> Array:
	var result: Array = []
	for parcel_value: Variant in _authority.get("packages", []):
		var parcel: Dictionary = parcel_value.duplicate(true)
		parcel.erase("hiddenTruth")
		result.append(parcel)
	return result


func get_parcel(parcel_id: String) -> Dictionary:
	if not _parcels_by_id.has(parcel_id):
		return {}
	var result: Dictionary = _parcels_by_id[parcel_id].duplicate(true)
	# The screen must not disclose an unperformed inspection's hidden verdict.
	result.erase("hiddenTruth")
	return result


func station(station_id: String) -> Dictionary:
	if not _stations_by_id.has(station_id):
		return {}
	return _stations_by_id[station_id].duplicate(true)


func current_station() -> Dictionary:
	return station(active_station_id)


func fingerprint() -> String:
	# Recompute, rather than merely returning a captured digest, so regression
	# tests detect any accidental authority mutation in future implementations.
	if not ready:
		return ""
	return JSON.stringify(_authority, "", true).sha256_text()


func original_fingerprint() -> String:
	return _authority_fingerprint


func set_anchor_validator(validator: Callable) -> void:
	_anchor_validator = validator


## Optional [P] presentation-only clearance context. The callback may explain
## a visible static bench/rack/pallet collision; it cannot commit occupancy,
## inventory, purchases or conveyor connections. S5 replaces this with GridService.
func set_preview_blocker(blocker: Callable) -> void:
	_preview_blocker = blocker
	if ready:
		_validate_preview()


func register_target(entity_id: String, station_id: String) -> bool:
	if not ready or entity_id.strip_edges().is_empty() or not _stations_by_id.has(station_id):
		last_error = "選択対象と作業場所の関連が不正です。"
		return false
	if _targets.has(entity_id) and str(_targets[entity_id]) != station_id:
		last_error = "同じ対象を二つの作業場所へ登録できません。"
		return false
	_targets[entity_id] = station_id
	last_error = ""
	return true


func select_station(station_id: String, target_id: String = "") -> bool:
	if not ready or not _stations_by_id.has(station_id):
		last_error = "作業場所が見つかりません。現在の画面を維持します。"
		return false
	var definition: Dictionary = _stations_by_id[station_id]
	if not bool(definition["enabled"]):
		last_error = str(definition.get("lockedReason", "未解放（条件未設定）"))
		return false
	if _anchor_validator.is_valid() and _anchor_validator.call(str(definition["cameraAnchor"])) != true:
		last_error = "カメラ位置が見つかりません。現在の画面を維持します。"
		return false
	var resolved_target: String = target_id
	if resolved_target.is_empty():
		resolved_target = str(_remembered_targets.get(station_id, ""))
	if not resolved_target.is_empty() and (not _targets.has(resolved_target) or str(_targets[resolved_target]) != station_id):
		last_error = "対象が存在しないか、この作業場所に属していません。"
		return false
	if active_station_id != station_id:
		_selection["previous_station_id"] = active_station_id
	_selection["active_station_id"] = station_id
	_selection["active_target_id"] = resolved_target
	if not resolved_target.is_empty():
		_remembered_targets[station_id] = resolved_target
		if _parcels_by_id.has(resolved_target):
			select_parcel(resolved_target)
	last_error = ""
	station_changed.emit(station_id, resolved_target)
	return true


func remove_target(entity_id: String) -> void:
	_targets.erase(entity_id)
	for station_id: Variant in _remembered_targets.keys():
		if str(_remembered_targets[station_id]) == entity_id:
			_remembered_targets.erase(station_id)
	if active_target_id == entity_id:
		_selection["active_target_id"] = ""
	# Only presentation selection is cleared. Keep the parcel and its draft:
	# removing a visual must not destroy a logical parcel or an editing state.
	if selected_parcel_id == entity_id:
		_selection["selected_parcel_id"] = ""
	last_error = "対象がなくなったため、作業場所の表示へ戻りました。"


func select_parcel(parcel_id: String) -> bool:
	if not ready or not _parcels_by_id.has(parcel_id):
		last_error = "荷物が見つかりません。選択を維持します。"
		return false
	if not _drafts.has(parcel_id):
		_drafts[parcel_id] = {"id": "VIEW-DRAFT-" + parcel_id, "rotation_degrees": 0.0, "label_expanded": false}
	_selection["selected_parcel_id"] = parcel_id
	_active_draft_parcel_id = parcel_id
	_selection["inspection_draft_id"] = str(_drafts[parcel_id]["id"])
	last_error = ""
	return true


func view_snapshot() -> Dictionary:
	var result: Dictionary = _selection.duplicate(true)
	result["inspection_rotation_degrees"] = inspection_rotation_degrees
	result["label_expanded"] = label_expanded
	result["drafts"] = _drafts.duplicate(true)
	return result


func preview() -> Dictionary:
	return _preview_state.duplicate(true)


func move_preview(x: int, z: int, floor_index: int = 0) -> void:
	if not ready:
		return
	_preview_state["x"] = x
	_preview_state["z"] = z
	_preview_state["floor"] = floor_index
	_preview_state["active"] = true
	_validate_preview()


func rotate_preview() -> void:
	if not ready or not bool(_preview_state.get("active", false)):
		return
	_preview_state["rotation_quarter_turns"] = (int(_preview_state["rotation_quarter_turns"]) + 1) % 4
	_validate_preview()


func flip_preview() -> void:
	if not ready or not bool(_preview_state.get("active", false)):
		return
	_preview_state["flipped"] = not bool(_preview_state["flipped"])
	_validate_preview()


func cancel_preview() -> void:
	if not ready:
		return
	# Reset the edited view only; never touch the frozen occupied cells.
	_reset_preview_values()
	_preview_state["active"] = false
	_validate_preview()


func reset_preview() -> void:
	if not ready:
		return
	_reset_preview_values()
	_validate_preview()


func _reset_preview_values() -> void:
	var source: Dictionary = _authority["ghostConstruction"]
	var cell: Dictionary = source["gridPosition"]
	_preview_state = {
		"equipment_type_id": str(source["equipmentTypeId"]),
		"x": int(cell["x"]), "z": int(cell["z"]), "floor": int(cell["floor"]),
		"rotation_quarter_turns": posmod(int(source["rotationQuarterTurns"]), 4),
		"flipped": bool(source["flipped"]), "active": true,
		"valid": false, "reason": "",
	}


func _validate_preview() -> void:
	_preview_state["valid"] = false
	var initial_floor: int = int(_authority["ghostConstruction"]["gridPosition"]["floor"])
	if not bool(_preview_state["active"]):
		_preview_state["reason"] = "プレビュー取消。資金・設備は変更していません。"
	elif int(_preview_state["floor"]) != initial_floor or int(_preview_state["x"]) < PREVIEW_MIN or int(_preview_state["x"]) > PREVIEW_MAX or int(_preview_state["z"]) < PREVIEW_MIN or int(_preview_state["z"]) > PREVIEW_MAX:
		_preview_state["reason"] = "範囲外：試作用グリッドの内側へ移動してください。"
	elif _occupied.has(Vector3i(int(_preview_state["x"]), int(_preview_state["floor"]), int(_preview_state["z"]))):
		_preview_state["reason"] = "セル使用中：既設コンベアと重なっています。"
	else:
		var clearance_reason: String = ""
		if _preview_blocker.is_valid():
			var result: Variant = _preview_blocker.call(int(_preview_state["x"]), int(_preview_state["z"]), int(_preview_state["floor"]))
			if typeof(result) == TYPE_STRING:
				clearance_reason = str(result)
			else:
				# A malformed context must not display a false green success.
				clearance_reason = "配置条件を確認できません。表示用判定 [P] を確認してください。"
		if clearance_reason.is_empty():
			_preview_state["valid"] = true
			_preview_state["reason"] = "配置可能・表示のみ。本設置は後のSprintです。"
		else:
			_preview_state["reason"] = clearance_reason
	preview_changed.emit(preview())


func _current_draft() -> Dictionary:
	if _drafts.has(_active_draft_parcel_id):
		return _drafts[_active_draft_parcel_id]
	return {}


func _dictionary_copy(key: String) -> Dictionary:
	if not ready:
		return {}
	return _authority[key].duplicate(true)


func _array_copy(key: String) -> Array:
	if not ready:
		return []
	return _authority[key].duplicate(true)


func _json_value(value: Variant, depth: int) -> bool:
	if depth > 64:
		return false
	match typeof(value):
		TYPE_NIL, TYPE_BOOL, TYPE_STRING, TYPE_INT:
			return true
		TYPE_FLOAT:
			return is_finite(value)
		TYPE_DICTIONARY:
			for key: Variant in value:
				if typeof(key) != TYPE_STRING or not _json_value(value[key], depth + 1):
					return false
			return true
		TYPE_ARRAY:
			for item: Variant in value:
				if not _json_value(item, depth + 1):
					return false
			return true
	return false


func _freeze_json(value: Variant) -> void:
	if typeof(value) == TYPE_DICTIONARY:
		for child: Variant in value.values():
			_freeze_json(child)
		value.make_read_only()
	elif typeof(value) == TYPE_ARRAY:
		for child: Variant in value:
			_freeze_json(child)
		value.make_read_only()


func _text(source: Dictionary, key: String) -> bool:
	return typeof(source.get(key)) == TYPE_STRING and not str(source[key]).strip_edges().is_empty()


func _flag(source: Dictionary, key: String, expected: bool) -> bool:
	return typeof(source.get(key)) == TYPE_BOOL and bool(source[key]) == expected


func _integer(value: Variant) -> bool:
	if typeof(value) == TYPE_INT:
		return true
	return typeof(value) == TYPE_FLOAT and is_finite(value) and absf(float(value)) <= 9007199254740991.0 and float(value) == floorf(float(value))


func _validate(data: Dictionary) -> String:
	if data.get("schemaVersion") != "prototype_station_fixture_v0.1":
		return "未対応の試作データ形式です。"
	if not _flag(data, "prototypeOnly", true) or not _flag(data, "promoteToProduction", false) or data.get("sourceStatus") != "P" or not _flag(data, "mustNeverOverrideReferenceCatalog", true):
		return "本番カタログは読み込めません。隔離された試作データ [P] が必要です。"
	for key: String in ["world", "ui", "fakeExportGate", "ghostConstruction"]:
		if typeof(data.get(key)) != TYPE_DICTIONARY:
			return "試作データの項目がありません：" + key
	for key: String in ["stations", "packages", "mockTruckRuns"]:
		if typeof(data.get(key)) != TYPE_ARRAY:
			return "試作データの一覧がありません：" + key
	var world_data: Dictionary = data["world"]
	var ui_data: Dictionary = data["ui"]
	if not _text(world_data, "profileId") or not _text(world_data, "weekday") or not _text(world_data, "time") or not _text(world_data, "initialStation"):
		return "試作世界の表示項目が不足しています。"
	if not _integer(world_data.get("currencyUSD")) or int(world_data["currencyUSD"]) < 0 or world_data.get("currencySourceStatus") != "P_DEMO_ONLY":
		return "資金は整数の試作用表示値に限定します。"
	if not _integer(world_data.get("day")) or int(world_data["day"]) < 1 or not _integer(world_data.get("tier")) or int(world_data["tier"]) < 0:
		return "日付またはTierの試作値が不正です。"
	if not _flag(world_data, "simulationClockResetOnCameraChange", false) or not _flag(ui_data, "autoCompleteParcelByMenu", false) or not _flag(ui_data, "showPrototypeWatermark", true):
		return "表示切替で時計や荷物を変更するデータは読み込めません。"
	if ui_data.get("orientation") != "landscape" or ui_data.get("mode") != "tap-station-navigation-no-player-walking":
		return "横向き・自由歩行なしの操作設定が必要です。"
	var station_ids: Dictionary = {}
	for value: Variant in data["stations"]:
		if typeof(value) != TYPE_DICTIONARY:
			return "作業場所の定義が不正です。"
		var definition: Dictionary = value
		for key: String in ["id", "category", "cameraAnchor", "uiPanel"]:
			if not _text(definition, key):
				return "作業場所の項目が不足しています：" + key
		var station_id: String = str(definition["id"])
		if station_ids.has(station_id) or not STATION_CATEGORIES.has(str(definition["category"])) or typeof(definition.get("enabled")) != TYPE_BOOL:
			return "作業場所のID・種類・解放状態が不正です。"
		if not bool(definition["enabled"]) and not _text(definition, "lockedReason"):
			return "未解放の作業場所には理由が必要です。"
		station_ids[station_id] = bool(definition["enabled"])
	var initial_station: String = str(world_data["initialStation"])
	if not station_ids.has(initial_station) or not bool(station_ids[initial_station]):
		return "開始する作業場所が未定義または未解放です。"
	var parcel_ids: Dictionary = {}
	var owner_slots: Dictionary = {}
	for value: Variant in data["packages"]:
		if typeof(value) != TYPE_DICTIONARY:
			return "荷物の定義が不正です。"
		var parcel: Dictionary = value
		if not _text(parcel, "id") or not _flag(parcel, "onlyForTest", true) or parcel_ids.has(str(parcel.get("id", ""))):
			return "試作用荷物のIDが重複または不正です。"
		for key: String in ["owner", "visibleLabel", "hiddenTruth"]:
			if typeof(parcel.get(key)) != TYPE_DICTIONARY:
				return "荷物の所属・伝票・真偽情報が不足しています。"
		var owner: Dictionary = parcel["owner"]
		var label: Dictionary = parcel["visibleLabel"]
		var truth: Dictionary = parcel["hiddenTruth"]
		for key: String in ["kind", "entityId", "slot"]:
			if not _text(owner, key):
				return "荷物の所属情報が不足しています。"
		for key: String in ["cargo", "countryName", "flagId"]:
			if not _text(label, key):
				return "荷物の伝票情報が不足しています。"
		if typeof(truth.get("cargoValid")) != TYPE_BOOL or typeof(truth.get("countryValid")) != TYPE_BOOL or not _integer(parcel.get("massGrams")) or int(parcel["massGrams"]) < 0:
			return "荷物の真偽または重量が不正です。"
		var owner_key: String = JSON.stringify([owner["kind"], owner["entityId"], owner["slot"]])
		if owner_slots.has(owner_key):
			return "同じ所有スロットに二つの荷物があります。"
		owner_slots[owner_key] = true
		parcel_ids[str(parcel["id"])] = parcel
	var run_ids: Dictionary = {}
	var cargo_ids: Dictionary = {}
	for value: Variant in data["mockTruckRuns"]:
		if typeof(value) != TYPE_DICTIONARY:
			return "トラック見本の定義が不正です。"
		var run: Dictionary = value
		if not _text(run, "runId") or not _text(run, "dockId") or not _text(run, "state") or typeof(run.get("cargoParcelIds")) != TYPE_ARRAY or run_ids.has(str(run.get("runId", ""))):
			return "トラック見本のID・ベイ・積載情報が不正です。"
		var run_id: String = str(run["runId"])
		run_ids[run_id] = true
		for id_value: Variant in run["cargoParcelIds"]:
			if typeof(id_value) != TYPE_STRING or not parcel_ids.has(id_value) or cargo_ids.has(id_value):
				return "トラックに未知または重複する荷物があります。"
			var parcel: Dictionary = parcel_ids[id_value]
			if parcel["owner"]["kind"] != "TRUCK_CARGO" or parcel["owner"]["entityId"] != run_id:
				return "トラックの積載一覧と荷物の所属が一致しません。"
			cargo_ids[id_value] = true
	for id_value: Variant in parcel_ids:
		var parcel: Dictionary = parcel_ids[id_value]
		if parcel["owner"]["kind"] == "TRUCK_CARGO" and not cargo_ids.has(id_value):
			return "荷台の荷物がトラック積載一覧にありません。"
	var gate_data: Dictionary = data["fakeExportGate"]
	if not _text(gate_data, "gateId") or not _text(gate_data, "cargoMode") or not _flag(gate_data, "actualSubmitAllowed", false) or not _flag(gate_data, "actualMoneyMutationAllowed", false):
		return "出荷は表示専用の試作データに限定します。"
	if not _integer(gate_data.get("maxCapacity")) or not _integer(gate_data.get("currentBufferedCount")) or int(gate_data["maxCapacity"]) < 0 or int(gate_data["currentBufferedCount"]) < 0 or int(gate_data["currentBufferedCount"]) > int(gate_data["maxCapacity"]):
		return "出荷口の表示個数が不正です。"
	if typeof(gate_data.get("visualLastBatchSummary")) != TYPE_DICTIONARY:
		return "前回バッチの表示明細が不足しています。"
	var batch: Dictionary = gate_data["visualLastBatchSummary"]
	for key: String in ["correctCount", "correctParcelValueUSD", "incorrectCount", "incorrectParcelValueUSD", "handlingFeeUSD", "netUSD"]:
		if not _integer(batch.get(key)):
			return "前回バッチの試作用整数値が不正です：" + key
	if int(batch["correctCount"]) < 0 or int(batch["incorrectCount"]) < 0:
		return "前回バッチの件数が負数です。"
	var ghost: Dictionary = data["ghostConstruction"]
	if not _text(ghost, "equipmentTypeId") or not _flag(ghost, "placementTransactionsEnabled", false) or not _flag(ghost, "noCostOrInventoryMutation", true) or typeof(ghost.get("gridPosition")) != TYPE_DICTIONARY or typeof(ghost.get("exampleOccupiedCells")) != TYPE_ARRAY or not _integer(ghost.get("rotationQuarterTurns")) or typeof(ghost.get("flipped")) != TYPE_BOOL:
		return "建設は資金・在庫を変更しない表示プレビューに限定します。"
	var cells: Array = [ghost["gridPosition"]]
	cells.append_array(ghost["exampleOccupiedCells"])
	var occupied_ids: Dictionary = {}
	for index: int in range(cells.size()):
		if typeof(cells[index]) != TYPE_DICTIONARY:
			return "建設グリッド座標が不正です。"
		var cell: Dictionary = cells[index]
		for key: String in ["x", "z", "floor"]:
			if not _integer(cell.get(key)):
				return "建設グリッドには整数座標が必要です。"
		if index > 0:
			var cell_key: String = JSON.stringify([cell["floor"], cell["x"], cell["z"]])
			if occupied_ids.has(cell_key):
				return "試作占有セルが重複しています。"
			occupied_ids[cell_key] = true
	return ""
