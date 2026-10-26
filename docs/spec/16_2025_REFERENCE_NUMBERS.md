# 2025年版の公開計測値・料金早見表（2026現行の正式値ではない）
文書ID: SPEC-16 / 版: 0.4 / 出典は2025年6〜7月のプレイヤーガイド
目的: Codexが無根拠な設備数値を作らないため、**出典に明記された参考値**と「暫定」の境界を作る。
重要: これらの値は元作の現行版2026年と一致する保証がない。ゲーム内で使用前にEvidenceStatus=LEGACY_2025を必ず設定する。

## 根拠
- R25-GUIDE = Steamコミュニティ「Parcel Simulator Stats and Facts」、作成2025-06-27、更新2025-07-27。https://steamcommunity.com/sharedfiles/filedetails/?id=3509976176
- R25-AUTO = ShippoBox自動化解説 2025-06-30。https://shippobox.com/archives/16262
- R25-INSPECT = ShippoBox検査解説 2025-06-29。https://shippobox.com/archives/16245
- R26-PATCH = Steam公式2026-07〜09アップデート。https://steamcommunity.com/app/2424010/announcements/

## 1. 2025 Shop設備価格と強化費用（ドル）
| CatalogId候補 | 2025初期価格 | Lv2/Lv3強化価格 | 出典 |
|---|---:|---|---|
| STRAIGHT | 100 | 50 / 100 | R25-GUIDE |
| CORNER | 100 | 50 / 100 | R25-GUIDE |
| JOINER | 350 | 50 / 100 | R25-GUIDE |
| BRIDGER | 500 | 50 / 100 | R25-GUIDE |
| STICKER_CHECKER | 1,200 | 2,000 / 4,000 | R25-GUIDE |
| WEIGHT_SCANNER | 1,500 | 5,000 / 10,000 | R25-GUIDE/R25-AUTO |
| SERIAL_SCANNER | 1,500 | 5,000 / 10,000 | R25-GUIDE/R25-AUTO |
| COUNTRY_SCANNER | 1,500 | 5,000 / 10,000 | R25-GUIDE/R25-AUTO |
| CARGO_ICON_SCANNER | 1,500 | 5,000 / 10,000 | R25-GUIDE/R25-AUTO |
| STAMP_SCANNER | 1,500 | 5,000 / 10,000 | R25-GUIDE/R25-AUTO |
| PROHIBITED_SCANNER | 2,000 | 5,000 / 10,000 | R25-GUIDE/R25-AUTO |
| CONTENTS_DIVERTER | 1,250 | 50 / 100 | R25-GUIDE |
| APPROVE_APPLIER | 3,000 | 50 / 100 | R25-GUIDE |
| DENY_APPLIER | 3,000 | 50 / 100 | R25-GUIDE |
| CARGO_DIVERTER | 1,200 | 50 / 100 | R25-GUIDE |
| SMART_SPLITTER | 800 | 50 / 100 | R25-GUIDE |
| LAUNCHER | 650 | 250 / 500 | R25-GUIDE |
| PAUSER | 125 | 50 / 100 | R25-GUIDE |
| RACK_LOADER | 600 | 50 / 100 | R25-GUIDE |
| RACK_UNLOADER | 600 | 50 / 100 | R25-GUIDE |
| RACK_S/M/L | 500 / 900 / 1,600 | N/A | R25-GUIDE |
| PALLET_S/M/L | 125 / 500 / 2,000 | N/A | R25-GUIDE |

R25-GUIDEの一部Scanner Lv3費用は「1,0000」と誤記されている。R25-AUTOで$10,000が明記されるので10,000として示した。ただし将来の現行版で検証が必要。

## 2. 2025記載の処理能力
| 対象 | 参考値 | 出典・信頼度 |
|---|---|---|
| Scanner Lv1 | 10秒/箱 | R25-GUIDE, R25-AUTO（中程度） |
| Scanner Lv2 | 5秒/箱 | 同 |
| Scanner Lv3 | 1秒/箱 | 同 |
| WeightClass A | 0〜5kg | R25-INSPECT |
| WeightClass B | 0〜10kg | R25-INSPECT |
| WeightClass C | 0〜15kg | R25-INSPECT |
| WeightClass D | 0〜20kg | R25-INSPECT |
| WeightClass E | 0〜30kg | R25-INSPECT |
| シリアル | 数字9桁（XXX XXX XXX形式） | R25-INSPECT |
| 保管Rack S | 8箱 | R25-GUIDE |
| 保管Rack M | 12箱 | R25-GUIDE |
| 保管Rack L | 16箱 | R25-GUIDE |
| Pallet S | 6箱 | R25-GUIDE |
| Pallet M | 12箱 | R25-GUIDE |
| Pallet L | 30箱 | R25-GUIDE |
| ベルトLv1速度 | 2.0ゲーム秒/1ユニット | R25-GUIDE コメント 2025-09-16（低信頼） |
| ベルトLv2速度 | 1.33ゲーム秒/1ユニット | 同 |
| ベルトLv3速度 | 1.0ゲーム秒/1ユニット | 同 |
| Loader速度 | 2.5ゲーム秒/箱 | 同 |
| Unloader速度 | 0.5ゲーム秒/箱 | 同 |

ゲーム内秒と現実秒、tick変換は元作の描画FPSと分離して扱う。古いガイドの秒をUnity Time.deltaTimeの現実秒に無条件置換しない。

## 3. Old倉庫の図面（2025参考）
- 初期エリア: コンベア1ユニット幅を基準に横10ユニット×縦20ユニット。
- 床タイル: 2×2ユニット。
- 壁パネル: 横5ユニット。
- 区画拡張: 1回10×10ユニット程度のエリア追加。
- 当時のガイドは全14Tier（または拡張）と記述するが、Tierの数え方・新倉庫追加後の全段階は未確認。
- 初回拡張: 重量計と重量ライセンス取得後に$700と記載。
- 2026年に新倉庫は元より面積大幅拡張（公式: 約5倍延床）、旧版のサイズから単純に全部重ねない。

## 4. 2026公式更新の確定できる数値
| 内容 | 公開値 | 根拠 |
|---|---|---|
| 新Warehouse面積 | 元倉庫のほぼ5倍の延床 | 2026-07-20公式 |
| 新倉庫のExpress分類 | 12国・地域区分 | 2026-07公式予告 |
| セーブスロット | 20 | 2026-07-20公式 |
| Pallet Build Mode解放 | Tier11 | 2026-07-30公式 |
| Auto Pauser解放 | Tier15 | 2026-07-30公式 |
| Auto Pauser価格 | $30,000 | 2026-07-30公式 |
| セーブのバックアップ | 直近5世代 | 2026-08-04公式 |
価格とTierが特定倉庫のどのカタログに属するか、全進行に当てはまるかは現行実機で確認する。

## 5. 厳禁事項
- 2025の表を2026の全ショップ最終カタログと誤記しない。
- 1/16はユーザー画像の出荷口進捗であり、「Rack Large=16」の値と混同しない。
- 2025の搬送ベルトの速度を同じ数値としてスマホ描画フレームで移動させない。
- 2026-09/10で新設備や変更があれば表を更新し、古い価格をそのまま利用しない。
- サイズ/パレット積載数/出荷ゲート数の型を混同しない。個別のUnitTypeごとに保持する。

## 6. カタログ候補の必須メタデータ
{catalogId, warehouseProfile, sourceEvidenceId, sourceGameVersion,
 price, unit, maxCapacity, unlockTier, upgradeLvCosts, processingTime,
 validatedOnCurrentBuild, provisional, notes}
currentBuild未確認のデータは全てvalidatedOnCurrentBuild=false, provisional=trueとする。
