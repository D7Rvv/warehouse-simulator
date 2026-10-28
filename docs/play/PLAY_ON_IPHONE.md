# iPhoneだけで開く

**Scriptable用のローカル確認版です。PC、Unity、公開サーバー、Apple Developer登録は不要です。**
App Storeから無料の [Scriptable](https://apps.apple.com/jp/app/scriptable/id1405459188) を入れて使います。独立したiOSゲームアプリとしてインストールする版ではありません。

## 最初の起動

1. `WarehouseStation-iPhone-Offline.zip` をiPhoneでダウンロードし、「ファイル」へ保存します。
2. 「ファイル」でZIPをタップして展開します。中の `WarehouseStation-iPhone.html` がゲーム本体です。
3. Scriptable本体を開き、「＋」で新しいスクリプトを作って、次のコードを貼り付けます。

```javascript
const files = await DocumentPicker.open(["public.html"]);
if (files.length) await WebView.loadFile(files[0], undefined, true);
```

4. 保存して実行し、展開した `WarehouseStation-iPhone.html` を選びます。
5. iPhoneを横向きにし、「倉庫を開く」を押します。

次回は同じスクリプトを実行してHTMLを選びます。同梱の `Open-Warehouse.js` でも起動できます。Scriptableのウィジェット・ショートカット拡張ではなく、本体から実行してください。HTMLをSafariや「ファイル」のプレビューで直接開いても動作しません。

## 操作できる範囲

搬入・検査・出荷・倉庫・建設の切替、箱の回転、伝票拡大、倉庫のパン／ズーム、半透明コンベアの移動・90度回転・反転・取消を操作します。右上「画質」から省電力を選べます。

これは **S0/S1の操作試作** です。本物の荷物移送、検査合否、購入、確定設置、出荷精算は未実装です。無音で、永続セーブもありません。

## 対応と検証の範囲

- ゲームエンジンはSafari15.2未満を拒否します。Scriptableの現行対応iOSもApp Storeで確認してください。
- 初回ダウンロードとScriptable導入後、ゲーム本体は端末内のファイルから実行します。サーバー起動や証明書の信頼設定は不要です。
- デスクトップWebKitの実file://起動と、Chromiumの実ループバックHTTPで表示・入力を検証します。iPhone実機の動作や性能を代用したとは扱いません。
- iPhone 13 Pro Maxの実OS版、実機の互換性・30fps・30分以上の発熱は **未検証** です。読込み速度も実機では未測定です。

通常のHTTPによるWi-Fi配信は安全なコンテキストにならず、前回の試験では起動できませんでした。今回はWebKitが正式に安全なコンテキストとして扱うローカルfile://を使用します。HTTPS判定やブラウザ管理ポリシーは変更していません。

生成コマンドは `python3 mobile/tools/package_iphone_local.py`。生成物は `builds/godot-mobile/`、詳しい証跡は [iPhoneローカル版の実装記録](../implementation/IPHONE_LOCAL_REVIEW.md) を参照してください。Webゲームの一般公開は行っていません。
