# 経済・ショップ・ライセンス・倉庫Tier・アップグレード
文書ID: SPEC-08 / REQ: REF-019..021,034..036,043..047 / 状態: 2025参考値と未知の経済式を分離

## 1. お金のデータと収益
- `MoneyMinorUnit`: 最小通貨単位の符号付64bit整数で管理し、floatの丸め誤差を資金計算へ混ぜない。
- 正しい提出で売上、間違った出荷で罰則、ショップ・Upgrade・License・Tier購入で費用。**承認シールを貼っただけでは売上を計上しない**。
- 参考作品にない可能性が高い給料・借入利息・輸送燃料費などは推測で実装しない。一方、**契約用バンの専用納品と数日ごとのBillはプレイヤー証言で存在が示されているため**、SPEC-16に従い必須調査・実装候補に含める。Billが電力従量制かは未確認。
- 提出ごとの資金/ミスを`LedgerEntry`として記録。イベントに`submissionId,parcelId,ruleVersion,reasonCode`を保持。
- 同じSubmissionIDとParcelIDへの二重入金を冪等キーで防ぐ。

### PaymentEngine interface
```text
CalculateSubmission(submission, ruleSnapshot, feeCatalog) ->
 {correctItems:[{parcelId, amount}], incorrectItems:[{parcelId, reason, amount}], net}
PostLedger(submissionId, result, idemKey) -> Applied | AlreadyApplied | Rejected
```
正確な原作の報酬式・違反時罰金は実機で未検証[U]。`feeCatalog`は根拠バージョン付き構成として差替可能にする。テスト専用の仮金額（例: 正常+20、違反-10）は**[P]であり本番の再現値ではない**。

## 2. LicenseとTierとUpgradeの違い
- **License**: 新しい検査項目を有効化し、荷物単価を上げる。購入しなければ新検査は要求されない[R/S3@2025]。
- **WarehouseTier**: 倉庫敷地、設備購入枠、ベイ/出荷口、新しい設備・設備レベルを解放。
- **FunctionalUpgrade**: Conveyor Deliveries、Automatic Exports、Pallet Build Mode、RackSigns等の機能が使えるようになる。
- **EquipmentLevel**: 置いてある機械ごとのレベルで、搬送速度/検査時間が変わる。
- **WarehouseProfile**: ORIGINALとREGIONALは別の解放マトリクス。
- 必要条件は`ALL(requiredLicenses,minimumTier,previousUpgrades,availableBalance)`。[P]全依存グラフは実機調査で埋める。

## 3. 2025コミュニティ参考値（2026最新版とは限らない）
これらの数値には`sourceId=S5,sourceYear=2025,status=R_HISTORICAL`を付与し、2026ゲームとの一致未検証を保持。

| 設備 | 購入価格 | Lv2 | Lv3 | 補足 |
|---|---:|---:|---:|---|
| Straight / Corner | $100 | $50 | $100 | ベルト |
| Joiner | $350 | $50 | $100 | 合流 |
| Bridger | $500 | $50 | $100 | 交差 |
| Pauser | $125 | $50 | $100 | 停止 |
| Sticker Checker | $1,200 | $2,000 | $4,000 | 既貼付を検出 |
| Scanner(Weight/Serial/Country/Cargo/Stamp) | $1,500 | $5,000 | $10,000 | 検査 |
| Scanner(ContentsProhibited) | $2,000 | $5,000 | $10,000 | 禁止品 |
| Contents Diverter | $1,250 | $50 | $100 | 開封要否 |
| Sticker Applier Approve/Deny | $3,000 | $50 | $100 | シール |
| Cargo Type Diverter | $1,200 | $50 | $100 | 便別 |
| Smart Splitter | $800 | $50 | $100 | 3分岐 |
| Launcher | $650 | $250 | $500 | 射出 |
| Loader / Unloader | $600 | $50 | $100 | ラック接続 |
| Rack S/M/L | $500/$900/$1,600 | - | - | 8/12/16箱 |
| Pallet S/M/L | $125/$500/$2,000 | - | - | 6/12/30箱 |

- 2025資料: 一般スキャナーLv1/2/3の検査時間が10/5/1秒。
- 2025コミュニティ実測コメント: ベルト1区画のLv1/2/3通過時間が約2/1.33/1秒。厳密な現行値ではない。
- 2025コミュニティ: 最初の拡張が重量秤+重量ライセンスを前提に$700との記述。現行の全Tier表ではない。
- 2026-07-30公式: Pallet Build ModeはTier11、Auto PauserはTier15で価格$30,000に変更。
- 2026-09-25公式: T-Joiner / RackSigns / Shop Order History。価格は未確認[U]。
- 2025コミュニティScanner表のLv3は誤植らしき`1,0000`を含む。2025日本語ガイドの$10,000と合わせて注記するが、現在の正確な価格は要照合。

## 4. ショップ・在庫・配置の一意な金銭処理
```text
CatalogItem {
 id, kind: Device|License|Tier|Upgrade|Cosmetic,
 warehouseProfiles[], requiredUnlockIds[], minTier?,
 purchasePriceMinor, defaultDeliveryDepotId?, stackLimit?,
 sourceStatus:R|D|P|U, sourceId, sourceDate, catalogVersion, visualId
}
Order {
 orderId, lines:[{itemId,quantity}], deliveryDestinationId,
 status: Basket|Purchased|WaitingDelivery|Delivered|Collected|Closed,
 chargedAtEventId?, shopDeliveryRunId?, inventoryReceiptIds[]
}
```
- 購入ボタンで資金チェック→1回だけ決済→Order作成。`orderId`を一意に管理。
- 倉庫へ納品された場合はInventoriedItemに移し、設置時は「在庫-1、設備Instance+1」。購入代金を再請求しない。
- 即購入即設置の別UIがあれば決済は設置確定時の一度だけ。注文済み在庫を使う場合は即購入扱いにしない。
- 購入履歴からの再注文は**新しいOrderId**と別の支払いを作る。過去の注文を二重納品しない。
- 2026-07パッチではショップ配送先選択が睡眠/セーブ後も記憶される。[R/S2]
- 売却返金率・全額返金・取り消し可能時間は未確認[U]。原作実機を調べるまで固定しない。
- シャッターへ来る通常荷物とショップの納品物は異なる台帳・運行便で管理。

## 5. 必要なTierマスターの列
`warehouseProfile,tierId,cost,requiredLicenseIds[],requiredTierId,unlockedCellSet[],newInboundDockIds[],newExportGateIds[],newCatalogIds[],maxLevelMap,sourceId,sourceDate,verificationCaptureUri`
- 全Tierの価格と形は現行実機で確認して登録する。10×20→10×10はあくまで2025初期倉庫資料に対応。
- プレイヤーが資金不足でも手動の荷物検査と出荷で復帰可能な流れを維持。
- 新しいライセンスのルールは購入時に解放し、未購入なら突然検査対象にしない。
- コストはエディタ/カタログデータで修正できるようにし、C#ハードコード禁止。

## 6. 例外・検証
- 重複Submit: Ledgerは一度だけ計上。
- Submit中にOSから終了: セーブ時ジャーナルで確定/未確定を区別。
- Wrong cargo/day/sticker: 結果内訳に理由が保存される。
- Shop購入直後に強制終了: 二重決済・納品の重複なし。
- 受取済み在庫→配置→売却→再注文でも個体数と金額の保存が一致。
- WarehouseTier購入中にセーブ: 敷地拡張・設備解禁・費用を一括適用。

QA-ECO-01 残高不足注文は全データ不変。
QA-ECO-02 購入済み機械1個を配置して支払い1回のみ。
QA-ECO-03 提出とトラック出発で同一箱に二重報酬なし。
QA-ECO-04 License購入前は未解放検査が荷物へ現れない。
QA-ECO-05 Tier変更と倉庫拡張/ショップ解放が一致。
QA-ECO-06 ショップ再注文で注文別IDと決済/納品が各1回。
QA-ECO-07 参考価格は変更可能なデータにありコード定数でない。
QA-ECO-08 セーブ・ロード後も残高/売上履歴/アイテム数が一致。

出典 S3=https://shippobox.com/archives/16245 / S4=https://shippobox.com/archives/16262 / S5=https://steamcommunity.com/sharedfiles/filedetails/?id=3509976176 / S2=https://steamcommunity.com/app/2424010/announcements/


## 7. 追加確認：ライセンス解放関係とLauncher
- **[R/2025コミュニティ攻略]** Weight licenseにはIndustrial Scales→Tier1、Serial licenseにはSerial Keypad→Tier2、ContentsにはTier3とOpening Toolbox→Tier4、Stamp licenseはTier5→Tier6という解放連鎖の記述がある。https://steamcommunity.com/sharedfiles/filedetails/?id=3505458195
- **[R/2025-09-29公式]** LauncherのLv2/Lv3は**ベルト送り速度**を上げる。発射角度/強度は機器側の独立設定で変更する。https://steamdb.info/patchnotes/20175356/
- 2025年参照価格表・Tier依存は2026年現行版で再検証するまでステータス`R_HISTORICAL`を維持。



## 2026-10-26 調査上書き（最優先：SPEC-16）
**旧節の「参考作品に無い/未確認の電気代、契約ボーナスを再現しない」は再調査で不正確。機能の存在を示す資料が見つかった。**
- 契約（Contracts）: 別室の契約バンを呼び、条件に合う複数箱を積み込み、専用の納品操作で確定する。2025 SteamのQ&A複数投稿に手順がある。条件は個数/国/貨物タイプ/承認拒否。原作の単価と出現間隔はU。通常出荷Gateと別のContractLedgerを設ける。
- 定期Bill: 数日ごとにBillが来るというSteam2025の体験報告。Electricityと説明するレビューもあるが、従量計算は未確認。Bill due/reason/amountと支払いイベントの型を用意し、**架空のコンベア電力単価を固定しない**。
- ライセンス価格/報酬加算（韓国コミュニティWiki 2026-10-10）: Weight 300/+10、Serial 300/+15、Contents 500/+10、Stamp 500/+10。古い価格から変わった可能性はU。
- 箱の種類（同Wiki）: Envelope/Small $100/+5、Insulated $1000/+7、Hardcase $5000/+15。
- 入荷頻度強化 $500/$1000/$1500、搬入数強化 $500/$1000（初段3→5）、ConveyorDeliveries $1500/$3000/$4500、出荷容量強化 $1000/$2000（12→16）、AutomaticExports $3000、床材 $5000。全てE3b、現行の再確認はU。
- 旧倉庫Tier価格はTier1=700からTier14=4600まで+300、合計37100。複数資料の一致確認あり。SPEC-16の価格テーブルをCatalogへ登録する。
- ユーザーの出荷ゲートIMG_0181に、CorrectParcelValue+1008、IncorrectParcelValue-0、HandlingCharge-5、Total+1003の明細が見える。**HandlingChargeという経費項目は資料上確認できる**。ただし全バッチで-$5かはU。
- 原作の収益は**荷物単価/LicenseBonus/ParcelTypeBonus/正誤と倍率/提出手数料**で分解して計算。未確認のBaseValueや係数をC#定数で固定しない。画面に見える平均$63を全箱の基本単価と断定しない。
- 請求書、契約、Submissionの3種類はLedgerEntryKindで区別し、いずれもCommandId/SourceEventIdで二重精算防止。
- 従業員給与・銀行利息・燃料価格などは現在も明確な原作根拠がないため必須にしない。



## 2026-10-26 定価カタログの確認度アップ（SPEC-17B/SPEC-18）
- Regional Sorting Centre購入**$500,000**（2026対応実績攻略＋複数プレイヤー報告、非公式B）。Tier18最終/12国ゲート（2026プレイヤー証言、C）。Tier15～18の価格/Express必要枚数はU。
- Speedway $5,000/Lv2 $5,000/Lv3 $10,000、Elevator $10,000/Lv2 $10,000/Lv3 $20,000（2026対応実績攻略、非公式B）。
- Auto Pauser Tier15/$30,000（開発者公式2026-07-30、A）、PalletBuildMode Tier11（同A）。
- Billについて2025年の2件のプレイヤー証言で**数日ごと/$500以内**との報告あり。だが$500を全プラットフォーム/難度の保証上限とせず、電力従量式を捏造しない。https://steamcommunity.com/app/2424010/discussions/0/601917139265464619/
- 契約はDedicatedVanへの箱転送で、普通の出荷はGatewayへの転送。契約がいつでも通常出荷より得とは限らず、後半で非効率というプレイヤー体験あり。契約報酬式/条件はU。
- **データ表**: `docs/data/reference_catalog_2026-10-26.json` に各価格とソース強度を持たせてある。2026確定額ではないものも含まれるためConfigの本番値へ無審査で転用不可。
