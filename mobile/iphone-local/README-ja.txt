Warehouse Station — iPhoneだけで開くローカル確認版

必要なもの：iPhone、App Storeの無料アプリ「Scriptable」（Simon B. Støvring）。
PC、Unity、Apple Developer登録、公開URL、証明書の追加は不要です。
Scriptableの現行対応iOSはApp Storeで確認してください。
ゲームエンジンのSafari下限は15.2です。iOSは更新した状態を推奨します。

起動手順
1. iPhoneでZIPをダウンロードし、「ファイル」へ保存します。
2. 「ファイル」でZIPをタップして展開します。中にある
   WarehouseStation-iPhone.html がゲーム本体です。
3. Scriptable本体を開き、「＋」で新しいスクリプトを作成します。
   同梱のOpen-Warehouse.jsの内容、または下のコードを貼って保存します。
4. そのスクリプトを実行し、展開したWarehouseStation-iPhone.htmlを選びます。
5. iPhoneを横向きにし、「倉庫を開く」を押します。

貼り付けるコード：

const files = await DocumentPicker.open(["public.html"]);
if (files.length) {
  const view = new WebView();
  await view.loadFile(files[0]);
  await view.present(true);
}

次回はScriptableの同じスクリプトを実行し、同じHTMLを選びます。
HTMLをSafariや「ファイル」のプレビューで直接開いても実行できません。
Scriptableのウィジェット／ショートカット拡張ではなく、本体から起動してください。

何ができるか
下の「搬入」「検査」「出荷」「倉庫」「建設」で作業場所を切り替えます。
箱の回転と伝票拡大、倉庫のパン／ズーム、半透明ベルトの移動／回転／反転／取消を操作できます。
音はありません。起動後にサーバーへ接続する必要はありません。
搬入→検査合否→出荷→収益の本編は未実装のS0/S1操作試作です。

性能と検証
通常30fps上限で、右上「画質」から省電力を選べます。
iPhone実機の互換性、安定30fps、長時間の熱性能は未検証です。
画像・入力のデスクトップ試験は、iPhone実機の合格とは区別しています。
固まる／落ちる場合はiOS版と画面に出るエラーを確認してください。

配布
ゲームはHTML内に入っています。外部サイトへのアップロードは不要です。
このパッケージはWebゲームを一般公開していません。
Godotとフォントの第三者ライセンスはHTMLの開始画面から確認できます。
