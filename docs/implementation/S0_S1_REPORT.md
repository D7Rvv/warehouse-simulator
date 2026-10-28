# Issue #1 — S0/S1 実装・検証記録

記録日：2026-10-26。作業ブランチ：`feature/issue-1-s0-s1`。実装コミット：`85a40a2670badb63b663f3370a7ebc83ecaaaaa5`。起点は `origin/main` の `9f3fa13` です。後続の入力取消修正・テスト／文書コミットを含むブランチ差分でレビューしてください。

**実装候補を作成しましたが、Unityでの動作受入は未完了です。** 指定Editorの起動を実際に試み、ライセンス不足・exit 198で検証／EditMode／PlayModeが停止しました。描画画像、ネイティブビルド、スマホの実測値は今回ありません。

## 設計とスコープ

最初にAGENTS、SPEC-00、SPEC-19、SPEC-20、TASK-S0-S1を確認しました。関連するSPEC-03／05／09／10／11／12／13／15／15BとFixtureを実装・試験へ対応付け、SPEC-17B／18の出荷・金額の補正を確認しました。今回のソースはカメラ・表示・入力・プレビューに限定し、原作の未確定数値を補っていません。

| 追加・変更した範囲 | 主要ファイル |
| --- | --- |
| Unityプロジェクトと依存固定 | `Packages/manifest.json`、`ProjectSettings/`、`Assets/Warehouse/Settings/` |
| 起動Scene／Prefab | `Assets/Scenes/WarehouseStation.unity`、`Assets/Warehouse/Prefabs/WarehouseApp.prefab` |
| 旧自由歩行隔離 | `Assets/Scripts/Phase1/` 全5ファイル |
| Fixture・不変スナップショット・選択・プレビュー | `Assets/Warehouse/Domain/` |
| 3D倉庫・単一カメラ・半透明ゴースト | `Assets/Warehouse/Presentation/World/` |
| UIと起動統合 | `Assets/Warehouse/UI/`、`Assets/Warehouse/Presentation/StationApp.cs` |
| 入力所有権・キャンセル・診断 | `Assets/Warehouse/Presentation/Input/`、`Diagnostics/` |
| 回帰試験・再現コマンド | `Assets/Warehouse/Tests/`、`tools/unity/` |
| 正確なモック・Shader保持・日本語 | `Assets/Resources/Prototype/`、`WarehouseMaterials/`、`WarehouseJapanese.*` |

Unity **6000.3.27f1**／URP **17.3.0**／Input System **1.17.0**／uGUI **2.0.0**／Test Framework **1.6.0**。Editor同梱パッケージ情報、公式Input Systemタグのパッケージ情報を確認して固定しました。正規UPM解決はNOT_RUNで、未解決の依存を解決済みとするlockファイルは作っていません。

## 実装した操作

- 一つのWorld・一台の主Cameraを保持し、固定Anchorへ瞬時に切り替えます。場所ごとのパン／ズーム状態を保存し、無効・ロック・欠落Anchorは現在のビューを維持して理由を出します。
- 搬入はドック、シャッター、接車トラック、Fixtureと同じIDの3箱を表示します。検査は別の表示用見本で、同じデータから3Dラベルと拡大伝票を作り、回転・Draft状態を保持します。
- 出荷の現在バッファ1/16と、前回の正16件・正箱価値1008・手数料5・純額1003は別のFixture表示です。現在の1/16に16を掛けたり、資金へ加算したりしません。AIRの曜日は未確定と表示します。
- 下部5画面＋メニューで全11項目へ2タップ以内に到達し、未解放契約は理由を通知します。実未実装の処理を成功したように演出しません。
- 建設は緑／赤の半透明ベルト、格子、IN→OUT矢印、90°回転、反転、範囲外・占有理由、取消を表示します。「確認（デモ）」は本設置を増やしません。
- UI／3Dの指所有権をDownで決め、ドラッグ途中で他の操作へ譲りません。ピンチ開始、モード変更、中断、フォーカス喪失、入力デバイス切断でキャンセルします。UI押下時の可否とキャンセル世代を保持し、後から発火させません。
- 横向きSafeArea、48dp目安のボタン、詳細のスクロール、戻るを実装しました。高さ300dp相当の短い画面では説明を左側へ移し、ボタンと表示領域を維持する計算にしています。実際の表示確認はNOT_RUNです。

### 旧自由歩行

5ファイルの原文全体を `#if WAREHOUSE_LEGACY_FREE_WALK` で囲みました。PlayerSettingsへこのシンボルは追加していません。`RuntimeInitializeOnLoadMethod` を含む旧コード全体が既定ビルドではコンパイルされません。元の内容とGit履歴を保持し、元ファイルが囲みの内部でバイト一致することも確認しました。PlayModeには旧Player／Bootstrapが存在しない試験を用意しています。

## 今回採用した暫定技術判断 [P]

- フェードや補間を使わない即時カット、単一Scene・Camera・起動時生成の仮モデル。
- プレビューだけの1セル＝1 Unity単位、座標 -6..6、Fixtureの階・占有セルのみを検証。実機器サイズ・本番GridServiceの仕様ではありません。
- Fixture時刻09:00は固定表示と明示し、別の表示経過秒だけを継続します。物流時計・金銭・所有権の変更は実装していません。
- Android開発設定はARM64／IL2CPP、min API26／target36。iOSはIL2CPPで未署名Xcodeプロジェクト出力の入口。対応機種・最低性能を検証した決定ではありません。
- 図形と材質は独自の仮アート。Unity公式テンプレートのURP設定資産を使用し、原作品の画像・メッシュ・UI資産は使用していません。

## 実行した検証

| 検証 | 結果 | 証跡 |
| --- | --- | --- |
| 実JSONを読む純粋C# | **PASS_LOGIC 43/43**（Domain 27、Pointer 16） | [validation.json](evidence/validation.json)、[logic-tests.log](evidence/logic-tests.log) |
| Project／依存／Legacy／Fixture／asmdef／URP／プロファイル／GUID静的確認 | **PASS 8/8** | 同上validation.json |
| 実Unity参照の外部Roslynコンパイル | **PASS 6アセンブリ、エラー0** | [external-csharp.json](evidence/external-csharp.json) |
| 指定Unity Editorの検証／EditMode／PlayMode起動 | **NOT_RUN**、各exit 198・ライセンス不足 | [editor-attempts.json](evidence/editor-attempts.json) |
| Android／iOSビルド、実機UI・GPU・性能・熱 | **NOT_RUN** | 開発プロファイルと下表 |

外部コンパイルは実Editor 6000.3.27f1のEngine DLLを使いますが、同梱公式テンプレートのパッケージDLLは **Input 1.8.1／URP 17.0.1** です。固定したInput 1.17.0／URP 17.3.0でのインポート・コンパイル、asmdef解決、Font／Scene／Materialのインポート、Shader描画を検証した結果ではありません。UnityEngineを模倣するスタブによる合格ではありません。

EditMode 43ケース、PlayMode 14ケースのソースを用意しています。PlayModeは実Scene、50回ナビ、Physics Raycast、30回ゴースト、UI押下とピンチ／フォーカス、SafeArea、Draft保持、カメラ範囲、透明材質、診断統計を対象にしています。**作成・外部コンパイル成功を実行成功としません。**

## SPEC-20の受入表

PASS_LOGIC／静的PASSとUnity／端末のNOT_RUNを分けています。全面合格・G1達成とは宣言しません。

| ID | 結果 | 実装・試験・残る確認 |
| --- | --- | --- |
| S0-01 | 静的PASS／Open NOT_RUN | 指定版・manifest・Settings・Sceneを作成、STATIC-001/002。正規Editorインポートが必要 |
| S0-02 | NOT_RUN | 外部6アセンブリはPASS。固定パッケージのEditorコンパイルは未実施 |
| S0-03 | 静的PASS／実行NOT_RUN | STATIC-003、旧5ソースを囲みで隔離、PlayMode S0_03 |
| S0-04 | NOT_RUN（TestRunner） | CLI／NUnit／CI用入口を用意し起動試行。純粋C#43件は実行成功 |
| S0-05 | 静的PASS／ビルドNOT_RUN | STATIC-007、Android／iOS別設定・不足項目明記 |
| S0-06 | PASS（原文・履歴） | 旧ソースの内容を維持、Git差分と原文バイト一致確認 |
| S1-01 | NOT_RUN | 各3D区画・ゴースト実装、描画の目視は未実施 |
| S1-02 | NOT_RUN（実Scene） | 即時切替実装、PlayModeの実ボタン50回試験は未実施 |
| S1-03 | NOT_RUN | 3D Binding／Raycast統合、実Raycast試験は未実施 |
| S1-04 | PASS_LOGIC／Unity NOT_RUN | 50回切替後にID・Owner・資金・固定時計・Draftを保持 |
| S1-05 | PASS_LOGIC／Unity NOT_RUN | 30回Move／Rotate／Flip／Cancelと不変スナップショット |
| S1-06 | PASS_LOGIC／実入力NOT_RUN | 16ケースで指所有・Cancel・UI押下世代を検証。実UI／OSは未実施 |
| S1-07 | NOT_RUN | SafeArea・短画面・戻るを実装、端末／シミュレータ確認が必要 |
| S1-08 | PASS_LOGIC／Unity NOT_RUN | 未知・ロック・Anchor欠落・削除Targetを安全拒否／解除 |
| S1-09 | PASS（資産由来監査） | 独自手続き形状・仮材質と許諾済みフォント。完成品質は未達 |
| S1-10 | NOT_RUN | 計測器は実装、代表端末での負荷・メモリ・切替計測は未実施 |
| S1-11 | NOT_RUN | 30fps・30分熱・ネイティブ動作の達成値なし |
| S1-12 | PASS（Git記録） | コード・Scene・Prefab・アセット・試験・README・本記録をコミット。GitHub上の `feature/issue-1-s0-s1` を参照 |

## スマホ性能対策と測定の限界

モデル・Anchor・Ghostを起動時に生成して再利用し、通常切替で同期ロード・Instantiate／Destroyを行いません。共有URPマテリアル、区画ごとのRenderer／Colliderカリング、単一の透明ゴースト、全箱Rigidbodyなし、リアルタイム影なしの仮照明で負荷要因を抑えています。保持用の不透明／透明MaterialにShaderを参照させ、実機ビルドでのShader strippingに備えています。日本語OTFは必要文字を抽出し、元TTC約19MBに対して約209KBです。

診断器は平均／p95／p99フレーム間隔、CPU側カメラ切替処理、描画数、Unity割当メモリを記録します。設定目標は30fpsです。**実測FPS、GPU時間、熱、電池、メモリ増加量、タップから描画までの時間は未測定**。計測器内の統計へ合成値を入れるテストは、端末性能の証拠ではありません。Lightmapベイク、最終LODと高品質アート、実機でのカリング／透明描画の効果は後続確認です。

## 残作業・次Sprint

1. 正規ライセンスの指定UnityでUPM解決・Scene／Font／Materialインポート・Editorコンパイル・EditMode／PlayModeを実行し、不具合があればS0/S1で修正する。
2. iPhone／Androidの安全領域・マルチタッチ・中断・戻る・文字を実機で確認する。1,000回切替と30分以上のFPS／p95／p99／GPU／メモリ／熱を測定する。
3. S0/S1の動作受入後、S2で一箱の明示的なOwnership移送→検査→提出の縦切りへ進む。未確定の報酬・曜日は引き続き本番値にしない。

以前のGodot変更は検証済みソースZIPとGit stashへ保全し、今回のブランチへ混ぜていません。公開しないという回答に従い、旧Webゲームの配信ブランチから実行データを除去し、公開停止ページを確認しました。Pages設定自体の削除はGitHub連携の権限不足（HTTP403）で実施できませんでした。既存Git履歴の消去は行っていません。
