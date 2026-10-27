# Unityを使わないS0/S1実装・検証記録

対象: Issue [#1](https://github.com/D7Rvv/warehouse-simulator/issues/1)、2026-10-27、`feature/godot-native-mobile`。

## 判断と実装範囲

最新のユーザー指示「Unityなし」「公式と同等以上を細かく確認」「スマホの熱と滑らかさを優先」を採用しました。[D]はUnity不使用・品質目標・優先順位、[P]はGodot **4.6.3 standard**／Compatibilityの技術選択です。SPEC-19のDEC-019～022へ記録しています。

`mobile/project.godot` が現行プロジェクトです。ゲームの実行にUnity、Unityライセンス、.NET、Blenderは不要です。旧Unity PR [#2](https://github.com/D7Rvv/warehouse-simulator/pull/2) とGit履歴は保全し、旧 `Assets/Scripts/Phase1` はGodotの `res://` の外に置きます。Godot標準版からC#のUnity自動起動は実行されません。以前の自由歩行／7日間キャンペーンは移植していません。

原作の価格・曜日・収益・Tierの仕様は変更していません。`docs/data/prototype_station_fixture_v0_1.json` と同一の隔離Fixtureを `mobile/data/` に収録し、`prototypeOnly=true`／`promoteToProduction=false` を検証します。資金・時刻・箱の所属を読み取り専用にし、画面にも試作表示を出します。物流、購入、本設置、検査合否、収益、運行の完成を意味しません。

## 実装した機能

| 対象 | 実装と主なファイル |
| --- | --- |
| 一つの3D倉庫 | `mobile/main.tscn`, `main.gd`, `scripts/station_world.gd`。1World／1Camera、切替時に資産再生成なし |
| 作業場所 | 搬入・検査・出荷・倉庫・建設を下部から直接選択。仕分け・保管・トラック・ショップ・経営・契約はメニュー経由。未解放理由を表示 |
| 選択と検査 | `station_model.gd` にStation／Target登録・不正選択拒否・箱ごとのDraft、回転・伝票拡大状態。荷台の3箱は固有IDでRaycast選択 |
| 入力 | `station_pointer_router.gd`, `station_hud.gd`。指IDごとのUI／World捕捉、ドラッグ、ピンチ、Cancel、中断復帰、SafeArea、短い横画面 |
| 建設 | 格子へ半透明コンベアを移動、90度回転、IN/OUT反転、赤／緑と理由、取消。固定設備の境界も参照するが確定Gridや財布は変更しない |
| 3D資産 | `assets/models/` に独自制作8GLB、23,812ユニーク三角形、4,906,348bytes。512px素材、面取り、梁、車体、柵、ローラー、支柱、ラベル |
| 計測 | `scripts/mobile_quality.gd`。フレーム間隔p95／p99、切替CPU時間、エンジンのDrawCall／メモリ推定とCSV。GPU時間・温度の実測は提供しない |

## スマホ性能・熱の対策

- 既定30fps上限。省電力／バランス／高品質のどれも通常30fps。高品質選択だけで60fpsにしません。
- 3Dだけの解像度要求を制限し、UI解像度は保ちます。初期倍率0.72／0.85／1.0、縦720／900／1080px上限は[P]予算です。実GPUのバッファ寸法を測定済みとは扱いません。
- p95が継続的に予算を超えると段階的に解像度を下げ、20秒の調整間隔と60秒の回復待ちで往復を抑制します。短い瞬間負荷だけで画質を往復させません。
- 非表示区画をカリングし、メッシュ・材質・World・ゴーストを再利用。剛体箱、歩行キャラクター、Realtime影、SSAO、SSRは使いません。接地は限定した静的表現です。
- バックグラウンドでは描画と計測を停止し、入力を解除します。復帰直後の長い間隔を性能サンプルに混ぜません。
- OS熱センサー連携は **未実装**。CRITICALを注入する方針試験では入力を停止し5fpsへ制限しますが、本物の端末温度から自動制御した証拠ではありません。

iPhone 13 Pro Max／iOS不明を初回候補、Androidは未選定。開発PCは記録済みのi7-7700／16GB／Intel HD Graphics 630です。いずれの実機でも今回の性能は未測定です。`mobile/native/platforms.json` とREADMEは要件・未設定項目の記録で、実行可能なiOS／Android Export presetではありません。

## 実施した試験

| 試験 | 実行と結果 | 証跡 |
| --- | --- | --- |
| Godot実スクリプト | ヘッドレス **86/86 PASS**。Fixture、モデル不変条件、入力競合、合成負荷での画質制御 | [headless/report.json](evidence/godot/headless/report.json) |
| 実描画・入力 | X11／Mesa llvmpipeで **76/76 PASS、13画像**。複数フレームのタッチ・ドラッグ・ピンチ・取消、3箱のRaycast、50回のボタン切替、1,000回のEngine切替、30回のプレビュー操作 | [render/report.json](evidence/godot/render/report.json) |
| 追加視点 | ヤード・保管・仕分けの **3画像** を実描画。開始／終了のソースSHA一致 | [visual-tour/report.json](evidence/godot/visual-tour/report.json) |
| 実GLB監査 | **8モデル PASS**。実Index・Node変換・PBR値・画像・SHA、1セルの実外形を検証 | [asset-audit.json](evidence/godot/asset-audit.json) |
| ソースの独立展開 | キャッシュなしZIP展開→import→起動→86件→資産監査を別作業者が再実行して成功 | 最終アーカイブの全エントリーSHA／CRCも検査 |
| Linux／Windows export | 正規4.6.3 release templateでELF／PEを実出力。Windows OS実行はNOT_RUN | `builds/godot-mobile/packages/manifest.json` |
| 配布用Linuxの隔離起動 | **11/11 PASS、4画像**。エディター・開発プロジェクトなしで起動し、X11の実マウス操作・画像を確認。時計部分を除外した3D領域の変化を検査し、画面名・日本語・モデルを目視確認。内部状態とモバイル性能は対象外 | [packaged-validation/report.json](evidence/godot/packaged-validation/report.json) |

Godotは `4.6.3.stable.official.7d41c59c4`。実描画はMesa 23.2.1 llvmpipe（ソフトウェア描画）です。既存コンテナのX11／Mesaのみを利用し、Unity Editor・ライセンス・プロジェクトは使用していません。VSync切替をサポートしないドライバーの警告はあります。ダミーレンダラーを視覚検証に用いていません。

13枚の正式画像で総DrawCallは **48～187（UI・メニューを含む）**。1,000回切替の処理時間合計は1,197.748ms、追跡静的メモリ差は-2,648bytes、Worldは163ノードで不変でした。これらは一度のデスクトップ試験の観測値で、実機の30fps、タップから表示までの遅延、メモリリークの不存在、低発熱を証明しません。

実行中のソース・GLB・フォント・インポート結果のハッシュ一致を記録しています。インポート済み `.scn` のハッシュは今回のキャッシュの証跡であり、別環境で同一バイナリを保証するものではありません。

## SPEC-20受入への対応

Unity固有条件はDEC-019に従ってGodotへ読み替えます。スマホ条件はデスクトップ試験と分けます。

| ID | 状態 | 根拠・残条件 |
| --- | --- | --- |
| S0-01 | PASS（Godot版） | 版固定、project.godot、main.tscn、8GLBを実import |
| S0-02 | PASS（Godot版） | 実parse・起動・描画。Unityコンパイルを意味しない |
| S0-03 | PASS（Godot版） | 現行res://外に旧Assets、CharacterBody／RigidBody／歩行なしを実チェック |
| S0-04 | PASS | check.sh／render.shと実テスト。専用CIサービスの実行は未実施 |
| S0-05 | PARTIAL | プラットフォーム別の未設定記録あり。実native preset／SDK／署名／ビルドはNOT_RUN |
| S0-06 | PASS | 旧PR・Git履歴保全、現行プロジェクトをmobile/に隔離 |
| S1-01 | PASS（デスクトップ） | 16枚の実描画、原作同等性の承認ではない |
| S1-02 | PASS（デスクトップ） | 各主要ナビを実InputEventで切替 |
| S1-03 | PASS（デスクトップ） | PICK-*／TAP-*で3箱、対象Station／IDの保持 |
| S1-04 | PASS | 50／1,000回切替で権威データ不変、Draft保持 |
| S1-05 | PASS | 30回移動／回転／反転／取消、実配置と資金不変 |
| S1-06 | PASS（合成イベント） | 指保持・ピンチ・UI同時押し・Cancel・復帰。実タッチパネルはNOT_RUN |
| S1-07 | PARTIAL | 844×300／390の実描画はPASS。端末ノッチ・OS戻るはNOT_RUN |
| S1-08 | PASS（試験範囲） | 無効Station／Target・ロック拒否、カメラ保持、捕捉解除 |
| S1-09 | PASS（独立アート） | 独自生成と実資産監査。最終品質承認は未完了 |
| S1-10 | PARTIAL | デスクトップのサンプル取得。実機値はNOT_RUN |
| S1-11 | NOT_RUN | iOS／Androidネイティブ実行、30fps、30分以上の熱測定なし |
| S1-12 | PASS（本変更） | 実コード、シーン、資産、テスト、手順と未検証事項をGitへ収録 |

## 3Dの細かい確認と残課題

[22項目の確認表](GODOT_VISUAL_CHECKLIST.md)に視点別のPASS／PARTIAL／NOT_RUNを記録しています。実画像を見て、俯瞰の遮蔽壁、出荷の二重看板、検査の浮遊文字、HUDのはみ出し、箱を遮るトラックCollider、構造物へ重なる緑プレビューを修正しました。追加ツアーで見つけた道路とドックのZ-fighting、保管の文字重なり、仕分けの遠すぎるカメラも修正・再撮影しました。

**公式と同等以上とは判定していません。** 元の3枚の画像が環境になく、公式Steamへの実接続はCONNECT 403でした。比較未実施を不一致のない証明にしません。斜線のジャギー、接触陰影の深さ、全資産のUV／微細PBR、最終照明・LOD・情報密度には改善が残っています。熱の優先順位を守り、重い影や画面効果で隠していません。

## 再現と成果物

Godot4.6.3 standard、Python3.11以降、Pillowを用意し `mobile/tools/check.sh`。実DisplayまたはXvfb＋Mesaで `mobile/tools/render.sh`。正規Export Templatesで `python3 mobile/tools/package.py`。完全なログは `builds/godot-mobile/`、レビューに必要な画像とJSONは本書から参照する証跡へ収録しています。

確認用Linux／WindowsパッケージとGodotだけのソースZIPを `builds/godot-mobile/packages/` に保存します。Windowsの書き出しは成功していますがWindowsでの実行は未検証です。署名、ストア登録・審査・配布、ネイティブスマホビルド、実機の電池・温度・GPU時間はNOT_RUN。Webゲームの再公開や公開Releaseは行っていません。

次は、native export／署名・実機導入の成立確認と30分測定、取得できる原画像との比較です。S2以降の箱移送・合否・出荷・台帳は今回のモックに紛れ込ませず、SPEC-15／15Bの不変条件を満たす別Sprintとして扱います。
