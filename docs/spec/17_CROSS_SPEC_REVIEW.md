# 設計書横断レビューと矛盾解消規則
文書ID: SPEC-17 / 版: 0.4 / 2026-10-26 / 状態: 審査結果とCodex向け強制チェック
※00〜16の各文書を実装したときに発生しうる矛盾を先に解消するための統合契約。未確定値を原作仕様として固定するものではない。

## 1. 優先順位
SPEC-00の確定/暫定/未確認規則を優先。本書の決定事項は横断インターフェースの技術仕様[D/P]に限る。原作の実際の値はREFERENCE_PLAYTESTでしか確定できない。
各機能の主な仕様はSPEC-03..10にあり、本書はそれらを変更する場合の差分レビュー用。

## 2. 重要な相互依存矛盾の一覧
| REVIEW-ID | 危険な混同 | 正しい規則 | 必須テスト |
|---|---|---|---|
| CR-001 | Station切替だけで箱が次工程に行く | NavigateはWorldStateを変更しない。Pickup/PlaceまたはConveyorCommandで移送 | QA-UI-01,TC-SAVE-001 |
| CR-002 | 出荷区分Airを可視化する車両と同一IDで管理 | CargoMode/ExportGate/Shipment/TruckRunを分離する | TC-EXP-002,RUN-TRUCK-004 |
| CR-003 | ステッカー確認機を通常のScannerと同じ2出口にする | StickerChecker: 入力1＋出力Approved/Denied/NoStickerの3、通常Scanner:入力1＋Valid/Invalidの2 | RUN-BELT-002 |
| CR-004 | ゴースト設置時に資金やグラフを書換える | Previewは読み取りのみ。確定時だけ取引 | TC-CVY-004..006 |
| CR-005 | ショップで購入した機器を設置時にもう一度購入扱い | 購入済InventoryUnitsから1個消費。支払いは購入時1度 | TC-ECO-002 |
| CR-006 | 荷物をApprovedにした瞬間収益を確定 | ステッカーと提出、Shipment/報酬イベントは別 | TC-ECO-003 |
| CR-007 | 画像の1/16表示をゲート一律最大16個と解釈 | 画像に表示された一時的な状態。Gate.capacityはCatalog管理 | TC-EXP-001 |
| CR-008 | 2025旧倉庫価格を2026新倉庫の確定価格にする | EvidenceStatusとWarehouseProfileで分離 | QA-ECO-07 |
| CR-009 | 画面外荷物/車両を消してゲームの処理も止める | RenderedVisual≠DomainEntity。表示復帰時に現在状態を投影 | RUN-TRUCK-004 |
| CR-010 | 元作に存在するPC歩行操作をスマホへ戻す | スマホはタップ・メニューのStation切替のみ | QA-UI-06 |
| CR-011 | フレームレートの瞬間値を温度とみなす | 実機熱API/状態とFrameTimeを独立に収集、計測に基づく調整 | TC-PERF-001 |
| CR-012 | 検査装置の10秒をゲーム内10秒に直結 | ScannerはSimElapsedSeconds基準、便・曜日はGameCalendar基準 | CR-TIME-01 |
| CR-013 | 不正曜日/誤区分箱を自動受入段階で必ず拒否 | 2025ガイドでは入ってしまう場合がある。所有/満杯はハード拒否、誤出荷は結果判定で罰則 | TC-EXP-002/003 |
| CR-014 | 2025にショップ便未荷下ろしで翌日不可を永続規則にする | 2026-08-04公式でショップ配送トラックを空にせず就寝可能 | RUN-DAY-001 |
| CR-015 | ラベルが見える画面ごとに真値をランダム生成 | PackageTruthは一度決めて保存。ラベルと秤とスキャナは同一ID/真値参照 | ACC-INS-06 |
| CR-016 | 同じ注文/提出イベントを複数回実行 | requestId/eventIdによる原子確定/重複拒否 | TC-ECO-003 |
| CR-017 | 原画のモデル/看板デザインそのものを転載 | 参照するのは工業施設のリアリティと機能要件。独自資産で表現 | ART-09 |
| CR-018 | UnityのResizeBuffersを呼べばモバイルで動的解像度が必ず効く | URP/カメラ/APIの組合せで実効解像度とGPU負荷を検証 | TC-PERF-001 |

## 3. 時計の正式な分離仕様（SPEC-07とSPEC-09を接続）
ClockState {
  simulationTick:int64, // 機械処理用、固定論理Stepごとに増加
  simSecondsPerTick:0.1[P],
  calendarMinuteIndex:int64, // ゲーム内の連続した「分」（曜日/運行用）
  fractionalCalendarMinutes:decimal,
  currentCalendarRate:decimal, // sim1秒あたりのgame分、暫定
  paused:bool
}
- **SimulationTickは単調増加**。スキャナーの処理時間・ベルトの進行・検査機予約/車両の近景アニメ開始時間はSimulationTickで計算。
- GameCalendarはSimulationTickが進んだときにrateを使って進める別カウンター。日付/曜日/出荷可能区分、トラック到着予定はCalendarMinuteIndexとcalendarVersionで評価。
- TimeScale変更後も既に予定したScanner処理終了tickは変えず、トラック予定はCalendarMinuteに対して発火する。
- シミュレーション処理tickとCalendarMinuteに同時にDueイベントがある場合、優先順は1) Calendar境界イベント（曜日/便切替）→2) 機器処理/箱移送→3) 出荷評価/入金確定→4) 表示Snapshot公開。[P]
- 進行は `AdvanceSimulationTick()` 一回でSimTick+1とCalendarの分数加算。CalendarMinuteが複数分を跨いだ場合は旧分 < eventMinute <= 新分に一致する**全Dueイベント**を順番に処理する。
- 旧日/新日の境目は丸めで重複しない。CalendarRateを変えた場合はfracを保持、初期日時はCatalogで固定。
- 設備画面から変えられるゲーム倍率が今後登場しても、処理装置の速度が突然変わらない構造にする。ゲーム速度として全ての物理を加速する仕様が必要な場合は別途確定する。
- WorldPause時は両時計停止。メニューを開いているだけでは止めない。OS背景化時は明示PauseしOS実時間で配送処理を大量キャッチアップしない[P]。

### CR-TIME-01 テスト
SimRate=10Hz、scannerDuration=10s → 100SimTick後にスキャン完了。
GameCalendarRateをシミュレーション途中で1→4倍に変更してもScanner完了SimTickは100のまま。
CalendarMinuteによるtruckArrival=10:00はゲーム内10:00を跨いだ最初のAdvanceで1回発火。
同一時刻の2TruckRunはrunIdで別に処理、同じrunIdの同イベントは1度だけ。

## 4. 所有権と貨物ライフサイクルの優先
- 必須各PackageIdのownerRefが一意。
- 実時間のアニメをかけ終わる前にモデルが消えてもTransferが成功していれば論理移動済み。逆に演出だけ完了して論理移送が失敗したときは元の箱へ表示を巻戻す。
- 入荷→検査→出荷で出荷済み箱を最終ownerRefへ移した後も、ShipmentはPackageIdを参照して報酬台帳の二重計上を防ぐ。
- 「棚・コンベア・手持ち・出荷ベイ」に同時存在する表示を避ける。描画が遅れていても旧表示と新表示は同時にアクティブにしない。
- 開封中のContentsは箱の子IDとしてしか存在せず、ゲーム経済/物流パッケージと数を混ぜない。

## 5. ビルドとQAテスト名の対応
ドキュメント間でQA IDに複数の命名規則がある。**SPEC-11のTC-*-NNNを統合受入テスト名**とし、各専門SPECのACC-*, QA-*を補助ユニット試験として登録する。
対応候補:
- QA-UI-01..07（SPEC-03）→ TC-UI-001..003（SPEC-11）＋PlayMode専用の全テスト。
- ACC-INS-01..08（SPEC-04）→ TC-INS-001..008（SPEC-11）ただし1対1ではなくCoverageリンクで管理。
- ACC-CVY-01..10（SPEC-05）→ TC-CVY-001..007（SPEC-11）およびRUN-BELT-002。
- QA-TRK-01..08（SPEC-07）→ TC-CLK-001..003（SPEC-11）およびRUN-TRUCK-004。
- QA-ECO-01..08（SPEC-08）→ TC-ECO-001..003（SPEC-11）に加え不足分をEditModeテストで作成。
- EXP-01..12（SPEC-06）→ TC-EXP-001..005（SPEC-11）に加え残りの保存・国別・容量テスト追加。
**未登録の補助テストIDも削除しない。** 追跡表を機械生成する場合はCoverageRelation {testId, requirementIds[], aliasIds[]} を利用する。

## 6. スマホアートの版・完了の境界
- 正式なゲーム画面の背景はUnityの3D。元作品のスクリーンショットを貼った画像を3Dゲームとして報告しない。
- 仮モデルはシステム評価用。PC同等の質感の完成は別のART受入として管理。
- 原作の全Tier設備と更新履歴の最新状況を測っていないので「100%再現済」と表記しない。
- 文書の再現範囲が狭い場合は「資料で確認した範囲」と注記。実機にしかない隠れた機能はGAP管理。
