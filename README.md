# Warehouse Simulator

**iOS / Android向け3D物流倉庫経営シミュレーションゲーム**の開発リポジトリです。

## 確定している方針
- 最終ターゲットはスマートフォン（ブラウザゲームを製品版にはしない）
- 一人称視点で作業し、俯瞰視点で設備を配置・管理する
- 時刻に合わせてトラックが倉庫へ到着・出発する
- 物流ロジックと3D描画を分離する
- 発熱を抑え、滑らかな動作と高い画質の両立を目指す

## Phase 1（初期コード作成済み・未検証）
Unity 6 LTS / Universal Render Pipelineを候補とし、次の試作コードを追加しています。

- `Assets/Scripts/Phase1/WarehouseBootstrap.cs` — 初期シーンを実行時生成
- `Assets/Scripts/Phase1/WarehouseWorld.cs` — 倉庫・荷物・コンベア・屋外・トラックの仮モデル
- `Assets/Scripts/Phase1/MobilePlayerController.cs` — 一人称・俯瞰・移動
- `Assets/Scripts/Phase1/MobileHud.cs` — スマホの仮想スティック・スワイプ・視点切替
- `Assets/Scripts/Phase1/AdaptiveMobileQuality.cs` — 30fps基準・簡易描画負荷制御

詳細な手順と未検証項目は [docs/PHASE1_MOBILE.md](docs/PHASE1_MOBILE.md) を参照してください。

> 現時点ではUnity Editorによるコンパイル、iOS/Android実機確認、熱測定は未実施です。仮の3D形状は最終グラフィックではありません。

## 今後
1. Phase 1のコンパイル・操作・性能検証
2. 荷物検査と仕分けの論理設計・実装
3. コンベアと設備配置の自動化
4. トラックの時間運行・経営システム
