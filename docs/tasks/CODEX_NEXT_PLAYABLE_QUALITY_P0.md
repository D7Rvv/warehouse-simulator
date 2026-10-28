# CODEX TASK — P0：0.5/10評価からの是正・遊べる最初の5分・高品質3D
Task ID: TASK-P0-PLAYABLE-QUALITY-20261028
State: READY_FOR_IMPLEMENTATION_REVIEW
Created: 2026-10-28
Primary user-feedback: `docs/reviews/2026-10-28_USER_PLAYTEST_QUALITY_REJECTION.md`
Full acceptance: `docs/spec/21_PLAYABLE_FIRST_SESSION_QUALITY_GATE.md`
Tracking issue: (GitHub Issue #3を参照。番号変更時はこのリンクを更新)

## 最初に読む
1. AGENTS.md、docs/spec/00_MASTER_INDEX.md
2. `docs/reviews/2026-10-28_USER_PLAYTEST_QUALITY_REJECTION.md`
3. `docs/spec/21_PLAYABLE_FIRST_SESSION_QUALITY_GATE.md`
4. `docs/THREE_SCREENSHOTS_ACCEPTANCE_SPEC.md`
5. docs/spec/03_UI_CAMERA_INPUT.md、04_PARCEL_INSPECTION.md、06_STORAGE_EXPORT.md、09_ARCHITECTURE_SAVE.md、11_QA_ACCEPTANCE.md、12_ASSET_ART_DIRECTION.md、15_DOMAIN_CONTRACTS_AND_FIXTURES.md、15_DOMAIN_CONTRACTS.md
6. docs/spec/16_ECONOMY_EXPORT_DEEP_RESEARCH.md、17_2026_REFERENCE_VERIFICATION_AUDIT.md、18_EXPORT_REVENUE_RULE_RECONCILIATION.md（出荷/収益の現行資料）
7. `docs/development/LOCAL_PC_SPECS_2026-10-28.md`（PC側GPU制約）
8. feature/issue-1-s0-s1のPR #2レポート。**Draft/未受入S0/S1を完成ゲームと誤認しない**。

## ユーザーからの厳しい評価
2026-10-28、ユーザーはiPhoneで確認した現在の画面を **0.5/10** と評価。指摘: 「元モデルと全然違う」「ゲーム性が無い」「3Dの品質が低すぎる」「初心者は何をすべきか分からない」。**これは最重要の受入失敗**として改善する。Unityが起動したことをもって完成にしてはならない。

## Codexに要求する行動
- リポジトリの差分・PR・現在のブランチを確認し、実行可能なUnity 6000.3.27f1プロジェクトから作業を始める。勝手に新規別リポジトリを作ったり、古い自由歩行プロトタイプを復活させない。
- Unity実機でのConsole/Sceneを確認。まだ未検証のUnity Editor操作やiPhoneテストをPASSと書かない。ユーザーがスクリーンショットを与えたらその証拠に基づいて修正する。
- **本物のParcelId / 所有権 / 検査 / 出荷 / Ledger**を実装する。ボタンを押すとデモ表示が変わるだけの偽シミュレーションは不可。
- 初回操作の目的表示、3Dハイライトと短いガイド、誤操作時の説明、次ステップの導線を実装。ユーザーが何をすべきか分かることが第一。
- 3D搬入口/検査台/出荷口を**まともな実物のような独自HeroAsset**へ改善する。単なるCube/平板と試作マテリアルでは「完成」扱い禁止。ライセンスの分からない原作資産をコピーしない。
- Windows PCでのEditor低負荷と、iPhone用本番画質は別のQualityProfileに。PCが内蔵Intel HD 630であることからスマホ版まで一律に低画質化しない。
- 制作費0円を優先。Unity Personal/Blender/CC0素材や無料アセットはライセンスを確認して使う。外部購入や有料サービス登録をユーザーに無断要求しない。
- 原作ゲームの価格・全曜日・収益式で根拠のないものは[P]の試験Policyで分ける。本番の原作一致を偽らない。

## 技術PRの分割・合格条件
### PR-A：一箱が物理的に移動する [P0]
- TruckCargoSlotから1箱をPickup→HandSlot→InspectionTableへPlace→取る→GateBufferへ置く。
- Ownerは一意、箱のデータと見た目の個数が一致。50回CameraStation切替でBox/HandSlotが変化しない。
- PlayModeテスト: Pick/Placeの成功/拒否、二重持ち、満杯の置き先、中断/再開、箱の生成/削除ミス。
- Repo内S0/S1のScene/Materialを継承するが、実Unity ConsoleがERRORなら作業を進める前に修正する。

### PR-B：本物の初期検査・発送・収益表示 [P0]
- CargoMode、Country名+架空旗、承認/拒否シール（最初のLicense範囲）を実装。TruthとLabelを分離。
- 正解/誤りの3箱を用意し、プレイヤーが自分で判定可能。内部TruthをUIに自動表示しない。
- ValidCargoGate/RejectedGate、ManualSubmit、正誤計数とLedgerを実装。Tutorial用ゲートは容量1の[P] Fixture可。
- SubmissionID再実行で二重入金しない。利益はTestFeePolicy/Pデータを明示し、未確定の原作式を断定しない。
- 継続セーブが必要。少なくともBoxOwnerと出荷済Id/総計を安全に復元できること。

### PR-C：日本語チュートリアルと初回の気持ちよい体験 [P0]
- [搬入]→箱を取る→[検査]→ラベル確認→シール→[出荷]→結果を理解→次の荷物の「実際の手動操作」に合わせた誘導。
- ハイライト対象と次のボタン、現在の所持箱と処理進捗を常時明確に。説明が多すぎて画面の3Dを隠さない。
- 0から始める初見ユーザーに試遊してもらい、迷ったところを動画かスクリーンショットで修正。
- 教示だけのチュートリアルで、実際の荷物ID/結果が変わらないものは不合格。

### PR-D：3Dのクオリティ差を埋める [P0]
- 参照画像3件について【見える部品】【素材】【照明】【距離感】【UIの読みやすさ】を1画面ずつ再現水準として評価。
- 搬入口: トラック荷台・シャッター・アスファルト/床・段ボールのテープ・金属リブ。
- 検査台: 鮮明ラベル（拡大できる）・金属台・秤/測定機・箱の個別バリエーション・接地陰影。
- 出荷口: 金網・ローラー・ベルトの脚・厚みある開口/安全柵・読みやすいモニター。
- 独自制作PBR素材・適切なNormal/AO/Roughness・Prefab/LOD・軽量Collider・ArtAssetManifest。
- **実際のUnity GameView画像3画角と修正前画像、ユーザーフィードバックで受入**。画像のないレビューを「高品質達成」としない。

### PR-E：スマホ向け持続性能 [P0/継続]
- EDITOR_LOW vs MOBILE_BALANCEDを分離。PCのi7-7700/16GB/HD630はEditor描画制約。iPhone 13 Pro Maxはネイティブ端末の性能を別に測る。
- 30分連続テスト: fps/frameTime p95/p99/OS thermal/memory/scene count/箱数・コンベア数を記録する。
- 実iOSネイティブアプリを配布できないときは`NOT_RUN`。Unity RemoteによるPCレンダリング結果をiPhone GPUの性能・画質確認と混同しない。

## ユーザーへの完了報告形式
1. **動く操作の証拠動画/スクリーンショット**（何ができたかを順に見せる）。
2. 参照画像3枚に対する現在の独自3D画面の比較（同一ピクセルコピーは禁止）。
3. 初心者チュートリアルの手順と、本当に何の状態が変わるか。
4. 実行したUnity EditMode/PlayMode/端末テスト。結果はPASS/FAIL/NOT_RUN。
5. 画質/FPS/発熱はネイティブ実測値がなければ未検証と明示。
6. 使用ライセンス、無料素材の出典、未完了でユーザー評価待ちの箇所。
7. 追加のアート/技術課題、ユーザーの再評価をもらうための確認手順。
**何より、ユーザーの評価が0.5/10だった原因を具体的に説明し改善する。『Unityが起動したので完成』ではない。**
