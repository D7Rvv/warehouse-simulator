# 出荷・収益・契約・請求ルールの相互照合／競合修正
文書ID: SPEC-18 / 作成: 2026-10-26 / 版: 0.5
**本書はSPEC-06/08/15/16/17の出荷・価格・請求の矛盾に対する優先補正**。ソースが複数あるため、未証明の単純な「正確な原作完全ルール」は主張しない。

## 1. 優先度P0：部分出荷は完全に禁止されていない（誤読を修正）
従前の設計文書では、2026年9月25日の公式ニュース「Clear doors button to submit partially filled doors at the end of the day」を理由に、基本`GatePolicy=FULL_ONLY` とした。しかし次の一次の操作報告と矛盾がある。

**2026-07-21のSteamスレッド「New warehouse denials」**
- ユーザーDaveは **Denial Gate 15/16で手動Exportボタンを押したら、15箱すべてが誤提出として報告された** と述べる。
- 開発元 Dansan Digital本人が返信し、出荷した箱の国とCountryCalendarを確認するよう伝えている。15/16で出せたという事実を否定していない。
- 同スレッド別ユーザーは「就寝前に、たとえ1個しかなくても各ゲートを送る」「表示国を変えた後に送るとすでにゲートにある荷物が違う国扱いになる」と助言。
**資料** https://steamcommunity.com/app/2424010/discussions/0/570418436974366307/
**証拠** E2（ユーザー操作報告）+開発者応答。同時にこのスレッドは2026-07の新倉庫に関するため、旧倉庫でも少量提出が可能かは別の確認が必要。

**2026-09-25公式「開発中」項目**
- 「Clear Doors button to submit partially filled doors at the end of the day」という、**終業時に複数の部分出荷口を掃除する便利ボタン**の計画。一般的なゲート個別の手動Export操作自体がないと明言していない。
- 資料: https://steamcommunity.com/app/2424010/announcements/ 2026-09-25

### 結論（Codexへの実装拘束）
- `GateSubmissionPolicy`は `ManualSubmit`, `AutoWhenFull`, `EndOfDayClearAll`を**別フラグ/別コマンド**にする。**FULL_ONLYを全出荷共通の確定仕様として使うのは禁止**。
- REGIONALの個別ゲート`ManualSubmit`は 1..capacityのPartialBatchに対応する方向で暫定実装し（情報を再現するため）、専用`EndOfDayClearAll`は未確認/未実装として分離。
- ORIGINALについて個別のPartialSubmitが可能かは[U]のため、`allowPartialManualByProfile`をデータ設定とし、原作画像/動画確認まで未確定にする。
- AutomaticExportsは2025年の別プレイヤー実験で **満杯なら貨物の曜日に関係なく出荷する**。これは手動のPartialSubmitでもClearAllでもない。https://steamcommunity.com/app/2424010/discussions/0/591774738110508320/
- 自動出荷は誤曜日を避けてくれない。プレイヤーがPauserやRackで閉路を止める操作を必要とする。https://steamcommunity.com/app/2424010/discussions/0/591776209704079036/
- 自動化されたゲートでも、満杯前にボタンで出荷した場合の優先順はプロファイル別の設定。[U]

## 2. もう一つのP0：Regionalの拒否ゲートでも曜日評価があり得る
上記の2026-07-21開発者返信には、ユーザーが `Denial door` で15箱出したところ全部不正とされ、開発者が「国別の出荷可能日・国名・国旗を確認しているか」と回答した記録がある。
- **REGIONALのDeniedゲートでは曜日カレンダーを無視していつでも正しい拒否出荷になる**とする設計は危険。
- 旧Original倉庫のRejectedGateとのルール差を `ExportAssessmentPolicy(warehouseProfile,gateCategory,parcelCountry,calendarVersion)`で分離する。
- 新倉庫のDeniedで、拒否の理由が本来不正か・CountryCalendarでの未受付による不正か・当時のバグかは追加実機検証[U]。
- 某返信には「ゲート上の国選択画面を変更してから出荷すると、すでにゲート中の箱も新しく選んだ国のルールで評価される」という体験報告あり。提出**時点**の `ProgrammedCountryId` を記録して評価するモードが必要。
- 2026-09-25以降、`Country:None`と`Criteria:N/A`が不正扱いになっていたバグは修正。元作品のバグを再現のため復活させない。https://steamdb.info/patchnotes/25526810/

## 3. ゲートと提出を扱うデータ契約（再定義）
```text
Gate {
 GateId, WarehouseId, WarehouseProfile: ORIGINAL|REGIONAL,
 GateKind: VALID_CARGO|INVALID|PROGRAMMABLE_COUNTRY,
 Capacity, BufferParcelIds[], PhysicalAcceptable, CurrentProgramCountryId?,
 CalendarId, AutoExportsUnlocked, ManualSubmitEnabled,
 AllowManualPartial, AutoWhenFullEnabled, EndOfDayClearAllEnabled,
 LastSubmissionId, Status: ACTIVE|BLOCKED|SUBMITTING
}
Submission {
 SubmissionId, GateId, PackageIdsSnapshot[], SubmittedAtGameDayAndMinute,
 CalendarVersionAtSubmit, ProgramCountryIdAtSubmit?,
 EvaluationProfileVersion, CorrectCount, IncorrectCount,
 CorrectValue, IncorrectPenaltyValue, HandlingCharge, NetDelta,
 ReceiptLedgerId, Status: PREPARED|COMMITTED|SETTLED
}
TruckRun {RunId, PhysicalVehicleId, ScheduledArrival, DockId, ShipmentIds[]}
```
**PartialSubmitは、現在のBufferの箱ID集合を凍結する原子的処理**。成功後にBufferが空になるなら見た目も追従。エラー時はBuffer/金額/便の状態に影響なし。
同じSubmitCommandIdを2回押しても結果は1回。国設定は以前の積載日ではなくSubmit時のSnapshotを優先するテストを追加。
初期のBoxOwnerがGate内、Submit後はSubmission/Shipment内へ移行する。物理トラックの移動アニメーションは支払いコマンドではない。

## 4. 原作旧倉庫の曜日表（偽造せず未確認を明示）
現状の情報源:
- 2025攻略ではRoad専用ゲートを毎日処理するプレイヤー報告、Railは週末止める運用例が複数。
- Air・Maritimeは特定の出荷可能日があり、最大4日や最大6日保管という2026ユーザー例。
- ただし**7曜日×4区分の全マスクを示す公式テキストは未取得**。検査タブレットのCargo Type Calendarを実機で撮る必要がある。
現在の設計値表（"?" は未検証）:
| 曜日 | Road | Rail | Air | Maritime |
|---|---|---|---|---|
| 月 | ◯ (ユーザー報告) | ? | ? | ? |
| 火 | ◯ (ユーザー報告) | ? | ? | ? |
| 水 | ◯ (ユーザー報告) | ? | ? | ? |
| 木 | ◯ (ユーザー報告) | ? | ? | ? |
| 金 | ◯ (ユーザー報告) | ? | ? | ? |
| 土 | ◯ (ユーザー報告) | × (複数報告) | ? | ? |
| 日 | ◯ (ユーザー報告) | × (複数報告) | ? | ? |
- この表を**本番用完全なカレンダーデータと誤認しない**。配置されている◯はE3プレイヤー報告で公式確定ではない。
- 出荷不可日でも正しい荷物の`InspectionDecision=Approved`のまま。曜日でDeniedシールにはしない。
- `Hardmodeの候補日程案`がSteamフォーラムにあるが、**ゲームの現行カレンダーと混同しない**。

## 5. 2026 CountryCalendar（公式で確定する範囲）
- Regional Sorting CentreはExpress専用、12国/地域出荷先。
- 毎週月曜に週単位の国別カレンダーが切替わるランダム版が基本。
- 2026-07-24更新で`Options > Gameplay`に固定CountryCalendar設定追加。
- 各国の曜日数・週ごとの生成確率・固定表のビットマスクは[U]。未知のアルゴリズムから「完全な固定表」を発明しない。
- Denied出口の曜日判定は2026-07-21開発者参加議論から要確認。
- 出荷時のバッチ国設定を変更すると事前積荷の正誤が影響する可能性があるので、`SubmitSnapshot.ProgramCountryId`を必須に。

## 6. 精算式の実測からわかるもの
提供画像IMG_0181.jpeg：
- 現在Buffer 1/16、直近（別）SummaryでCorrect parcels=16。
- CorrectParcelValue +$1008
- IncorrectParcelValue -$0
- HandlingCharge -$5
- Net +$1003（1008−0−5）。
- 正送16個の平均は$63。**平均≠基本単価**。
- `Incorrect parcel submission multiplier`が -10 のように表示されるが、単位/適用順は不明。
- `Correct parcels (multiplier)`の16が荷物数か乗数かも、**文字ラベルからだけでは断定できない**。項目名と評価明細を分けてデータ化する。
- 元資料SRC-16のライセンス+10/+15/+10/+10、箱種類ボーナスはWiki単独の値であるため、これらだけで1008を必ず再現できるとは限らない。
```text
ValuePerParcel(P, Config) =
    BaseValue(P.Type, ...) [U]
  + EnabledLicenseBonuses(P, ...)[C]
  + ParcelTypeBonus(P,...)[C]
  + OtherRuleModifiers(P,...)[U]

BatchEvaluation =
  ProcessValidity(PackageIds, GateConfig, CalendarAtSubmit, RulesAtSubmit)

GrossPositive = CorrectValuePolicy(batch, PerParcelValues)[U]
GrossPenalty  = IncorrectPenaltyPolicy(batch, wrongReason, perParcelValues)[U]
HandlingFee   = SubmissionHandlingFeePolicy(batch, gate, tier)[U]
NetProfit     = GrossPositive - GrossPenalty - HandlingFee
```
推定式は`FeePolicyVersion`に紐付くScriptableObjectに置き換え可能。収益イベントは同一SubmissionId1回だけ。
実機キャプチャを集め、**加算と乗算の順序**、端数丸めを特定するまでは本番値として固定しない。

## 7. 契約・Billingは通常出荷から独立
- `ContractDefinition`: 最低箱数、条件Country/CargoMode/ApprovedOrDenied、受注時のContractId、専用バンへの積載、契約バン納品、報酬と達成状態。
- `ContractVan`は`TruckRunKind.CONTRACT`でも良いが、普段の荷物Inbound/OutboundとID・出荷評価を分離する。
- `PeriodicBill`には発行トリガー・金額・期限・支払状態を保持。2025プレイヤー報告で数日ごとのBill、上限$500とする体験があるが、**金額と発生頻度は固定できない**。
- 契約納品と通常出荷を二重計上しない。Bill支払やShop決済も独立したLedgerEntryId。

## 8. 再現精度を確定する測定実験の仕様
**EXP-CAL-01**: Originalの初期日に検査タブレットを撮り、月火水木金土日の4輸送区分（計28セル）をコード表へ転記。受入OK/NGとゲート灯色を同時記録。
**EXP-SUB-01**: Regionalの同じ条件・同一日にGateで箱1/16、15/16、16/16にし、手動出荷ボタンの有効/無効と結果を記録。AutoExportはOFF。少量で送れたらFeeと荷物数を記録。
**EXP-SUB-02**: Originalでも同手順を繰返しWarehouseProfile差異を記録。
**EXP-SUB-03**: 2つの国の箱を入れ、出荷ゲートをCountry A→Country Bに変更した時の評価日/誤配送を実測。
**EXP-PAY-01**: 単一基準箱、同一License/箱種で16箱提出する条件の前後残高とモニター明細。複数Tierで繰り返す。
**EXP-PAY-02**: 同じ条件で不正箱0/1/2/16個、表示倍率と最終精算を比較。
**EXP-PAY-03**: License AだけONにして同質箱16、A+Bで16、出荷区分/ゲートを変えた16（混合出荷を避ける）。基準一つだけ変える差分測定。
**EXP-FEE-01**: 1個手動部分提出、満杯手動、満杯AutoのHandlingCharge比較。
**EXP-CONTRACT-01**: 同じ有効箱をContract専用バンへ納品/通常ゲートへ納品で報酬・失敗条件比較。
**EXP-BILL-01**: Shop購入と倉庫操作を止めて数日休息、Bill到着日と金額記録。次に設備台数と稼働時間を変え反証実験。
**EXP-REG-TIER-01**: 地域倉庫Tier15～18を進め、必要Express提出数、現金、解放されるゲートID順をすべて撮影。
**EXP-2026-CHECK**: 2026-09-29以降のパッチにClear Doorsが実装されていないか確認。
すべて観測日/参照ゲームBuildID/Tier/License/CalendarVersion/荷物枚数/全イベントを記録し、別バージョンの実測を同じマスター値に混ぜない。

## 9. Codexへの実装用スキーマ・試験
- `GateSubmissionConfig`: `warehouseProfile, allowManualPartial, autoWhenFull, allowEndDayClearAll`。各フィールドはProfileごとに独立。
- `ExportCalendar`: `{dayOfWeek, cargoMode, accepted, sourceStatus, sourceId, calendarVersion}`。未確認は`accepted:null`にし、開発専用TestCalendarと本番原作再現データを区別。
- `FeeCatalog`: `{correctFormulaId, penaltyFormulaId, handlingFeePolicyId, sourceStatus, sourceId}`。未確定式はNull参照の本番判定を禁止、デモ専用Mockで画面動作のみ評価。
- `ContractCatalog`, `BillCatalog`もソース/バージョン付き、未確認の数値はハードコード禁止。
- 全コマンドは原子的、保存後も冪等で、描画fps/画面切替で金額やゲート内容を変えない。

## 10. 重要な優先順位（設計同士が矛盾した場合）
1. 直近バージョンの原作実機動画/同条件検証（まだ未取得）
2. 原作の公式2026更新が「実装済み」と述べた事実
3. この文書の2026-07 **15/16手動提出の証言と開発者の返信**
4. 過去のSPEC-06/15等の`FULL_ONLY`仮説（**無条件には使用しない**）
5. 実装上のモック
**SPEC-06とSPEC-15の旧`FULL_ONLY`注記は本書で上書きする。** 同種の将来の証拠が増えたら原作確認とともに全Spec/QAを同時更新。
