# 今の確認版を触る

現在の配布用確認版はWindows用です。UnityやGodot Editorのインストールは不要です。
ZIPをWindows PCへ保存して「すべて展開」し、`WarehouseStation/WarehouseStation.exe` を起動します。
Windows用ファイルの書き出し・ZIP/実行ファイルのSHA256とCRC確認は済んでいますが、このクラウドからWindows OSでの起動は検証していません。

操作できるのは、3D倉庫の作業場所切替、検査箱の回転と伝票拡大、コンベアの半透明配置プレビューです。
荷物の搬入→検査合否→出荷→収益まで進むゲーム本編は未実装です。

## iPhoneからPCの画面を操作する方法

**Windows PCを起動したまま使います。iPhone単独で動くアプリではありません。**
一般公開をしない選択肢として、Steam LinkによるPC画面の転送があります。
このゲームとSteam Linkの組合せ、iPhoneの入力・画質・発熱は未検証です。

1. Windows PCで確認版を展開し、まず `WarehouseStation.exe` を起動できるか確認します。
2. PCのSteamで「ゲーム」→「非Steamゲームをマイライブラリに追加」→「参照」から、そのexeを追加します。
3. iPhoneのApp StoreでValveの **Steam Link** を入れます。PCとiPhoneを同じWi-Fiにつなぎ、Steam Linkの案内でPCとペアリングします。
4. Steam Linkで接続し、追加したゲームを選びます。横向きで、タッチ操作をマウス操作として使います。

メニュー、箱の回転、ズーム用の画面ボタンを利用できます。スマホの2本指イベントがPCへそのまま伝わるとは確認していません。
Steam Linkの案内は [Steam Remote Play](https://store.steampowered.com/remoteplay) を参照してください。

## iPhoneだけではまだ起動できない理由

署名済みiOSアプリはまだ作成しておらず、Safariで開ける配信URLもありません。
ユーザーの「公開しない」という選択を保持し、Webゲームの公開・公開Release・外部トンネルは実行していません。

2026-10-28に、Godot4.6.3の正規single-thread Web書き出しを、実Chromiumの安全なコンテキストではないHTTPオリジンで確認しました。
ゲーム起動前に `Secure Context - Check web server configuration (use HTTPS)` が出ます。
したがって、単にPCからHTTPで同じWi-Fiへ配信するだけでは、この版は起動しません。
この方式の試作ZIPを動作品として配布しません。確認用に生成したファイルは、無視対象のビルドフォルダへ隔離しています。

HTTPS要件を無効化したり、iPhoneへ証明書の信頼設定を追加したりする手順は採用していません。
PCなしで遊べる状態は、実際のiOS署名・導入、または利用条件に合うHTTPS配信の成立を確認してから案内します。
