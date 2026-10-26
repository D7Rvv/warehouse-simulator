# スマホ版 高品質3D・性能・発熱設計
文書ID: SPEC-10 / 版: 0.4 / 対応: OWN-006..007 / Unity 6 LTS/URP候補 / 達成値は未計測

## 1. 最重要な受け入れ基準と限界
- 目指す体験: PC版Parcel Simulatorに匹敵するリアルな倉庫内材質、陰影、機械の動き、識別できる伝票と高密度の物流。
- 製品はiOS/Androidネイティブ。PCブラウザの性能で代用して「スマホで60FPS」を宣言しない。
- 基本プレイは安定30FPS。対応高性能端末に限り60FPSモード。実機FPS、画質、電池消費、thermal throttling（熱によるクロック低下）の両立を検証する。
- 33.333ms/Frame(30FPS), 16.667ms/Frame(60FPS) は**画面予算の上限**でありCPU/GPUそれぞれが丸々使える保証ではない。
- 30分以上の連続プレイに加え1時間試験を予定。画面切替/搬送最大数/出荷/建設/夜景/屋外トラックの負荷を別シナリオで確認。
- 「絶対に熱くならない」は不可。OSが温度を強制調整する前に描画・シミュレーション・アニメを抑え、ゲーム性に関係する処理は落とさない。

## 2. 固定ステーションカメラの最適化
- Home倉庫俯瞰: 倉庫全体の低/中LOD、遠方ライト省略、固定背景。
- Inspection（近接）: 箱、伝票、計量機など画面中央の少数オブジェクトに品質優先。無関係なコンベア・屋外は適切にカリング。
- INBOUND: トラック荷台と倉庫入口、見える荷物だけ高精度。背後の搬送グラフはロジック処理だけ。
- EXPORT: 出荷ベルト・フェンス・電光板・少数の箱を高品質。遠い設備はカリング。
- YARD: 視界に入る車・建物・道路の品質を優先。倉庫内部は必要なら低LOD/遮蔽。
- BUILD: グリッドと入力矢印、ゴースト表示に描画を優先。倉庫全体のポストエフェクトや装飾は軽量化。
- 各拠点の表示構成はVisibilitySet(viewId)-> meshCategories/LOD/lightingProfileで管理。非表示区画の3Dモデルは再生成せず表示を切る/プールを利用。
- 視点の瞬時切替でロードが必要な場合、メニュー応答は維持しながら軽量なアセット待機表示。ゲーム論理は破棄・リセットしない。

## 3. URP 描画予算設計
- 最初はForward系URPを評価。モバイル帯域と機能に合わせてForward+/Deferredの是非は実機測定。[P]
- 機材・床・壁にはオリジナルPBRマテリアル。ベースカラー、Roughness/Metallic、Normal/AOを丁寧に調整し、多すぎるマテリアル分裂を回避。
- 静的環境はベイクライトとLightProbe、必要な少数の動的車両/箱だけ陰影を追加。
- 高価なリアルタイム反射/SSR/多灯シャドウ/透明重ね描画は採用前に費用対効果を比較。
- 繰り返し部材（コンベア脚、段ボール、ラック）は共有メッシュ/共有マテリアル/アトラスを使用、SRP BatcherまたはGPU Instancingのどちらが効くかをProfile計測で判断。
- UI伝票の読みやすさは紙ラベル3Dを巨大テクスチャ化するのでなく、ユーザータップ後の高解像2D表示で保証。3Dにも適切な絵柄/材質を持たせる。
- 画質プリセットは HIGH/BALANCED/BATTERY_SAVERで定義。解像度/影距離/テクスチャLOD/アンチエイリアス/車両表示距離等を個別制御する。
- 30FPSでも画面遷移と箱の補間アニメは安定した時間軸で滑らかに見えること。アニメの中断時に整合性が壊れない。

## 4. 動的解像度・OS差の注意
- UnityのDynamic Resolutionは対象グラフィクスAPI/カメラ設定に依存。[公式] iOS Metal と Android Vulkanで対応（Unity 6系公式資料）。
- Camera.allowDynamicResolutionが有効で、対象RenderTextureがスケール対応していることを検証する。
- ScalableBufferManager.ResizeBuffersを呼ぶだけでは、未対応URP設定/グラフィクスAPIで実際にスケールできた保証にならない。FrameDebugger/実機GPU計測/実効解像度で確認する。
- 主要UIや文字の可読性を過度に犠牲にしない。3Dレンダーを縮小しても操作ラベルは鮮明なCanvas/overlay表示にする。
- 自動品質低下はP95 frameTime、GPU-bound/CPU-bound判別と端末熱状態で調整。1秒ごとに上下してちらつく制御を避け、ヒステリシスと数十秒の継続条件を持つ。
- 限界以下の端末を無理にサポートすると画質を維持できない。最低対応機種/OSを実機計測後に決める。

## 5. 熱状態の取得とアクション
iOS: ProcessInfo.thermalStateおよびthermalStateDidChangeNotification（ネイティブブリッジ利用を検証）。
Android: PowerManager thermal APIs/ADPF（対応端末でgetThermalHeadroom等が利用可能）。一部端末が未対応・空値である場合に備えてフォールバック。
**温度値や熱状態は機種差があり、FPS低下をそのまま温度とみなさない**。
応答プロファイル（[P]、機種別検証で調整）:
NORMAL: BALANCED、30fps（高級機はオプションHIGH/60）。
WARM: 影距離/LOD/解像度/パーティクルを軽くする。
HOT: 30FPS厳守、レンダースケール低下、背景装飾削減、画面更新頻度の低い看板などを間引く。**ロジックの結果は維持**。
CRITICAL: 端末OSが危険域を通知した場合、安定保存を優先して一時停止/安全な状態へ戻し、熱が下がるまで重い3Dを抑える。OSの停止を妨げない。
- 長時間試験時の環境記録: 機種、OS、SOC、解像度、充電状態、ケース、室温、fps/p95/p99、CPU/GPU、Battery状態、thermal status、プレイ内容。
- 連続プレイでのフレームレート低下と操作遅延/画質劣化のバランスを人が評価する。見た目が劣化しすぎる場合は支持端末を見直す。

## 6. 物流シミュレーションのCPU費用
- 定額tickシミュレーション。すべてのパーツにMonoBehaviour.Updateをつけず、アクティブ集合のコンベア・予約イベントと固定ステップでまとめる。
- 全荷物にRigidbody/Colliderを常時有効化しない。手元の箱を見せる近距離アニメ以外はネットワークパス/進行率で論理移動。
- PackageStateの生成・破棄や大量のGCを避け、構造体/配列/プール/バッファを事前確保。ECS/Jobs/Burst採用は本当にCPU計測で必要になった場合に評価し、初期から過度な複雑化をしない。
- スケジューラで次のイベントのある設備だけ起こし、稼働していない遠方装置は計算頻度を適切に減らす。ただし時間と正誤は一致。
- 1000箱の論理状態を保持しても画面に表示する箱は数十個程度から段階計測（数値は計測用[P]）。
- トラック運行はwaypoint/spline上の座標関数、物理演算多数は不可。表示モデルをオブジェクトプール化。
- PackageLabelは同一レイアウト/アトラス/頂点データ差替を検討し、1荷物ごとに高解像テクスチャを生成し続けない。

## 7. 初期性能テスト行列と合格ゲート
| SceneProfile | 論理箱数[P] | 同時描画箱[P] | コンベア機器[P] | トラック |
|---|---:|---:|---:|---|
| TEST_INSPECT | 50 | 1〜6 | 10 | 1 |
| TEST_EXPORT | 200 | 20 | 100 | 2 |
| TEST_BUILD | 500 | 50 | 300 | 3 |
| TEST_MAX_LOAD | 1000 | 100 | 500 | 5 |
※最大仕様の宣言ではなくプロファイル計測用の仮Fixture。
仮の合格基準: NORMAL/BALANCEDの目標端末で平均FPS>=30に加えp95 frameTime<=33.3msを目標、p99/1% lowも記録。1時間連続動作で熱降下による長期FPS減少が許容内か評価。実際の閾値や最低SOCは測定後に承認。p95は統計的に意味がある時間・フレーム数で集計する。
- 起動: 初回ロード時間、継続時の拠点切替時間、クラッシュ。
- GPU: 画面別pass数、overdraw、RenderScale、Triangle/DrawCall、VRAM。
- CPU: logic tickコスト、GC spikes、物理演算、UI、asset load。
- Memory: total resident/peak、ネイティブメモリ、Texture/Audio、Save/Load時の一時増加。
- Device: iPhoneとAndroid複数GPU/SoCの代表端末、最小スペックと中位と高位を含める。

## 8. 視覚品質の数値・制約と測定
- モデル予算（Tris/Drawable count/Materials）は端末帯ごとにベンチマークを通して確定し、根拠なしに「1箱は5000ポリゴンで良い」などを固定しない。
- 画像CAP-01の出荷サインと計算表はUIで読めること、CAP-02の半透明設置ゴーストは動いてもフレーム急落しない、CAP-03のトラック荷台は荷物を順に取っても画面がチラつかないこと。
- 光は色と物質の情報を表現するために使用。過剰な光沢・グローや白飛びで元作品の工業的実在感を失わない。
- 固定カメラごとにスクリーンショット基準を管理し、画質比較は他作品の原画流用ではなく独自素材の制作過程と視覚水準に基づく。

## 9. 公式技術資料
- Unity URP性能設定: https://docs.unity3d.com/ja/6000.0/Manual/urp/configure-for-better-performance.html
- Unity URPプロファイル: https://docs.unity3d.com/ja/6000.0/Manual/urp/understand-performance.html
- Unity動的解像度概要: https://docs.unity3d.com/ja/current/Manual/DynamicResolution-introduction.html
- Apple熱通知: https://developer.apple.com/documentation/xcode/responding-to-power-notifications
- Android Thermal API: https://developer.android.com/games/optimize/adpf/thermal
- 原作開発者による2026-07-30最適化: https://steamcommunity.com/app/2424010/announcements/
