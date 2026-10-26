# 全モジュール共通契約・境界条件（Codex実装基準）
文書ID: SPEC-15 / 設計版: 0.4 / 2026-10-26
本書はSPEC-03～11で同じ言葉が違って解釈されることを防ぐための共通契約。未確認事項は[P]/[U]のまま。

## 1. 正確な識別子と参照
| ID型 | 参照する実体 | 同じIDとして使用しないもの |
|---|---|---|
| PackageID | 1つの論理荷物 | 表示用Box GameObject ID |
| WarehouseId | 1倉庫Profileと状態 | プレイヤー選択中の画面 |
| EquipmentId | 設置済み設備1つ | 購入カタログ型番 |
| NodeId | 物流グラフ上の機器/ポートを持つノード | 配置セル番号だけ |
| DockId | 1つの接車区画 | TruckRunId |
| TruckRunId | 運行1便と所属車両の状態 | ShipmentId |
| GateId | 出荷口設備1つ | CargoModeやSubmissionId |
| SubmissionId | 1回分のゲート提出確定 | 個々のPackageId |
| ShipmentId | 外部輸送に割り当てた提出物 | 出荷ゲートそのもの |
| OrderId | 1度のショップ注文 | EquipmentId |
| LedgerEventId | 一度の資金変化 | Unity画面演出イベント |

すべての外部公開コマンドは`Id`と`expectedVersion`（競合検出のため）を要求し、同一の処理依頼を再送しても二度確定しない。

## 2. 所有場所を唯一にする
ある時点で生存している箱pについて、
```text
sum over all OwnerSlot s of Contains(s,p) = 1
ParcelRecord[p].ownerRef = exactly one matching OwnerSlot
```
OwnerSlot種類:
- TRUCK_CARGO: 便と荷台の格納スロット
- HAND_SLOT: プレイヤーに紐付けた手動保持枠。画面を変えても消さない
- FLOOR_SLOT: 床に置ける指定スロット
- INSPECTION_TABLE: 検査台の処理スロット
- CONVEYOR_SLOT: NodeId+LaneId+OccupancySlot+progress01
- RACK_SLOT: RackId+SlotIndex
- PALLET_SLOT: PalletId+SlotIndex
- EXPORT_GATE: GateId+BufferIndex
- SHIPMENT: ShipmentId+ItemIndex（車両と完全に同一にしない）
- COMPLETED_ARCHIVE: 受入/出荷結果の監査記録に移った荷物
- RECOVERY_QUARANTINE: セーブ復元で元Ownerが使えない場合の安全退避枠

見た目だけの3DリジッドボディはこれらのOwnerを自分で変更しない。
- 手で取る→別の場所へ行く（即時カメラ切替）→置くの各段階で、箱はHandSlot内に確かに1個存在する。
- ローダーがRackへ送るのはRackSlotに予約成功してから。
- ゲートが満杯なら、流入しようとした箱は元のConveyorSlotに残る。
- セーブ中の一時的なReservationは別状態であり、2つのOwnerを作らない。

## 3. 検査と輸送を直交させる
```text
InspectionTruth = pass/fail for each active rule
PlayerDecision = None | Approved | Denied
StickerApplied = None | Approved | Denied
ParcelOwner = exactly one OwnerSlot
SubmissionOutcome = NotSubmitted | Correct | Incorrect
ShipmentState = NotAllocated | Allocated | Dispatched | Delivered
PaymentState = Unposted | Posted
```
- 検査済み/Approvedシールを貼付しただけで、所有場所はInspectionTableのまま。
- 真値はUIに漏れない。検査判断を間違えることができる。
- 未提出では`PaymentState=Unposted`。1回の提出金額の確定イベントでLedger登録。
- 2026公式にある Country:None、Criteria:N/A の形式により無条件でIncorrectにしていたバグは修正されたため、特殊値は単純に「文字列が変だからNG」にせず`RuleDefinition`で扱う。[R/2026-09-25公式、原作の正しいバリデーションは[U]]

## 4. コマンドが失敗した時の決まり
```text
TryMovePackage:
  PRE: Package exists, expectedOwner==currentOwner, destination exists/hasCapacity,
       no other command owns transfer lock, compatible warehouse/route.
  COMMIT: update sourceSlot/destinationSlot/Parcel.owner/Parcel.version as a unit.
  POST: exactly 1 Owner, delta(totalParcels)=0.
  FAIL: no stored record changed, clear reasonCode.
```
```text
TryPurchase:
  PRE: balance>=price, tier/license satisfied, catalog version valid, OrderId not used.
  COMMIT: write one LedgerEntry and one OrderId, reduce balance exactly once.
  POST: balance_after = balance_before-price, orderExists.
  FAIL: balance/orders/unlocks unchanged.
```
```text
TrySubmitGate:
  PRE: not already submitted, Gate status accepts submitting, Batch policy satisfied.
  COMMIT: freeze PackageID set, compute correctness using rule+calendar snapshot,
          create Shipment/Submission and write Ledger idempotently.
  POST: same SubmissionId never pays twice.
  FAIL: parcel buffers, money, gate state unchanged.
```
重要: 原作の2026-09-25時点、容量未満ゲートを終業時にClearするボタンは「開発中」であった。基本ポリシーは[P]FULL_ONLY。容量未満の任意提出は自動で許さない。今後公式リリース確認後に追加する。

## 5. 出荷可能曜日と物理受付不可の違い
- `calendarAcceptsModeToday`：ゲーム内曜日の規則。正しい出荷として評価できるかどうか。
- `gateHasPhysicalSpace`：ゲートの容量残。満杯なら手動投入/搬送不可。
- `gateIsOperational`：設備ロック/使用不可状態。操作不可ならハードブロック。
- ゲートが物理的に使えても、貨物区分や曜日の誤投入はゲーム上のミスとして認め、提出時にWrong判定可能（2025攻略準拠）。
- 「今日は送れない」を理由に勝手に荷物が正しい出荷口へテレポートする実装は禁止。

## 6. ロジック時間の分離
- `SimTick`：荷物移送と機械所要時間を進める決定論的なステップ。
- `GameCalendar`：曜日・営業終了・トラック便などを制御する時分。
- `RenderFrame`：アニメーション補間とUI描画。低FPSでもSimTickの意味を変えない。
- `PlatformWallClock`：端末実時刻。配送条件を進める唯一の根拠にしない。
- 原作の検査装置「10秒」が実時間10秒かゲーム時計10秒かは[U]。実装初期[P]はSimTick10秒、現行実機比較後に設定だけ変更可能にする。
- 予定時刻イベントは `lastChecked < dueTime <= currentTime` を採用し、dueTimeとFrameの完全一致を前提にしない。

## 7. 同時処理の確定順序
同じSimTick内で、最終状態が入力順の偶然に左右されないように:
1. UI命令を入力キューに登録。IdempotencyKeyが処理済みなら同じ結果を返す。
2. GameCalendar予定イベントを `(dueTime,priority,eventId)` でソートして予約。
3. Dock割当/車両進行、出荷口/ラックの受入可能容量とコンベアNodeのスロットをSnapshot。
4. 各箱の移動候補を `(sourceNodeId,parcelId)` でソートし、目的地の独占予約。
5. 使用不可/重複の移動を弾く。最終的に有効な移動だけAtomic Commit。
6. 提出/入金/ライセンス進行を関連イベントIDで確定。
7. 新しいReadOnly Snapshotを発行しUIと3Dが使用する。
- [P]並列処理する場合でも確定スレッドはシングルライタで、スレッド競合による箱複製不可。
- 予約時の「下流が同じTickに空く」場合の受入可否は実装規則を統一（初期[P]は前Tickの占有Snapshot基準。空き即再利用を行わない。テストで変更可）。

## 8. 保存とクラッシュ時の整合性
- Runtime中のAtomicCommitだけではストレージへの書込み中断は保証できない。**保存用スナップショット**はTick境界で一貫してコピーする。
- 一時ファイル→データとチェックサムを書込→read-back検証→別世代へ切替→前世代保持。電源断・ストレージ不足に対する機種別のrename/fsync保証は別途検証する。
- Submit/Shop/Arrivalの1回だけ処理するイベントIDとLedgerをスナップショット内へ保存する。イベントジャーナルを採用する場合、State+EventKeysの原子的なスナップショットと適用順序を規定。
- Save時は `schemaVersion`, `catalogVersion`, `tickId`, `lastEventSeq`を保持。ロード後のEventQueue再作成でイベントの二重再適用なし。
- 書込が中断した場合は旧世代の**正しいセーブ**から戻す。最後の数分が失われる可能性がある点は説明しておき、破損データを強行読込しない。
- 記録するのは論理Owner/Node/RunId。UnityのGameObject InstanceIdや毎フレームのTransformを唯一の保存対象にしない。

## 9. 数値とデータの扱い
- 全収益/価格は `MoneyMinorUnit: signed long`。計算中のfloat丸めによる残高誤差を避ける。
- 積載/容量は整数 `0<=count<=capacity`。
- 重量は浮動小数点演算で境界を誤らないよう `massGrams: int`へ正規化し、5.00kg=5000g と 5.01kg=5010g を区別する。
- グリッド位置は整数 `floorIndex,x,z`。表示/配置プレビューはUnity Vector3で良いが、確定時に整数化。
- 流量・速度は`simTicksPerSegment`またはミリ秒等の固定単位とし、描画fpsで変動しない。
- 原作の2025参考価格や2026追加機械設定は`sourceId`,`sourceDate`,`status: R_HISTORICAL|R_CURRENT|P|U`を必ず持たせる。

## 10. 最小Property Test
PBT-001 任意の有効移送10000回後も、箱のOwnerは一意。
PBT-002 倉庫・買い物・出荷のコマンド列をランダム重複しても、同じコマンドの副作用は一度。
PBT-003 ラックとゲートの許容量を全Tick超えない。
PBT-004 同一Dockに同時に2台の車両が接車できない。
PBT-005 ランダムな建設/取消列で、取消したプレビューはInventory/Money/ConveyorGraphに影響しない。
PBT-006 30/60/15fps相当のレンダー条件を変えても同じSimTick列のGameStateハッシュ一致。
PBT-007 不正な曜日に貨物を提出した場合、箱の所有は正しく移動しつつ正誤評価はミス。
PBT-008 LOAD直後のイベント再実行でも出荷/報酬/予定トラック便は増殖しない。
PBT-009 所有者のない箱を意図的に読み込ませると、ロードを安全拒否またはRECOVERY_QUARANTINEへ移す。

## 11. 設計の優先順位
このSPEC-15とSPEC-00のグローバル不変条件は、局所的な仕様説明に曖昧さがあるときの裁定基準。
ただし原作の実機確認が取れた事実は高い優先順位を持つ。実機差分が発覚したら全関連Specとテストを同時更新する。
