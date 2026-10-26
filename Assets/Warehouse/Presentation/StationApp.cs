using System;
using System.Collections.Generic;
using System.Diagnostics;
using Warehouse.Domain;
using Warehouse.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Debug = UnityEngine.Debug;

namespace Warehouse.Presentation
{
    /// <summary>S0/S1 composition root. UI and camera commands cannot change the read-only mock world.</summary>
    public sealed class StationApp : MonoBehaviour
    {
        public static StationApp Instance { get; private set; }
        public ReadOnlyMockWorld World { get; private set; }
        public MobileStationRegistry Registry { get; private set; }
        public PlacementPreview Preview { get; private set; }
        public WarehouseSceneBlockout Blockout { get; private set; }
        public StationCameraController CameraController { get; private set; }
        public ConstructionGhostPresenter Ghost { get; private set; }
        public MobileInputRouter Input { get; private set; }
        public WarehouseHud Hud { get; private set; }
        public MobileQualityDiagnostics Diagnostics { get; private set; }
        public bool IsReady { get; private set; }
        public double DemoElapsedSeconds { get; private set; }
        private bool suspended;
        private double nextHudRefresh;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
        private void Start()
        {
            if (Instance != this) return;
            try { Initialize(); }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Hud == null)
                {
                    var errorHud = new GameObject("WarehouseHUD", typeof(RectTransform));
                    errorHud.transform.SetParent(transform, false);
                    Hud = errorHud.AddComponent<WarehouseHud>();
                    Hud.Initialize(null, null, null, null);
                }
                Hud.SetHeader("S0/S1 起動エラー・試作データを安全に拒否しました");
                Hud.ShowDetails("起動できません", exception.Message);
                enabled = false;
            }
        }
        private void Initialize()
        {
            if (IsReady) return;
            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 0;
            var fixture = Resources.Load<TextAsset>("Prototype/prototype_station_fixture_v0_1");
            if (fixture == null) throw new InvalidOperationException("Prototype fixture is missing.");
            World = new ReadOnlyMockWorld(JsonUtility.FromJson<PrototypeFixtureData>(fixture.text));
            Preview = new PlacementPreview(World.Ghost);
            Blockout = new GameObject("WarehouseWorld").AddComponent<WarehouseSceneBlockout>();
            Blockout.transform.SetParent(transform, false);
            Blockout.Initialize(World.TruckRuns[0].CargoParcelIds);
            Registry = new MobileStationRegistry(World.Stations, World.InitialStationId, id => Blockout.GetAnchor(id) != null);
            foreach (var binding in Blockout.Entities)
                Registry.RegisterTarget(binding.EntityId, binding.StationId);
            Registry.Selection.SelectedParcelId = World.Parcels[0].Id;
            Registry.Selection.InspectionDraftId = "VIEW-DRAFT-" + World.Parcels[0].Id;
            Registry.Selection.ConstructionPreviewId = "VIEW-PREVIEW-001";

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera"; cameraObject.transform.SetParent(transform, false);
                mainCamera = cameraObject.GetComponent<Camera>();
            }
            CameraController = gameObject.AddComponent<StationCameraController>();
            CameraController.Initialize(mainCamera, Blockout);
            Ghost = gameObject.AddComponent<ConstructionGhostPresenter>();
            Ghost.Initialize(Blockout.transform);
            Input = gameObject.AddComponent<MobileInputRouter>();
            Input.Initialize(mainCamera);
            EnsureEventSystem();
            var hudObject = new GameObject("WarehouseHUD", typeof(RectTransform));
            hudObject.transform.SetParent(transform, false);
            Hud = hudObject.AddComponent<WarehouseHud>();
            Hud.Initialize(id => TryNavigate(id), HandleAction, Back, () => Input.CanInvokeUiAction, () => Input.CancellationVersion);
            Input.IsUiBlocked = position => Hud.IsModalOpen;
            Input.OnEntityTap = OnEntityTap;
            Input.OnDrag = OnDrag;
            Input.OnPinch = delta => { if (!Hud.IsModalOpen) CameraController.Zoom(delta); };
            Input.OnBack = Back;
            Diagnostics = gameObject.AddComponent<MobileQualityDiagnostics>();
            Diagnostics.Initialize(() => Blockout.ActiveRendererCount);
            IsReady = true;
            if (!TryNavigate(World.InitialStationId)) throw new InvalidOperationException("Initial station has no camera.");
            Hud.SetMessage("試作操作デモ：場所をタップで切替。物流・購入・収益は未実装です。");
            UpdateHeader();
        }
        private void EnsureEventSystem()
        {
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem));
                go.transform.SetParent(transform, false);
                eventSystem = go.GetComponent<EventSystem>();
            }
            var oldInputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (oldInputModule != null) { oldInputModule.enabled = false; Destroy(oldInputModule); }
            var module = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (module == null) module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }
        public bool TryNavigate(string stationId, string targetId = null)
        {
            if (!IsReady) return false;
            var timer = Stopwatch.StartNew();
            string reason;
            if (!Registry.TrySelect(stationId, targetId, out reason))
            { Hud.SetMessage(reason); return false; }
            Input.CancelAll();
            Hud.CloseOverlay();
            if (!CameraController.TrySwitch(stationId, Registry.Current.CameraAnchor, out reason))
            { Hud.SetMessage(reason); return false; }
            var parcel = FindParcel(targetId);
            if (parcel != null) Registry.Selection.SelectedParcelId = parcel.Id;
            Blockout.SetInspectionRotation(Registry.Selection.InspectionRotationDegrees);
            var displayParcel = FindParcel(Registry.Selection.SelectedParcelId) ?? World.Parcels[0];
            Blockout.SetInspectionLabel(displayParcel.Id, displayParcel.Cargo, displayParcel.CountryName, displayParcel.FlagId, displayParcel.MassGrams);
            Input.SetMode(InputMode(Registry.Current.Category));
            RefreshGhost(); RefreshStation();
            if (Registry.Current.Category == "Inspection" && Registry.Selection.LabelExpanded) ShowLabel();
            timer.Stop(); Diagnostics.RecordStationSwitch(timer.Elapsed.TotalMilliseconds);
            Hud.SetMessage("試作データ：場所の切替は荷物・資金・時計を変更しません。");
            return true;
        }
        private static StationInputMode InputMode(string category)
        {
            if (category == "Inspection") return StationInputMode.Inspection;
            if (category == "Warehouse") return StationInputMode.Overview;
            if (category == "Build") return StationInputMode.Build;
            return StationInputMode.Fixed;
        }
        private void OnEntityTap(string entityId, string stationId)
        {
            if (Hud.IsModalOpen) return;
            SelectableEntityBinding binding;
            if (!Blockout.TryGetEntity(entityId, out binding) || binding == null)
            { Registry.TargetRemoved(entityId); Hud.SetMessage("対象は現在利用できません。"); return; }
            if (TryNavigate(stationId, entityId)) CameraController.Focus(entityId);
        }
        private void OnDrag(Vector2 delta, Vector2 screenPosition)
        {
            if (!IsReady || Hud.IsModalOpen) return;
            if (Registry.Current.Category == "Inspection")
            {
                Registry.Selection.InspectionRotationDegrees = Mathf.Repeat(Registry.Selection.InspectionRotationDegrees + delta.x * .4f, 360);
                Blockout.SetInspectionRotation(Registry.Selection.InspectionRotationDegrees);
            }
            else if (Registry.Current.Category == "Build")
            {
                var plane = new Plane(Vector3.up, Vector3.zero);
                Ray ray = CameraController.MainCamera.ScreenPointToRay(screenPosition);
                float enter;
                if (plane.Raycast(ray, out enter))
                {
                    Vector3 hit = ray.GetPoint(enter);
                    Preview.MoveTo(Mathf.RoundToInt(hit.x), Mathf.RoundToInt(hit.z));
                    RefreshGhost(); RefreshStation();
                }
            }
            else if (Registry.Current.Category == "Warehouse") CameraController.Pan(delta);
        }
        public void HandleAction(string id)
        {
            if (!IsReady) return;
            switch (id)
            {
                case "menu-opened": Input.CancelAll(); break;
                case "close-details":
                    Input.CancelAll(); Hud.CloseOverlay(); Registry.Selection.LabelExpanded = false; break;
                case "label": ShowLabel(); break;
                case "rotate-parcel":
                    Registry.Selection.InspectionRotationDegrees = Mathf.Repeat(Registry.Selection.InspectionRotationDegrees + 90, 360);
                    Blockout.SetInspectionRotation(Registry.Selection.InspectionRotationDegrees); break;
                case "focus-cargo":
                    if (World.TruckRuns[0].CargoParcelIds.Count > 0)
                    {
                        string parcel = World.TruckRuns[0].CargoParcelIds[0];
                        TryNavigate("LOC-INBOUND-01", parcel); CameraController.Focus(parcel);
                    }
                    break;
                case "bay": ShowDetails("搬入ベイ DEV-DOCK-01", "接車済みの試作用トラック\n積載：" + World.TruckRuns[0].CargoParcelIds.Count + "箱\n箱の移送・荷卸しは未実装。荷物の所属は変更しません。"); break;
                case "gate": ShowDetails("出荷口 " + World.Gate.GateId, GateDetails()); break;
                case "calendar": ShowDetails("出荷日カレンダー", "AIRの全曜日条件は未確定 [U]\n本Sprintでは出荷判定・予約・収益計上を行いません。\n試作用の固定時刻：" + World.Weekday + " " + World.DisplayTime); break;
                case "preview-reset": Preview.Reset(); RefreshGhost(); RefreshStation(); break;
                case "preview-rotate": Preview.Rotate(); RefreshGhost(); RefreshStation(); break;
                case "preview-flip": Preview.Flip(); RefreshGhost(); RefreshStation(); break;
                case "preview-cancel": Preview.Cancel(); RefreshGhost(); RefreshStation(); break;
                case "preview-confirm": Hud.SetMessage("プレビュー確認のみ。本設置・在庫・購入・搬送グラフは後のSprintです。"); break;
                case "zoom-in": CameraController.Zoom(30); break;
                case "zoom-out": CameraController.Zoom(-30); break;
                case "sorting": ShowDetails("ライン情報・試作", "青色コンベアは表示用です。\n接続設備の選択は3Dタップ。搬送・Diverter設定は未実装です。"); break;
                case "storage": ShowDetails("保管ラック・試作", "ラックの選択と3D表示のデモです。\n実際の保管枠、入出庫、曜日待ちは未実装・未確定です。"); break;
                case "yard": ShowDetails("接車・道路ビュー", "DEV-RUN-001：DOCKED_VISUAL_SAMPLE\n道路運行・予約・荷役スケジュールは未実装です。"); break;
                case "shop": ShowDetails("設備カタログ枠", "試作直線コンベアの表示のみ。\n本番価格・在庫・注文・納品は未実装です。"); break;
                case "economy": ShowDetails("運営概要・試作データ", "残高 $" + World.CurrencyUSD + " ／ Tier " + World.Tier + "\nFixture表示のみ。License、購入、Bills、収益処理は未実装です。"); break;
                case "diagnostics": Diagnostics.DebugVisible = !Diagnostics.DebugVisible; break;
                case "diagnostics-export": Hud.SetMessage("計測CSV：" + Diagnostics.WriteCsv()); break;
                default: Hud.SetMessage("この機能はS0/S1では未実装です。"); break;
            }
        }
        private void ShowDetails(string heading, string body)
        { Input.CancelAll(); Hud.ShowDetails(heading, body); }
        private void ShowLabel()
        {
            var parcel = FindParcel(Registry.Selection.SelectedParcelId) ?? World.Parcels[0];
            Registry.Selection.LabelExpanded = true;
            ShowDetails("伝票拡大・試作データ", parcel.Id + "\n区分：" + parcel.Cargo + "    国名：" + parcel.CountryName + "\n旗ID：" + parcel.FlagId + "    重量：" + parcel.MassGrams + " g\n表示用見本。真偽の判定・承認・ステッカー貼付は未実装です。\n所有先：" + parcel.OwnerKind + " / " + parcel.OwnerEntityId);
        }
        private string GateDetails()
        {
            var gate = World.Gate; var last = gate.LastBatchSummary;
            return "現在のバッファ：" + gate.CurrentBufferedCount + "/" + gate.MaxCapacity + "（例示）\n前回バッチ：正 " + last.CorrectCount + " ／ 誤 " + last.IncorrectCount + "\n正箱価値 $" + last.CorrectParcelValueUSD + " ／ 手数料 $" + last.HandlingFeeUSD + "\n前回純額 $" + last.NetUSD + "（現在個数とは別データ）\n手動提出・満杯時自動・終業一括は未実装。収益を加算しません。";
        }
        private ParcelSnapshot FindParcel(string id)
        {
            if (id == null || World == null) return null;
            foreach (var parcel in World.Parcels) if (parcel.Id == id) return parcel;
            return null;
        }
        private void RefreshGhost()
        {
            bool visible = Registry.Current.Category == "Build" && Preview.Active;
            Ghost.Render(Preview.X, Preview.Z, Preview.RotationQuarterTurns, Preview.Flipped, Preview.IsValid, Preview.InvalidReason, visible);
        }
        private void RefreshStation()
        {
            string target = Registry.Selection.ActiveTargetId;
            string suffix = string.IsNullOrEmpty(target) ? "" : "\n選択：" + target;
            switch (Registry.Current.Category)
            {
                case "Inbound": Hud.SetStation("搬入・荷台", "DEV-DOCK-01 ／ " + World.TruckRuns[0].CargoParcelIds.Count + "箱\n積載箱をタップして選択。\n移送・荷卸しは未実装。" + suffix, A("bay", "ベイ情報"), A("focus-cargo", "積載箱を見る")); break;
                case "Inspection": Hud.SetStation("検査台", "表示用の荷物見本をドラッグで回転。\n移送・検査の合否判定は未実装。\n" + Registry.Selection.SelectedParcelId, A("label", "伝票を拡大"), A("rotate-parcel", "箱を90°回転")); break;
                case "Export": Hud.SetStation("出荷・AIR", "現在 " + World.Gate.CurrentBufferedCount + "/" + World.Gate.MaxCapacity + "（例示）\n前回：正" + World.Gate.LastBatchSummary.CorrectCount + " ／ 純額 $" + World.Gate.LastBatchSummary.NetUSD + "\n提出・収益計上は未実装。" + suffix, A("gate", "進捗・前回明細"), A("calendar", "日付パネル")); break;
                case "Build": Hud.SetStation("建設・表示プレビュー", Preview.InvalidReason + "\n格子 (" + Preview.X + ", " + Preview.Z + ") ／ " + Preview.RotationQuarterTurns * 90 + "°", A("preview-reset", "直線"), A("preview-rotate", "回転"), A("preview-flip", "反転"), A("preview-cancel", "取消"), A("preview-confirm", "確認")); break;
                case "Warehouse": Hud.SetStation("倉庫・俯瞰", "ドラッグでパン、2本指でズーム。\n設備をタップして作業場所を選択。\n箱・設備は表示用です。" + suffix, A("zoom-in", "拡大＋"), A("zoom-out", "縮小−")); break;
                case "Sorting": Hud.SetStation("仕分け", "接続設備をタップして情報表示。\n自動搬送は未実装。" + suffix, A("sorting", "ライン情報")); break;
                case "Storage": Hud.SetStation("保管", "ラックをタップして選択。\n入出庫・容量判定は未実装。" + suffix, A("storage", "ラック情報")); break;
                case "Yard": Hud.SetStation("トラックヤード", "接車中の試作トラックと道路。\n運行スケジュールは未実装。" + suffix, A("yard", "ベイ・予定")); break;
                case "Shop": Hud.SetStation("ショップ・情報枠", "カテゴリ・設備の表示用見本。\n購入・納品・価格設定は未実装。", A("shop", "設備カテゴリ")); break;
                case "Economy": Hud.SetStation("経営・情報枠", "License / Tierの情報枠。\n購入・収益・Billsは未実装。", A("economy", "運営概要")); break;
                default: Hud.SetStation("未実装の作業場所", "後のSprintで追加します。"); break;
            }
        }
        private static HudAction A(string id, string label) { return new HudAction(id, label); }
        private void Back()
        {
            if (!IsReady) return;
            Input.CancelAll();
            if (Hud.CloseOverlay()) { Registry.Selection.LabelExpanded = false; return; }
            string previous = Registry.Selection.PreviousStationId;
            TryNavigate(string.IsNullOrEmpty(previous) ? World.InitialStationId : previous);
        }
        private void Update()
        {
            if (!IsReady || suspended) return;
            DemoElapsedSeconds += Time.unscaledDeltaTime;
            if (DemoElapsedSeconds < nextHudRefresh) return;
            nextHudRefresh = DemoElapsedSeconds + .5;
            string target = Registry.Selection.ActiveTargetId;
            SelectableEntityBinding binding;
            if (!string.IsNullOrEmpty(target) && (!Blockout.TryGetEntity(target, out binding) || binding == null))
            { Registry.TargetRemoved(target); RefreshStation(); Hud.SetMessage("対象がなくなったため、作業場所の表示へ戻りました。"); }
            UpdateHeader();
        }
        private void UpdateHeader()
        {
            int elapsed = (int)DemoElapsedSeconds;
            Hud.SetHeader("試作データ [P]　Day " + World.Day + " " + World.Weekday + " " + World.DisplayTime + "（固定）\n$" + World.CurrencyUSD + "  Tier " + World.Tier + "  正/誤：未集計　次便：未実装　表示経過 " + elapsed / 60 + ":" + (elapsed % 60).ToString("00"));
        }
        private void OnApplicationPause(bool paused) { suspended = paused; if (Input != null) Input.CancelAll(); }
        private void OnApplicationFocus(bool focused) { if (!focused && Input != null) Input.CancelAll(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
