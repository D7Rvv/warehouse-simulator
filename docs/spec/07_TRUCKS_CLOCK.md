# ゲーム内時間・搬入/出荷トラック・作業の時間
文書ID: SPEC-07 / REQ: REF-001,037, OWN-005 / 状態: 独自トラック仕様[D]、基準時刻と容量の多くは[P/U]

## 1. 3種類の時間を分離
(1) `SimElapsedSeconds`: 実際にゲームの稼働が進んだシミュレーション時間。機械の「10秒で検査」はまずこの尺度で定義。明示Pauseなら進まない。
(2) `GameCalendar`: 日、曜日、時分。トラック予定や出荷曜日、営業時間を判定する時計。`GameTimeScale`（ゲーム分/1実働秒）に従って増える。
(3) `RealWallClock`: OS時刻。ログ/バックアップ時刻以外ではゲームの配送判定に利用しない。

**重大な注意**: 原作スキャナ「10秒」はガイド上のプレイ時秒数で、ゲーム内時計の10秒とは限らない。ゲーム内時間倍率は未確認。スキャナ所要時間と曜日進行速度を同じ係数で変化させない。[U]
[P初期技術値] 固定ロジック更新間隔=0.1s(10Hz)、描画は30fps以上で補間。倍率はConfigで変更可能。低FPSでもロジックを飛ばさない。大きな欠落フレームの追いつきはイベントバッチで処理。

## 2. 1日のイベント
- `DayStart`→その日のShippingCalendar/検査規則を有効化。
- `IntakeOpen`→搬入ゲートとトラック予定便を開始。
- `IncomingRunArrives`→道路から進入→待機→接車。
- `ShopOrderRunArrives`→設備発注用の別荷台。
- `OutboundReady`→出荷ゲート提出に対応し便を用意。
- `IncomingCutoff`→2025資料では17:00に新規入荷停止[R/S3@2025]。
- `DayClose`→就寝、当日サマリー、セーブ、翌日。
- 17時以前に接車した便が荷役中なら完了まで継続するかは`allowFinishAfterCutoff`[P/U]。
- スリープ中に実世界の時刻で大量進行させない。明示「翌日」を押してDayChange。
- 2026-08-04にショップ配送トラックを空にしなくても睡眠可能になった[R/S2]。

## 3. TruckRunとDock構造
```text
TruckRun {
  runId, runKind: ParcelInbound|EquipmentDelivery|OutboundPickup|Decorative,
  warehouseId, vehicleVisualType, sourceRoadId,
  expectedArrivalGameMinute, plannedDepartureGameMinute?,
  state: Scheduled|Approaching|WaitingEntry|Entering|WaitingDock|
         Docking|DoorOpening|Loading|Unloading|ReadyToLeave|
         Undocking|Exiting|Completed|Cancelled,
  dockId?, cargoParcelIds[], cargoOrderItemIds[],
  capacity, unloadRate, loadRate, routeSplineId,
  visualProgress01, lastStateChangeSimSeconds, eventVersion
}
Dock {dockId,warehouseId,isOpen,acceptedRunKinds,occupyingRunId?,
      waitingRunIds[],inputPortId?,outputPortId?,status}
```
- Scheduledの予定表からRunIdを一度だけ生成。ヤードの画面へ切替えるたびに新規トラックを生成しない。
- ベイ占有は同時に一台。満車は到着順キュー[P]で待機。
- `TruckRun`と`Shipment`のIDは別。複数ゲートの荷物を一台が回収、または1ゲートから複数トラックへ積載可能なモデルとする。
- 荷台の見える箱とParcelRepository上のIDを結ぶ`TruckLoadSlot[]`を用意。手動・自動で出すと両方1減る。
- 外部道路を走る可視化は本作独自拡張。原作の輸送時刻と混同せず設定ファイルで別管理[P]。

## 4. 荷役シーケンス
1. Schedulerが到着予定を検出、道路Spline上の車両表示を準備（可視時）または論理状態のみ更新（不可視時）。
2. Dockを予約し接車完了後に荷役許可。シャッター閉鎖/接車中は荷下ろし不可。
3. 初期は`TransferParcel(InTruck -> HandSlot)`→検査台の手動移送。トラック内残数と3D荷台から1箱減。
4. `ConveyorDeliveries`購入済みでDock出力Portとラインが接続され空きがあれば、自動`TransferParcel(InTruck -> Conveyor)`。
5. 満杯なら車内に箱を保持。接車を維持し原因を表示。画面外だからと荷物を消さない。
6. 出荷は提出済みSubmission/Shipmentの荷物を運行車両へ割当。積載予約と完了を分ける。
7. トラックの出発と配送完了は別。売上確定はSPEC-06/08の一意な提出イベントが基準。物理演出で増額しない。
8. 別の画面から戻っても同じRunIdの積載数、状態、時刻を再表示。

## 5. 視覚表現と論理の同期
- `TruckView`は`TruckRunId`で論理を読む。Tween完了コールバックに荷物消滅や報酬加算を任せない。
- ヤードの固定カメラで走行→減速→停車→ドア開閉→荷役→退出が分かる。
- 見えない車両はTransformを毎フレーム更新しないが、論理状態は進む。
- 近距離に突然スポーンしない。遠景、曲がり角、倉庫壁などの出現/退出Anchorを利用。復帰時も走行を巻き戻さない。
- 荷台の3Dモデルは必要な部分をプール表示し、表面上の箱数と所有情報を整合させる。
- 接車・荷役速度・照明・音は[P]。端末負荷/操作の楽しさ/実機で決定。
- トラック数増加は倉庫Tier、搬入口数、入出荷量、自動化速度、契約を考慮。無制限には増やさない。

## 6. 予定イベント処理
```text
AdvanceCalendar(previousMinute, currentMinute):
  due = events where dueMinute > previousMinute and <= currentMinute
  sort by (dueMinute, priority, stableEventId)
  for e in due:
    if not EventLedger.Contains(e.id):
       validatePrerequisites()
       applyAtomically(e)
       EventLedger.MarkCompleted(e.id)
```
- FPS低下/就寝/日付飛び越しでも予定時刻一致のフレームがなくてよい。
- `runId + operation + sequence`で便の荷役イベントを冪等化。
- アプリバックグラウンド中は原則ゲーム時間停止[P]。復帰時に急な大量配送をしない。
- RunId、残積載、予定、ゲート関連、CalendarWeekIndexをセーブしロード後に重複しない。
- HUDの到着予測はSchedulerと同じ時刻源から取得。

## 7. 例外/受入
| ID | 条件 | 対応 |
|---|---|---|
| TRK-EX-01 | ベイ満車で到着 | 入場待ちキューに残す |
| TRK-EX-02 | ベルト接続不良 | 自動荷下ろししない、手動継続可 |
| TRK-EX-03 | トラックに残荷/出口満杯 | 荷役停止、箱消失不可 |
| TRK-EX-04 | 出発予定超過で残荷 | `departurePolicy`[P/U]に従う、勝手に捨てない |
| TRK-EX-05 | 保存/ロード繰り返し | RunId/在庫/出発イベント重複なし |
| TRK-EX-06 | カメラ連続切替 | 同一車両・同一位置・同一積載 |
| TRK-EX-07 | 曜日変更と便到着が同時 | 規定のソート順で処理 |
| TRK-EX-08 | ショップ便に機材が残る | 2026版通り就寝を妨げない |
| TRK-EX-09 | OS時刻改ざん/オフライン | ローカルゲーム時計で継続 |

QA-TRK-01 同じ時刻で見えても見えなくても同じ一便。
QA-TRK-02 1ベイに2便到着しても接車は一台。
QA-TRK-03 10箱→手動1箱でトラック9/手持ち1。
QA-TRK-04 ベルト接続なしで自動排出されない。
QA-TRK-05 予定時刻跨ぎ/ロードで便数増殖なし。
QA-TRK-06 連続30分でも入荷・出荷総数が不変条件を満たす。
QA-TRK-07 ヤード可視中は道路上で突然ワープしない。
QA-TRK-08 FPS変動が搬送量/荷役レートを変えない。

出典 S3=https://shippobox.com/archives/16245 / S2=https://steamcommunity.com/app/2424010/announcements/
