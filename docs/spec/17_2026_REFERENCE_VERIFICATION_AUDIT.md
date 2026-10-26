# Parcel Simulator 2026年10月公開資料 総合調査・出典監査
文書ID: SPEC-17B / 版: 0.5 / 調査基準: 2026-10-26 / 状態: 公開情報調査済み・実機の全体照合は未実施

## この文書の目的
2026-10-26時点で公開されている公式更新、2025/2026のSteam攻略、プレイヤーの実測/議論、非公式配置Planner、ユーザーが提供した3枚の画像をクロスチェックした結果を記録する。**設計が網羅されていることと原作の完全な再現を証明したことは別**。原作製品のバイナリ/セーブデータ/著作物を無断抽出・複製しない。

## 1. 出典の強さ
- **A（公式の事実）**: Steamの開発者発表と現行の公開リリースノート。記載された機能・変更日・値に限定。
- **B（複数の独立した二次資料またはユーザー提供画面）**: 詳細攻略 + 複数の実プレイヤー記録が一致。原作の最新版の同一条件下の保証ではない。
- **C（単一のコミュニティ攻略またはプレイヤー実測）**: 条件や測定日が古い、再現条件が十分でない可能性。
- **D（技術提案/試験用の仮値）**: 再現証拠として扱わず、原作と一致するとうたわない。
- **U（未確定）**: 原作のゲームプレイ、実機の操作動画、ゲーム内ヘルプ/カレンダー等からの追加確認が必要。
- Aでも「開発中の機能」は *未リリースと判断する材料* であり「現在実装済み」の根拠ではない。

## 2. 確認先URL
| Source ID | タイトル / 掲載元 | 注意点 |
|---|---|---|
| O-STEAM | https://store.steampowered.com/app/2424010/Parcel_Simulator/ | 公式の大枠 |
| O-NEWS | https://steamcommunity.com/app/2424010/announcements/ | 2026-07-20～2026-09-29など開発者更新 |
| O-NEWS-JP | https://steamcommunity.com/app/2424010/allnews/?l=japanese | 2026-07-30や08-04の詳細確認 |
| G-ACHIEVE | https://steamcommunity.com/sharedfiles/filedetails/?id=3505458195 | 2026年の地域倉庫対応まで追記のある実績攻略 |
| G-STATS | https://steamcommunity.com/sharedfiles/filedetails/?id=3509976176 | 2025-06-27～07-27の旧倉庫分析、古いLauncher情報に注意 |
| G-MANUAL | https://shippobox.com/archives/16245 | 2025の手動検査 |
| G-AUTO | https://shippobox.com/archives/16262 | 2025の自動化 |
| G-PLANNER | https://parcelplanner.app/ | 非公式Planner、設備価格とTier参照 |
| G-THROUGHPUT | https://steamcommunity.com/sharedfiles/filedetails/?id=3563578111 | 2025機器処理量の参考値 |
| G-CALENDAR | https://steamcommunity.com/sharedfiles/filedetails/?id=3664942587 | 2026の旧倉庫カレンダー/ゲート混載の攻略者体験 |
| F-REGIONAL | https://steamcommunity.com/app/2424010/discussions/0/583931831085259817/ | 2026-08-19「Tier18ですべての国別ゲート」プレイヤー回答 |
| F-REGIONAL-PRICE | https://steamcommunity.com/app/2424010/discussions/0/589558792744857728/ | 2026-07-24地域倉庫$500kとのプレイヤー言及 |
| F-REGIONAL-ERR | https://steamcommunity.com/app/2424010/discussions/0/589559717132679323/ | 地域倉庫はCountryの名・旗を検査する必要あり |
| F-BILLS | https://steamcommunity.com/app/2424010/discussions/0/601917139265464619/ | 2025-10の請求書$500程度のプレイヤー証言 |
| F-CONTRACTS | https://steamcommunity.com/app/2424010/discussions/0/591777299172521111/ | 2025の専用契約バンの操作体験 |
| F-CONTRACT-CRITERIA | https://steamcommunity.com/app/2424010/discussions/0/591775773438869828/ | 個数/国/輸送区分/正誤の契約条件 |
| G-LIC-WIKI | https://www.namu.moe/w/Parcel%20Simulator | 韓国語コミュニティWikiの価格表。2026-10時点で直接ページの再取得が不安定、再検証要 |
| O-LAUNCHER | https://steamdb.info/patchnotes/20175356/ | 2025-09-29 Launcherのpower/angleとベルトレベルを分離 |
| O-SEPT | https://steamdb.info/patchnotes/25526810/ | 2026-09-25 ラックサイン/T-Joiner/追加の最適化 |
| O-AUG2025 | https://steamcommunity.com/app/2424010/announcements/ | 2025-08 Conveyor Deliveriesの複数段階とMulti-Joiner |

## 3. 必ず区別すべき倉庫（A/B/C）
### 3.1 Original Warehouse（旧倉庫）
- 手動検査と既検査品のバイパス、Road/Rail/Air/Maritime/Invalidへの出荷を軸とする。[A/C]
- **Tier14が旧倉庫の最大拡張**と攻略で明記されている。[B: G-ACHIEVE, G-STATS]
- 初期の平面は10×20グリッド、1拡張+10×10グリッド（2025測定）。[C: G-STATS]
- Tier 1の拡張は重量計と重量検査License購入後、$700という2025資料。[C: G-STATS]
- LauncherはTier12でショップ購入できる。[B: G-ACHIEVE]

### 3.2 Regional Sorting Centre（2026-07-20追加）
- **約5倍の延床面積**と**2階の倉庫**、Express荷物を12国/地域区分で仕分ける機能。[A: O-NEWS]
- 旧倉庫のCargo Typeではなく、Expressの国別ルーティング。任意設定できる国別ゲート。国別カレンダーは**毎週月曜**更新。[A]
- 2026-07-24からCountryCalendarを固定するGamePlay設定も存在。[A]
- **購入価格$500,000**：2026年対応実績攻略と7月のプレイヤー証言が一致。[B: G-ACHIEVE, F-REGIONAL-PRICE]
- **Tier18がすべての12国別出荷口を解放する最終Tier**：2026年8月のプレイヤー回答。[C: F-REGIONAL]。**Tier14からWarehouse2購入で総合Tier15へ進むという報告があるため、倉庫ごとTierカウントのかたちを勝手に変更しない。**
- 地域倉庫拡張には**提出済みExpress荷物数の要件**がある。[B: G-ACHIEVE]。必要個数の各Tier表は[U]。
- 海外の車両が道路を走る演出は本作品独自拡張。CargoModeと外部のPickupTruckを同じ種別にしない。

## 4. 倉庫Tierと価格の詳細（B/Cの歴史参照）
旧倉庫Tier 1～14（G-PLANNER/G-STATS/G-LIC-WIKI）、各段増分+$300。最新版の全価格と一致するかは[U]。
| Tier | Price $ | Tier | Price $ |
|---:|---:|---:|---:|
|1|700|8|2800|
|2|1000|9|3100|
|3|1300|10|3400|
|4|1600|11|3700|
|5|1900|12|4000|
|6|2200|13|4300|
|7|2500|14|4600|
合計=$37,100。これ以外の新倉庫解放コスト$500,000と合算しない（別購入）。
- Tier1 Conveyor解放。[B: G-ACHIEVE]。重量ライセンスで購入可能になる順序はゲーム内Unlockメッセージで確認必要。
- Tier2の初期ラックSmall（8個）/Sticker Checker/Weight Scanner。[B]
- Tier3のProhibited Contents License取得関連。[C: G-ACHIEVE]
- Tier4/5/6のLicense連鎖は攻略上で確認できるが、完全解放グラフは[U]（SPEC-02を参照）。
- Tier11 Pallet Build Mode、Tier15 Auto Pauser（$30,000）。[A: O-NEWS, 2026-07-30]
- Tier12 Launcher。[B: G-ACHIEVE]
- Tier14→Regional購入後Tier15へ進むというプレイヤー報告。[C: F-REGIONAL-PRICE]
- Tier18 新Warehouseで全12出荷ゲート。[C: F-REGIONAL]
- **Tier15～18の個別価格／Express要求件数／各ゲート解放順**は[U]。ゲーム画面の未解放案内を順次記録しない限り推測不可。

## 5. 2026追加設備と価格
| 機械 | 購入額 | 追加強化 | 出典強度 |
|---|---:|---:|---|
| Speedway | $5,000 | Lv2 $5,000, Lv3 $10,000 | B: G-ACHIEVE |
| Elevator | $10,000 | Lv2 $10,000, Lv3 $20,000 | B: G-ACHIEVE |
| Country Diverter | $10,000 | 詳細未確認 | C: G-PLANNER |
| Auto Pauser | **$30,000** | 詳細未確認 | A: O-NEWS 2026-07-30 |
| T-Joiner | 実装あり、価格不明 | 詳細未確認 | A: O-SEPT 2026-09-25 |
| Storage Rack Signs | Upgrade追加、価格不明 | 自動/手動/Buffer指定 | A: O-SEPT 2026-09-25 |
| Multi Joiner | $850との非公式Planner値 | 最大3入力と2025公式説明 | A(存在/3入力)+C(価格) |
| Launcher | $650（2025資料） | Lv2 $250, Lv3 $500（2025） | C: G-STATS |
- Speedway総購入+強化=$20,000、Elevator総購入+強化=$40,000（G-ACHIEVE）。
- **Launcher** は2025-09-29以降「発射のpower/angle設定が別、Upgradeは送りベルト速度」。2025-07古攻略の「Lv3=飛距離16〜18単位」は廃止前の挙動であり現行式に用いない。[A: O-LAUNCHER]
- 2026-07更新以降Elevatorは上昇**と下降**。[A: O-NEWS]
- Country Diverterに`None`カテゴリ。[A: O-NEWS]
- 新倉庫はCargo Typeの仕分けではないが、Country（名称/旗）の正誤は必要という報告。[C: F-REGIONAL-ERR]。旧倉庫のCargo Type有効性を新倉庫のExpressへ機械的に適用しない。

## 6. 旧設備の価格（2025基準のC、2026現行版は要再確認）
| type | Base $ | Lv2 $ | Lv3 $ |
|---|---:|---:|---:|
| Straight/Corner | 100 | 50 | 100 |
| Joiner | 350 | 50 | 100 |
| Bridger | 500 | 50 | 100 |
| Pauser | 125 | 50 | 100 |
| Sticker Checker | 1,200 | 2,000 | 4,000 |
| Weight/Serial/Country/Cargo/Stamp Scanner | 1,500 | 5,000 | 10,000 |
| Prohibited Contents Scanner | 2,000 | 5,000 | 10,000 |
| Contents Inspection Diverter | 1,250 | 50 | 100 |
| Sticker Applier Approve/Denied | 3,000 | 50 | 100 |
| Cargo Type Diverter | 1,200 | 50 | 100 |
| Smart Splitter | 800 | 50 | 100 |
| Loader/Unloader | 600 | 50 | 100 |
| Pallet S/M/L | 125/500/2,000 | - | - |
| Rack S/M/L | 500/900/1,600 | - | - |
- Rack: 8/12/16箱、Pallet: 6/12/30箱。[C: G-STATS]
- 2025 G-STATS Scanner Lv3費用に`1,0000`との誤植。別資料の10,000値で補うが最新確定とは見なさない。
- Conveyor L1/L2/L3が1区画2/約1.33/1秒、Scanner L1/L2/L3処理10/5/1秒という2025プレイヤー報告。レベル以外に詰まり/処理待ちを考慮する。
- 2026改修ではベルトとラベル描画/CPU等が最適化されているが、**元作品のエンジン改修をUnityコード上で同じ実装とする必要はない**。[A: O-NEWS]

## 7. ライセンス価格と加算ボーナス：出典の弱い情報に注意
SPEC-16では韓国語コミュニティWikiを参照し、Weight $300/+10、Serial $300/+15、Contents $500/+10、Stamp $500/+10、Envelope/Small $100/+5、Insulated $1,000/+7、Hardcase $5,000/+15などの値を記録している。
- **今回、元ページの再取得に制約があるため直接クロスチェックが不足している**。これらを2026年最新確定の購入価格・報酬としない。
- 運用には`sourceStrength=C, version=historical_or_unknown, requiresVerification=true`を付け、最終リリース直前に原作のShop画面を取得する。
- TierやLicenseの依存条件は「TierによってLicenseが買える」と「License購入がTierを解放する」を取り違えやすい。SPEC-02とG-ACHIEVEに照らして向きを記録する。

## 8. 出荷曜日と提出の厳密な区別
- ORIGINALの貨物区分は Road, Rail, Air, Maritime。[A: O-STEAM]
- **Railが土日閉鎖**という2025コミュニティ体験がある。[C: F-BILLS と別のRail議論]
- Roadを毎日稼働として組む報告もあるが、曜日ごとの公式表は現時点で取得できていない。[C]
- AirやMaritimeの出荷可能曜日に偏りがあり、数日保管が必要。[C: G-CALENDAR]
- **完全な7曜日×4区分のビットマスクは現状[U]**。「Rail=月水金/Air=火/Maritime=木/Road=月土」等を説明するSteamフォーラムの例は**将来のHardmodeを提案する仮定のカレンダー**であり、現行ルールではない。https://steamcommunity.com/app/2424010/discussions/0/601917139265464619/
- 「出荷可能曜日」と「検査合格」は別条件。正しい箱でも曜日違いなら保管する。
- 2026年版攻略の具体例: 火曜にMaritime13個を共用Gate C2へ、翌水曜Air3個で16箱充填し提出すると、火曜に入れたMaritimeも**水曜の出荷可能条件**で誤提出になる。[C: G-CALENDAR]。初期のゲート充填日ではなく**Submit時のCalendarSnapshot**を評価するモードを実装する。
- 画像`1/16`は今いる箱数、表示された「16正送/+$1008/-$5=+$1003」は前回精算かもしれない。**1/16と履歴16を同じバッチ数として掛算しない**。
- 2026-09-25開発者は`Clear Doors (partially filled gate)`を「開発中」と記載。[A] 部分満杯の無制限提出を実装済みと断定しない。暫定`FULL_ONLY`が再現に近い。

## 9. 契約用バンと定期Bills
- 契約は普通の出荷とは別の受注→条件選択→専用Vanを召喚→条件を満たす荷物を積み→完了ボタンを押す機能。契約条件には数/国/輸送区分/承認拒否。[C: F-CONTRACTS, F-CONTRACT-CRITERIA]
- **契約はいつでも最も高い利益を与えるとは限らない**。後半プレイヤーが通常売却を優先するとの報告もある。[C]。原作の契約報酬式・期限・必要Tierは[U]。
- 数日ごとにBillが届くとの報告があり、少なくともプレイヤー二人が「この請求の額は$500以内・最大$500」との体験を述べる。[C: F-BILLS]。**$500を全ケース共通上限として断定しない**。発行間隔、電力使用量との関係、支払期限と未払い時の結果は[U]。
- プレイヤーがその日の売上を0にしても、時間を飛ばす場合にBillは発生するとの観察。[C]。「賃金」「燃料費」「ローン利息」が原作に存在すると実装してはいけない。
- ContractVanRunId / NormalTruckRunId / ShopDeliveryRunIdは別種類。荷物IDは所有場所を原子的に移す。
- billsには`BillId, issuedDay, amount, paidDay?, sourcePolicyVersion`、重複発行/支払を防ぐ一意IDを持たせる。

## 10. 日付とトラック（参考の観察値）
- 2025年攻略には9:00〜17:00の8ゲーム時間が約8実分で進むとの観察。[C]。現行版で`1ゲーム分=現実1秒`を全設定に固定しない。
- 旧倉庫で搬入口5、全強化26便/日×7箱/便×5=910箱/日という理論値報告がある。[C]。プレイヤーの手動Pallet使用で48便/日とのプレイ記録もあり、**「1入口上限は26で厳密に固定」という結論は誤り**。通常自動配送とPalletの方法の違いを記録する。
- ショップ専用配送車両を完全に空にしないと寝られない規則は、2026-08-04の公式アップデートで削除。[A]
- 道路外部から走行するトラックの可視化は本プロジェクト独自追加。元作品の入荷頻度を尊重しつつ、見た目のアニメーションは新しく設計する。

## 11. 開発者の性能対策（具体例で参照可能）
2026-07-30公式の最適化:
1. Conveyor CPUの不要な呼出削減。
2. ラベル、箱、搬送矢印、アップグレードのドローコール/VRAM削減。
3. 搬送矢印・レベル表示バーの一部を軽いデカール表示へ。
4. 倉庫間の不可視オブジェクトをより合理的に除外して向きを変えた時のスタッター削減。
5. 複数の離れた未接続コンベア間で箱が瞬間移動するバグを修正。
6. ロード時の「見た目には空きセルなのに設置不可」のセル管理修正。
このバグ履歴は本プロジェクトの**受入テスト**へも追加すべき。Unityネイティブのスマホではメッシュの表示単位とConveyorNodeの接続処理を分離し、正当なPortだけ搬送する。

## 12. 最新アップデート（2026-10-26までの公開情報）
- 2026-07-20：Regional Sorting Centre、国別Express、Elevator、Speedway、Pallet Build等の大型更新。[A]
- 2026-07-23：Elevator上下移動、Country Diverter None追加、ショップ返品用箱など。[A]
- 2026-07-24：固定CountryCalendar、Saveバックアップ。[A]
- 2026-07-30：上記性能対策、Tier11 PalletBuild/Tier15 AutoPauser $30k。[A]
- 2026-08-04：直近5世代セーブバックアップ、Shopトラック空荷で就寝する制約を撤去。[A]
- 2026-09-25：Storage Rack Signs、T-Joiner、Shop Order History/Re-Purchase、レベルアップ塗装/最適化。[A]
- 2026-09-29：Submitted chute壁/クラッシュ/ローカライズの修正。[A]
- Steamの検索上では2026-09-29が確認できる最近の**公表パッチ**。それ以降に一切配信されていないと断定しない。

## 13. Codexが行ってはいけないこと
- 旧Tier14と新Tier18を別ゲームに置き換える／最終Tier18の国ゲート12個を初期開放する。
- $500kを「開発者が断言」と注記する（攻略ベースBである）。
- SteamのHardmode提案中の曜日を実際のゲームの設定として確定する。
- 地域倉庫にもCargo Type検査を無条件に要求する。
- 2025年旧Launcherの射出距離をレベル上げで自動増加させる。
- 2026年9月時点で開発中のClear Doorsを当たり前に実装済みと扱う。
- 出荷ゲートの上部`1/16`と下部の直近バッチ`16正送`を同じ箱数として計算する。
- Billを`100W * conveyorCount`など架空の電力計算で確定する。
- 実機/Editor/端末で確認していないのに原作品全仕様再現済みと言う。
- オリジナル素材ではなく原作固有の3Dモデル/画面レイアウトを無断コピーする。

## 14. 残る要実機調査：優先度
P0:
1. **旧倉庫の7曜日×4貨物の正確なカレンダー**。
2. **収益の完全な式**：BaseParcelValue、License加算、ParcelType加算、Correct/Incorrect倍率、出荷手数料、丸め。
3. **ゲート容量・満杯提出・評価日・出荷イベント**の正確な規則。
4. **契約報酬・契約条件・請求額/請求サイクル**の実測。
5. **ライセンスとTier解放の依存の向き、2026版の変更点**。
P1:
6. Regional Tier15/16/17/18の必要Express数、価格、個別ゲートの解放順。
7. 2026追加設備（T-Joiner, CountryDiverter等）の全ポートと価格/速度。
8. 装置強化の最大速さ、搬入到着便数/箱数。
9. 2026年の原作最新情報のアップデートの有無。
P2:
10. 実績/装飾/NPC猫/チュートリアル/全ての細かな入力可能UI、品質と音響。
**ユーザーに原作のゲーム実行環境/詳細プレイ画面がなく、こちらから実機操作できない場合でも、P0〜P2を曖昧な推測で埋めない。**

## 15. 実装資料の優先順位
本書は2026-10-26の追加調査結果。SPEC-00〜16の一般方針は引き継ぐが、**価格・第2倉庫・アップグレード・曜日・請求書・専用バン・旧Launcher**については本書の資料評価を優先する。
原作の現行版を画面録画して反証が出た場合は記録し、より強い根拠に差し替える。
