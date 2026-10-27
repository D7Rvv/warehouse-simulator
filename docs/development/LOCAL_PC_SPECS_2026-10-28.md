# 開発PCのハードウェア仕様（ユーザー提供）
記録日: 2026-10-28
対象: Unity 6.3 LTS / Warehouse Simulator のWindows開発PC
記録根拠: ユーザーが2026-10-28にチャットへ送付したWindowsシステム情報のスクリーンショット
このファイルは開発・最適化・ハードウェア導入判断の参照用。個人情報（Windowsユーザー名や端末固有ID）は記載しない。

| 項目 | 確認値 |
|---|---|
| CPU | Intel(R) Core(TM) i7-7700 CPU @ 3.60GHz |
| メモリ | 16.0 GB、表示速度 2400 MHz |
| グラフィックス | Intel(R) HD Graphics 630（CPU内蔵GPU） |
| グラフィックメモリ表示 | 128 MB（Windows画像で表示された値。共有メモリを含むGPU実効使用可能容量を示す値ではない） |
| ストレージ | 477 GB 容量、使用済 304 GB（画面表示時点） |
| ストレージ空き容量 | 約173 GB（477−304、概算。ストレージ種類がSSD/HDDかは未確認） |
| OS | Windows（細かいバージョンは未確認） |
| Apple実機 | iPhone 13 Pro Max（iOSバージョンは未確認） |

## 開発方針への影響
- Unityの3D EditorやGame Viewは**PC上のIntel HD Graphics 630**で描画する。Codexのクラウド推論を使ってもUnity EditorのGPU負荷はクラウドに自動転送されない。
- CPU i7-7700 / RAM16GBは軽量なUnity URP試作とコード編集の開始は可能。ただし大型3D倉庫、重いシャドウ、リアルタイム反射、フル解像度の多数のPBRマテリアル、Unity+Blender+ブラウザの同時使用などではPC側が重くなる可能性が高い。
- **最大の懸念は内蔵Intel HD Graphics 630**。高品質なシーン制作の設計・コードを妨げるわけではないが、PC内Editorでの高品質シーンのリアルタイム描画・プレビューには制限がある。PCで遅いことは、iPhone 13 Pro Maxで遅いことの直接の証明ではない（逆も同じ）。
- 追加費用ゼロで進める: シンプルなScene Blockout, Unlit/軽量URP View, 品質プリセット(EDITOR_LOW/MOBILE_TARGET), 遠景LOD, 静的ベイク光源, 必要な時だけ描画, 小さいテクスチャ, 実体Rigidbody最小, ロジックと描画の分離。
- iPhoneのFPS・持続負荷・発熱はネイティブビルドの実機計測によって判定し、Unity Remote 5やWindowsのEditor FPSを代用しない。
- ストレージは現時点で約173GB空き。Unity追加Editor、Libraryフォルダ、Android SDK/NDK、キャッシュの増加に注意。LibraryやTempは**バックアップ/バージョン管理しない**（.gitignoreで除外）が、既存ProjectのAssets/Packages/ProjectSettingsを削除しない。
- まだ確認していない項目: ディスクSSD/HDD、GPUドライバー、モニタ解像度、Windowsの詳細ビルド、PCの発熱・電源構成、DirectX互換、ゲーム実機のフレーム計測。
- **PCの性能が十分だと断言しない**。S0/S1で初回Editor起動とScene表示・Consoleエラー有無を確認してから調整する。

## 連携メモ
- 設計書の主要性能基準: docs/spec/10_MOBILE_VISUAL_PERF.md
- 新しい作業Scene: Assets/Scenes/WarehouseStation.unity（feature/issue-1-s0-s1 branch）
- Unity Project: C:\UnityProjects\warehouse-simulator
- 元の空Project（誤って開かない）: C:\UnityProjects\WarehouseSimulator
- GitHubリポジトリ: https://github.com/D7Rvv/warehouse-simulator

## 記録・保持について
GitHubにcommitして保持する技術ノート。ChatGPTメモリ機能に保存したものではない。ファイルが削除されるかリポジトリ権限が失われればアクセスできなくなるため、GitHubやローカルクローンで管理する。変更したハードウェア情報は新しい計測を伴って更新する。
