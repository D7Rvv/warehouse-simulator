# Warehouse Simulator — Godot Station Edition

**現行実装はGodot4.6.3標準版です。Unityは使用しません。** `mobile/project.godot` を開いて実行してください。Godot Editor／ゲーム実行にはUnityライセンスや.NET、Blenderは不要です。

ネイティブiOS／Android向け倉庫ゲームのS0/S1操作を移植しました。自由歩行はなく、同じ3D倉庫の作業場所をタップで切り替えます。スマホの滑らかさ・発熱を、追加の画質効果より優先しています。**実機の30fps・熱性能と、公式作品との画質同等性は未検証です。**

## 起動と操作

配布用Windows確認版はZIPを展開して `WarehouseStation.exe` から起動します。iPhoneへ直接導入できる版はまだありません。PCが使える場合のSteam Link手順と現在の制限は [確認版を触る手順](docs/play/PLAY_ON_IPHONE.md) を参照してください。

1. Godot **4.6.3標準版** で `mobile/project.godot` をインポートしてF5。
2. 横向き画面の下部から搬入 → 検査 → 出荷 → 倉庫 → 建設を切り替えます。3D設備や箱のタップも同じ選択へつながります。
3. 検査はドラッグ／ボタンで箱を回転し、伝票を拡大。倉庫はパン／ピンチズーム。建設は半透明ベルトを格子へ移動・90°回転・反転・取消できます。
4. 「画質」から省電力／バランス／高品質を選びます。既定は30fps上限で、UI文字とは別に3D解像度を調整します。

Windows開発PCは既存記録のIntelHD630／i7-7700／16GBを考慮したCompatibility構成です。そのPCの実測結果ではありません。iPhone13ProMaxを最初の実機試験候補とし、最低対応機種を確定したものではありません。

## 今回作成した3D

倉庫、荷台トラック、検査台、コンベア、出荷ゲート、ラック、パレット、箱の8種類をBlenderで実制作し、GLBへ書き出しました。モデル全体のユニーク面数は **23,812三角形**、約4.9MB。独自512px材質、面取り、支柱・脚・ローラー・荷台内部・安全柵・ラベルを含みます。

実Godot描画を見て、俯瞰で邪魔になる右壁、看板の重複、UIのはみ出し、箱ラベル、ドック高、箱を遮るCollider、プレビューの物理的な重なりを修正しました。接地は小範囲の静的表現で補い、リアルタイム影・SSAO・SSRを使いません。ゴーストとWorldを再利用し、通常切替で再ロードしません。

**公式と同等以上は品質目標です。達成判定ではありません。** 元の3枚の画像が現在の環境になく、公式Steam画像の取得も接続制限で停止しています。斜めエッジ、最終照明・LOD・細かい使用感の改善も残っています。[22項目の3D確認表](docs/implementation/GODOT_VISUAL_CHECKLIST.md)を参照してください。

## 検証と配布

検証ツールにはPython3.11以降とPillow、描画にはDisplayまたはXvfb＋Mesaが必要です。これらは配布ゲームの実行依存ではありません。

```bash
mobile/tools/check.sh
mobile/tools/render.sh
python3 mobile/tools/package.py
```

Godotのヘッドレス検証と、実描画での複数フレームのタッチ／ドラッグ／ピンチ／取消、箱3個のRaycast、50回のボタン切替、1,000回のエンジン切替を実行します。画像、実フレーム統計、資産・ソースのハッシュを記録します。描画には実DisplayまたはXvfb＋Mesaが必要で、ダミーレンダラーを画像確認の代用にしません。

Linux／Windowsの確認用ビルドと、このフォルダだけのソースZIPを `builds/godot-mobile/packages/` に作成します。自動公開は行いません。iOSの署名・実機導入には引き続きmacOS／Xcodeや実際のTeam情報等が必要です。詳細は[移行・検証記録](docs/implementation/GODOT_MIGRATION_DECISION.md)と [mobileの案内](mobile/README.md)を参照してください。

## データと性能の制約

- 全て「試作データ [P]」。元のFixtureと一致する読み取り専用スナップショットです。画面切替・プレビューで資金、時計、箱の所属を変更しません。
- 実際の移送・検査合否・貼付・購入・設置・出荷精算・コンベア搬送・運行は後続Sprintです。価格、収益式、未確認の曜日を追加していません。
- 継続負荷で3D解像度を段階調整し、回復を遅くして往復を抑えます。背景では描画・計測を停止します。
- OS熱センサーは未実装です。注入したCRITICALヒントでの操作停止・5fps制限を、本物の端末熱の測定と混同しません。
- 計測はフレーム間隔、p95／p99、切替CPU処理、描画数、メモリ推定です。GPU実行時間、電池、温度、30分後のiPhone／Android性能は **NOT_RUN** です。

仕様の入口は [SPEC-00](docs/spec/00_MASTER_INDEX.md)、[SPEC-19](docs/spec/19_DECISION_REGISTER_AND_SAFE_DEFAULTS.md)、[SPEC-20](docs/spec/20_MOBILE_STATION_VERTICAL_SLICE.md)。以前のUnity実装は過去のPR・履歴として保全し、以前のGodot自由歩行／7日間キャンペーンは再導入していません。公開しないという回答を保持し、Web版の再公開は行いません。
