# Warehouse Simulator — 実装用設計書 目次・仕様統制
文書ID: SPEC-00 / 版: 0.4 / 設計基準: 2026-10-26 / ステータス: **設計レビュー中（未確認事項の実機照合前）**

## 目的
スマートフォン（iOS/Android）向け高品質3D物流倉庫ゲームを、Codexが誤読せず段階的に開発する。参考作品はDansan Digital『Parcel Simulator』。自由歩行は廃止し、ステーションをボタンやタップで切替。検査・出荷・設備の手作業から自動化へ進む遊びを保つ。トラックの道路進入→接車→積卸→出発は独自拡張。

**「完全再現」＝原作品のゲームルール・主要機能・進行・手触りの十分な機能的同等性を目標にすること。原作固有の3D資産・UI画面・ロゴ・音・ソースコードを複製しない。** 見た目は同格以上の品質を目標とし、独自アートを制作する。

## ファイル一覧と読む順番
| 文書 | 実装の責務 |
|---|---|
| 00_MASTER_INDEX.md | 仕様の優先順位・変更管理・完了基準 |
| 01_REFERENCE_PARITY.md | 原作品の機能一覧、根拠、未確認の境界 |
| 02_GAMEFLOW_PROGRESSION.md | ゲームループ・ライセンス・Tier・ショップ |
| 03_UI_CAMERA_INPUT.md | キャラ移動を無くしたスマホ入力・メニュー・各画面 |
| 04_PARCEL_INSPECTION.md | 荷物生成・6検査・承認/拒否の完全状態遷移 |
| 05_BUILD_CONVEYORS.md | グリッド設置・ゴースト・コンベア網・機器定義 |
| 06_STORAGE_EXPORT.md | ラック/パレット・輸送区分・曜日・出荷ゲート |
| 07_TRUCKS_CLOCK.md | 搬入トラックと運行時刻・見た目の同期 |
| 08_ECONOMY_CATALOG.md | 報酬・購入・Tier・アップグレード・数値出典 |
| 09_ARCHITECTURE_SAVE.md | モジュール分割・データ定義・トランザクション・保存 |
| 10_MOBILE_VISUAL_PERF.md | モバイルネイティブ・見た目・発熱・計測 |
| 11_QA_ACCEPTANCE.md | 機能/境界/性能/セーブの検証マトリクス |
| 12_ASSET_ART_DIRECTION.md | オリジナル3DアートとUIの具体的品質仕様 |
| 13_CODEX_EXECUTION.md | Codexの開発単位、実装順、PR・レポート定型 |
| 14_GAPS_AND_EVIDENCE.md | 再現を妨げる未確認条件・調査記録のフォーマット |
| 15_DOMAIN_CONTRACTS_AND_FIXTURES.md | ID/状態/コマンド/セーブ仕様・再現テスト用データ |
| 16_2025_REFERENCE_NUMBERS.md | 出典付きの旧版価格・容量・処理速度（現行未検証） |

**読む順番:** 00 → 01/14/16（原作情報と未確定値の把握）→ 02〜10/12/15（個別実装）→ 11（QA）→ 13（Codex実行）。

参考資料:
- docs/PARCEL_SIMULATOR_RESEARCH_2026-10-26.md（旧調査）
- docs/GDD_V0_3_REFERENCE_PARITY_MOBILE.md（全体の方向性）
- docs/THREE_SCREENSHOTS_ACCEPTANCE_SPEC.md（画像3枚の視覚受入）
- このフォルダの仕様が旧GDDと矛盾するときは**このフォルダの更新日/決定状態を確認**。未確定を新しい推測で勝手に埋めない。

## 仕様の状態タグ（すべてのCodex実装指示に適用）
- **[R] 参考作品の確認事項**：公式Steam、開発者発表、提供画像、明記した攻略/プレイ資料に存在が裏付けられる。各項目に出典IDと資料日時を付す。
- **[D] このプロジェクトで明示確定**：スマホネイティブ、キャラ自由歩行なし、メニュー/設備タップで即座に作業場所切替、物流処理の一貫性。
- **[P] 実装用の暫定規則**：原作の未確認箇所を埋めるテスト用初期値。ScriptableObject/JSON等のデータで交換可能にする。プレイヤーへ「原作と同一」と宣伝しない。
- **[U] 未調査・判断待ち**：コードに固定しない。必要なら依存先を注入して仮値にし、QAに不足を残す。
- **[X] 禁止**：原作アートの無断コピー、UI画像の直接貼付、自由歩行を復活、報酬の二重計上、GPU性能の未検証保証。

根拠優先順位: ①原作現行版の検証付き実機計測 ②公式の現行パッチ/マニュアル ③参考画像（可視範囲のみ）④プレイガイド/コミュニティ（発行時点明記）⑤仮説。[R]でも**時点が古ければ現行一致は保証しない**。

## 要求ID、実装、試験の追跡
一つの項目につき `REQ-ID -> SPEC-ID -> CodePath/Commit -> TEST-ID -> TestedDevice/Build -> EvidenceLink` を保存。どれかが欠けた項目は「完了」とみなさない。
項目の受入状態: NOT_STARTED / IMPLEMENTED_UNTESTED / UNIT_VERIFIED / DEVICE_VERIFIED / REFERENCE_MATCHED / BLOCKED.
すべての新仕様は変更理由・依存影響・回帰試験・互換マイグレーションを記録する。

## 不変条件（システム共通の禁止事項）
1. PackageIDは一意、かつある瞬間の所有場所は一つだけ。
2. 物品の物理的な所属変更は必ず一つのコマンドで検証→確定し、描画の有無に左右されない。
3. Inspectorの判定結果、貼付ステッカー、荷物の位置、出口への提出、運行、売上は**別々の事実**。
4. 各トラック運行、荷物搬送、出荷、収益のイベントはidempotency key（一意キー）で重複実行を拒否。
5. カメラ切替はゲームの時刻/物流をリセットしない。
6. 配置プレビュー中は費用・グリッド・搬送グラフを一切変更しない。
7. 低FPSでシミュレーションだけ飛ばしたり、画面外の荷物を消さない。
8. 税/給料/電気料金など参考作品に無いか未確認の機能を再現要件に勝手に加えない。

## 参照ゲームの正式な検証範囲
Old Warehouse: 貨物区分（ROAD/RAIL/AIR/MARITIME/REJECTED）、検査・ライセンス・倉庫拡張・機械自動化。
New Regional Sorting Centre (2026-07): Expressの国別ルーティング、毎週変更される国カレンダー、プログラム可能な出荷扉、2階とElevator、Pallet Build Mode、Poly Bag等。
この2種は**同じ倉庫に全部の仕組みを重ねない**。WarehouseProfileで切替える。

## 機能の段階的な完成基準
- G0 設計資料・参照比較、未確認条件一覧が存在。
- G1 一連のカメラ・タップ入力、3D倉庫、設備配置ゴーストが**Unityで実行できる**。
- G2 トラックから一個を手動搬入→手動検査→ステッカー→正しい出荷口→報酬が一度だけ計上。
- G3 検査項目拡張、正誤/期限/曜日/保管が成立、出荷・収益までテスト。
- G4 コンベア全種類、仕分け、ラック接続、スキャナーが論理的に連携。
- G5 ライセンス/Tier/ショップ/設備注文/アップグレードが原作比較を通過。
- G6 時計に連動したトラック移動/荷役/出発＋倉庫2の国別仕分け。
- G7 iOS/Androidの代表端末で30分以上測定、画質比較、Save/Load、事故系テストを通過。

**現在の状態: 仕様書00〜16の設計草案を作成。Unity実行・熱計測・原作全機能の現行照合は未完了。** Codexが開始前に「全部出来た」と言わないこと。

## 仕様変更の手順
1. 根拠と対象REQ/IDを特定。
2. 旧仕様と新仕様を明文化。
3. 影響（UI→モデル→保存→QA→パフォーマンス）を点検。
4. ゲームループを壊していないか、原作との差分を表に記載。
5. Codex作業ブランチ、テスト、PR/コミット、レビューを記録。

## 参考URL
SRC-STEAM https://store.steampowered.com/app/2424010/Parcel_Simulator/
SRC-NEWS https://steamcommunity.com/app/2424010/announcements/
SRC-MANUAL https://shippobox.com/archives/16245
SRC-AUTO https://shippobox.com/archives/16262
SRC-STATS https://steamcommunity.com/sharedfiles/filedetails/?id=3509976176
SRC-PATCH https://steamdb.info/patchnotes/25526810/
