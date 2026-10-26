# 未確定仕様の管理簿・調査計画（実機照合必須）
文書ID: SPEC-14 / 版: 0.4 / 完全再現の阻害要因・優先検証表

## 1. 重要な前提
参考作品の公開情報と3枚のスクリーンショットだけでは、2026年現行版の全操作・経済バランス・設備表・イベント時刻を100%確定できない。
そのため、ゲームを実際にプレイした時の挙動、出典、録画日時、対応するバージョンを記録し、観察できた範囲で初めて数値を確定する。
原作コード・アセットの抽出、無断複製、利用規約を破る逆コンパイルによる調査は行わない。

## 2. 根拠レコード形式
EvidenceRecord {
 evidenceId,
 claimText,
 sourceKind: DEVELOPER_ANNOUNCEMENT/STEAM_PAGE/SCREENSHOT/PLAY_GUIDE/REFERENCE_PLAYTEST/IMPLEMENTATION_MEASURE,
 sourceUrl?,
 publicationDate?, observedDate, observedGameVersion?, language?,
 warehouseProfile?, tier?, licenseIds[], screenshotOrVideoRef?,
 reproductionSteps[], exactObservedValue?, uncertainty, confidence:H/M/L,
 reviewer, linkedRequirementIds[], lastVerifiedDate, supersedesEvidenceId?
}
- H=公式現行版または同じ版で再現性ある動画/実機。
- M=画像で直接確認できるが一般化できない値、または古い版の詳しい攻略。
- L=コミュニティの一例、未照合の推測。
- 価格/体験時間/設備容量には sourceKind+version が必須。ゲーム側のCatalogにEvidenceIdを保持。
- 失敗例: 画像の1/16をすべての出荷口の最大16個と決めつける。プレイ画像1枚だけでは一般化できない。
- 先行の「ParcelSimulator」の名称表記ゆれは『Parcel Simulator』を正式参照名とする。

## 3. 未解決の主要項目（優先度 P0＝基礎に影響）
| GapID | 参照SPEC | 内容 | 優先 | 必要な証拠 |
|---|---|---|---|---|
| GAP-001 | 02/04 | 全ライセンス解放順、価格、検査ルール開始日 | P0 | 現行版の全Licenses画面と購入前後の荷物 |
| GAP-002 | 02/08 | Old倉庫の全Tier、必要条件、開放座標 | P0 | Tier全画面・段階解放の録画 |
| GAP-003 | 06/08 | 便別ゲート容量、満杯条件、出荷ボタン/自動出荷の動き | P0 | 出荷口を0→満杯→提出まで録画 |
| GAP-004 | 06 | 曜日別CargoCalendarの実際の表とランダム性 | P0 | 7日分のInspectionTablet記録 |
| GAP-005 | 04 | 貨物・国旗・重量・番号・内容物・スタンプの全パターンと比率 | P0 | ライセンス段階別、正常/異常の複数例 |
| GAP-006 | 08 | 報酬・ペナルティ・倍率・集計の式 | P0 | 提出前後の現金とゲート詳細の記録 |
| GAP-007 | 05 | 全ベルト/Scanner/Joinerの接続口と向きの仕様 | P0 | 該当機材の全回転/Flip/出口例 |
| GAP-008 | 05/08 | 全設備費用、アップグレード速度、容量・UnlockTier | P0 | Shop機材全画面/強化実測 |
| GAP-009 | 07 | 入荷の時刻、到着間隔、複数ベイ、未荷役箱の扱い | P0 | シャッター前後/終業のトラック録画 |
| GAP-010 | 02/07 | Sleep、翌日、締切、未発送箱、ショップ配送の持ち越し | P0 | 在庫残し・ゲート部分満載のDayClose |
| GAP-011 | 05/06 | ラック/パレット、Loader/Unloaderの全容量と接続条件 | P1 | 収納フル・出入庫と寝る前後 |
| GAP-012 | 06 | Rejected出荷の課金、事前Approved/Deniedの取り扱い | P0 | 既検査箱の正誤別出荷 |
| GAP-013 | 08 | 注文配達時間、返金、ショップ履歴と一括注文 | P1 | 注文→ショップ便→受取→再購入 |
| GAP-014 | 02/06 | Regional倉庫の12地域/カレンダー/出荷扉設定 | P1 | New倉庫の国別カレンダー複数週 |
| GAP-015 | 05/08 | Speedway/Elevator/T-Joiner/AutoPauserの最終仕様 | P1 | 2026版対象Tier、挙動の録画 |
| GAP-016 | 09 | 原作のSave/Load補助機能とセーブスロット | P1 | 2026現行Save画面と復元試験 |
| GAP-017 | 03 | スマホ画面の操作回数・選択誤り率・戻る操作 | P1 | 本作品のタッチUIプロトタイプの実機ユーザーテスト |
| GAP-018 | 10/12 | 30分連続で30fps維持できる最低SOC/画質 | P0 | iPhone/Android実機Profilerと熱計測 |
| GAP-019 | 10/12 | PC原作に匹敵する見た目の基準 | P0 | 同等シーン構成の比較撮影と独自モデル評価 |
| GAP-020 | 12 | 高品質オリジナル3Dアセット・商用使用ライセンス | P0 | AssetManifest/ライセンス証拠 |
| GAP-021 | 06/07 | 便別出荷口と実際の可視トラックとの関連 | P1 | 原作提出後の表示と独自仕様境界 |
| GAP-022 | 02 | ゲーム本編以外の機能（実績、ネコ、カスタマイズ等） | P2 | Tier制覇後にショップ/オプションを完全調査 |

## 4. 現行ゲームで確認する順序
S0 現行バージョン表示を録画、倉庫初期状態と開始日/時刻を記録。
S1 初期1日の入荷、CargoType/CountryFlagの正常/異常、出荷曜日、拒否ゲート、正誤明細を記録。
S2 各検査ライセンスの購入直前/直後にパネルと箱の違いを記録。
S3 最初の倉庫Tier/設備Shopの解放と価格、コンベアゴースト配置/キャンセル/Flipを記録。
S4 各スキャナーの入口、出口、処理秒数、詰まり、最終スタンプ装置を記録。
S5 手動/自動荷下ろし、ゲート満杯/自動出荷、保管ラックによる曜日持ち越しを記録。
S6 実際の出荷ボタンと所持金変化の関連、間違った曜日/出口による誤出荷の費用を記録。
S7 全Tier解放・出荷口・ベイ拡張の地図と価格/条件を記録。
S8 RegionalSortingCentre、国別/エレベーター/週次カレンダー/ポリバッグ/新機材を記録。
S9 保存データの端末違い、辞書/計算の互換性を比較。
S10 本作品の同一シナリオと操作/難度/音響/グラフィックの比較。

## 5. 記録テンプレート
GapID, GameVersion, CaptureTimestamp, Day/Tier/License/Equipment/UpgradeState,
TestAction, Expected, Actual, ScreenshotClipRef,
MeasuredValue and Units, Confidence, ReportedSource, ReviewStatus,
DesignChange?, CatalogChange?, RequiredTestIds.
- 確認者が違っても、同一条件から2回再現できれば「observed=true」。
- 複数の矛盾する資料がある場合は発行日時/ゲームバージョンを調べ、現行確認を優先。旧仕様を自動削除せず履歴管理。
- 人間のレビューなしにLの根拠を最終仕様へ昇格しない。
- 仮の出現確率/金額/曜日表はProvisionalTagで保存、将来の実測変更でCatalogのみ差替可能な設計にする。

## 6. 非推奨の誤った再現作業
- SteamのPC最低スペックを基にスマホで動くと推定する。
- 2025の攻略の価格/レベルを2026の新倉庫へそのまま適用する。
- 画像のUIレイアウトやゲーム内アセットをそのままコピーする。
- 画面を切替えるだけで荷物がいつの間にか次工程へ進んだとみなす。
- 実装したテストを実行していないのにPASSにする。
- 不明なライセンス/Tierコストを想像で埋めて原作と同じと宣言する。
- 自動化のやりがいを削るほど手動操作を極度に簡略化する。

## 7. この文書が埋まるまでの進捗
GAP-001..022は現段階で概ね未解決。SPEC-03..12はCodexが進めるための「厳密な構造設計」であり、**完成した原作の完全仕様辞書ではない**。実機照合後に数値・仕様を変え、QAでreference parityを測定していく。
