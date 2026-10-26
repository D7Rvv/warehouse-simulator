# 内部アーキテクチャ・データモデル・コマンド・セーブ
文書ID: SPEC-09 / 対応: 全REQ / 技術候補: Unity 6 LTS + URP, C# / 状態: 実機動作未検証

## 1. 最重要設計方針
**ゲームの真実はUnity GameObjectではなく、純粋なデータモデルにある。**
- 3DViewを隠しても荷物・物流・資金・便は進む。
- すべてのコマンドは入力検証→アトミック反映→イベント保存→表示へ通知の順番。
- 参照ゲームの機械を部品ごとに再現するが、倉庫全体をRigidbodyで動かす設計は禁止。
- 明示的なOwnerとシリアル番号/ゲート判定ルールを分離。
- 描画品質の設定が荷物の真値/処理時間/出荷判定を変えてはならない。
- 全てのカタログにSourceStatus(R/D/P/U)とVersionを持つ。

## 2. ディレクトリ案
```text
Assets/
  Game/
    Runtime/
      Core/                  GameLoop, SimClock, EventScheduler, IDs
      Domain/                Parcel, Inspection, Gate, Rack, Conveyor, Truck, Economy
      Services/              Domain commands, rules, routing, settlement
      Persistence/           Snapshots, journal, migration, checksum
      Data/                  ScriptableObject definitions, catalog loader
      Presentation/          CameraStation, SceneBinding, World/Parcel/Truck Views
      UI/                    InputRouter, HUD, Inspector, Construction, Shop
      Graphics/              LOD, culling, texture cache, quality profiles
    Tests/
      EditMode/              Core, domain, numerical tests
      PlayMode/              Navigation, 3D alignment, touch and integration
    Content/
      OriginalWarehouse/
      RegionalWarehouse/
      Art/
Packages/manifest.json
ProjectSettings/
docs/spec/
```
Legacyの`Assets/Scripts/Phase1`には自由歩行版コードがあるため、開発時に明示的に隔離し、新仕様のシーンに自動登録させない。直ちに削除する前に履歴と参照関係を確認する。

## 3. 最小ドメイン集合
```text
GameState
  schemaVersion, catalogVersion, worldVersion, lastCommittedEventSeq,
  SimClockState, CalendarState, RandomSeedState,
  PlayerSessionState {activeLocationId, selectedTargetId?, handSlotParcelId?},
  WarehouseState[] {warehouseId, profileId, tier, unlockedCells[]},
  ParcelRecord[] {parcelId, immutableTruth, mutableLabelStatus, inspection, owner, version},
  ConveyorInstance[] {nodeId, coords, typeId, rotation, flip, level, laneSlots, state},
  RackInstance[] {rackId, itemIds[], capacity, loader, unloader, filters},
  ExportGateInstance[] {gateId, boxIds[], exportMode, calendar, submissionPolicy, status},
  TruckRunRecord[] {runId, dockId?, manifestIds[], state, scheduleEventIds},
  ShipmentRecord[] {shipmentId, submissionId, parcelIds, transportMode, state},
  CatalogUnlockState {licenseIds[], upgrades[], tiers[], orderedItemIds[]},
  EconomicState {moneyMinor, purchaseIds[], immutableLedgerEntries[]},
  ScheduledEventQueue[], AppliedIdempotencyKeys[], UIWorkDrafts[]
```
- ParcelID/NodeID/RackID/GateID/RunID/SubmissionID/ShipmentID/OrderIDは重複しない。
- OwnerRefは`{ownerKind,ownerEntityId,slotId?,version}`で、HandSlot, RackSlot, ConveyorSlot, TruckSlot, InspectionSlot, GateSlot, FloorSlot, Shipmentを区別。
- GameState内で箱の所有参照は必ず1箇所。各コンテナのParcelId一覧と逆参照は矛盾しない。
- InspectionRuleVersionは箱受領時や判定時の規則の参照を明記（原作未検証につきオプション切替を保持）。
- ラベルのプレイヤー可視情報と内部の真値は別モデル。
- Multi-WarehouseはwarehouseIdを必須とし、保存から復元しても箱を違う倉庫へ紐付けない。

## 4. DomainCommand / Result / Event
```text
ICommand {
  commandId: UUID, issuedAtSimTick: long,
  expectedWorldVersion?: long,
  actorKind: Player|Conveyor|Truck|System
}
ICommandResult {
  success, statusCode, affectedEntityIds[], emittedEventIds[], newWorldVersion
}
DomainEvent {
  eventId: UUID, sequence: long, occurredAtSimTick: long,
  eventType, payload, priorEntityVersion, nextEntityVersion
}
```
必須コマンド:
`SelectStation`（UIのみ、DomainEventは不要）、
`PickupParcel(parcelId, expectedOwner)`、
`PlaceParcel(parcelId,targetOwner)`、
`BeginInspection`、
`ApplySticker(parcelId,decision,ruleVersion)`、
`RepackParcel`、
`PlaceEquipment(defId,cell,orientation,inventoryId,previewToken)`、
`MoveEquipment`, `RemoveEquipment`, `ConfigureDiverter`,
`AdvanceConveyorTick`, `BeginSubmission`, `CompleteSubmission`,
`PurchaseItems`, `UnlockLicense`, `UpgradeTier`,
`ScheduleTruckRun`, `AdvanceGameCalendar`, `CloseDay`。
失敗ステータスは機械判別可能（OWNER_CHANGED/CAPACITY_FULL/INSUFFICIENT_FUNDS/OUT_OF_BOUNDS/INVALID_PORT/NO_LICENSE/ALREADY_APPLIED等）。

### MoveParcel（擬似規則）
```text
Transfer(parcelId, fromOwner, toOwner, commandId):
  if appliedCommands.contains(commandId): return previousResult
  if parcels[parcelId].owner != fromOwner: fail OWNER_CHANGED
  if toOwner.invalid || !toOwner.hasCapacity: fail CAPACITY_FULL
  if not allowedTransition(fromOwner,toOwner): fail INVALID_TRANSFER
  reserve(toOwner, parcelId)
  removeFrom(fromOwner, parcelId) + insertInto(toOwner, parcelId)
  parcels[parcelId].owner = toOwner; parcels[parcelId].version++
  commit reservation; recordCommand(commandId); appendEvent(ParcelMoved)
  assertGlobalOwnershipInvariant()
```
上のremove/insert/owner更新とイベント記録はゲームのシミュレーション・スレッド内の1トランザクション、クラッシュ復旧時には一括再適用か一括未適用を保証する。

## 5. 更新順序と決定性
- GameSimulationServiceのシングルライタを基本にし、UnityのUpdateとは別の固定Tick（例10Hz[P]）。
- 毎Tick: (1) user commands入庫 (2) calendar due events (3) dock/truck progression (4) conveyors/rack capacity reservation (5) parcel move commits (6) inspection timers and gate submission (7) economy ledger (8) invariants + read-only snapshot publish。
- 同時予約は`(tick, priority, nodeId, parcelId)`を安定キーにソート。同じ入力seed/コマンド列なら結果が同じ。
- ViewsはSnapshotの差分で更新。表示用TransformのInterpolatedMoveはロジックと分離。
- 低FPS時の処理追いつきを無限ループで行わず、長いスリップは過渡状態とイベントキューで安全に処理。ロジックを黙って捨てない。
- Time.timeScaleはゲーム時計の唯一の基準にしない。明示的にSimClock, GameCalendar, RenderClockを別管理。
- Pathfindingは機械に不要。自由歩行、NPC群衆のナビメッシュは初期不要。

## 6. セーブ形式
```text
SaveEnvelope {
 magic: "WHSE", schemaVersion, appBuildVersion, catalogVersion,
 createdUtcForMetadata, checksum, payloadLength,
 state: GameState
}
```
- JSON/バイナリは性能・移行ツールに合わせて決定。Unity JsonUtilityでDictionaryをそのまま保存できると想定しない。検証済シリアライザーで配列DTOへ変換。
- 保存用Snapshotは**Tickの安定境界で**作成。描画位置の小さな揺れを保存の唯一ソースにしない。
- `autosave`, `manual save`, `onPause`, `sleep`。初期頻度は[P]。OSが即終了させる可能性があるためOnPauseだけに依存しない。
- Tempへ書込→フラッシュ→チェックサム読取検証→同一ファイルシステムで有効ファイルへ安全に置換→直近正常データをバックアップ。実際のrename/fsync特性はiOS/Androidの保存先で検証する。
- 不完全な保存ファイル、カタログ変更、途中の分岐、報酬二重計上をMigration/Recoveryで処理。
- セーブはスロットを識別し、同一スロットにバージョンとUTC更新時刻/ゲーム内時刻のメタ情報を持つ。
- 2026参照作品は20スロットと複数世代バックアップを持つ。実装目標は同水準、モバイルのストレージ負荷を実測する。
- ネットワーク必須にしない。クラウド連携は将来候補であり、初期仕様ではない。

## 7. SnapshotとEventJournalの役割
- Snapshot: 再開に必要なGameState完全像（ID、金額、レベル、マップ、所有関係、トラック便、ゲート内荷物）。
- Journal: Snapshot後の重要な未適用/完了コマンドの最小集合。ゲーム進行中のすべての描画フレームログは不要。
- PersistedAppliedEventKeys: 同一の出荷/購入/荷下ろし/進行イベントを復元時に二重適用しない。
- Load: 校正したCatalogVersion→Snapshot Deserialize→Entity ID integrity→Occupancy rebuilt→Connection graph rebuilt→Command/Event recovery→UI再バインド→3DView割当。
- 3DオブジェクトのUnity InstanceIDやコンポーネントへの生参照はセーブしない。

## 8. 分析ログ/クラッシュ調査
- 非個人情報のLogEvent: `frameMs, cpuMs, gpuMs, memoryMB, simTickTimeMs, visibleBoxes, totalLogicalBoxes, conveyorCount, truckCount, cameraStationId, thermalStatusIfAvailable`。
- 端末でのログ出力頻度を下げ、製品版で毎Tick巨大ログをしない。
- UIに「この荷物が動けない理由」のデバッグ情報を表示可能にする（機器ID、ポート、満杯先など）。
- ランダム種・GameState保存と操作列で不具合を再現可能にする。

## 9. 必須の不変条件チェッカー
```text
assert AllParcelIds.Unique
assert EachLiveParcelHasExactlyOneOwner
assert ForEveryOwner (containedIds == inverseOwnerIndex)
assert NoNodeOccupancyExceedsCapacity
assert NoTwoTrucksOccupySameDock
assert NoDuplicateLedgerIdempotencyKeys
assert NoShipmentParcelBelongsToTwoSubmissions
assert AllConveyorEdgesConnectValidFacingPorts
assert AvailableMoney == InitialMoney + Sum(ledger entries)
assert AllReferencedWarehouseIdsExist
assert AllSavedTiersAndLicensesAreInCatalog
```
本番は重い全検査を毎フレーム行わず、テストモード/周期チェックで検証。

## 10. 受入
QA-ARC-01 同一seed+入力コマンド列が同一GameStateのハッシュになる。
QA-ARC-02 描画/3Dモデル0個でも一日の物流と収益が進む。
QA-ARC-03 保存/ロード後、箱の数・所属・ゲート・便・資金が全一致。
QA-ARC-04 途中切断された保存はバックアップへ安全にフォールバック。
QA-ARC-05 表示カメラを切替えても送料・移動先・時計の結果が変化しない。
QA-ARC-06 1000コマンド重複送信で購入/出荷/積載の二重適用がない。
QA-ARC-07 古いCatalogVersionの保存を移行または安全拒否できる。
QA-ARC-08 古い自由歩行試作のシステムが新しい起動シーンへ混入しない。
