# Parcel Simulator — 価格・報酬・出荷規則の深掘り調査
文書ID: SPEC-16 / 調査基準日: 2026-10-26 / 状態: 公開資料照合済み、原作現行版の全数値実機検証は未完了

> **重要な修正**: これまで「価格・収益・出荷ルールは調べられない」と扱っていたものの一部が、公開ガイド・コミュニティWiki・ユーザー提供の精算画面から具体化した。特に**Contracts（契約用バン）と定期的なBill（請求書）が原作に存在する**との具体的プレイヤー証言があり、従前のSPEC-08の「原作未確認なので経営機能に含めない」という説明は再検討が必要。

## 1. 出典と精度コード
| ID | 種類 | URL・日時 | 判定 |
|---|---|---|---|
| SRC-2026-OFFICIAL | 開発者ニュース | https://steamcommunity.com/app/2424010/announcements/ （2026年7月～9月） | 公式の存在/変更点。金額がないものは未確定 |
| SRC-2026-WIKI | 韓国語コミュニティ百科 | https://www.namu.moe/w/Parcel%20Simulator （最終更新2026-10-10） | 具体的価格が豊富。ただしどの値が最新版で測定されたかは不明 |
| SRC-PLANNER | 非公式Warehouse設計ツール | https://parcelplanner.app/ （取得: 2026-10-26） | 旧倉庫Tier/設備価格の比較先 |
| SRC-GUIDE-2025 | Steam「Parcel Simulator Stats and Facts」 | https://steamcommunity.com/sharedfiles/filedetails/?id=3509976176 （2025年6月/7月） | 初期版価格/配置/レベルの歴史資料 |
| SRC-INS-2025 | ShippoBox手動検査 | https://shippobox.com/archives/16245 （2025年） | 原作初期の検査ルール |
| SRC-AUTO-2025 | ShippoBox自動化 | https://shippobox.com/archives/16262 （2025年） | コンベア/Scanner速度 |
| SRC-THROUGHPUT | Steam実測「Level 3 throughputs」 | https://steamcommunity.com/sharedfiles/filedetails/?id=3563578111 （2025年9月） | 実測、後のパッチで変更の可能性 |
| SRC-CONVEYOR-2026 | Steam攻略「Conveyors for dummies」 | https://steamcommunity.com/sharedfiles/filedetails/?id=3664942587 （2026年2月、2026年7月更新を含む） | 原作旧倉庫のゲート運用例、標本値 |
| SRC-CONTRACTS | Steam契約実行Q&A | https://steamcommunity.com/app/2424010/discussions/0/591777299172521111/ （2025年7月） | 専用バンの操作に関する複数プレイヤー証言 |
| SRC-CONTRACT-CRITERIA | Steam契約対象 | https://steamcommunity.com/app/2424010/discussions/0/591775773438869828/ （2025年7月） | 契約の荷物数/国/合否/輸送区分 |
| SRC-UTILITY | Steam数日ごとの請求書報告 | https://steamcommunity.com/app/2424010/discussions/0/601917139265464619/ （2025年10月） | Billの存在の具体的なプレイヤー証言。金額の一般化禁止 |
| SRC-USER-SCREEN | ユーザー提供IMG_0181.jpeg | 2026-10-26会話内、出荷ゲート「VALID SUBMISSIONS」画面 | スクリーンショットの可視範囲のみ確認 |

信頼度:
- **E1**: 公式が直接明記
- **E2**: ユーザー提供画面の表示が読み取れる
- **E3a**: 複数の2025資料・非公式Planner等で一致（同一古資料を転載した可能性あり）
- **E3b**: コミュニティ単独Wiki/攻略/プレイヤー観察
- **U**: 2026-09-29公開ビルドで再検証していない事項

## 2. 旧倉庫Tier 1〜14 価格マスター（E3a、現行照合はU）
| Tier | Price USD | 一段の増分 |
|---|---:|---:|
| 1 | 700 | - |
| 2 | 1000 | +300 |
| 3 | 1300 | +300 |
| 4 | 1600 | +300 |
| 5 | 1900 | +300 |
| 6 | 2200 | +300 |
| 7 | 2500 | +300 |
| 8 | 2800 | +300 |
| 9 | 3100 | +300 |
| 10 | 3400 | +300 |
| 11 | 3700 | +300 |
| 12 | 4000 | +300 |
| 13 | 4300 | +300 |
| 14 | 4600 | +300 |
| **1～14の合計** | **37100** | |

参考資料: SRC-2026-WIKI、SRC-PLANNER、SRC-GUIDE-2025。旧倉庫の初期10×20格子、拡張1回ごとに+10×10格子という2025報告。**倉庫2の費用をこの表で計算しない。**

## 3. ライセンス購入価格・加算報酬（E3b、現行照合はU）
| ID | License | Purchase | PerParcelBonus | 必要機器 | Wiki記載のTier番号 |
|---|---|---:|---:|---|---:|
| LIC-WEIGHT | 重量クラス検査 | 300 | +10 | Industrial Scales | 1 |
| LIC-SERIAL | 番号認証 | 300 | +15 | Serial Verification Keypad | 2 |
| LIC-CONTENTS | 禁止品開封検査 | 500 | +10 | Opening Toolbox | 4 |
| LIC-STAMP | スタンプ真正検査 | 500 | +10 | なし | 6 |

出典: SRC-2026-WIKI（更新2026-10-10）。2025 Steamガイドでは「重量検査機器＋ライセンスでTier1が解放」と説明されており、Wikiの「Tier1必要」という文面と**条件の向きが曖昧**。
**Tier番号は即座にrequiresTierと固定しない。** 「Licenseが必要でTier1解放」なのか「Tier1が必要でLicense解放」なのかをゲーム画面で照合してから依存関係に設定。
四ライセンスの報酬加算合計は **+$45/個**（10+15+10+10）。これは各ライセンスの額が加算式であり、全て有効な箱に適用するという仮定の下での算数。原作の全適用条件/精算タイミングは別途検証。

### 荷物の種類（E3b）
| ParcelClass | UnlockCost | PerParcelBonus | Wiki Tier |
|---|---:|---:|---:|
| Standard Cardboard | default | +0 | 初期 |
| Envelope/Small Box | 100 | +5 | Tier1 |
| Insulated Box | 1000 | +7 | Tier5 |
| Hardshell Case | 5000 | +15 | Tier10 |
- 2026新倉庫のPoly Bagタイプは別アイテム/別Unlock規則。未計測の価格を上の表から推測しない。
- 画像に見える箱の外観から直接報酬値を決めず`ParcelClassId`からCatalogで取得。

## 4. その他アップグレード（E3b）
| UpgradeId | 価格（順に） | 記載された効果 | 不確定 |
|---|---|---|---|
| DELIVERY_FREQUENCY | 500 / 1000 / 1500 | 便の到着頻度増加 | 到着周期の具体値 |
| DELIVERY_BATCH_SIZE | 500 / 1000 | 1便の荷物数増加、第一段は3→5 | 第二段の数。別攻略では最大7との実測 |
| CONVEYOR_DELIVERIES | 1500 / 3000 / 4500 | トラックの自動荷下ろしと/または速度改良 | 2026で「初回購入」「Lvアップ」がどう分かれるか |
| OUTBOUND_CAPACITY | 1000 / 2000 | 出荷口1バッチの容量12→16 | 初期容量（2025攻略には8個単位の記述あり） |
| AUTOMATIC_EXPORTS | 3000 | ゲート満杯時に自動提出 | 出荷曜日ミスは自動で救済しない |
| FLOORING_COSMETIC | 5000 | 床材・色のカスタマイズ | 詳細 |
- 出典SRC-2026-WIKI。数値は複数の改訂が混在している可能性があるため**本番決め打ちせずCatalogへ**。
- 2026年公式変更: Auto PauserはTier15、$30000。Pallet Build ModeはTier11解放。出典SRC-2026-OFFICIAL。
- 倉庫2（Regional Sorting Centre）の購入額について2026年7月のSteamユーザーから約$500000の発言があるが「I believe?」と不確実。精度不足のため未確認[U]であり決定値には採用しない。参考: https://steamcommunity.com/app/2424010/discussions/0/589558792744857728/

## 5. 既知の設備購入価格・レベル費用
2025 Steam設備表（SRC-GUIDE-2025）、日本語攻略（SRC-AUTO-2025）、Planner（SRC-PLANNER）で確認された表:
| Equipment | Base | Lv2 | Lv3 | 区分 |
|---|---:|---:|---:|---|
| Straight/Corner Conveyor | 100 | 50 | 100 | old |
| Joiner Conveyor | 350 | 50 | 100 | old |
| Bridger Conveyor | 500 | 50 | 100 | old |
| Pauser Conveyor | 125 | 50 | 100 | old |
| Sticker Checker | 1200 | 2000 | 4000 | old |
| Weight/Serial/Country/Cargo/Stamp Scanner | 1500 | 5000 | 10000 | old |
| Prohibited Contents Scanner | 2000 | 5000 | 10000 | old |
| Contents Diverter | 1250 | 50 | 100 | old |
| Approved/Denied Sticker Applier | 3000 | 50 | 100 | old |
| Cargo Type Diverter | 1200 | 50 | 100 | old |
| Smart Splitter | 800 | 50 | 100 | old |
| Launcher | 650 | 250 | 500 | old（2025秋以降power/angle変更） |
| Loader/Unloader | 600 | 50 | 100 | old |
| Rack Small/Medium/Large | 500 / 900 / 1600 | - | - | 8/12/16個の収納 |
| Pallet Small/Medium/Large | 125 / 500 / 2000 | - | - | 6/12/30箱の積載 |
| Speedway | 5000 | U | U | 2026 new, Planner |
| Elevator | 10000 | U | U | 2026 new, Planner |
| Country Diverter | 10000 | U | U | 2026 new, Planner |
| Auto Pauser | **30000** | U | U | **2026-07公式** |
注意: 2025資料でScanner Lv3に`1,0000`と誤表記。別ソースの$10000に合わせつつ、2026一致は[U]。

## 6. 物流能力の実測（2025年版、E3b）
- 1ゲーム日（ゲーム内9:00～17:00に相当）の搬入受付時間 ≒現実8分との計測 ⇒ 時計は概ね1ゲーム分/現実1秒（ただし時間停止・睡眠・加速があると変わる）。
- 全Upgrade後、1搬入口あたり約26便/ゲーム日、1便7箱 ⇒182箱/日。旧倉庫5ベイなら理論上910箱/日（182×5）。プレイ中のジャムなどで低下する。
- Level 3の測定 throughput/分: Conveyor29.6, Bridge26.5, StickerChecker19.5, Scanner19.7, Depot15, TransportGate24.4。
- コンベア1区画通過L1/L2/L3=2/約1.33/1秒。Scannerの処理L1/L2/L3=10/5/1秒（単純な処理時間と実効スループットは違う）。
- Loaderは2.5秒/荷物、Unloader0.5秒/荷物と2025報告。ただし**2026年4月のコメントでは後のアップデートで挙動がばらつく**。2026実測値として固定しない。
資料: SRC-THROUGHPUT, SRC-CONVEYOR-2026。
- 2026年の別倉庫/Speedwayなどへこの上限値を当てはめない。

## 7. 出荷可否の分離（2026ガイド、E3b + 公式）
- Original WarehouseではCargoModeにRoad/Rail/Air/Maritimeがある。現行ゲート状態はゲーム内`Cargo Type Calendar`で表示する。
- Roadは毎日受付とする攻略報告あり。Railは**土曜・日曜不可**。
- Airは最大4日程度、Maritimeは最大6日程度の保管が必要という2026年攻略者の計算例。**「Airは○曜日だけ」「Maritimeは○曜日だけ」と特定できる出典が揃っていないため曜日ビットマスクを発明しない**。
- 古い倉庫のC2ゲートはAir/Maritimeを扱えるが、**13箱のMaritimeを火曜に残し、水曜にAir3箱を加えて16箱を提出すると、Maritime13箱が水曜の不正な出荷となる**という具体的な攻略例あり。投入時ではなく**満杯提出した当日のカレンダーで一括正誤評価**される挙動を優先実装候補とする。出典SRC-CONVEYOR-2026。よってGateBatchは詰め始めた日付を保持するだけでなく、評価日=提出日を保持する。
- ユーザーが示した出荷画像の航空アイコンは**出荷区分**。表示文言`NEXT OUTBOUND TRUCK INCOMING`は屋外回収車両の予定。CargoMode=AIRとTruckRunKind=TRUCKは両立し、同一タイプIDにしない。
- 営業17:00以降も搬入は停止するが、既に荷台から出した荷物の処理や仕分けが可能という2025攻略とレビュー。就寝時刻とゲーム時計が止まる詳細は[U]。
- 別施設Regional WarehouseではCargoMode別ではなく国別Express。**国カレンダーは毎週月曜に更新され、Gameplay設定で固定できる（公式2026-07）**。Originalの曜日テーブルは流用しない。

### クリアドアの状態
- 2026-09-25の公式パッチの**開発中は終業時に出口をまとめてクリアする便利操作**。別途2026-07-21のRegional倉庫プレイヤー報告では**15/16の手動Exportが成立**しており、個別手動部分出荷は別の仕組み。詳細SPEC-18。
- 仮設計`ManualSubmit`、`AutoWhenFull`、`EndOfDayClearAll`を分離。Regional個別手動部分出荷を支持する資料があり、Old倉庫は未確認。全ゲートで`FULL_ONLY`とする仮定は禁止。

## 8. ユーザー提供スクリーンショットの精算明細（E2）
提供画像 IMG_0181.jpeg の出荷口モニター表示から次を**画面の数値として**読むことができる:
- 現在のゲート状態は `1/16` （**今まさに入っている箱数**。後述の前回集計と混ぜない）
- 直近の集計欄: `Correct parcels (multiplier): 16`
- `Correct parcel value: +$1,008`
- `Incorrect parcels: 0`
- `Incorrect parcel submission multiplier: -10`（画面に表示されているペナルティ係数と考えられるが、**係数の単位/適用順は未確定**）
- `Incorrect parcel value: -$0`
- `Submission handling charge: -$5`
- `Total: +$1,003`
**算術検証:** $1,008 - $0 - $5 = $1,003。16個すべて正解なら正解価値平均= $1,008/16=$63だが、これはあるバッチの平均であって「全箱一律63」「ベース単価63」という一般規則を証明しない。
**注意:** UI下部の `+1008/-0/-5` は以前提出した直近バッチのSummaryで、上部`1/16`は現在のゲート進捗かもしれない。**同一の16箱と1/16を掛け合わせて解釈しない。**

### 収益エンジンの暫定分解モデル（[P]、原作式の証明ではない）
```text
NominalParcelValue(parcel, activeLicenses, typeUnlocks)
    = BaseValueByParcelClass
      + Sum(EnabledLicenseBonus)
      + OptionalModifierByInspection/ParcelType   // [U]

SubmissionEvaluation(batch, submittedAtGameDate, rules)
    -> CorrectIds, IncorrectIds, reasons

CorrectParcelValue = ApplyCorrectMultiplier(CorrectIds, NominalParcelValues, config)
IncorrectParcelValue = ApplyPenaltyMultiplier(IncorrectIds, config)
HandlingCharge = GetHandlingFee(batch, config)
NetDelta = CorrectParcelValue - IncorrectParcelValue - HandlingCharge

PostLedger(submissionId, NetDelta) only once
```
- `Correct parcels multiplier`の値が16ということは実際の`CorrectParcelValue`全体が単純に16倍とは限らない。与えられた画像だけで実装確定禁止。
- `HandlingFee=-5`は**このバッチについてのみ**確認。すべてのゲート/曜日/レベルで$5固定とは断定しない。
- `BaseParcelValue`、LicenseBonusの適用条件、箱種類/満杯時の倍率、Invalid罰則の増幅の厳密式、営業終了処理のタイミングは[U]。同一規則で荷物1個の種類やLicenseだけを切替えて**差分計測**する必要がある。
- また出荷精算の金額を「輸送トラックが走った距離」で乗算してはならない。原作の提出精算と独自車両演出の料金は分離。

## 9. Contracts（契約）: 以前の設計からの重要な追加
- **複数のプレイヤーがゲーム中の契約用バンを利用したと報告**。契約画面で契約を有効化したあと、奥の部屋で緑ボタン→専用バン召喚→条件を満たす箱を積載→別の緑ボタンで契約を提出する。SRC-CONTRACTS（2025-07）。
- 契約条件として`parcelCount`,`Country`,`Approved/Denied`,`CargoMode`を指定することがある。SRC-CONTRACT-CRITERIA（2025-07）。
- 通常の出荷ゲートへ正しく投げ入れることと、専用バンによる契約納品は別。**ContractSubmission/ContractVanはGateSubmissionやRoad TruckRunに混ぜない**。
- Shop/Tierを上げると契約が解放されるという2025初心者ガイド https://gamerblurb123.squarespace.com/articles/parcel-simulator-beginner-guide-tips があるが、Tier番号や正確な条件は[U]。
- 契約の追加報酬額、条件評価や間違った納品時の罰則、進捗制限、期限は公開資料でまだ確定できず[U]。
- モバイル設計: [契約]→契約受注→[契約用バン]カメラへ即切替→選択中の箱を積む→[納品確定]。アバター歩行は不要だが箱IDとバンの積載を必ず同期。

## 10. 定期的な請求書（Bill）も原作の候補である
- 2025-10のSteamプレイヤー報告には「数日ごとにBillが届く」「体験上は$500未満だった」との実例がある。SRC-UTILITY。
- 他のプレイヤーレビューではelectricity billと説明。どちらもプレイヤー証言のため、**請求書の存在は複数情報で強く示唆されるが、請求間隔・金額・原因・設備ごとの従量計算は未確認**。
- `RecurringBill` と `BillLedgerEntry` の型・UI表示枠を設ける。電力量による計算式が未確認なのに「コンベア数×単価」を実装しない。
- これまでのSPEC-08に「給料/電気代を原作未確認なので初期要件に含めない」と書いていた内容を修正する。電気関連Billは原作の機能調査対象に変更。**従業員の賃金・借入利息などは依然未確認**。

## 11. コード・データ駆動の提案
実装するCatalogは用途/根拠を別ファイルへ:
- `ReferencePriceCatalog`: sourceVersion=2025,sourceStatus=E3a/E3b, equipment/upgrade/license/tier/costPerLevel。
- `SimulationEconomyConfig`: 実際のゲームの収益式に使う係数。初期は[P]を明示し、原作現行版の計測によって設定を差し替え。
- `CargoCalendarOriginal`: Road/Railの確認済曜日、Air/Maritimeは未確定のビット列（開発時はテスト専用仮カレンダーを明示）。
- `CountryCalendarRegional`: seed,weekNumber,countryAvailabilityByDay,randomWeekly/fixedMode。
- `ContractDefinition`: contractId,targetCounts,targetCountryIds,targetCargoModes,targetStickerDecision、rewardPolicyId、manualVanSubmissionRequired。
- `PeriodicBillDefinition`: billId,triggerPolicy,calculationPolicy,sourceStatus。
- `SubmissionLedger`は通常提出と契約納品と請求支払いを区別し、イベントIDによる重複請求/報酬を防ぐ。

## 12. 新たに狭められる調査課題
- GAP-PRICE-A: 2026-09-29 Build#25586459における旧倉庫全価格表/ライセンス前提/Tier方向を実機画面で突き合わせる。
- GAP-INCOME-A: ライセンス/箱種類を1つだけ変えた複数の16箱提出を比較し、`Correct parcel value`の算出式、掛け算順序、切捨/丸めを特定。
- GAP-INCOME-B: 0/1/2不正箱の各提出で`Incorrect multiplier`の意味と罰則を特定。
- GAP-FEE-A: 異なるGate/容量/曜日でSubmission handling chargeの$5固定可否を特定。
- GAP-CALENDAR-A: Original倉庫で7曜日×4モードの実際の受入カレンダーをスクリーンショットで記録する。
- GAP-CALENDAR-B: 火曜のMaritime13箱を水曜のAir3箱で満杯にする試験を原作で再現し、提出日判定の条件を確定する。
- GAP-CONT-A: 契約用バン召喚、積載数、達成報酬、期限、Tier条件、評価タイミング。
- GAP-BILL-A: Billのゲーム内発行日、計算基準、何を支払うのか（電気/その他）を実機で記録。
- GAP-UPGRADE-A: 第二段階のトラック便荷物数/初期出口容量をShop画面で照合。
- GAP-WAREHOUSE-A: 2026 RegionalWarehouseの正確な$購入額/Tier18上限/国別カレンダー条件を実機計測。

**決められるところは決め、そうでない部分は「調べられない」ではなく「どの条件をどう観測するか」を具体化する。**
