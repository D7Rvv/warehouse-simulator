# Codexへの次タスク: S0 + S1 — スマホ専用の3D倉庫と拠点切替
タスクID: TASK-S0-S1-20261026 / 状態: READY_FOR_CODEX / 作成: 2026-10-26

## 必須リポジトリ
https://github.com/D7Rvv/warehouse-simulator

## Codexへの冒頭指示（このファイルを先に読む）
あなたはUnityモバイル担当の主任エンジニアです。
**計画書だけを書いて終わらず**、既存リポジトリを確認して、このタスクで必要なUnityコード・シーン・Prefab・Unity設定・テスト・READMEを変更し、GitHubへコミットしてください。Unity/Android/iOSの実行環境がない場合は実行できなかったことを正直に報告してください。

## まず読む（順序重要）
1. `AGENTS.md`
2. `docs/spec/00_MASTER_INDEX.md`
3. `docs/spec/19_DECISION_REGISTER_AND_SAFE_DEFAULTS.md`（確定[D]と補完[P]を分離）
4. `docs/spec/20_MOBILE_STATION_VERTICAL_SLICE.md`（今回のUI仕様・テスト条件）
5. `docs/spec/03_UI_CAMERA_INPUT.md`, `05_BUILD_CONVEYORS.md`, `10_MOBILE_VISUAL_PERF.md`, `12_ASSET_ART_DIRECTION.md`
6. `docs/spec/09_ARCHITECTURE_SAVE.md`, `15_DOMAIN_CONTRACTS_AND_FIXTURES.md`, `15_DOMAIN_CONTRACTS.md`
7. `docs/data/prototype_station_fixture_v0_1.json`
8. `docs/spec/11_QA_ACCEPTANCE.md`

## スコープを守る
- **最終製品はネイティブiOS/Android**。Unity 6のサポート中LTS+URPを正確なversionで採用し、Packages、ProjectSettings、基本Sceneまで整える。
- **自由歩行は禁止**。WASD移動、仮想歩行スティック、操作キャラクターの移動を新規実装しない。
- **最重要バグ回避**: `Assets/Scripts/Phase1/WarehouseBootstrap.cs`は `[RuntimeInitializeOnLoadMethod]` で自由歩行の旧Playerを自動生成する。これを安全に無効化または本番から隔離し、検証する。Git履歴は保持し、無関係の資産を破壊しない。
- 一つのUnityシーンで、3D倉庫・荷台・検査台・便別の出荷口・建設/設備エリア・道路を区画として構築する。高品質アートへの交換可能性を確保。**仮モデルだけで最終品質達成と報告しない**。
- 共通上部HUD、スマホ用横向きのナビとActionPanelを作る。メニューや3D設備タップで**固定作業カメラを直ちに切り替える**。切替で毎回シーン全体を破棄/再ロードしない。
- 検査ビューでは荷物を回転・ラベル拡大（今回はダミー情報のみ）、倉庫俯瞰ではパン/ズーム/設備選択。ベイ/出荷口は読みやすい区分と状態パネル。
- **コンベアの半透明ゴースト**が3D表示され、ドラッグ・90°回転・反転・赤/緑と理由・キャンセルができる。**S1では本物の買い物/設備配置/出荷/お金の動作を実装しない**。プレビューはWorldデータを変更しない。
- UIはノッチ/SafeArea、マルチタッチ、操作優先順位、ジェスチャーキャンセル/OS復帰を扱う。タッチは旧InputManagerに無条件依存せず、新InputSystemとの互換を確認。
- データ表示は`docs/data/prototype_station_fixture_v0_1.json`等のモックから取得。これは `prototypeOnly`, `promoteToProduction=false` で本番カタログと分離する。誤って収益を加算/箱を別スロットへテレポートさせない。
- 必要なグラフィック最適化: 非表示区画を描画しない、共有マテリアル、明暗の自然なバランス、遠景LOD候補、オブジェクトプール、過剰なRealtime Lighting/物理演算を避ける。**ただ軽い見た目にするのではなく近景の品質も考慮**。
- FPS/フレーム時間とUI切替時間の測定コードを追加。端末での30fps/熱は未測定なら未達と報告し、根拠のない最適化効果を断言しない。

## 作業の順序
### S0: プロジェクト構築
1. 既存ファイル、Editor用設定、Packages/manifest、Scene、Legacy自動起動箇所を監査。
2. Unity 6 LTSと対応URP版を固定し、UnityでProjectを開ける最小構成・初期シーンを構築。
3. InputSystem、UI EventSystem、URP renderer assetを適切に設定。
4. 旧Playerが絶対に起動しないことをテスト。Editorがない場合もコードをレビューして静的チェック。
5. `README.md`に正確な起動/検証手順と必要モジュールを記載。

### S1: 3Dステーションとタッチ操作
1. `StationRegistry / CameraStationController / SelectableEntityBinding`を実装。
2. 横向きのTopHUD/BottomNavigation/ActionPanelを実装。
3. 3D搬入口・検査台・出荷ゲート・倉庫俯瞰・建設/コンベア・道路ヤードのBlockoutを実装。
4. 入力Routerで1本指Tap/Drag、2本指Pinch、画面ナビ競合、PointerCancelをテスト。
5. ゴースト配置の移動/回転/反転/無効原因を実装（**在庫や所持金を変更しない**）。
6. 既存Fixtureの値を画面に表示する場合は「試作データ」と明示。
7. EditMode/PlayMode/モバイルビルドの可能なテストを実行し、結果を記録。
8. GitHubに小さなコミットで反映。差分・未検証を日本語報告。

## 合格条件
`docs/spec/20_MOBILE_STATION_VERTICAL_SLICE.md`のS0-01..06、S1-01..12に対応するPASS/FAIL/NOT_RUN表を作る。
最低限確認する実際のゲーム操作:
- [搬入]→荷台内部の3D、[検査]→箱、[出荷]→ゲート、[倉庫]→俯瞰、[建設]→半透明コンベアを**歩かず切替**。
- 同じアイテムを3Dタップしても正しい画面へ移る。
- 連続50回切替で黒画面・シーンロード・意図しない箱数/資金リセットなし。
- ゴーストを30回移動/Rotate/Flip/Cancelしても所持資金・実配置数は不変。
- 画面タップとズーム同時で操作競合がない。
- UnityEditor/Android/iOS実機で試していない条件は必ず`NOT_RUN`と報告。

## 次のフェーズへ持ち越すこと（今回は実装しない）
- 本物の荷物Transfer、検査合否・ラベル貼付、曜日と収益、コンベアシミュレーション、トラック時刻運行、本番用アート、課金/クラウド。これらはS2以降。
- 前提が整えば、S2は1箱を荷台から手で取得→検査→提出する縦切り。箱ID・Ownerとイベントの不変条件を優先。

## 完了報告フォーマット
作業ブランチ/コミット:
読んだ設計書:
変更ファイル:
実装した入力と画面:
旧自由歩行の無効化方法:
Unity/URP/InputSystem版:
テスト ID + PASS/FAIL/NOT_RUN:
実際のビルド・動作が確認できた環境:
iOS/Androidで未検証の点:
パフォーマンス・熱・メモリ計測値（計測無しなら未測定）:
参照作品と同じ部分、スマホ用に変更した部分:
次Sprintの残課題:

**始める際に質問を重ねず、確認できるGitHubの状態から実装に着手してください。** 未確認の収益式/曜日を勝手に埋めず、S0/S1の範囲を確実に完了してください。
