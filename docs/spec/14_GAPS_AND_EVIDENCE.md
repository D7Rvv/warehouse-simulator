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


## 重要な調査追加（2026-10-26）
- GAP-054 [P0] **個別手動部分出荷と終業時まとめClear Doorsを分けて調べる**。2026-07-21の新倉庫では15/16の手動Exportが成立した証言と開発者返信がある。一方2026-09-25のClear Doors一括操作は開発中。Regional個別手動Partialは資料上支持、Originalも可能か/固定容量/時刻条件は実機検証。[SPEC-18]
- GAP-055 [P1] LauncherのPower/Angle選択の段階数・弾道・レベルによるベルト速度変化。2025-09-29公式Update1.0.4を参照して実機検証。
- GAP-056 [P0] 2025年のLicense→Warehouse Tier 1/2/4/6解放連鎖が2026年版の最初の倉庫にも適用されるか。ライセンス購入前後のShopとTier表示を記録。
- GAP-057 [P1] 2026-09-25修正にあったCountry=NoneやCriteria=N/Aをどう正誤処理するか。原作現行データでは無条件に不正と扱わないケースがある。https://steamdb.info/patchnotes/25526810/



## 2026-10-26 調査進展と次の実機照合（SPEC-16）
- GAP-PRICE-TIER: 旧倉庫Tier1～14価格と合計はE3aまで確定（700,1000,…4600; 合計37100）。現行版に変わっていないか最終E0実機照合を残す。
- GAP-LIC-PRICE: 重量/番号/内容物/スタンプのLicense価格・報酬加算はE3bまで具体化（300/300/500/500、+10/+15/+10/+10）。
- GAP-TYPE-PRICE: Small/Envelope, Insulated, Hardcaseの購入・報酬値もE3bまで具体化。
- GAP-EXPORT-CALENDAR: Road毎日、Rail土日不可のガイド根拠は見つかった。Air/Maritimeの正確な曜日表はUのまま。
- GAP-EXPORT-BATCH: C2混載の翌日誤提出事例により「ゲートへ箱を積んだ日ではなく**提出日**に評価」の仮説が強まった。現行原作E0検証が必要。
- GAP-PAYOUT-DETAILS: IMG_0181で集計16/+$1008/-$0/-$5=+$1003の式が可視。実装用計算項目が分かるが、ベース単価・全バッチ倍率・誤配係数-10・丸め順はU。
- GAP-HANDLING-FEE: この画像のSubmissionHandlingCharge-$5が全ゲート固定か不明。
- GAP-CONTRACTS: **新規P0** 通常出荷とは別のContract Van / 複数箱納品 / 個数・国・判定・輸送区分の条件・報酬・解禁Tierを現行版で確認。
- GAP-BILLS: **新規P0** 数日毎のBillが原作に存在するとの2025プレイヤー証言。電気料金として計算されるのか、何日ごとで何を消費するか実機で確認。
- GAP-DELIVERY-SIZE: 初段3→5、次段数未確認（攻略では最大7）を実機で確認。
- GAP-NEW-WAREHOUSE: 新倉庫の購入額・Tier上限・出荷曜日の変化を実機で確認。
- 旧「電気代/契約は存在根拠がない」表現は今回の資料発見で訂正。



## 2026-10-26 総合監査後の未確認事項（仕様新設を防ぐ）
- GAP-058 [P0] 旧倉庫と地域倉庫それぞれで**手動部分提出が何箱から何時まで可能か**、AutoWhenFull/BulkClearとどう違うか。2026地域倉庫15/16の手動成功の報告あり（SPEC-18）。
- GAP-059 [P0] 地域倉庫の拒否品にもCountryCalendarによる出荷可否が必要なのか/初期バグなのか。2026年7月開発者返信の画面で検証。
- GAP-060 [P0] License 4種の値と報酬加算を2026のショップ/出荷明細で1種類ずつ確認。韓国語コミュニティWiki値は今回元ページ取得不安定につき「弱い参考情報」。
- GAP-061 [P0] Air/Maritimeのすべての曜日ビットマスク。SteamにあるHardmode日程案を実際のカレンダーとして転記しない。
- GAP-062 [P0] $1008正送16個と取扱手数料$5が、正確にどの原価/各種Bonus/倍率を使うか。平均$63を単価と誤認しない。
- GAP-063 [P1] Regional $500k解放後のTier15/16/17/18それぞれの必要Express件数/価格/12ゲート解放順。
- GAP-064 [P1] 2026秋に追加されたT-Joiner・RackSignの正確なShop価格・独自の設定値・接続方向。
- GAP-065 [P1] 契約用Vanの報酬、受注条件、通常出荷との比較、Bill上限$500と発行間隔。プレイヤー証言のみで単価式を確定しない。
- GAP-066 [P1] 既貼付シールに意図的な誤りが存在するか、それとも2025時点のScanner/建設途中の不具合か。複数のSteam証言が相反する。
- GAP-067 [P2] Android/iPhoneの最低対応機種と熱・FPS・メモリ上限、正式なリリース品質受入。

優先調査資料はSPEC-17BとSPEC-18、数値参考JSONは`docs/data/reference_catalog_2026-10-26.json`。元の参照ゲーム実機がなくてもCodexの開発を止める必要はないが、未解決の真値を勝手に[確定]にしない。
