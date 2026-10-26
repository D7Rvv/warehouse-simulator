# 参考作品との機能照合表（2026-10-26時点）
文書ID: SPEC-01 / 状態: 調査継続 / 検証済み＝**資料上の存在確認**。全実機同等性ではない。

## 根拠ID
| ID | 根拠と期間 |
|---|---|
| S1 | Steam公式ストア『Parcel Simulator』の機能説明（確認: 2026-10-26） |
| S2 | Steam開発者ニュース 2026-07-20 新倉庫、2026-07-30 最適化、2026-09-25 追加設備、2026-09-29修正 |
| S3 | ShippoBox手動検査ガイド（2025-06-29）。2026版では変更されている可能性 |
| S4 | ShippoBox自動化ガイド（2025-06-30）。同上 |
| S5 | Steam community『Parcel Simulator Stats and Facts』（2025-06/07）。同上 |
| S6 | ユーザー提供画像3枚（IMG_0181出荷、IMG_0180設備配置、IMG_0179トラック）。画像で確認できる部分のみ |
| D1 | 今回のユーザーの確定方針（スマホ、自由移動なし、メニュー・タップ拠点切替、熱と品質） |
| P1 | 原作の細部未確認、実装用仮規則（**同等性未保証**） |

## 機能要求の一覧（REQ-ID／証拠／状態／優先度）
| REQ-ID | 再現する動作・機能 | 根拠 | スコープ |
|---|---|---|---|
| REF-001 | シャッターを開け荷物搬入開始、営業終了まで入荷 | S3 | 必須 |
| REF-002 | 手動でトラックから箱を取る、任意の場所へ運ぶ | S6/S3 | 必須 |
| REF-003 | プレチェック済の緑・赤ステッカーがある箱を識別 | S3/S4 | 必須 |
| REF-004 | 手動検査のCargo Type有効性を確認 | S3 | 必須 |
| REF-005 | 架空国名と国旗の照合 | S3 | 必須 |
| REF-006 | 重量クラスと秤の測定 | S3 | 必須 |
| REF-007 | シリアル番号の形式/認証と手入力/スキャン | S3 | 必須 |
| REF-008 | 内容物要検査の箱を開封し禁制品を判定・再梱包 | S3 | 必須 |
| REF-009 | 有効スタンプと実際の押印図柄を比較 | S3 | 必須 |
| REF-010 | Inspection Tabletの各規則/カレンダー参照 | S1/S3 | 必須 |
| REF-011 | 合格（緑）・拒否（赤）シールを確定 | S3/S6 | 必須 |
| REF-012 | Road/Rail/Air/Maritimeの輸送区分で出荷 | S3/S6 | 必須 |
| REF-013 | 拒否品専用シュート/出荷口 | S5/S6 | 必須 |
| REF-014 | 曜日ごとに一部輸送区分が受付不可 | S1/S3 | 必須 |
| REF-015 | 出荷口ごとの容量、モニタ、正誤と収支明細 | S6 | 必須 |
| REF-016 | 出荷口へ手動で入れる/満杯時に止まる | S4 | 必須 |
| REF-017 | ラックに出荷不可品を保管し再投入 | S1/S4 | 必須 |
| REF-018 | 手動出荷ボタン、Automatic Exports | S4 | 必須 |
| REF-019 | 注文とショップ、ライセンス、アップグレード | S1/S3 | 必須 |
| REF-020 | Warehouse Tierでスペース/出荷・搬入ベイを拡張 | S1/S5 | 必須 |
| REF-021 | Conveyor Deliveriesの購入で自動荷下ろし | S4 | 必須 |
| REF-022 | Straight/Corner/Bridgerの搬送路 | S4/S5 | 必須 |
| REF-023 | Joiner、Smart Splitter、分岐と合流 | S4/S5 | 必須 |
| REF-024 | Pauser/Auto Pauserによる停止・再開 | S4/S2 | 必須 |
| REF-025 | Launcherの発射/飛距離と着地点 | S5 | 後半 |
| REF-026 | Sticker Checkerの既判定シール識別 | S4/S5 | 必須 |
| REF-027 | Weight/Serial/Country/Cargo/Stamp/Contentsのスキャナ | S4/S5 | 必須 |
| REF-028 | Valid/Invalidの出口と検査待ちキュー | S4/S5 | 必須 |
| REF-029 | Contents Inspection Diverter | S4/S5 | 必須 |
| REF-030 | Approve/Deniedの自動シール貼付 | S4/S5 | 必須 |
| REF-031 | Cargo Type Diverterの区分選択 | S4 | 必須 |
| REF-032 | Loader/UnloaderとStorage Rack | S1/S4 | 必須 |
| REF-033 | サイズ別ラック・パレット | S5 | 必須 |
| REF-034 | ベルト/スキャナーのレベルアップ | S4/S5 | 必須 |
| REF-035 | 初期は狭い倉庫、Tierで建設可能範囲拡張 | S1/S5 | 必須 |
| REF-036 | 塗装や外観カスタマイズ | S1 | 後半 |
| REF-037 | 仕事日の終了と翌日開始 | S3 | 必須 |
| REF-038 | 2026第2倉庫のExpressと国別出荷 | S2 | 後半 |
| REF-039 | 国別仕分け・12区分と週次カレンダー | S2 | 後半 |
| REF-040 | プログラム可能な出口/国別ディバータ | S2 | 後半 |
| REF-041 | 二階建て倉庫とElevator Conveyor | S2 | 後半 |
| REF-042 | Speedway Conveyor | S2 | 後半 |
| REF-043 | Pallet Build Mode | S2 | 後半 |
| REF-044 | Poly Bag parcels | S2 | 後半 |
| REF-045 | Storage Rack Signs（自動/手動ラベル） | S2 | 後半 |
| REF-046 | T-Joiner Conveyor | S2 | 後半 |
| REF-047 | Shop Order History / Re-Purchase | S2 | 後半 |
| REF-048 | Country Calendar固定版/週次ランダム版切替 | S2 | 後半 |
| REF-049 | マルチセーブスロットとバックアップ | S2 | 必須 |
| REF-050 | 実績、ネコとの交流などの補助要素 | S2 | 低 |
| OWN-001 | メニュー/設備タップで3D作業場所へ即移動 | D1 | 必須 |
| OWN-002 | キャラクター自由歩行廃止 | D1 | 必須 |
| OWN-003 | 手動受取・検査・分類はタップ/ドラッグ操作で継続 | D1 | 必須 |
| OWN-004 | コンベア設置のゴースト・回転・反転・スナップ | S6/D1 | 必須 |
| OWN-005 | トラックが道路から来る、接車・荷役・退出 | D1 | 独自必須 |
| OWN-006 | iOS/Androidの持続30fps、画質と熱の両立 | D1 | 必須 |
| OWN-007 | 近景高画質、背景LOD、可視性ベースの描画 | D1 | 必須 |

### 注意：資料が教えていないこと
REF-015の「1/16」は特定画像の表示値であって、全ゲート16個固定とは確認できない。REF-025の飛距離は2025ガイドの測定推定で、現在のLauncher設定追加後とは異なる可能性。REF-020の初期寸法・Tierは2025参考値として残し、原作現行版一致は未検証。REF-050の実績/ネコは商用版の同等性では必須度を下げ、固有表現をコピーしない。

## 対照表の運用規則
- 各REQを `REFERENCE_CONFIRMED`, `APPROVED_PROVISIONAL`, `NEEDS_REFERENCE_CAPTURE`, `IMPLEMENTED_UNTESTED`, `DEVICE_VERIFIED`, `REFERENCE_MATCHED` で管理。
- 事前に検証シナリオ（画面+操作手順+期待結果+録画/ログ）を必須とする。
- 参考資料の出典が古い場合は、2026版のプレイ動画・実機確認まで `REFERENCE_MATCHED` にしない。
- カメラ操作は再現**対象外の改変点**。その代わり、作業タップ回数と作業時間を計測して、元の楽しさを維持する。

## 参照
S1 https://store.steampowered.com/app/2424010/Parcel_Simulator/
S2 https://steamcommunity.com/app/2424010/announcements/
S3 https://shippobox.com/archives/16245
S4 https://shippobox.com/archives/16262
S5 https://steamcommunity.com/sharedfiles/filedetails/?id=3509976176
