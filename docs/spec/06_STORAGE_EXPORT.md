# 保管・輸送区分・出荷口・便・精算 詳細設計
文書ID: SPEC-06 / 版: 0.4 / 対応: REF-012..018,032..033,038..040 / 状態: [R]ルール存在、[P]イベント境界、[U]容量/金額

## 1. 重要: 4つの独立概念
CargoMode＝荷物に指定された輸送区分（ROAD/RAIL/AIR/MARITIME）であり車両そのものではない。
ExportGate＝敷地内の投入先設備。GateID、対象区分、運用曜日、充填状態を持つ。航空アイコンがあっても飛行機が倉庫へ着陸するわけではない。
Shipment＝出荷口での1回の提出バッチ。ShipmentID、対象PackageIDs、正誤の集計、収益イベントを持つ。
TruckRun＝倉庫に見える道路走行・接車・荷役の個別運行。TruckRunIDを保持。物理車両と出荷バッチの厳密な結び付けは旧作実機で調査し、未確認時に1対1と決めつけない。
RejectedGateは不合格箱の経路であり正規貨物区分とは別。

## 2. Cargo calendar
- [R/2025] ROAD/RAIL/AIR/MARITIMEは出荷できる曜日が異なる。「Cargo Type Calendar」で確認可能。
- CalendarKey = (warehouseProfile, gameWeek, dayOfWeek, cargoMode/countryId)。
- `CanExport(gateId, packageId, tick, calendarVersion)` は日にちと貨物種別を決定論的に返す。
- 出荷不可の曜日は収納ラックに置いて次の曜日まで待つ。意図的に誤投入できる仕様を保ち、誤出荷には正誤/費用が反映される。[R/2025ガイドでは誤曜日でも投入自体可能]
- 今日の出荷可能な区分の表示色とゲート表示を連携する。ただし判定用の緑/赤と、最終的な承認/拒否を混同しない。
- 2026新倉庫ではExpress向けCountryCalendarが毎週月曜変更。固定カレンダーの選択肢がある更新情報を踏まえ、random/fixed設定をCatalog化する。
- 国別Expressは旧倉庫のCargoModeをそのまま継承しない。WarehouseProfileによりRoutingPolicyを切り替える。

## 3. 保管モデル
StorageUnit {storageId, warehouseId, type:RACK/PALLET, size:S/M/L, position, occupancy[], capacitySlots, acceptedFilter, labelSignConfig?, reservationVersion, connectedLoaderIds[], connectedUnloaderIds[]}.
- Rack容量はサイズによって違う。価格/正確な枠数は出典未照合[U]。
- [R/2025] LoaderConveyorは隣のStorageRackへ投入、UnloaderConveyorは隣から取り出す。PalletはLoader自動投入不可。
- 収納はIDと順序を管理し、箱3Dをすべて常時活性化する必要はない。保管中の箱は同じ位置に実在しているように再表示する。
- 保管可能条件はcapacity、箱サイズ/種類、対象ラックのルール、空き予約の存在を確認。
- 取り出しは箱ID指定。荷物所有先変更・Rack占有変更・搬送先予約を原子処理として実行。
- RakSign自動表示は現在内容の最大頻度分類などから提示可能。元ゲームの正確な優先アルゴリズムは未確認。空棚では「未分類」表示[P]。

## 4. 出荷口
ExportGate {
 gateId, warehouseProfile, gateCategory:STANDARD/REJECTED/PROGRAMMABLE,
 lanePosition, transportFilter, countryFilter, calendarId, active,
 capacitySlots, bufferedPackageIds[], pendingShipmentId?, submissionMethod,
 displayProgress, lastSummary, associatedScheduleId?, configVersion
}.
- 各出口の表示: 正しい輸送アイコン/国、ゲート番号、出発/次便案内、現在箱数（例1/16は観測画面値）、正しく出した数/間違い数、報酬/ペナルティ・合計。
- キャラクター移動なし。[出荷]→区分/出口選択→箱を保持している時は「この出口へ投入」、またはコンベア連結で自動投入。
- 使用不可日、誤貨物区分、未検査/誤Approved、拒否品の誤出荷は**原作らしく誤出荷として受け付けたうえで罰則が発生する可能性**を設計として許す。すべて一律に投入拒否すると「失敗を通じて学ぶゲーム」が失われる。[R/2025攻略では不正・曜日違いでも出口に送れる]
- 一方で「満杯」「物品IDがない」「既提出箱」「搬送中で所有者が違う」はハード拒否し、複製を防ぐ。
- 拒否専用ゲートでは実際の検査真値/貼付ステッカーと突き合わせる。緑ステッカーの間違い出荷は誤提出とカウント。
- 便の提出時に正誤を計算して集計を固定、便の予定時刻・報酬確定との時差は[U]。**出荷口に置いた瞬間に収益が出る前提をコードに埋め込まない**。

## 5. 結果評価の純粋関数
`EvaluateSubmission(package, targetGate, calendarSnapshot, ruleSetSnapshot)` -> {isCorrect, reasonCodes[], valueDelta, resultCategory}.
- 正解条件は [検査真値、ステッカー、拒否/正規出口、貨物区分または国、出荷許可曜日] の組み合わせ。
- 既に別Shipmentへ割り当てられたPackageIDを受け付けない。
- ゲート満杯中は設備上流にブロック通知し、再提出操作で二重受入しない。
- 評価結果は出荷時点のcalendar/ruleSetのversionとともにSnapshot保存。後の曜日変更で過去の正誤が変わらない。
- 金額はEconomyCatalogが決定し、ここでは値を受け取るだけ。何がトリガーで売上に反映されるかは専用LedgerEventで記録。
- 完全な計算式は原作未確認のため `CalculationProfile` で切替可能とし、勝手に独自の契約罰金を導入しない。

## 6. Shipmentの状態機械と出荷開始
OPEN（ゲートに箱を積む）→ READY（数量/操作条件）→ SUBMITTED（ユーザー/自動ボタンで提出）→ DISPATCH_SCHEDULED → DISPATCHED → SETTLED。
- [R/2025] Automatic Exportsは満杯時の「出荷ボタンを自動で押す」ためのUpgrade。アップグレード前でもコンベアを出荷口へ接続可能。
- [P] SUBMITTED時に一次精算レコードを作り、SETTLED時に確定する例。実際の資金計上時点は原作実機で確定する。モデルはイベントを分けて後から切替可能に。
- [U] 容量未満を任意提出する場合の制約、手動/自動出荷の起動タイミング、17時営業終了時の扱い、1/16の上限の意味。
- 終業時や寝る操作で日跨ぎしても、未提出のOPENバッチ・保管中の箱は明示的な処理規則で持ち越す。原作の取り扱い実機検証まで売却/消滅をしない。

## 7. コマンド一覧
- `TransferIntoStorage(packageId, storageId, expectedVersion)`
- `TransferOutOfStorage(packageId, targetEquipmentId, expectedVersion)`
- `SubmitToGate(packageId, gateId, expectedOwner, expectedVersion)`
- `RequestManualShipment(gateId, requestId)`
- `RequestAutomaticShipment(gateId, capacityReachedEventId)`
- `PublishExportCalendar(weekId, warehouseProfile)`
- `ReconfigureProgrammableGate(gateId, targetCountryId)`
失敗コード: PACKAGE_NOT_FOUND, OWNERSHIP_CHANGED, NOT_READY, STORAGE_FULL, GATE_FULL, ALREADY_SUBMITTED, GATE_LOCKED, TRANSACTION_BUSY, WRONG_WAREHOUSE.
同一requestIdを再受信したら保存済み結果を返して二重提出を回避する。

## 8. ユーザー提供画像との検証
IMG_0181: 飛行機アイコン、B2相当の番号、1/16型表示、Valid Submissions集計、当日の時刻/曜日/Tier/資金表示。
- 画像の正確なUI意匠は模写せず、見える情報の密度と読みやすさ・工業設備の立体的な質感を再現。
- 入出力ベルトと出口の物理的な位置を一致させ、コンベアから箱が入ると数が+1。
- ゲートを切り替えると各ゲートの個別進捗へ切替。別ゲートの収支データを混ぜない。
- 航空区分の箱を出しても、トラックが出るアニメーションとの整合はshipment/truckRunの明示関係で担保。

## 9. テストケース
EXP-01 ROAD箱→ROAD稼働曜日→Approved→正しい出口で正解。
EXP-02 AIR箱→AIR非稼働曜日にAIRへ出した場合、投入自体は認めるが誤提出（暫定。原作比較要）。
EXP-03 DENIED箱→拒否ゲート→正解。APPROVEDを拒否ゲート→誤提出。
EXP-04 Gate満杯→追加投入拒否→上流の箱は消えずWAITING_EXIT。
EXP-05 同じPackageIDを2ゲートへ同時提出→1件だけ成功。
EXP-06 SUBMITTEDイベント連打/自動と手動同時→Shipmentは1つ、二重精算なし。
EXP-07 3日後に倉庫へ戻ってもラック中のID総数が保存されている。
EXP-08 CountryProfileで月曜更新後に昨日の提出結果が書き換わらない。
EXP-09 容量16はテストFixtureのみ、他の任意容量でもロジックが成立する。
EXP-10 出荷前に設備削除が要求されたら含有箱を保護。
EXP-11 ラックLoader/Unloaderを交換しても箱IDと接続方向が保たれる。
EXP-12 出荷口画面を見ていなくてもコンベアが搬送し、帰還時に最新残数になる。

## 10. 未確定事項と参照
原作の全曜日表・デイリー運行表、ゲートごと容量/拡張Tier、評価明細、1/16の意味、便の実物トラックとの対応は未実機照合。参照 https://store.steampowered.com/app/2424010/Parcel_Simulator/ と https://shippobox.com/archives/16245 ・ https://shippobox.com/archives/16262。


## 11. 2026-10-26 再調査による重要な優先補正（本節を第6/7節より優先）
- **[R/2026-09-25 開発者告知]** 「終業時に容量未満の出荷口をクリアするボタン」は *現在開発中* と明記された。**2026-09-25時点で既存機能と断定できない**。出典 https://steamcommunity.com/app/2424010/announcements/ 、https://steamdb.info/patchnotes/25526810/
- **[P 基本動作]** `GateSubmissionPolicy` を `FULL_ONLY` / `ALLOW_PARTIAL_AT_DAY_CLOSE` / `CUSTOM` としてデータ化する。原作同等性を確認するまでは `FULL_ONLY` を優先し、`RequestManualShipment` は `readyToSubmit=true` かつ必要数量充足で成功する。容量未満で無条件に手動提出できるUIを正式機能にしない。
- **[R/2025資料]** `AutomaticExports` はゲート充填後の提出操作を自動化する機能。手動提出コマンドが「1箱のみ」でも機能することを意味しない。
- **[U]** 原作2026年10月26日までの以後パッチでClear Doorsが正式実装されたかは最新実機確認が必要。確認までは部分提出は拡張候補に留める。
- **テストの補正**: 1箱単位の入金テストを行う場合は *テスト専用ゲート容量1* を設定して満杯にする。通常本編の最大数が1という意味ではない。
