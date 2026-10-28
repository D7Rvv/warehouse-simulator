# iPhone Safari確認版の公開記録

2026-10-28、ユーザーが一般公開を明示的に了承しました。以前の公開拒否後の新しい許可です。GitHubリポジトリとブラウザへ配信するプログラムは閲覧・取得可能であり、訪問者に配信元の書き込み権限を付与しないことを説明しました。

## 配信内容

- URL: https://d7rvv.github.io/warehouse-simulator/
- 元ソース: `8ce838d31bc7fef21c16d524c9bbba8c91e548d7`
- 配信ブランチ: `gh-pages`
- 配信コミット: `9f11d29f234fc478ae70efea6ef2478581ae6616`
- HTML: 54,880,781bytes、SHA256 `c044ee39357dd1b05010f0d7b9a2c2d9992d9e8c52c053b55fd5a63bc2d2170b`
- 配信ファイル: `index.html`、`.nojekyll`、`publication-manifest.json`

検証済みの単一HTMLをそのまま使います。Godot4.6.3 standardのsingle-thread Web版で、Unity、PC、Scriptable、署名済みIPAを必要としません。開始画面のローカル版表記と、失敗時のScriptable案内は同一バイトを維持するため残っています。

対象はS0/S1の読み取り専用操作試作です。搬入・検査・出荷・倉庫・建設切替、箱回転と伝票、倉庫パン／ピンチ、半透明コンベアの移動・回転・反転・取消を確認します。物流移送、検査合否、購入、確定設置、出荷精算、本編の完成を意味しません。

## 配信前の確認

- デスクトップWebKit file:// **9/9 PASS**、Chromiumの実ループバックHTTPタッチ試験 **62/62 PASS**と同一HTML。Godotヘッドレス回帰 **86/86 PASS**。詳細は [既存の最終試験記録](IPHONE_LOCAL_REVIEW.md)。配信方式の変更でゲームプログラムを変更していません。
- HTML・埋込WASM/PCKのSHA照合。PCK全89エントリーを読み、ゲーム資産・スクリプト・設定・ライセンスを確認。署名鍵、環境変数ファイル、Git管理情報、個人ファイルは含まれません。
- 外部script、iframe、送信formなし。CSPで外部通信・workerを遮断。WASM/PCKは文書内から読みます。Godot・第三者通知とフォントOFLを含みます。
- 正規HTTPSのsecure-context判定を維持。single-threadのためCOOP/COEPを必要としません。公開閲覧者が配信元を更新するAPI等はありません。

## 配信と未検証事項

[Pages配信 #3](https://github.com/D7Rvv/warehouse-simulator/actions/runs/40691681522) は **Success**。対象が上記の `9f11d29f234fc478ae70efea6ef2478581ae6616` と一致することを確認しました。build14秒、report5秒、deploy49秒、合計1分12秒。リモート `gh-pages` のコミットと、GitHubの同コミットから取得したHTML全体の54,880,781bytes・SHA256も一致しました。[機械可読記録](evidence/iphone-safari-publication.json)を保存しています。

現在の作業環境は配信先ホストへの直接接続をHTTPプロキシが403で拒否するため、制限を迂回せず、許可済みGitHubのPages実行結果で配信コミットと成功状態を確認しました。これは公開URLへの直接取得試験や、実iPhoneからの表示確認とは別です。

HTMLは約55MBで、初回読込み・base64復号の負担があります。iPhone13ProMax実機のiOS版、Safari起動、ノッチ・回転、GPUメモリ、安定30fps、30分以上の発熱・電池は **NOT_RUN**。公式作品と同等以上の画質達成も未検証です。

既定30fps、段階的な3D解像度調整、背景描画停止、共有材質、World再利用、リアルタイム影なしの性能対策を保持します。OS熱センサーの実測はありません。
