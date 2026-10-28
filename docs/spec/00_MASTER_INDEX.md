# Warehouse Simulator — 実装用設計書 目次・仕様統制
文書ID: SPEC-00 / 版: 0.5 / 設計基準: 2026-10-26 / ステータス: **設計レビュー中（未確認事項の実機照合前）**

## 目的
スマートフォン（iOS/Android）向け高品質3D物流倉庫ゲームを、Codexが誤読せず段階的に開発する。参考作品はDansan Digital『Parcel Simulator』。自由歩行は廃止し、ステーションをボタンやタップで切替。検査・出荷・設備の手作業から自動化へ進む遊びを保つ。トラックの道路進入→接車→積卸→出発は独自拡張。

**「完全再現」＝原作品のゲームルール・主要機能・進行・手触りの十分な機能的同等性を目標にすること。原作固有の3D資産・UI画面・ロゴ・音・ソースコードを複製しない。** 見た目は同格以上の品質を目標とし、独自アートを制作する。

## ファイル一覧・文書IDと読む順番（2026-10-26 更新）
**同じ先頭番号の複数ファイルは別の仕様です。必ずファイルのフルパスと文書IDの両方で参照すること。**

| 文書ID | ファイル | 責務 |
|---|---|---|
| SPEC-00 | 00_MASTER_INDEX.md | 仕様の優先順位・変更管理・完了基準 |
| SPEC-01 | 01_REFERENCE_PARITY.md | 原作の確認機能一覧と根拠 |
| SPEC-02 | 02_GAMEFLOW_PROGRESSION.md | ゲーム進行・License/Tier・ショップ |
| SPEC-03 | 03_UI_CAMERA_INPUT.md | スマホタップで即ステーション切替・操作 |
| SPEC-04 | 04_PARCEL_INSPECTION.md | 伝票・6検査・ステッカーと状態 |
| SPEC-05 | 05_BUILD_CONVEYORS.md | グリッド・ゴースト・搬送ポート・自動化 |
| SPEC-06 | 06_STORAGE_EXPORT.md | 保管・出荷口・曜日・誤出荷 |
| SPEC-07 | 07_TRUCKS_CLOCK.md | ゲーム時計と車両の論理/3D運行 |
| SPEC-08 | 08_ECONOMY_CATALOG.md | 経済・報酬・注文・License/Upgrade |
| SPEC-09 | 09_ARCHITECTURE_SAVE.md | データ構造・トランザクション・保存 |
| SPEC-10 | 10_MOBILE_VISUAL_PERF.md | ネイティブ性能・画質・発熱評価 |
| SPEC-11 | 11_QA_ACCEPTANCE.md | テストケース・受入基準 |
| SPEC-12 | 12_ASSET_ART_DIRECTION.md | 独自3Dアート・質感・UI画面 |
| SPEC-13 | 13_CODEX_EXECUTION.md | Codex工程・実装順・終了報告 |
| SPEC-14 | 14_GAPS_AND_EVIDENCE.md | 未確認事項・実機検証項目 |
| SPEC-15 | 15_DOMAIN_CONTRACTS_AND_FIXTURES.md | コマンド・所有・保存の型・Fixture |
| SPEC-15B | 15_DOMAIN_CONTRACTS.md | 全機能共通のID/所有・イベント不変条件の補遺 |
| SPEC-16 | 16_2025_REFERENCE_NUMBERS.md | 2025年の設備価格・容量・速度、歴史資料 |
| SPEC-16B | 16_ECONOMY_EXPORT_DEEP_RESEARCH.md | 2026年までの価格・報酬・曜日・契約/Bill調査 |
| SPEC-17 | 17_CROSS_SPEC_REVIEW.md | 画面/ドメイン/時計/テスト間の技術矛盾解消 |
| SPEC-17B | 17_2026_REFERENCE_VERIFICATION_AUDIT.md | **2026の公式更新・Tier18・$500k・機器・未確認の出典監査** |
| SPEC-18 | 18_EXPORT_REVENUE_RULE_RECONCILIATION.md | **手動部分出荷・満杯時自動出荷・終業時Clear Doorsの相違、報酬の未確定式を整理** |
| SPEC-19 | 19_DECISION_REGISTER_AND_SAFE_DEFAULTS.md | **原作未確認値を勝手に確定せず、こちらで補完できる技術仕様・UXを定義する判断ログ** |
| SPEC-20 | 20_MOBILE_STATION_VERTICAL_SLICE.md | **S0/S1技術プロトタイプの画面・操作・3D・タッチ・受入テスト** |
| SPEC-21 | 21_PLAYABLE_FIRST_SESSION_QUALITY_GATE.md | **2026-10-28の0.5/10評価を是正する、初回5分の本物の遊び・チュートリアル・3Dアート・実機品質ゲート** |

データ定義: `docs/data/reference_catalog_2026-10-26.json`（価格54件、曜日Null、出典ステータス付きの**調査参考JSON**。製品版へ無審査で投入しない）。

**読む順番:**
1. 00 → 01 → 17B → 18 → 14（参照ゲームの事実と限界）
2. 15（データ契約）＋15B（共通不変条件）＋17（設計矛盾）
3. 02～10・12（各個別機能）＋16（古い数値）＋16B（新しい数値調査）
4. 11（QA）→13（Codex）
**競合時は「最新の原作実機根拠 → 公式更新 → 画面/日時がある一次報告 → 年代付き攻略 → 仮定」の順。** カメラのタップ移動・スマホ専用・オリジナルアートなどユーザー決定の[D]は変えない。

### 出荷・金額に特有の優先補正
- 2026年7月の**新倉庫で15/16の手動部分提出が成立したというプレイヤー報告と開発者返信**がある（SPEC-18）。
- 2026年9月開発中の「Clear Doors」は**終業時に残った扉をまとめて処理する便利機能**。別の個別手動出荷操作と混同しない。
- よって旧SPEC-06/15/16の`FULL_ONLY`仮説は共通ルールとしては**失効**。2026の原作実機で再検証するまでWarehouseProfileごとに設定。
- OldのCargoカレンダーとRegionalの週替わりCountryカレンダーは別。DayOfWeekの未知セルはNull、価格/Bonusの古い資料は歴史参考。
- 原作の設計を参考にするが、画像・3Dモデル・UIの固有表現を無断複製しない。

参考資料:
- docs/PARCEL_SIMULATOR_RESEARCH_2026-10-26.md（旧調査）
- docs/GDD_V0_3_REFERENCE_PARITY_MOBILE.md（全体の方向性）
- docs/THREE_SCREENSHOTS_ACCEPTANCE_SPEC.md（画像3枚の視覚受入）
- このフォルダの仕様が旧GDDと矛盾するときは**このフォルダの更新日/決定状態を確認**。未確定を新しい推測で勝手に埋めない。

## 仕様の状態タグ（すべてのCodex実装指示に適用）
- **[R] 参考作品の確認事項**：公式Steam、開発者発表、提供画像、明記した攻略/プレイ資料に存在が裏付けられる。各項目に出典IDと資料日時を付す。
- **[D] このプロジェクトで明示確定**：スマホネイティブ、キャラ自由歩行なし、メニュー/設備タップで即座に作業場所切替、物流処理の一貫性。
- **[P] 実装用の暫定規則**：原作の未確認箇所を埋めるテスト用初期値。ScriptableObject/JSON等のデータで交換可能にする。プレイヤーへ「原作と同一」と宣伝しない。
- **[U] 未調査・判断待ち**：コードに固定しない。必要なら依存先を注入して仮値にし、QAに不足を残す。
- **[X] 禁止**：原作アートの無断コピー、UI画像の直接貼付、自由歩行を復活、報酬の二重計上、GPU性能の未検証保証。

根拠優先順位: ①原作現行版の検証付き実機計測 ②公式の現行パッチ/マニュアル ③参考画像（可視範囲のみ）④プレイガイド/コミュニティ（発行時点明記）⑤仮説。[R]でも**時点が古ければ現行一致は保証しない**。

## 要求ID、実装、試験の追跡
一つの項目につき `REQ-ID -> SPEC-ID -> CodePath/Commit -> TEST-ID -> TestedDevice/Build -> EvidenceLink` を保存。どれかが欠けた項目は「完了」とみなさない。
項目の受入状態: NOT_STARTED / IMPLEMENTED_UNTESTED / UNIT_VERIFIED / DEVICE_VERIFIED / REFERENCE_MATCHED / BLOCKED.
すべての新仕様は変更理由・依存影響・回帰試験・互換マイグレーションを記録する。

## 不変条件（システム共通の禁止事項）
1. PackageIDは一意、かつある瞬間の所有場所は一つだけ。
2. 物品の物理的な所属変更は必ず一つのコマンドで検証→確定し、描画の有無に左右されない。
3. Inspectorの判定結果、貼付ステッカー、荷物の位置、出口への提出、運行、売上は**別々の事実**。
4. 各トラック運行、荷物搬送、出荷、収益のイベントはidempotency key（一意キー）で重複実行を拒否。
5. カメラ切替はゲームの時刻/物流をリセットしない。
6. 配置プレビュー中は費用・グリッド・搬送グラフを一切変更しない。
7. 低FPSでシミュレーションだけ飛ばしたり、画面外の荷物を消さない。
8. 人件費・燃料費など根拠がない経費は追加しない。ただし**数日ごとのBillと契約用バンには原作のプレイヤー証拠がある**。算出額は調査済みでないため勝手に固定しない。

## 参照ゲームの正式な検証範囲
Old Warehouse: 貨物区分（ROAD/RAIL/AIR/MARITIME/REJECTED）、検査・ライセンス・倉庫拡張・機械自動化。
New Regional Sorting Centre (2026-07): Expressの国別ルーティング、毎週変更される国カレンダー、プログラム可能な出荷扉、2階とElevator、Pallet Build Mode、Poly Bag等。
この2種は**同じ倉庫に全部の仕組みを重ねない**。WarehouseProfileで切替える。

## 機能の段階的な完成基準
- G0 設計資料・参照比較、未確認条件一覧が存在。
- G1 一連のカメラ・タップ入力、3D倉庫、設備配置ゴーストが**Unityで実行できる**。
- G2 トラックから一個を手動搬入→手動検査→ステッカー→正しい出荷口→報酬が一度だけ計上。
- G3 検査項目拡張、正誤/期限/曜日/保管が成立、出荷・収益までテスト。
- G4 旧倉庫のコンベアと仕分け、ラック接続、スキャナーが論理的に連携（新倉庫固有部品はG6へ）。
- G5 ライセンス/Tier/ショップ/設備注文/アップグレードが原作比較を通過。
- G6 時計に連動したトラック移動/荷役/出発＋倉庫2の国別仕分け。
- G7 iOS/Androidの代表端末で30分以上測定、画質比較、Save/Load、事故系テストを通過。

**現在の状態: 本フォルダに24本の設計Markdownが存在。資料と実装方針は記録済みだが、Unity実行・熱計測・原作全機能の現行照合は未完了。** Codexが開始前に「全部出来た」と言わないこと。

## 仕様変更の手順
1. 根拠と対象REQ/IDを特定。
2. 旧仕様と新仕様を明文化。
3. 影響（UI→モデル→保存→QA→パフォーマンス）を点検。
4. ゲームループを壊していないか、原作との差分を表に記載。
5. Codex作業ブランチ、テスト、PR/コミット、レビューを記録。

## 参考URL
SRC-STEAM https://store.steampowered.com/app/2424010/Parcel_Simulator/
SRC-NEWS https://steamcommunity.com/app/2424010/announcements/
SRC-MANUAL https://shippobox.com/archives/16245
SRC-AUTO https://shippobox.com/archives/16262
SRC-STATS https://steamcommunity.com/sharedfiles/filedetails/?id=3509976176
SRC-PATCH https://steamdb.info/patchnotes/25526810/



## 2026年10月26日の最新調査
- 2026の参照値と追加機能: SPEC-17B（ソース信頼度A/B/C/U）。
- 出荷の部分提出、誤曜日、自動化のルール: SPEC-18。
- 収益式/契約/BillはSPEC-16B、未確認セルはSPEC-14。
- Codexは本書に記した**一意の文書ID、フルパス、最新版ソース優先順位**を守る。

## 次の実装段階 — 2026-10-26
- **SPEC-19**で補完してよい事項と原作根拠を要する事項を分ける。
- **SPEC-20**でスマホ向け3Dステーション移動S0/S1の受入条件を規定。
- **Codexタスク:** `docs/tasks/CODEX_NEXT_S0_S1.md` / GitHub Issue #1 https://github.com/D7Rvv/warehouse-simulator/issues/1
- **開発テスト用Fixture:** `docs/data/prototype_station_fixture_v0_1.json`。本番ゲームの経済や曜日の正解値とは一切混ぜない。
- 最新のユーザー方針に従い、未確認の原作金額は調査継続しつつ、カメラ・UI・コンストラクションPreview等の独立部分は先行開発可。

## 2026-10-28 重大なユーザー試遊評価と現行最優先事項
- User Playtest: `docs/reviews/2026-10-28_USER_PLAYTEST_QUALITY_REJECTION.md`。ユーザーがiPhoneで確認後、モデル・ゲーム性・初心者導線・3D品質について**0.5/10**と評価。**ユーザー受入 FAIL**。これは単にEditorを表示できる/ロジックテストが走ることと別の品質ゲート。
- **SPEC-21** `docs/spec/21_PLAYABLE_FIRST_SESSION_QUALITY_GATE.md`が最優先。Truck→Inspection→Manual Sticker→Gate→Submission/Resultを本物の状態更新で実装し、初心者へ導線を出す。3シーンの独自HeroAssetを実際のGameViewで確認。
- **Codexタスク**: `docs/tasks/CODEX_NEXT_PLAYABLE_QUALITY_P0.md` / Issue #3 https://github.com/D7Rvv/warehouse-simulator/issues/3
- 現在のS0/S1 Draft PR #2は未受入の**技術基盤のみ**。ゲームの可用性/美術品質を合格扱いしない。以前のS0/S1優先指示よりも本項が優先。
- iPhone試遊方式（Unity Remote/ネイティブ）は未確認。FPS/発熱を実測済と断言しない。
