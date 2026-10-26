# データ契約・コマンド・再現用Fixture 詳細仕様
文書ID: SPEC-15 / 版: 0.4 / 対応: SPEC-04..09, SPEC-11
目的: Codexが型名・ID・状態・入力・出力をバラバラに発明するのを防ぎ、Unity表示側に強く依存しない実装にする。

## 1. 推奨アセンブリ・型の最小仕様
Assembly: Warehouse.Domain（UnityEngineを参照しない）
- readonly struct GameTick(long Value) : Comparable; 原則long整数、現実時間からの変換はClockServiceへ。
- readonly record struct PackageId(Guid Value); EquipmentId; GateId; ShipmentId; TruckRunId; WarehouseId; OrderId; EventId。IDごとに異なる型。
- enum CargoMode { Road,Rail,Air,Maritime,Rejected,ExpressCountry,Invalid }
- enum InspectionVerdict { NotInspected,Approved,Denied }; enum CheckResult { NotRequired,NotPerformed,Pass,Fail }
- enum PackagingState { Sealed,Opened,Repacking }
- enum OwnerKind { TruckSlot,PlayerHand,InspectionTable,Scale,ConveyorNode,RackSlot,PalletSlot,FloorSlot,ExportGate,ShipmentTransit,Finalized }
- enum TransferError { None,NotFound,OwnerMismatch,CapacityFull,InvalidState,WrongWarehouse,VersionConflict,NotUnlocked }
- enum DeviceMode { Inbound,Inspection,Sorting,Storage,Export,Yard,Warehouse,Build,Shop,Economy,Settings }

Domain interfaces (擬似C#):
- IGameClock.GetTick(), AdvanceBy(realTimeDelta), Pause(), Resume(), EndDay().
- IWorldRepository.GetPackage(id), GetEquipment(id), GetGate(id), ExecuteTransaction(actions,expectedVersion).
- IParcelRuleEngine.Evaluate(PackageView, InspectionRuleSnapshot) -> InspectionOutcome.
- IParcelTransfer.TryExecute(TransferCommand) -> CommandResult.
- IConveyorSimulation.Step(tick, ConveyorGraph, WorldState) -> List<DomainEvent>.
- IExportService.TryAddParcel(gateId,packageId), TrySubmit(gateId,idempotencyKey).
- ITruckScheduler.ApplyDueEvents(gameTick), AssignBay(runId, bayId).
- ILedgerService.PostUnique(eventId,delta,reason)。
- ISaveService.SaveSnapshot(slotId,worldVersion), Load(slotId)。
- IStationNavigator.Navigate(screen,stationId), IStationCameraPresenter.Render(WorldSnapshot).

## 2. モデルのスナップショット境界
- 所有場所のenumとIDの組み合わせに空欄を残さない。Package.owner = {kind,ownerEntityId,slot,warehouseId}。
- Package.inspectionTruthは輸送状況を変えない。ステッカーや玩家操作のdecisionは別。
- Package.activeRuleVersionは生成時に固定[P]。License追加前の既存荷物は変化しない。
- スナップショットにはカメラTransformやUnity GameObject InstanceIDを含めず、screenIdとtargetEntityIdのみ保存。
- コンベアポートの予約もWorldState保存対象。再開時にソース箱ID・ターゲット予約IDの参照が健全でなければRecoveryQueueに隔離し、消去はしない。

## 3. Command Envelopeとリターン
全コマンドにrequestId（再送で同じ値）、expectedEntityVersion（CAS）、issuedTick、WarehouseIdを含む。
成功ならAppliedEvents[]とupdatedVersions。失敗なら状態不変でErrorCode。
トランザクション中は「元の所有者から箱を消したが移動先に入らなかった」という部分成功を許さない。
UIはリクエスト中アニメを出せるが、最終データ変更を受ける前にボタンを連打しても同じRequestIdへまとめる。
- TransferPackage: 検証→キャパ予約→保存済みWorldState更新→完了イベント。失敗時は予約解除。
- ApplyDecision: 既に確定した同じ判定ならIdempotent OK、別の判定なら原作ルール照合前はVERSION_CONFLICTとして扱う[P]。
- SubmitGate: 一度提出済みのバッチは別Shipmentへ再出荷しない。
- PurchaseEquipment: WalletとInventory/StoreOrderが一取引で確定。
- AdvanceClock: 予定イベントを重複しないようdueQueueのeventId記録。

## 4. Fixture例: Old Warehouseの初回検査（下記はP値）
JSONイメージ（実装時は厳密な型仕様とschemaVersionを付ける）:
{
  "fixtureId":"F01_FIRST_PARCEL",
  "ruleset":{"version":"old-v1","enabled":["CARGO_ICON","COUNTRY_FLAG"],
             "validCargo":["Road","Rail","Air","Maritime"],
             "validCountryFlagPairs":[["ALBA","FLAG_A"],["NOVA","FLAG_N"]]},
  "packages":[
    {"id":"P-A","country":"ALBA","flag":"FLAG_A","cargo":"Road","sticker":"None","trueVerdict":"Approved"},
    {"id":"P-B","country":"ALBA","flag":"FLAG_N","cargo":"Rail","sticker":"None","trueVerdict":"Denied"}
  ],
  "operations":[
    {"id":"CMD1","type":"Move","packageId":"P-A","from":"TruckSlot:T1:0","to":"PlayerHand:1"},
    {"id":"CMD2","type":"Move","packageId":"P-A","from":"PlayerHand:1","to":"InspectionTable:IT1:0"},
    {"id":"CMD3","type":"Stamp","packageId":"P-A","decision":"Approved"},
    {"id":"CMD4","type":"Move","packageId":"P-A","from":"InspectionTable:IT1:0","to":"PlayerHand:1"},
    {"id":"CMD5","type":"Move","packageId":"P-A","from":"PlayerHand:1","to":"ExportGate:G_ROAD:0"}
  ],
  "expected":{"packageAOwner":"ExportGate:G_ROAD:0","packageBStillOnTruck":true,
              "approvedA":true,"balanceUnchangedBeforeSubmission":true}
}
この例は値、国、位置、設定が**独自のテスト用仮値**。原作の実データ/曜日/画像を再現したと主張しない。
仕様実装では上記文字列位置を強い型で表す。JSON自体はテストFixtureの意図を示す。

## 5. Fixture例: ベルト詰まり
- SourceNode S（1箱P-1進行率100%）、TargetNode T（1スロットをP-2が占有）。
- Step tick 100→P-1はSに残りWAITING_EXIT、TはP-2のみ。
- Tick 101でP-2がRackへ移動→Tに空き。
- Step tick 102→P-1がTへ移動、Sからは削除、P-2はRack。総PackageID集合不変。
- 同じコマンドを重複処理→バージョン検証で結果は一度だけ。

## 6. Fixture例: 重複出荷/報酬
- Gate G_AIR capacity=2[P]、2箱を入れ、ManualSubmit with requestId=SUB-1。
- Shipment SH1は1件だけ作成され、同じSUB-1を再送しても同じResponse。
- LedgerのeventId LEDGER:SH1:SETTLEを2回送信してもWalletDeltaは1回。
- 貨物が別ゲートG_ROADへ移送済み扱いになったらSH1所有中の箱を二重投入できない。

## 7. Fixture例: トラック表示と運行データ
- TruckRun TR1 at tick=1000 enters EN_ROUTE、tick=1200 ENTERING、tick=1300 PARKED、tick=1500 DOOR_OPEN。
- tick=1600 INBOUND画面を閉じ、tick=1900 YARDへ切替。車の位置/状態はTR1データから復元。
- TruckVisualが一時非表示でもTR1.runId不変。2度目のカメラ表示でTR2として増殖しない。
- 1箱手動取り出しでTruckRun.manifestCount-1、PlayerHandCount+1。再描画後の箱数と一致。

## 8. 保存互換の型ID
save.schemaVersion、rulesVersion、warehouseProfileVersion、catalogVersions{equipment,upgrades,licenses,calendar} を保存。
enumの数値やUnity Component InstanceIDを直接saveフォーマットの唯一識別にしない。
新種設備が追加されても旧保存のEquipmentTypeIdが未解決なら、ログを残してUnknownEquipmentPlaceholderで隔離し、所有箱は回収できること。
シミュレーション tick 単位の変更は既存イベント時刻へのmigrationを実装し、ロード後に2重入荷しない。

## 9. 最終不変条件
- foreach package: exists ownerRef exactly once。
- ∑箱のLogicalOwners + ∑Finalized箱 = ∑GeneratedPackageIds。
- no duplicate EquipmentId in active equipment map; GridMapセルのentityIdが必ず実在する。
- no duplicate Submission EventId or Ledger EventId。
- no truckRun assigned to >1 exclusive bay。
- no two PhysicalVisuals bound to same PackageId inside one active scene。
- UI/View changes do not mutate WorldState except for explicit DomainCommand。
- Physics/3D GameObject lifecycle cannot remove logical packages。

## 10. Code Reviewが見る箇所
- ドメイン層がUnityEngineをimportしていないか。
- 画面上の数値を基準に元の業務ロジックを計算していないか。
- 金額/時刻/価格/荷物の数にmagic numberがないか。
- 単一イベントに複数の処理が紐づく場合、途中クラッシュから復元できるか。
- ランタイム描画のSpawn/DespawnがロジックのOnDestroyで箱を消していないか。
