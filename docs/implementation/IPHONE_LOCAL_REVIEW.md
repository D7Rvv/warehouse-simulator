# iPhone単独のローカル確認版

2026-10-28、`feature/iphone-offline-review`。ユーザーの「iPhoneだけで遊べる版」に対応します。Unity不使用、一般公開しない、スマホの滑らかさ・熱を優先する方針は保持します。

## 配布方式と範囲

Godot4.6.3 standardのsingle-thread Web書き出しから、エンジンJS、WASM、PCK、ライセンスを **一つのHTML** へ組み込みます。iPhoneのScriptable本体で `WebView.loadFile` → `present` し、端末内で実行します。Scriptableのインストールと最初のダウンロード後、PC、サーバー、公開URL、Apple Developer署名、追加の信頼証明書は不要です。

これはScriptableを実行コンテナとする **S0/S1の読み取り専用操作試作**。独立ネイティブiOSアプリや、搬入→検査合否→出荷→収益の本編の完成ではありません。既存の箱所有、Fixture、資金、時計を変更しません。最終製品のネイティブiOS/Android要件は保持します。

## 実装

| 対象 | 内容 |
| --- | --- |
| `mobile/iphone-local/shell.html` | タップで起動、縦向き案内、標準 `Engine.getMissingFeatures({threads:false})`。埋込WASMをメモリ上Responseへ、PCKを `preloadFile` へ供給 |
| 埋込資源 | 小分けbase64復号後にDOMの大きなpayload要素を除去。WASM供給後に参照を解放。外部ファイルfetchなし |
| WebView | CSP `connect-src 'none'`、外部script/worker、ServiceWorker、IDBFS永続保存なし。無音の試作としてDummy Audioを選択 |
| `Open-Warehouse.js` | 明示的に選んだHTMLをScriptable本体で開く。第三者アプリ本体の動作・通信まで検証したとは主張しない |
| `iphone_local_review` | このexportだけ、ブラウザの推定DPIではなくCanvasの実CSS高さをUI倍率へ反映。1280×720からの縮小時もボタン48CSSpxを目標にする |
| DOM入力取消 | Godot4.6.3はDOMtouchcancelを通常Upへ変換するため、capture phaseで取消epochを記録。GodotがUpを扱う前にRouterとHUDの捕捉を破棄する |
| `viewport-fit=contain` | WebViewの安全な表示領域内での配置を要求。ノッチ付き実機での受入は未実施 |
| `tools/package_iphone_local.py` | 正規Web export、base64復号SHA、元資源ハッシュ、ZIP全entry SHA/CRCを検査。公開・アップロード機能なし |

既定30fps、段階的な3D解像度調整、背景時の描画・計測停止、共有材質・World再利用は既存実装を保持します。起動ボタン前はGodotを初期化しません。これらが実際のiPhoneの発熱を減らしたと測定したものではありません。

## ローカルファイルを選んだ根拠

前回のHTTP LAN試験では、Godotが `Secure Context - Check web server configuration (use HTTPS)` を返しました。今回のWebKit実測ではfile://が通常の `isSecureContext=true` となり、WebGL2とWASMを使用できます。安全なコンテキストを偽装したり、HTTPS要件を削除したりしていません。

通常のWeb exportをfile://でそのまま開くと、別ファイルの読込みに失敗します。そのため必要な資源を埋め込み、ファイルfetchに依存しないローダーを実装しました。

APIの参考根拠：

- [Scriptableの型定義](https://github.com/DefinitelyTyped/DefinitelyTyped/blob/master/types/scriptable-ios/index.d.ts)：DocumentPicker.open、WebView.loadFile／present。公式文書の直接取得は環境のCONNECT403で停止。型定義の根拠と実iPhone検証を区別する。
- [Godot4.6.3 Engine API](https://github.com/godotengine/godot/blob/4.6.3-stable/platform/web/js/engine/engine.js)：preloadFile(ArrayBuffer)、init、start。
- [WebKit SecurityOrigin](https://github.com/WebKit/WebKit/blob/main/Source/WebCore/page/SecurityOrigin.cpp)、[local scheme registry](https://github.com/WebKit/WebKit/blob/main/Source/WebCore/platform/LegacySchemeRegistry.cpp)：fileの取扱い。参考ソースの設計とデスクトップWebKitの実測を区別する。

## 検証記録

最終JSON・画像・成果物ハッシュは `docs/implementation/evidence/iphone-local/` へ記録します。以前の3D移行時の76項目は、その時点のnative presentation試験として保持し、今回のiPhone実機試験へ読み替えません。

| 最終試験 | 結果 | 証跡 |
| --- | --- | --- |
| WebKitGTK 2.50.4・実file:// | **9/9 PASS、7画像**。ネットワークなしコンテナ内で実Godot起動、主要5画面と844×390 CSSの表示 | [WebKit report](evidence/iphone-local/webkit/local-game-report.json)、[目視記録](evidence/iphone-local/webkit/visual-review.json) |
| Chromium・実ループバックHTTP | **62/62 PASS、23画像**。実タッチ保持、箱ドラッグ／回転／伝票、プレビュー移動／回転／反転／取消、DOMCancel、2指ピンチとUI競合、画面回転 | [Chromium report](evidence/iphone-local/chromium-http/report.json) |
| Chromium・実file:// | **BLOCKED_ENVIRONMENT**。ERR_BLOCKED_BY_ADMINISTRATOR。管理ポリシー変更なし | [拒否記録](evidence/iphone-local/chromium-file/report.json) |
| Godotスクリプト回帰 | **86/86 PASS**、開始／終了ソースSHA一致 | [headless report](evidence/iphone-local/headless/report.json) |
| 実GLB監査 | **8モデル PASS**、23,812ユニーク三角形 | [資産監査](evidence/iphone-local/asset-audit.json) |
| 包装・再利用のガード | **3/3 PASS**。正しいcohort再利用成功、偽の元ソース記録拒否、変更されたexport bytes拒否。試験後は元に復元 | [guard report](evidence/iphone-local/packaging-guards.json) |
| 最終ZIP | **4エントリーの全SHAとCRC PASS**、埋込WASM/PCKのSHA一致。export前後の元データ90ファイル、再利用元cohortを照合 | [delivery manifest](evidence/iphone-local/delivery.json) |

実際に検証した最終HTMLは54,880,781bytes、SHA256 `c044ee39357dd1b05010f0d7b9a2c2d9992d9e8c52c053b55fd5a63bc2d2170b`。30画像の全SHAも照合しました。ChromiumのHTTP取得は初回のローカル文書1件、外部HTTP／WSは0件です。WebKitはコンテナのネットワークを切っています。

WebKitの初回読込みには長い待ち時間がありました。デスクトップの初期ロード時間からiPhoneの速度を予測していません。短い画面はDPR1のため文字がやや柔らかく見え、iPhoneのDPR3表示は未検証です。

DOMCancelの初回実試験では、保持中のナビがキャンセル後に実行されました。GodotのWeb入力がtouchendとtouchcancelを同じreleaseへ変換することを確認し、今回の取消epochで修正。最終試験では俯瞰を維持し、新しい入力で復帰することを確認しています。ピンチ後のナビ全体のbyte比較は、半透明の隙間に背景が透けるため不適切でした。選択インジケーターの列と実画面を確認する判定へ修正しています。

- デスクトップWebKit：ネットワークを切ったコンテナ内の実file://。標準secure-contextチェック、実Godot起動、倉庫・搬入・検査・出荷・建設の実画面と操作を確認する。WebDriverの画像APIがGL面を取得しないためX11実画面も撮影する。
- Chromium：環境の管理ポリシーがfile URLを `ERR_BLOCKED_BY_ADMINISTRATOR` で拒否。ポリシーは変更しない。同一HTMLを実127.0.0.1 HTTPで開く入力試験は、file起動試験とは分ける。
- ZIP：エンジンWASM/PCKと埋込バイトの全SHA256、ZIP CRCと全entry SHAを確認する。
- 元のGodotスクリプト：headless suiteと実GLB監査で回帰を確認する。

## 未検証事項

**iPhone 13 Pro Max実機、Scriptable本体での起動、実iOS版、OS中断復帰・ノッチ、GPUメモリ、安定30fps、30分以上の熱・電池はNOT_RUNです。** この確認版にOS熱センサーはありません。デスクトップWebKitの速度をiPhone速度と呼びません。

正規エンジンはSafari15.2未満を拒否します。現行Scriptableアプリの最低iOSはApp Storeで別途確認する必要があります。大容量ファイルの初回読込みと端末ごとのメモリ余裕も実機での確認が残ります。

起動・操作は [iPhoneの手順](../play/PLAY_ON_IPHONE.md)。公開Webサイトや公開Releaseは作成していません。
