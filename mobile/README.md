# Godot Station Edition

このフォルダの `project.godot` を **Godot4.6.3標準版** で開いて実行します。Unity、Unityライセンス、.NET、Blenderは実行に不要です。横向きの固定ステーション操作で、自由歩行はありません。

iPhoneだけで使うローカル確認版は `python3 tools/package_iphone_local.py` で生成します。Scriptable本体のWebViewで単一HTMLを開く方式です。起動手順は `iphone-local/README-ja.txt`。PC・公開サーバー・独立iOSアプリの署名を必要としません。iPhone実機の動作・発熱は未検証です。

搬入・検査・出荷・倉庫・建設をタップで切替。検査は箱の回転と伝票拡大、倉庫はパン／ピンチ、建設は表示プレビューの格子移動・回転・反転・取消を操作できます。全て隔離した試作Fixtureで、実際の移送・購入・検査合否・収益は未実装です。

画質メニューの既定はバランス／30fps上限です。省電力で3D解像度を下げ、UIは別の解像度で表示します。60fpsは実機検証なしに有効化しません。iPhone13ProMax・Androidの性能と30分以上の熱試験はNOT_RUNです。

Linuxの検証コマンド：

検証にはPython **3.11以降** とPillow（`python3 -m pip install Pillow`）、描画試験にはDisplayまたはXvfb＋Mesaを用意します。ゲーム自体の実行にはPythonは不要です。

```bash
tools/check.sh
tools/render.sh
python3 tools/package.py
```

WindowsはGodotのプロジェクトマネージャーから `project.godot` をインポートしてF6/F5。検証用のWindows exeを使用する場合はGodot Editorも不要です。Godotの正式なエクスポートテンプレート4.6.3を導入すれば、Linux／WindowsのReviewプリセットから書き出せます。iOS／Androidには別途SDK・署名・実機検証が必要です。

3Dを再生成する場合だけBlender4.3.2を追加で使います。`art/generate_textures.py`、`art/generate_assets.py` の使い方と品質記録、`assets/ART_MANIFEST.json` を参照してください。GodotはGLBを読み、Blenderファイルに依存しません。日本語フォントの再生成は `art/generate_font.py --source /path/to/NotoSansCJK-Regular.ttc` とfontToolsを使います。元フォントのSHA256とOFLライセンスを同梱しています。

原作の画像・モデル・テクスチャは使用していません。公式と同等以上の画質・安定30fps・低発熱を達成したとは判定していません。現在の細部と未検証項目はリポジトリの `docs/implementation/GODOT_VISUAL_CHECKLIST.md` に記録しています。Godot・フォントの第三者ライセンスを `licenses/` と `assets/fonts/LICENSE.txt` に同梱しています。
