# Warehouse Simulator

高品質な iOS / Android 向け3D物流倉庫経営シミュレーションゲームの開発リポジトリ。

## 最新の設計（2026年10月26日）
- **[Codex向け詳細設計書目次 Ver.0.4](docs/spec/00_MASTER_INDEX.md)** — 全18文書（00〜17）の正本
- [原作の機能比較・確定/未確認](docs/spec/01_REFERENCE_PARITY.md)
- [画面操作](docs/spec/03_UI_CAMERA_INPUT.md) / [荷物検査](docs/spec/04_PARCEL_INSPECTION.md) / [コンベア](docs/spec/05_BUILD_CONVEYORS.md) / [出荷](docs/spec/06_STORAGE_EXPORT.md)
- [トラック・時計](docs/spec/07_TRUCKS_CLOCK.md) / [経営](docs/spec/08_ECONOMY_CATALOG.md) / [データ・セーブ](docs/spec/09_ARCHITECTURE_SAVE.md)
- [モバイル性能](docs/spec/10_MOBILE_VISUAL_PERF.md) / [QA](docs/spec/11_QA_ACCEPTANCE.md) / [アート](docs/spec/12_ASSET_ART_DIRECTION.md)
- [Codex実装計画](docs/spec/13_CODEX_EXECUTION.md) / [未確定仕様](docs/spec/14_GAPS_AND_EVIDENCE.md) / [データ契約](docs/spec/15_DOMAIN_CONTRACTS_AND_FIXTURES.md)
- [2025年価格・容量の参考表（現行未検証）](docs/spec/16_2025_REFERENCE_NUMBERS.md) / [設計横断レビューと時間軸の明確化](docs/spec/17_CROSS_SPEC_REVIEW.md)
- [旧Ver.0.3全体仕様](docs/GDD_V0_3_REFERENCE_PARITY_MOBILE.md) / [旧参考調査](docs/PARCEL_SIMULATOR_RESEARCH_2026-10-26.md)

## ゲームの核
- 搬入トラックから最初は手動で荷物を取り出す。設備投資によりコンベアで自動化する。
- ラベル・重量・シリアル・内容物・スタンプなどの検査、承認・拒否、種類別/曜日別の出荷、保管、ショップ、ライセンスと倉庫Tierの成長を実装する。
- 半透明のゴーストプレビューでコンベアの位置・回転・反転を決め、ラインを自由に構築する。
- ゲーム内時刻に合わせてトラックが道路から来て接車・荷役・出発する独自演出を加える。
- 操作キャラの自由歩行は廃止。ボタン/メニュー/3D設備タップで作業カメラを瞬時に切り替える。
- iOS/Android向けに描画・シミュレーション分離、安定FPS、発熱・電池を検証しつつ、工業施設の質感を高品質に制作する。

## 現在の状況
- [x] GitHub初期化
- [x] 操作方式の再設計
- [x] 画像と公開資料の詳細調査書
- [x] 設計書 Ver.0.4（18文書）のCodex向け草案を作成
- [ ] 設計書レビュー・未確定仕様の原作現行版プレイ/全Tier照合
- [ ] 新しいカメラ/タップ/3D配置の実装
- [ ] 1荷物の手動搬入〜検査〜出荷〜収益までの完成
- [ ] 自動化/トラック運行/多階層倉庫
- [ ] Unity Editorコンパイル/端末実機/発熱検証

### 旧コードに関する注意
Assets/Scripts/Phase1 配下は **古い自由歩行版の試作コード** であり、現行仕様に非対応です。最新設計の完成品ではありません。旧コードを拡張する前に仕様に合わせた構成へ修正してください。

公開情報の検証と実機での同一挙動検証は別管理。原作品のモデル/画面/音声の無断コピーは行わず、同水準の品質を目指す独自アセットを制作します。
