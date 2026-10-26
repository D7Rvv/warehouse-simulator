# 建設グリッド・コンベア・自動検査機 詳細設計
文書ID: SPEC-05 / REQ: REF-021..034,041..047, OWN-004 / 状態: 原作実機のサイズ・価格要照合

## 1. 座標と設置モデル
- `GridCoord(warehouseId,floorIndex,x,z)`: 整数格子。高さ/階層を区別。世界座標換算と描画は別モジュール。
- 原作2025コミュニティ資料では「1 conveyor幅=単位1、床タイル2×2単位、初期区域10×20」と説明される。[R/S5@2025]。この数値は2026版実機照合まで[P]。
- すべての機械は `EquipmentDefinition`（typeId、footprintOffsets、rotate/flip許容、ports、price、maxLevel、visualPrefabId）と `EquipmentInstance`（entityId、originCoord、rotationQuarterTurns、flipped、level、state）を持つ。
- 装置は格子内の占有セットを持つ。壁・床・出口の予約セル、複数階の高さ重なり、スキャナーの複数セルを区別。
- 出荷口、トラック搬入、ラックは独立したNodeで、接続ポートの規格と接続要件を保持する。
- 3Dのメッシュ/Colliderの見た目だけで接続判定しない。**ポートが向かい合い、同一座標辺・同一階層/レベルで適合している場合のみEdgeを作る**。

## 2. ゴースト配置
モード `Idle -> CatalogSelecting -> GhostActive -> PlacementChecking -> Committed | Cancelled`。
1. [建設]→ショップ購入済みインベントリからパーツを選択（その場で購入できる場合の費用はショップ責務で一度だけ精算）。
2. タップ/ドラッグで候補セルへ移動。床へスナップ、方向矢印、足元のハイライト表示。
3. [回転]で90度ずつ方向変更、[反転]で接続の左右/上り下りを変更（対応機種だけ）。
4. `ValidatePlacement`の結果により**有効=緑半透明、ブロック=赤半透明と理由、接続注意=黄アイコン**。
5. [設置]で所有権・セル占有・グラフ再構築・注文インベントリの消費を1トランザクションで実行。
6. [取消]で編集前に戻り、設置済みではない設備と金銭状態を変えない。
7. 機器を撤去/移動/売却する場合、内部荷物・近隣機器・接続辺への影響プレビューを出し、**安全な停止と退避**を先に行う。

**二重課金防止**: ショップで購入して在庫を持つなら設置時には追加費用0。購入即設置方式なら設置確定で1度だけ請求。在庫注文とインスタンス配置を同一注文として2回計上してはいけない。

### PlacementResult
`{valid, blockingReasonCodes[], warningCodes[], occupiedCells[], connectedPortPairs[], inventoryCost, balanceCost, affectedNodeIds[]}`
Blocking: OUTSIDE_AREA / CELL_OCCUPIED / FLOOR_INCOMPATIBLE / NO_ASSET_LICENSE / NOT_ENOUGH_INVENTORY / NOT_ENOUGH_CURRENCY / COLLIDES_STRUCTURE / PORT_INTERNAL_OVERLAP.
Warning: NO_OUTPUT_CONNECTED / NO_INPUT_CONNECTED / EXPORT_DAY_CLOSED / LOOP_DETECTED / STORAGE_NO_LOADER。空き出口へ将来接続するため、未接続そのものはブロックにしない[P]。

## 3. コンベアグラフ
- `ConveyorNode`はInPort[]、OutPort[]、Edge[]と`OccupancySlots`（スロット単位）で表す。
- Port: `{portId, facing:North|East|South|West|Up|Down, gridCell, floorIndex, laneId, routeTag}`。
- 搬送はグラフを辿る。単純に隣接していても、合流専用ポートがない場合は受け渡し不可。[R/S4]
- 各ノードの長さ/速度/処理時間はデータ。簡単なベルトも有限長・荷物の占有間隔をもつ。
- ラック、スキャナー、出口は専用受け渡し契約を持ち、別扱い。
- Loopは無限走行やイベントループを避ける検査と警告を行う。すべてのループを禁止するのではなく、搬送先がない状態を警告する。

### Deterministic Tickアルゴリズム（概念）
```text
Tick(fixedStep):
  1. コマンドキューの受理と所有ID検証
  2. 設備/ゲート/ラックの利用可能容量を固定
  3. 各ノードの候補出力を決定（先着順 + 合流の公平なラウンドロビン）
  4. 受取先の占有を予約、容量と間隔を検証
  5. 全予約の衝突を解消して確定
  6. 転送先登録と移動元解放を同一状態更新で実行
  7. 処理時間・タイムアウト・アラート・統計を更新
  8. 描画用Snapshotを公開
```
- 更新中に直接`Dictionary`をforeachで書き換えない。コマンド/予約による二相的処理。
- 隣接するノード間の同時「A→B」「B→A」は空きスロット予約に基づき順序決定。転送元の一時的空きを二重に使わない。
- ランダムな分岐はSeedとCursorを保存してリプレイ可能にする。メニュー切替で速度が変わらない。
- 渋滞が長引いたら機器/ゲートの不足を警告するが、荷物を消去しない。
- Rendererは`nodeId, laneId, progress01`を補間してスムーズに動かす。画面外でRigidbodiesを常時生成しない。

## 4. コンベア部品の種別とポート仕様
| typeId | 構成 | 動作と重要な制約 |
|---|---|---|
| STRAIGHT | 1In 1Out | 直進・有限速度 |
| CORNER | 1In 1Out 90° | 方向を回転/反転 |
| BRIDGER | 1In 1Out 上部通路 | 別ルートを下で交差させる。接続しない |
| JOINER | 2In 1Out | 合流公平性、入庫順とBackpressure |
| T_JOINER | 多入力 1Out | 2026-09追加。正しいポート数は現行実機調査[P] |
| MULTI_JOINER | 機種ごと多入力1Out | 発表機種。ポート詳細要検証 |
| SMART_SPLITTER | 1In 3Out | 接続先のある支線に配分、詰まりを回避 |
| PAUSER | 1In 1Out | ユーザー操作で停止/再開 |
| AUTO_PAUSER | 1In 1Out | 条件/カレンダーに応じ自動停止。Tier15/価格3万は2026公表[R] |
| SPEEDWAY | 1In 1Out | 高速な運搬。実際の速度倍率は要検証 |
| LAUNCHER | 1In 1Out? | 物理的に弾く演出。方向、強さ、角度に応じ着地点を計算、着地先の占有を保証する保守モデル[P] |
| ELEVATOR | 下/上接続 | 倉庫2の上下階を移動、上方向/下方向に対応 |
| STICKER_CHECKER | 1In 3Out | APPROVED/DENIED/NONEを分類 |
| SCANNER_WEIGHT | 1In 2Out | 項目のValid/Invalid、処理タイマー |
| SCANNER_SERIAL | 1In 2Out | 上に同じ |
| SCANNER_COUNTRY | 1In 2Out | 上に同じ |
| SCANNER_CARGO | 1In 2Out | 上に同じ |
| SCANNER_STAMP | 1In 2Out | 上に同じ |
| SCANNER_CONTENTS | 1In 2Out | 禁制品の判定。開封分岐とは別 |
| CONTENTS_DIVERTER | 1In 2Out[P] | 開封要否で経路分割、現在の出力数は要確認 |
| CARGO_DIVERTER | 1In 3Out | All/Road/Rail/Air/Maritime/None/Other等の出口フィルタ |
| COUNTRY_DIVERTER | 1In?多Out? | 2026新倉庫の国別条件、配線数は現行実機調査 |
| STICKER_APPLIER_APPROVE | 1In 1Out | 未貼付のみ緑シールを貼る |
| STICKER_APPLIER_DENY | 1In 1Out | 未貼付のみ赤シールを貼る |
| LOADER | 1In RackOut | 隣接ラックに収納、満杯なら停止 |
| UNLOADER | RackIn 1Out | 隣接ラックから取り出す、空なら待機 |

※タイプ名は内部の仮識別子。参考作品で存在確認済みの部品名はSPEC-01で管理。未確認の`?`ポートは製品完成前に検証必須。

## 5. スキャナーの処理時間と分岐
- 2025年の攻略では一般スキャナーLv1=10秒、Lv2=5秒、Lv3=1秒（2026版で値変更の可能性）。Sticker Checkerの時間には独立の根拠が必要。
- 正常分岐と異常分岐は出力ポートを左右反転できる。
- 入力キューが満杯であれば上流停止。検査中の箱はその機械で占有状態を保持。
- 一度検査した項目はEvidenceに記録、次段機器で再評価できるが重複報酬は発生しない。
- スキャナーを撤去する前に内部の荷物を出力端へ移送/安全ラックへ退避する。
- Scannerの出力が両方未接続でも購入できるが、利用時に詰まる。設置時に警告。
- 動作中の速度/描画を機器レベルと明確に分離し、視界外でも同じ所要時間で進む。

## 6. ラックと出荷ベイへの接続
- LOADER: 設置時に隣接Rackの一致するポートを認識。接続成功のインジケータ。
- UNLOADER: 所属Rackの保管品から条件を満たす物を取り出して流す。ラックへの誤接続は起動不可。
- Palletは2025ガイドではLoader/Unloaderが直接扱えない[R/S4]。人手の持ち運び容器として別設計。
- 出荷口の最後のポートでは、箱が受け入れ成功した場合のみ`ExportGate`へOwner移動。
- ゲート満杯なら上流を停止。**曜日・誤貨物ラベルだけでは自動流入を防止しない**のが2025攻略で確認できる挙動（間違った投入は後で誤出荷扱い）。これを「正解しか流れない」ガードに変更しない。

## 7. 原作に近い設備の値の保存
- 価格/速度/容量の資料値はSPEC-08の`ReferenceCatalog`に置き、参照年を必ずタグ付け。
- 設置位置などが完全に定義されていない機器は`CannotShipWithoutEvidence=true`。
- 一時的なテストのためのスロット長、速度、インベントリ単価は[P]と記録しデータ定義に逃がす。
- 2026追加機器（T-Joiner, CountryDiverter, Speedway, Elevator, AutoPauser）は元作品の更新を確認してから詳細動作を固定。

## 8. 基本受入テスト
- ACC-CVY-01 Straight→Corner→Scanner→Valid/Deny→Gateで箱1つに対するルートが正しい。
- ACC-CVY-02 隣に置いてもポートが接続されない2ベルト間で荷物が移動しない。
- ACC-CVY-03 同時に複数荷物が合流に来ても消失/重複/積み重なりなし。
- ACC-CVY-04 出口満杯で上流停止、空き回復で再開。
- ACC-CVY-05 ゴースト10回移動・取消で貨幣/荷物/設置済一覧が変わらない。
- ACC-CVY-06 Rotate/Flip時の入出力方向と3D矢印が一致。
- ACC-CVY-07 既存稼働機器撤去で内部の箱が消えず、状態を保ち安全に再開。
- ACC-CVY-08 2階Elevatorでも荷物が一意で、上下移動できる。
- ACC-CVY-09 FPSを落としても装置のスキャン処理時間が変わらない。
- ACC-CVY-10 リロード後の各ポート接続は保存前のグラフと一致。
