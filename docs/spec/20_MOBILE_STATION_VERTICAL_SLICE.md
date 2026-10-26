# S0–S1 実装可能な仕様 — スマホ拠点切り替え型3D倉庫
文書ID: SPEC-20 / 版: 0.6 / 設計日: 2026-10-26
適用範囲: Codex初回開発M00/S0（Unity基盤）＋ M02/S1（タッチ操作・固定カメラ・3D画面）
Status: [D]操作方針確定、[P]細部UI/性能目標、Unity/端末テストは未実施

## 0. 今回完成させるゲーム体験
初回起動時、3D倉庫の概観が見える。
画面の[搬入]→シャッター・接車トラックの荷台を近景で表示。
[検査]→箱/検査台/ツールが正面に見える。
[出荷]→搬出口と便カテゴリ・容量モニタが見える。
[倉庫]→配置済コンベアの接続方向/ラックが俯瞰で見える。
[建設]→ゴーストをタップで選択・移動・回転/反転・キャンセルできる。
**プレイヤーキャラクターは動かさない。シーン全体をリロードせず、数回のタップで上記場所を切り替える。**
S1は「操作と映像の骨格」なので本物の箱検査/経済・トラック運行は未実装。ボタンが動いていないのに完成と報告しない。

## 1. シーンとリソース管理 — S0
- 最終プラットフォーム: iOS/Androidネイティブ。Unity 6系のサポート中LTS/URPを候補、Codexがバージョンを公式情報で確認して`ProjectVersion.txt` / `manifest.json`固定。
- 新規Unity Projectは`Assets/, Packages/, ProjectSettings/`を含み、空リポジトリのC#断片だけを「プロジェクト完成」としない。
- `Assets/Scripts/Phase1/`の旧`WarehouseBootstrap`には`[RuntimeInitializeOnLoadMethod]`があり、**新シーンを表示しても自由歩行プレイヤーが自動生成される**。旧実装を本番のAssetsから安全に隔離/条件付き無効化し、動作とgit diffで検証する。
- Input SystemはUnityの新Input Systemを第一候補とし、UI InputModuleとゲーム入力を一本化する。古いInput Managerを無理に「Both」前提としない。パッケージ・Unity版を固定。
- 仮の材質/モデルはUnity URP対応。ライトは静的、頻繁にInstantiate/Destroyしない。将来の独自高品質メッシュへ交換しやすいPrefabルート。
- アセンブリは`Warehouse.Domain`（純粋C#、後のS2）と`Warehouse.Presentation`、`Warehouse.UI`を分離する準備。S1はDomainの本実装より`ReadOnlyMockWorld`を優先してよい。
- OS安全エリア（ノッチ/ジェスチャーバー）対応、画面は暫定横向き固定。実際のiOS署名/配布にはmacOS/Xcode等のビルド・証明書環境が別途必要。
- 新シーンのランタイム初期化順: CatalogMock→WorldModelMock→3D World→StationRegistry→MainCamera→InputRouter→HUD→QualityDiagnostics。重複初期化を防ぐ。

## 2. 視点データ（StationRegistry）
```text
StationDefinition {
 stationId: string,
 category: Warehouse|Inbound|Inspection|Sorting|Storage|Export|Yard|Build|Shop|Economy|Contract|Settings,
 warehouseId: string,
 targetEntityId?: string,
 cameraAnchorId: string,
 focusEntityId?: string,
 minDistance?: float, maxDistance?: float,
 allowOrbit: bool, allowPinchZoom: bool, allowTapEntities: bool,
 viewVisibilityProfileId: string,
 uiPanelId: string,
 unlocked: bool, reasonWhenLocked: string?
}
StationSelection {
 activeStationId, activeTargetId?, previousStationId?, lastCameraPositionsByStation[],
 selectedParcelId?, handSlotParcelId?, inspectionDraftId?, constructionPreviewId?
}
```
- 実3D対象は`EntityId`で紐付け。Canvasのボタン経由、設備3Dタップ経由は同じステーションへ解決。
- 検査台や搬入口が2つ以上ある場合: 現在ターゲットがあれば維持、なければタップ対象または一覧で指定。
- アンカーの存在しないStationIdを選んでもカメラをワールド原点に飛ばさず、元のビューを保ちエラーメッセージを表示。
- 非解放機器の画面はロック表示と必要条件を提示。無反応ボタンにしない。
- Cameraは極力**1台のmainCameraを位置/向き/Profileで切り替える**。各AnchorTransformは保持する。Profileごと可視レイヤ/LOD/near-far clip/影の負荷設定を持てるが、Unityビルド/端末で効果を検証する。
- Camera切替はユーザー操作直後に開始。瞬時カットまたは[P]最大150msの短い補間。**カメラ切替に合わせて倉庫Worldを破棄・再生成しない**。一時的に黒画面を出さない。
- 切替中もGameClock/物流が動いているという設計を維持。S1にはデモ時計を出すだけでも、画面切替で時刻が0に戻らない。

## 3. 画面構成：スマホ横向き
### 共通Top HUD
- 日時/曜日、資金、正/誤の件数、Tier、次便・アラート（S1は**Fixtureの数値だと画面上でも区別**）。
- 画面上で荷物の正誤を示す場合、正否ボタンと出荷可能日を混同させない。
- アラートは横幅を占有しすぎず、タップで詳細へ。

### 下部ナビゲーション
`[搬入][検査][仕分け][保管][出荷][倉庫][トラック][建設][ショップ][経営][契約]`
- 常時全11個が1画面に入りきらない場合、頻繁な5～6項目を表示し、[メニュー]で残りを開く。**見えない項目でも2タップ以内で到達**。
- 配置は必要なら左右入替/縮尺設定。操作ボタンはiOS44pt相当/Android48dp相当のタッチ領域を目安とする。
- 下部バーで実際の3D対象の視認性を大きく損ねないこと。ノッチ/ホームバーと重ならないこと。
- 空の場面でも「未解放」「荷物未到着」「ベイ空きなし」など理由を表示。

### 画面別ActionPanel
| Station | 最低ボタン（S1では動作・選択のデモ） | S2以降の本来の機能 |
|---|---|---|
| 搬入 | ベイ選択/積載箱にフォーカス/戻る | Pickup→HandSlot→Place |
| 検査 | ラベル拡大/箱の回転/閉じる | 重量・番号・開封・シール |
| 仕分け | 接続設備の選択/情報表示 | Diverter設定/ライン停止 |
| 保管 | Rack選択/容量表示 | 入出庫/曜日待ち |
| 出荷 | Gate選択/荷物の進捗/日付パネル | 手動提出/自動出荷/誤出荷判定 |
| 倉庫 | 俯瞰/ズーム/設備選択 | 拡張/経営管理 |
| トラック | ベイ・予定選択/道路ビュー | RoadSplineと荷役状態 |
| 建設 | パーツ選択/ドラッグ/90°回転/Flip/取消 | 実インベントリ/費用/ポート接続 |
| ショップ | カテゴリ/アイテム詳細/閉じる | 注文/納品/再注文 |
| 経営 | 運営概要/License/Tierの情報枠 | 購入/アップグレード |
| 契約 | 契約/バン/条件のUI枠 | ActiveContractとDedicatedVan |
S1では本物の未実装動作をするかのような偽ボタンは禁止。将来機能は**[未実装]**と明示し無意味な成功演出をしない。

## 4. 入力ルータ
- `PointerPhase`: Down / Move / Up / Cancel。スマホのマルチタッチでPointerIdごとの所有者/ロックを保持。
- 入力優先順位: OSジェスチャーの安全領域 > モーダル > ナビ/Action > 選択設備UI > 3D Raycast > 背景パン。
- タップ3D: `SelectableEntity(entityId,stationId,interactionKind)`を取得し、その設備/箱のActionPanelへ。
- 1本指ドラッグ: 検査は箱モデル回転、建設はゴースト移動、俯瞰はカメラ平行移動。開始した対象に指をCaptureし、指が動いても他パネルのコマンドを実行しない。
- Pinch: 検査・俯瞰・建設の限定距離内でズーム。ヤード/出荷固定ビューでは不要なら無効と明示。
- 回転/反転は**目に見えるボタン**で操作、ジェスチャー必須にしない。
- 指Up/Cancel、アプリ非アクティブ時、ビュー切替時にはDragの所有状態を解除する。無効になったEntityへのイベントは拒否し、Worldを変えない。
- 画面切替タップで持っていたHandSlotを空にしない。検査中のDraftが失われない仕組みは後続Domain連携用の型を予約。

## 5. プレイヤー提供画像に相当するデモ表示
### CAP-03 搬入口
- 倉庫のシャッター/ドックパネル、トラック荷台内部、紙質感の箱が見える。箱の数はReadOnlyMockWorldのSampleParcelIdsと一致。
- 手動搬入自動化の設備接続はS1では未実装なので、箱を取得したふりをしてトラックの箱数を変えない。手動搬入機能はS2/S3で実装する。

### CAP-01 出荷口
- ゲート、金網柵、コンベア、区分別サイン、容量/正否/収益明細の表示枠がある。
- Original倉庫のCargoModeとRegionalのCountry/Expressを混ぜない。S1ではOriginalの例示。
- 上側の現在個数と下側の前回バッチSummaryは別データ欄。1/16などの原作画像例をゲーム固有の永続定数にしない。
- S1ではマニュアルPartial/AutoWhenFull/ClearDoorsの金銭処理をしない。ボタンは「実装予定」とする。

### CAP-02 建設
- 倉庫に置かれた青系の既設コンベアに対し、新しい部品の半透明の**緑色ゴースト**を重ねる。
- 選択部品の足元にグリッド位置を表示、矢印は物流のIN→OUT方向に一致。セル占有/範囲外で**赤色＋理由**。
- S1のインタラクションは表示用 `PlacementPreview`の位置/回転/反転/取消であり、**購入・搬送グラフの書込みを一切しない**。S5から本配置コマンドを連携。
- [設置]ボタンはS1時点では「プレビュー確定（デモ）」のみとし、本物の設置済み機械を増やさない。または「本設置は後のSprint」と明記。**運用データがないのに在庫を失わせない**。

## 6. 技術実装の責任分解（Codex）
- `MobileStationRegistry`: 指定IDが合法か・カメラ・操作パネルと結び付ける。
- `StationCameraController`: 1台の主カメラの位置/焦点/範囲を管理。ローカルViewStateを復元。
- `MobileInputRouter`: Pointer ID・UI/3D競合・Tap/Drag/Pinch判定。
- `SelectableEntityBinding`: 3D対象とEntityIdの関連。
- `WarehouseHud`: 時刻/資金/正否などの**読み取り専用**情報を表示。
- `ConstructionGhostPresenter`: 3Dゴースト・色・方向矢印・設置可能性を描画。S1は仮のValidationResult、S5で真のGridServiceに接続。
- `WarehouseSceneBlockout`: 出荷口/荷台/検査台/ライン/ラック/ヤードの3D区画。S1は仮モデルでも動作受入対象。
- `MobileQualityDiagnostics`: リアルのFPSとフレーム時間測定、カメラ切替回数/アクティブRenderers等の統計。ユーザーに測定済みと誤認させない。
- `PrototypeFixtureProvider`: 3D画面に出すサンプルの荷物・ゲート・稼働表示。**本番経済カタログと隔離**。

## 7. モバイル性能と画質への配慮
- 初期画面の起動は倉庫全体ではなく可視区画のグリッド＋必要なHero資材だけ。
- すべてのカメラAnchorと小型モデルは起動時に準備。通常の切替で大きな同期ロードや大量Instantiate/Destroyをしない。
- 正面の検査箱/荷台/出荷口を高品質に見せるが、遠くの箱・ラックはLODまたは描画制限。重要な検査用ラベルは2D拡大UIで可読。
- URP品質は端末Profilerで確認して段階調整。大量のRealtime Shadow、SSR、重い透明描画や全箱Rigidbodyを前提にしない。
- 30fps基準。SwitchToStationの体感は即時、**画面上のタップフィードバックは100ms以下、短い視点切替はおよそ150ms以内を暫定目標**。結果値は端末で測定してから承認。
- モード変更/1,000回拠点切替でメモリ増加・目に見えるロード・長いフレーム停止がないか計測。
- FPSデバッグはユーザー設定で表示切替可。平均だけでなくp95/p99フレーム時間をログ記録。
- 30分の端末熱テストはS1仮モデル時点と最終アート導入後の両方で必要。S1の通過だけで製品の発熱/画質合格としない。

## 8. 入力/表示/旧コードの異常系
| Error | Expected |
|---|---|
| 不正StationId | カメラ維持・エラー表示・画面操作継続 |
| TargetIdが削除された | 選択解除・所属するステーションへフォールバック |
| ゲート・ベイ未解放 | ロック表示・解放条件のプレースホルダ |
| 2本指ジェスチャー中にタップ | Pointer占有に従い、手動/建設コマンドを誤発火しない |
| OS中断/通知着信 | ポインターをCancel、ゲームPause/保存方針へ受け渡す |
| 検査中の画面切替 | 検査Draftのモデルを保持（S1は型とデモ保持） |
| 建設ゴーストをキャンセル | 位置/回転UIだけリセット、資金/実機器変化ゼロ |
| 継ぎ目で近距離カメラが建物内へ | CameraAnchorの制約で貫通しない |
| Unity Editorで旧Phase1が自動起動 | テスト失敗。LegacyBootstrapをビルド対象から隔離/無効化 |
| フレーム落ち・解像度変更 | GameState/Fixtureの値が勝手に変わらない |
| Canvas重複・Touch UI無反応 | 起動時に1セットだけ生成し、EventSystemの複数生成なし |

## 9. S0受入条件
- S0-01 Unity Editor指定版でOpen可能なProjectSettings/manifest/初期シーンがある。
- S0-02 コンパイルエラー0（Editorが使える場合に実測）。
- S0-03 旧自由歩行Bootstrap/PlayerControllerは新規シーンを再生しても生成されない。
- S0-04 CI/TestRunnerが実行可能、実行できない場合は手順をREADMEへ明記。
- S0-05 Android/iOSビルド設定を分け、プロファイルに未設定・未検証の項目を記載。
- S0-06 旧コードの重要資産やGit履歴を失わずに隔離。

## 10. S1受入条件
- S1-01 デモの倉庫・搬入・検査・出荷・俯瞰・建設が3Dとして視認できる。
- S1-02 ボタン経由で[搬入]→[検査]→[出荷]→[倉庫]→[建設]を連続切替し、**歩行アニメなし**。
- S1-03 3D設備タップ経由でも対応StationIdに切り替わり、選択したTargetIdを保持。
- S1-04 画面切替で箱ID・Mock資金・Mock時計・選択中のDraftが意図なく変わらない。
- S1-05 建設プレビューを30回移動/回転/反転/取消しても装置の確定数/資金変化ゼロ。
- S1-06 2本指とUI押下が同時でも誤操作なし。PointerCancelで確実に解除。
- S1-07 画面比率・ノッチ・Android戻る操作などでUIが欠けない（実機/シミュレータ）。
- S1-08 表示未解放/不存在/切替中にTargetが消滅してもクラッシュなし。
- S1-09 3D外観は独立アートの仮モデル。元作品の画像/モデル/ラベルをコピーしない。
- S1-10 3D表示の負荷とメモリ、切替フレーム時間のサンプルを収集。実機が無ければ`NOT_RUN`。
- S1-11 30fps/熱/ネイティブビルドはiOS/Android実機未検証なら**未達成として報告**。
- S1-12 GitHubへコード/シーン/アセット・テスト・セットアップ/端末未検証のREADMEをコミット。

## 11. S2への移行条件
- S0/S1でメニューとカメラが再現できたら、次は**トラック荷台の一箱→HandSlot→検査台→伝票チェック→承認/拒否→対応ゲートに置く→出荷提出→台帳**を最小単位で実装。
- 正確な原作価格/曜日が不足している場合は、`prototypeOnly=true`のTestCalendar/MockFeePolicyだけで先に一連の操作を検証する。
- 実際の原作同等性はSPEC-14/SPEC-17B/SPEC-18で確認、段階的にカタログ置換。
- この動作を実装前にSPEC-15/15BのOwnership不変条件、SPEC-09のSave、SPEC-11のQAケースを満たす。

## 12. 参照
- SPEC-03 UI・カメラ・タッチ
- SPEC-05 コンベア・ゴースト
- SPEC-09 データとセーブ
- SPEC-10 性能・発熱
- SPEC-12 独自アート
- SPEC-15/15B ドメイン
- SPEC-17B/18 出典/出荷ルール
- ユーザー提供画像CAP-01（出荷）、CAP-02（配置）、CAP-03（搬入）
