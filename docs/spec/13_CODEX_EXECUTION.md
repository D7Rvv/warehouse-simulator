# Codex 実装命令とスプリント分割
文書ID: SPEC-13 / 版: 0.4 / 内容: 誤実装・未テスト完了報告の防止

## 0. Codexの最初の命令
1. README.md、AGENTS.md、docs/spec/00_MASTER_INDEX.md、同一機能に該当するSPECを読んでから変更する。
2. 既存の旧自由歩行Prototypeコードと新仕様を区別。新ゲームの主カメラ・入力は「作業場所タップ切替」であり、PlayerController歩行は削除するかLegacyへ隔離する（破壊的削除前に保存する）。
3. 現在のGitHubリポジトリはUnityプロジェクトとして必須のsettings/packages/sceneが未整備かもしれない。これらを調査し、正確なUnity/URP版とAPI互換性を設定する。
4. 実装前に参照ゲームとの差分・仮値の一覧を作る。推測で原作の数値を確定するのは禁止。
5. ローカルUnity Editor/対象iOS/Android実機が無いなら「その試験は未実施」と明確に報告し、代わりに純粋ロジックテスト可能な機能を作る。
6. 各スプリントのゴールを達成してから次に進む。勝手に未設計のNPCや自由経営料金システムを増やさない。

## 1. 作業ブランチとPR
- mainは基準となるブランチ。原則 feature/{epic}-{id} で独立した変更をPRにする。
- 変更のコミットは目的別に整理（code, scene, test, docs, assets）。既存の安定部分を上書きせず、移行する場合はdiffで理由を説明。
- 実装更新時には該当するREQ-ID、SPEC-ID、TEST-IDをPR概要に記載。
- レビュアーに提示する内容: 実装したこと／参照元との一致・差異／受入テスト実施結果／スクリーンショット／Performance計測／未検証。
- 手元でUnityプロジェクトを起動できない場合でもGitHubで完了を宣言しない。
- 大量の未確認動作を機能完成としてまとめず、先に1荷物手動検査の垂直スライスを完成させる。

## 2. Sprint一覧
| Sprint | 実装対象 | 合格条件 | 主なSPEC |
|---|---|---|---|
| S0 | Unity正式プロジェクト構成とCI、Legacy隔離 | Editorでコンパイル、起動シーンとテストRunner動作 | 00,09,10,13 |
| S1 | 倉庫/検査/入荷/出荷/屋外の3D Blockout、StationNavigator、タッチUI | 歩行なしで各ビューへタップ即切替、SafeArea/ジェスチャ動作 | 03,10,12 |
| S2 | Package Domain、所有権Transfer、GameClockの基本 | 1箱のIDと所有場所が移り、リプレイとテスト成功 | 04,07,09 |
| S3 | 手動検査全種の基本型、判定、ステッカー、タブレット | 正しい/間違った箱を正しく扱い、未解放ルールを課さない | 04 |
| S4 | CargoCalendar、出口、WrongSubmission、Ledger | 曜日とゲートに応じて集計、金額は一度だけ | 06,08 |
| S5 | 建設Ghost/セル/向き/ストック/購入済みアイテム | IMG_0180レベルの操作、キャンセルは副作用なし | 05,08 |
| S6 | 論理コンベア、容量、詰まり、Joiner、Scanner、StickerChecker | 同一箱重複なし、Valid/Invalid分岐、接続テスト成功 | 05,09 |
| S7 | ConveyorDeliveries、ラック、Loader/Unloader、AutomaticExports | 手動→自動が実際の設備接続で成立 | 05,06,07 |
| S8 | ショップ、ライセンス、Tier、設備強化、在庫配送 | 原作照合可能な進行、残高/注文重複なし | 02,08 |
| S9 | トラックの道路走行・複数ベイ・接車・荷役と時計連動 | 到着予定に同期、他画面でも進む、残荷整合 | 07 |
| S10 | RegionalSortingCentre、CountryCalendar、Elevator、PalletBuild等 | 2階・国別ルール独立、週替わりと設置テスト | 02,05,06 |
| S11 | 保存/復旧、プロファイル、音、独自高品質3Dアート | 途中状態も復元、外観の基準比較を通過 | 09,10,12 |
| S12 | 本格的なQA・熱/端末評価・最終調整 | 対象端末の性能・整合・UI・画質受入すべてPASS | 11 |

各Sprintは1つのPRまたは小さなPR群に分割し、未完の高優先バグがあれば次Sprintへむやみに進まない。

## 3. 実装インターフェース固定順
1. typed ID / GameTick / Money / LocationRef / Package / Ruleset。
2. CommandBus / Result / Transaction / Idempotency / WorldSnapshot。
3. NavigationView / CameraAnchor / TouchGestureArbiter。
4. TransferPackage / InspectionEngine / DecisionSticker。
5. ExportCalendar / Gate / Shipment / WalletLedger。
6. GridMap / EquipmentCatalog / ConnectionPort / ConveyorGraph。
7. TruckRun / BayAllocator / ScheduleQueue。
8. Save/Load/Migration/Diagnostics。
描画に依存せずUnit testsを実行できる順に進める。

## 4. サンプル実装作業チケットのフォーマット
TicketID / 機能と目的 / Requirements / SourceEvidence / Constraints / Domain data / Commands / UI actions / Rendering responsibilities / Error cases / Automated tests / Device tests / Acceptance screenshot / Future unresolved assumptions。
例 Ticket S5-GHOST:
- 目的: 3Dコンベアを緑/赤プレビュー、タップ操作で設置。
- 入力: InventoryUnitId、WorldGridPosition、Orientation、Flip。
- Domain: Occupancy MapとPortGraphは確定前不変。
- Rendering: 共有GhostMesh、方向矢印、費用と設置可否。
- Tests: CV-001、CV-002、CV-008。未実行なら「未検証」。

## 5. 終了報告テンプレート
- フェーズ/PR/コミット:
- 実装ファイル一覧:
- 設計REQ/SPEC対応:
- 実際に完成した操作:
- 参照作品と一致した点:
- 変更した操作（スマホ最適化）:
- 実行したEditMode/PlayMode/Build/実機テスト:
- Test IDごとのPASS/FAIL/BLOCKED/NOT_RUN:
- 出力画面やビルドの参照:
- CPU/GPU/FPS/メモリ/熱状態の測定条件と結果:
- 未確認原作仕様・暫定値の一覧:
- 残存バグと重大度:
- 次のSprintへの条件:

## 6. エージェントが勝手にしないこと
- 自由歩行を前提としてゲーム全体をつくり直すこと。
- 仮の段ボールと床を置いただけで「高品質アート実装済み」と報告すること。
- 元作の値段、ゲート最大数、翌日処理、検査条件を資料なしに「完全再現」と宣言すること。
- 危険なリポジトリの全削除/非公開設定の変更/ライセンス無し外部素材取り込み。
- 勝手な追加: 人件費、税金、ガチャ、広告、アイテム課金、オンライン必須化、NPC物流AI。
- ベンチマークせずに30/60fpsで動くと断言すること。
- QA記録なしに本番アプリストア公開を行うこと。

## 7. 設計書の更新フロー
Codexが仕様欠陥を見つけたら ISSUE（GapID）を登録→根拠（実機、公式、他資料）を追記→設計レビュー→仕様/fixture更新→テスト追加→コード変更。仕様が未確定でもS1のような独立部分だけは仮設計を明示したうえで先行可能。
