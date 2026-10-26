# 荷物データ・検査・承認/拒否 詳細設計
文書ID: SPEC-04 / REQ: REF-002..011,026..030 / 状態: [R]の基準＋[P]の内部規則

## 1. 参照ゲームで確認したルール
- 初期の検査は貨物種類と国名・架空国旗の整合性。[R/S3]。
- 重量クラスはA(≤5kg), B(≤10kg), C(≤15kg), D(≤20kg), E(≤30kg)。2025ガイドの値、現行2026版は照合対象。[R/S3@2025]
- シリアルは数字9桁（XXX XXX XXXの表示例）とキーパッド検証/スキャナー。[R/S3@2025]。
- Contents Requiredの荷物だけ開封、禁止品を確認し再梱包。2025ガイド上の例は漂白剤・薬品、ガス缶、未包装電池、現金、ナイフ（ゲーム内ルール）。[R/S3@2025]。
- スタンプは有効図案と照合。住所自体は検査しない。[R/S3@2025]。
- 緑（Approved）/赤（Denied）ステッカー。1つでも有効中の検査ルールに失敗した場合は拒否。[R/S3]。
- 初期からステッカーが貼られている既検査箱は基本的に再検査不要（2025攻略では初期ステッカー自体が誤っている例はないとの観察）。[R/S3@2025]
- ライセンスを購入するまでは新項目を検査対象に加えない。[R/S3]。

## 2. データ（隠し真値と見えるラベルを混ぜない）
```text
Parcel {
  id: UUID,
  warehouseId: string, batchId: string, sourceTruckRunId?: string,
  appearance: {shape: Box|PolyBag, sizePreset, materialPreset, labelSide, damageMark?},
  label: {cargoIconCode, countryNameCode, flagCode, weightClass?,
          serialPrinted?, requiresContentsInspection, stampDesignCode?,
          displayAddressText, displayedStatusSticker},
  truth: {trueCargoMode, flagValid, cargoIconValid, massKg,
          serialFormatValid, serialVerified, contentsItemCodes[],
          contentsContainsProhibited, stampValid},
  ruleSetVersionAtReceipt: string, activeInspectionRuleIds: string[],
  transport: {state, ownerKind, ownerId, conveyorNodeId?, progress01,
              exportGateId?, shipmentId?, actualTruckRunId?},
  inspection: {stationId?, draftId?, result: Unknown|Approved|Denied,
               evidencePerRule[], repackState},
  payout: {feeScheduleId, exportEventId?, ledgerEntryId?}
}
```
- **truthはプレイヤーUIに渡さず**、InspectorService/ScannerServiceだけが扱う。
- visible labelには意図的に不正な値がある。配送区分マーク自体が無効なら正しい出荷種別と表示を別に保持。
- 国名/旗は著作物のコピーではなく、独自の架空国と旗デザインで「名称との一致を判定する仕組み」を再現。
- 入荷時点のルールバージョンをスナップショットに持たせる。[P] 途中でライセンス購入しても既に受け取った荷物の必須項目が変わらない（現行原作との照合前は設定で変更可能）。
- serial verificationは「9桁形式」と「検証結果」を分離。9桁でも無効な番号を生成できる。

## 3. 荷物の物流状態・検査状態
### PackageTransportState
`Scheduled -> InTruck -> InHand | OnFloorSlot | OnInspectionTable | OnConveyor | OnRack | OnExportGate | InShipment -> Dispatched -> Delivered`。
追加: `ReturnedRejected`, `AwaitingRecovery`。状態は厳密なenum1個ではなく**owner location + logistics progress**で表現し、手渡しや設置解除との整合を守る。
- InHand は手持ち枠に1個だけ。
- OnInspectionTableは原則台の占有スロット1個[P]。
- 一時的な予約は TransferReservation として扱い、所有の二重登録を避ける。

### InspectionDecisionState
`NotStarted -> InProgress -> DecisionApplied(Approved|Denied)`。
- Opened/NeedsRepackは別ステート。開封したままでは他設備へ所有移動できない[P]。
- `ObservedEvidence`: ruleId、検査方法、提示した入力、結果表示（誤操作できる）、時刻、機器ID。**観察済み≠検査結果の必ず正しい判定**。
- 既検査ラベルは `PreCertified` に区別。ゲームルール上の最終合否は真値で保持する。重複のシール上書きは原作自動シール装置では行わない[R/S4]。

## 4. ルール判定の式
`TrueEligibility(parcel, ruleSet) = all(activeRule.evaluate(parcel.truth, parcel.label))`
不明/未観察でも、プレイヤーはApprovedかDeniedを選択できる。[P] 誤判定はゲート提出時の正誤・収支で明らかになる。検査中に正解を答える自動ヘルプを出さない。
個別判定:
- CARGO: label.cargoIconCodeが有効な4種の一つである。
- COUNTRY: (name,flag)が存在する有効な対応か。存在しないマークはNG。
- WEIGHT: truth.massKg <= weightClassMaxKg[label.weightClass]。
- SERIAL: 文字列形式（ASCII数字9桁）かつ認証結果が有効。表示の空白は入力前に正規化する。
- CONTENTS: `requiresContentsInspection==false`ならその項目は評価不要。要検査なら開封時に見つかる prohibited item がない。
- STAMP: stampDesignCodeが現在の有効スタンプ一覧にあること。
- PRELABEL: 正しい既検査シールは検査工程をスキップ可能。[P] 二重貼付禁止。

### 用途別の機器評価
- ManualCheck: プレイヤーが見たり数字入力して確認する。入力が正解と違うなら失敗/再入力。
- MachineScan(inspectionCriterionId): 真値から項目だけ評価し、Valid/Invalidに分岐。未解放ライセンスの項目はNoOpか購入不可（初期推奨は購入不可、詳細要照合）。
- StickerChecker: Approved / Denied / Noneの3分類を独立して返す。
- ContentsDiverter: requiresContentsInspectionのフラグで分岐し、開封ステーションへ。
- StickerApplier: status=Noneの場合のみ適用。ステッカー既存なら上書きしない[R/S4]。

## 5. Manual Inspection操作の順序
INS-UI-01 検査台へ移送した箱を選択し画面を近距離に切替。
INS-UI-02 3D箱を指ドラッグで回転しラベル面を確認。ラベルの「詳細」タップで視認性の高い独自2D拡大ウィンドウ。
INS-UI-03 [規則]で今日のCargo、架空国対応、重量限界、番号形式、禁制品、スタンプ図案、出荷カレンダーを確認。
INS-UI-04 初期はCargo/Countryだけを確認。ライセンス所持時のみ計量ボタン→数値、数字テンキー→検証結果、開封→アイテムを確認→再梱包、スタンプ拡大を順次表示。
INS-UI-05 赤/緑ステッカーを選び、箱のInspectionStatus欄に貼付。処理後は検査台の箱が自動で消えず、次の手動移送または自動搬送待ち。
INS-UI-06 未処理の箱がある場合は「次の荷物」へ無条件ジャンプさせず、原子移動操作が必要。
INS-UI-07 項目ごとの判定を開示するタイミングは検査道具の仕様に依存。シミュレーションの真値は出荷確定まで原則隠す。

## 6. 荷物生成器
入荷便に紐付けた `ParcelGenerator(seed, licenseSetSnapshot, warehouseProfile, difficultyConfig)` で決定的に作る。
- 正常荷物を少なくとも1個生成可能。異常候補（偽マーク、旗の不一致、超過重量、認証番号不正、禁止品、偽スタンプ）を各ルール導入後にのみ有効化。
- 指定seedで同じ荷物が作られるが、リトライで勝手に正解が変わらない。
- 同時に複数の異常を持つ荷物を許可する[P]。1つの異常だけ直してApprovedにしてしまう誤実装を防ぐ。
- `InvalidReason[]` は内部だけに保存。ラベル画像の赤/緑でプレイヤーに事前の正誤を知らせない。
- 少数/重量/大箱/ポリバッグなどの比率を設定ファイル化する。実機再現度検証までは数値を[P]。

## 7. 開封検査
`Sealed -> Opening -> Opened -> Repacking -> Sealed`。
- 開けた後は棚/専用トレーに内容物のオリジナル3Dプロップを表示。開封要否のラベルを読み取る。
- プレイヤーが禁制品を発見できるよう選択・回転する。物品の禁止ルールはデータで設定。
- 再梱包ボタンで中身を同一ParcelIDへ戻す。中身を無限増殖させる実体化は不可。
- 別画面へ移動しても開封状態保持。再開時に内容物が別の箱にならない。

## 8. 重要な例外
| ID | 条件 | 期待結果 |
|---|---|---|
| INS-EX-01 | すでに他処理中の箱をタップ | 選択不可、理由表示 |
| INS-EX-02 | 検査台満杯 | 手持ちスロット/元場所に保持 |
| INS-EX-03 | 3回連続テンキー誤入力 | 状態を失わず再入力可能、誤罰金を加えない |
| INS-EX-04 | 形式は有効だがserial verificationは不正 | 偽のApprovedを検出できる |
| INS-EX-05 | 同じStickerApplierを再通過 | シールを二重発行しない |
| INS-EX-06 | 検査中に日付/ライセンスが変化 | Snapshot RuleVersionに従い、一貫した判定 |
| INS-EX-07 | 開封中にゲーム保存 | 中身・台の占有・梱包状態まで復元 |
| INS-EX-08 | 検査済み箱が機械へ回った | StickerCheckerで適切にバイパス |
| INS-EX-09 | 同一箱を2台で同時に処理要求 | OwnershipVersion CASにより一方のみ成功 |

## 9. テスト（SPEC-11で詳細）
ACC-INS-01 初期2ルール正常/偽国旗/偽貨物マークを別々に検査しApproved/Deniedにできる。
ACC-INS-02 免許購入前後で検査UIと生成器の有効項目が一致する。
ACC-INS-03 A=5kgは許可、5.01kgは拒否（2025資料準拠のテスト）。
ACC-INS-04 数字9桁と英数字の混在を別判定し、手動検証結果を表示。
ACC-INS-05 開封→内容物確認→再梱包→保存→ロードで状態一致。
ACC-INS-06 自動スキャナーと手動が同じ真値を利用し、矛盾なく分岐する。
ACC-INS-07 シールのみが貼られても売上が二重発生しない。
ACC-INS-08 画面外にした箱の論理検査状態と近景に戻した際の表示が同じ。

出典: S3 https://shippobox.com/archives/16245 / S4 https://shippobox.com/archives/16262
