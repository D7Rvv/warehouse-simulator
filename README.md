# Warehouse Simulator — Unity S0/S1

ネイティブiOS／Android向け3D倉庫ゲームの、固定ステーション操作を実装した開発用スライスです。[Issue #1](https://github.com/D7Rvv/warehouse-simulator/issues/1) と [SPEC-20](docs/spec/20_MOBILE_STATION_VERTICAL_SLICE.md) に対応します。自由歩行はありません。

**Unity Editor実行とスマホ実機確認は未実施です。** この環境ではライセンス不足でEditorがexit 198となりました。純粋C#43件・静的検査8項目・実Unity参照による外部C#コンパイル6アセンブリは成功しています。外部チェックのパッケージDLLは固定版より古く、Unityインポートや描画確認を代替しません。[受入結果と制約](docs/implementation/S0_S1_REPORT.md) を参照してください。

## 起動

1. Unity Hubで **Unity 6000.3.27f1（Unity 6.3 LTS）** をライセンス認証し、このリポジトリを開きます。
2. パッケージの解決・インポート後、`Assets/Scenes/WarehouseStation.unity` を開いてPlayを押します。
3. Gameビューを横向きにし、下部の **搬入 → 検査 → 出荷 → 倉庫 → 建設**、または3D設備のタップで即時切替します。

`Packages/manifest.json` はURP **17.3.0**、Input System **1.17.0**、uGUI **2.0.0**、Test Framework **1.6.0**を固定しています。UPM解決を実行できていないため、推測した `packages-lock.json` は作成していません。初回の正規Editorインポートで生成し、依存関係を確認する必要があります。

## 今回の操作

| 画面 | 操作 |
| --- | --- |
| 搬入 | 荷台・接車ベイ、Fixtureの3箱の選択・フォーカス |
| 検査 | 表示用の箱をドラッグ／ボタンで回転、同じFixture情報の伝票を拡大 |
| 出荷 | ゲートと容量、現在バッファと前回バッチ明細を別々に表示 |
| 倉庫 | 制限付きパン・ピンチズーム、設備タップで対象選択 |
| 建設 | 半透明ベルトの格子移動、90°回転、IN/OUT反転、緑／赤と理由、取消 |
| メニュー | 仕分け・保管・トラック・ショップ・経営・契約の情報枠、診断・CSV |

SafeArea、48dpを目安とするボタン、短い横画面での説明配置、スクロール付き詳細、戻る操作を用意しました。新Input Systemで指ごとの所有権、ピンチ、取消、中断、画面切替を扱い、ピンチ中やキャンセル前に始まったUI押下の遅延発火を防ぎます。

**すべて「試作データ [P]」です。** [Fixture](docs/data/prototype_station_fixture_v0_1.json) は `prototypeOnly=true`、`promoteToProduction=false` の読み取り専用モデルへコピーします。切替・プレビューは資金、荷物の所有、固定表示時刻、確定設備を変更しません。検査台の箱は表示専用見本で、荷台から移送された箱ではありません。

実際の移送・検査合否・貼付・購入・設置・出荷精算・コンベア搬送・運行時計はS2以降です。価格、収益式、未確認の曜日、Tier条件を追加していません。3Dは独自の仮モデルで、完成アートではありません。

## 検証

ライセンス認証済みの指定Editorを指定します。

```bash
export UNITY_EDITOR=/path/to/6000.3.27f1/Editor/Unity
python3 tools/unity/run_editor.py validate
python3 tools/unity/run_editor.py editmode
python3 tools/unity/run_editor.py playmode
```

Unity Test RunnerからもEditMode 43ケース、PlayMode 14ケースを実行できます。結果XMLとログは `Builds/Validation/` に出力します。過去の結果を今回の成功として使いません。

このクラウドに残るUnity付属Monoで、エンジン実行不要の検証ができます。

```bash
python3 tools/unity/validate_s01.py
python3 tools/unity/compile_sources.py
```

前者は実際のJSONを読み、50回の拠点切替・30回のプレビュー・不正対象・所有権保持・入力競合など42ケースと静的8項目を実行します。後者は実Unity DLLへの追加コンパイルです。参照キャッシュはInput System 1.8.1／URP 17.0.1なので、**固定版でのコンパイル成功とは扱いません**。必要なツールがない環境はNOT_RUNです。[保存した証跡](docs/implementation/evidence/) を参照してください。

## Android／iOSの開発ビルド

```bash
python3 tools/unity/run_editor.py android
python3 tools/unity/run_editor.py ios
```

Android Build Support・SDK／NDK／OpenJDK、iOS Build Supportを追加します。開発プロファイルは `Assets/Warehouse/Settings/Profiles/` に分離しました。iOS署名・実機導入にはmacOS／Xcode・実際のTeam ID等が必要です。秘密鍵やライセンスファイルはコミットしません。ネイティブ成果物は今回未作成です。

## スマホ性能と資産

- 一つの倉庫と主カメラを維持し、切替で再ロード・モデル再生成を行いません。
- 区画ごとのRenderer／Collider切替、共有マテリアル、再利用ゴースト、物理Rigidbodyなしの表示を採用しました。
- URPモバイル設定と保持用マテリアルをコミットし、不透明／透明Shader参照を残しました。
- 日本語フォントはSIL OFLのNoto JPから必要文字を抽出した約209KBのOTFです。ライセンスと生成元ハッシュを同梱。文字追加時の再生成には `tools/unity/generate_ui_font.py --source /path/to/NotoSansCJK-Regular.ttc` とfontToolsを使用します。
- 診断／CSVは直近1,800フレームの平均・p95・p99、CPU側切替処理時間、描画数、Unity割当メモリを記録します。GPU時間・タップから表示までの遅延・温度の測定値ではありません。

30fpsは設定上の目標です。iPhone／Androidの画質、長時間FPS、1,000回切替のメモリ、30分以上の熱試験は **NOT_RUN** です。

## 設計と履歴

- [仕様の目次](docs/spec/00_MASTER_INDEX.md)、[確定事項と暫定判断](docs/spec/19_DECISION_REGISTER_AND_SAFE_DEFAULTS.md)
- [今回の画面・入力・受入条件](docs/spec/20_MOBILE_STATION_VERTICAL_SLICE.md)、[Codexタスク](docs/tasks/CODEX_NEXT_S0_S1.md)
- [実装判断・受入表](docs/implementation/S0_S1_REPORT.md)

旧 `Assets/Scripts/Phase1/` の5ファイルはGit履歴と原文を残し、未定義の `WAREHOUSE_LEGACY_FREE_WALK` によりコンパイル・自動起動を無効にしました。このシンボルを新プロジェクトで有効にしないでください。以前のGodot実験は今回のUnity実装・試験・配布物に含めていません。
