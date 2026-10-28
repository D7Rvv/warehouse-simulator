using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Warehouse.UI
{
    public sealed class HudAction
    {
        public readonly string Id;
        public readonly string Label;
        public HudAction(string id, string label) { Id = id; Label = label; }
    }

    /// <summary>One persistent, safe-area mobile HUD. All actions are callbacks, never model writes.</summary>
    public sealed class WarehouseHud : MonoBehaviour
    {
        private readonly Color background = new Color(.035f, .065f, .09f, .95f);
        private readonly Color surface = new Color(.075f, .12f, .16f, .96f);
        private readonly Color accent = new Color(.13f, .48f, .55f, 1);
        private RectTransform safeArea;
        private RectTransform actionPanel;
        private RectTransform actionGrid;
        private RectTransform infoViewport;
        private Text header, title, information, message, diagnostics;
        private GameObject menuOverlay, detailOverlay;
        private Text detailTitle, detailBody;
        private Font font;
        private readonly List<Button> actionButtons = new List<Button>();
        private readonly List<string> actionIds = new List<string>();
        private Rect previousSafeArea;
        private int previousWidth, previousHeight;
        private float previousDpi;
        private Action<string> selectStation, performAction;
        private Action back;
        private Func<bool> allowAction;
        private Func<int> cancellationVersion;
        private Canvas rootCanvas;
        private CanvasScaler scaler;
        private RectTransform menuGrid;
        private bool initialized;
        private bool compactLayout;
        private string stationDescription = "";
        private string lastFeedback = "";
        public bool IsModalOpen { get { return detailOverlay != null && detailOverlay.activeSelf || IsMenuOpen; } }
        public bool IsMenuOpen { get { return menuOverlay != null && menuOverlay.activeSelf; } }
        public Canvas Canvas { get { return rootCanvas; } }
        public string CurrentStationText { get { return title == null ? "" : title.text; } }
        public string InformationText { get { return information == null ? "" : information.text; } }

        public void Initialize(Action<string> select, Action<string> action, Action onBack, Func<bool> canAct, Func<int> version = null)
        {
            if (initialized) return;
            initialized = true;
            selectStation = select; performAction = action; back = onBack; allowAction = canAct; cancellationVersion = version;
            font = Resources.Load<Font>("WarehouseJapanese");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rootCanvas = gameObject.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 20;
            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            gameObject.AddComponent<GraphicRaycaster>();
            safeArea = Rect("SafeArea", transform);
            Stretch(safeArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var top = Panel("TopHud", safeArea, background);
            Stretch(top, new Vector2(0, 1), Vector2.one, new Vector2(0, -60), Vector2.zero);
            var backButton = Button("Back", top, "戻る", () => { if (CanAct() && back != null) back(); });
            Stretch((RectTransform)backButton.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(6, 6), new Vector2(68, -6));
            header = Label("PrototypeStatus", top, 15, TextAnchor.MiddleLeft);
            Stretch((RectTransform)header.transform, Vector2.zero, Vector2.one, new Vector2(82, 4), new Vector2(-74, -4));
            var diagButton = Button("DiagnosticsToggle", top, "計測", () => Invoke("diagnostics"));
            Stretch((RectTransform)diagButton.transform, Vector2.right, Vector2.one, new Vector2(-68, 6), new Vector2(-6, -6));

            var nav = Panel("BottomNavigation", safeArea, background);
            Stretch(nav, Vector2.zero, Vector2.right, Vector2.zero, new Vector2(0, 62));
            var navLayout = nav.gameObject.AddComponent<HorizontalLayoutGroup>();
            navLayout.padding = new RectOffset(6, 6, 6, 6); navLayout.spacing = 5;
            navLayout.childControlWidth = true; navLayout.childForceExpandWidth = true;
            navLayout.childControlHeight = true; navLayout.childForceExpandHeight = true;
            Navigation(nav, "搬入", "LOC-INBOUND-01");
            Navigation(nav, "検査", "LOC-INSPECTION-01");
            Navigation(nav, "出荷", "LOC-EXPORT-AIR-01");
            Navigation(nav, "倉庫", "LOC-WAREHOUSE-OVERVIEW");
            Navigation(nav, "建設", "LOC-BUILD-01");
            var menuButton = Button("Menu", nav, "メニュー", ShowMenu);
            menuButton.gameObject.AddComponent<LayoutElement>().minWidth = 48;

            actionPanel = Panel("ActionPanel", safeArea, surface);
            Stretch(actionPanel, new Vector2(1, 0), Vector2.one, new Vector2(-286, 70), new Vector2(-8, -68));
            title = Label("StationTitle", actionPanel, 19, TextAnchor.UpperLeft);
            Stretch((RectTransform)title.transform, new Vector2(0, 1), Vector2.one, new Vector2(12, -35), new Vector2(-10, -8));
            infoViewport = Panel("InformationScroll", actionPanel, new Color(0, 0, 0, 0));
            Stretch(infoViewport, Vector2.zero, Vector2.one, new Vector2(12, 70), new Vector2(-10, -40));
            infoViewport.gameObject.AddComponent<RectMask2D>();
            var infoScroll = infoViewport.gameObject.AddComponent<ScrollRect>();
            infoScroll.horizontal = false; infoScroll.movementType = ScrollRect.MovementType.Clamped;
            information = Label("StationInformation", infoViewport, 14, TextAnchor.UpperLeft);
            information.horizontalOverflow = HorizontalWrapMode.Wrap;
            information.verticalOverflow = VerticalWrapMode.Overflow;
            var infoContent = (RectTransform)information.transform;
            infoContent.anchorMin = new Vector2(0, 1); infoContent.anchorMax = Vector2.one;
            infoContent.pivot = new Vector2(.5f, 1); infoContent.offsetMin = infoContent.offsetMax = Vector2.zero;
            information.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            infoScroll.content = infoContent; infoScroll.viewport = infoViewport;
            var actions = Rect("Actions", actionPanel);
            actionGrid = actions;
            Stretch(actions, Vector2.zero, Vector2.right, new Vector2(8, 8), new Vector2(-8, 166));
            var grid = actions.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(126, 48); grid.spacing = new Vector2(6, 5);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
            for (int i = 0; i < 6; i++)
            {
                int index = i;
                var button = Button("Action" + i, actions, "", () => { if (index < actionIds.Count) Invoke(actionIds[index]); });
                actionButtons.Add(button);
                button.gameObject.SetActive(false);
            }
            message = Label("Feedback", safeArea, 15, TextAnchor.LowerLeft);
            message.color = new Color(1, .88f, .53f);
            Stretch((RectTransform)message.transform, Vector2.zero, Vector2.one, new Vector2(12, 72), new Vector2(-304, -70));
            message.raycastTarget = false;
            diagnostics = Label("PerformanceDiagnostics", safeArea, 13, TextAnchor.UpperLeft);
            Stretch((RectTransform)diagnostics.transform, Vector2.zero, Vector2.one, new Vector2(12, 105), new Vector2(-304, -70));
            diagnostics.gameObject.SetActive(false);
            BuildMenu(); BuildDetails(); RefreshSafeArea();
        }

        private void Navigation(Transform parent, string label, string station)
        {
            var button = Button("Navigate-" + station, parent, label, () => Select(station));
            button.gameObject.AddComponent<LayoutElement>().minWidth = 48;
        }
        private void Select(string station)
        {
            if (!CanAct()) return;
            CloseOverlay();
            if (selectStation != null) selectStation(station);
        }
        private bool CanAct() { return allowAction == null || allowAction(); }
        private void Invoke(string id) { if (CanAct() && performAction != null) performAction(id); }
        public void SetHeader(string text) { if (header != null && header.text != text) header.text = text; }
        public void SetMessage(string text) { lastFeedback = text ?? ""; ApplyFeedback(); }
        private void ApplyFeedback()
        {
            if (message == null) return;
            message.text = compactLayout ? lastFeedback + "\n" + stationDescription : lastFeedback;
        }
        public void SetStation(string stationTitle, string description, params HudAction[] actions)
        {
            title.text = stationTitle; information.text = description; stationDescription = description ?? "";
            actionIds.Clear();
            for (int i = 0; i < actionButtons.Count; i++)
            {
                bool used = actions != null && i < actions.Length;
                actionButtons[i].gameObject.SetActive(used);
                if (!used) continue;
                actionIds.Add(actions[i].Id);
                actionButtons[i].GetComponentInChildren<Text>().text = actions[i].Label;
            }
            LayoutActions();
            infoViewport.GetComponent<ScrollRect>().verticalNormalizedPosition = 1;
        }
        private void LayoutActions()
        {
            int columns = actionIds.Count > 4 ? 3 : 2;
            int rows = Mathf.Max(1, Mathf.CeilToInt(actionIds.Count / (float)columns));
            float height = rows * 48 + (rows - 1) * 5;
            float width = -actionPanel.offsetMin.x;
            var grid = actionGrid.GetComponent<GridLayoutGroup>();
            grid.constraintCount = columns;
            grid.cellSize = new Vector2((width - 24 - (columns - 1) * 6) / columns, 48);
            actionGrid.offsetMax = new Vector2(-8, 8 + height);
            float logicalHeight = scaler == null || scaler.scaleFactor <= 0 ? Screen.height : previousSafeArea.height / scaler.scaleFactor;
            compactLayout = logicalHeight > 0 && logicalHeight < 340;
            infoViewport.gameObject.SetActive(!compactLayout);
            if (compactLayout)
            {
                // Short landscape: keep 48dp actions and show the description/reason in the left feedback area.
                // Give even the hidden viewport a positive, valid rectangle.
                infoViewport.anchorMin = new Vector2(0, 1); infoViewport.anchorMax = Vector2.one;
                infoViewport.offsetMin = new Vector2(12, -68); infoViewport.offsetMax = new Vector2(-10, -40);
            }
            else
            {
                infoViewport.anchorMin = Vector2.zero; infoViewport.anchorMax = Vector2.one;
                infoViewport.offsetMin = new Vector2(12, height + 22); infoViewport.offsetMax = new Vector2(-10, -40);
            }
            ApplyFeedback();
        }
        public void SetDiagnostics(string text, bool visible)
        {
            if (diagnostics == null) return;
            diagnostics.text = text;
            diagnostics.gameObject.SetActive(visible);
        }
        public void ShowMenu() { if (CanAct()) { if (performAction != null) performAction("menu-opened"); detailOverlay.SetActive(false); menuOverlay.SetActive(true); } }
        public void ShowDetails(string heading, string body)
        {
            menuOverlay.SetActive(false); detailTitle.text = heading; detailBody.text = body;
            detailOverlay.SetActive(true);
            detailBody.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 1;
        }
        public bool CloseOverlay()
        {
            bool had = IsModalOpen;
            if (menuOverlay != null) menuOverlay.SetActive(false);
            if (detailOverlay != null) detailOverlay.SetActive(false);
            return had;
        }
        private void BuildMenu()
        {
            var overlay = Panel("MoreStations", safeArea, new Color(.015f, .03f, .04f, .96f));
            Stretch(overlay, Vector2.zero, Vector2.one, new Vector2(10, 68), new Vector2(-10, -65));
            menuOverlay = overlay.gameObject;
            var gridRoot = Rect("MenuGrid", overlay);
            menuGrid = gridRoot;
            Stretch(gridRoot, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-264, -105), new Vector2(264, 105));
            var grid = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(170, 60); grid.spacing = new Vector2(9, 9);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
            Navigation(gridRoot, "仕分け", "LOC-SORTING-01"); Navigation(gridRoot, "保管", "LOC-STORAGE-01");
            Navigation(gridRoot, "トラック", "LOC-YARD-01"); Navigation(gridRoot, "ショップ", "LOC-SHOP");
            Navigation(gridRoot, "経営", "LOC-ECONOMY"); Navigation(gridRoot, "契約（未解放）", "LOC-CONTRACT");
            Button("ExportDiagnostics", gridRoot, "計測CSV保存", () => Invoke("diagnostics-export"));
            Button("ToggleDiagnostics", gridRoot, "計測表示", () => Invoke("diagnostics"));
            Button("CloseMenu", gridRoot, "閉じる", () => Invoke("close-details"));
            menuOverlay.SetActive(false);
        }
        private void BuildDetails()
        {
            var overlay = Panel("Details", safeArea, new Color(.015f, .03f, .04f, .98f));
            Stretch(overlay, Vector2.zero, Vector2.one, new Vector2(18, 70), new Vector2(-18, -68));
            detailOverlay = overlay.gameObject;
            detailTitle = Label("DetailTitle", overlay, 23, TextAnchor.UpperLeft);
            Stretch((RectTransform)detailTitle.transform, new Vector2(0, 1), Vector2.one, new Vector2(20, -52), new Vector2(-20, -15));
            var viewport = Panel("DetailScroll", overlay, new Color(0, 0, 0, 0));
            Stretch(viewport, Vector2.zero, Vector2.one, new Vector2(20, 68), new Vector2(-20, -58));
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            detailBody = Label("DetailText", viewport, 19, TextAnchor.UpperLeft);
            detailBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            detailBody.verticalOverflow = VerticalWrapMode.Overflow;
            var content = (RectTransform)detailBody.transform;
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            detailBody.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            var close = Button("CloseDetails", overlay, "閉じる", () => Invoke("close-details"));
            Stretch((RectTransform)close.transform, Vector2.zero, Vector2.right, new Vector2(20, 10), new Vector2(-20, 58));
            detailOverlay.SetActive(false);
        }
        private void Update() { if (initialized) RefreshSafeArea(); }
        private void RefreshSafeArea()
        {
            Rect area = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            if (previousSafeArea == area && previousWidth == Screen.width && previousHeight == Screen.height && Mathf.Approximately(previousDpi, Screen.dpi)) return;
            previousSafeArea = area; previousWidth = Screen.width; previousHeight = Screen.height; previousDpi = Screen.dpi;
            // Logical dp scaling, not fixed pixel buttons. Representative phones must still be measured.
            float scale = Screen.dpi >= 100 ? Screen.dpi / 160f : Mathf.Max(1, Screen.height / 540f);
            scaler.scaleFactor = Mathf.Max(1, scale);
            rootCanvas.scaleFactor = scaler.scaleFactor;
            safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            float logicalWidth = area.width / rootCanvas.scaleFactor;
            float panelWidth = Mathf.Clamp(logicalWidth * .33f, 248, 286);
            actionPanel.offsetMin = new Vector2(-panelWidth, 70);
            LayoutActions();
            float logicalHeight = area.height / rootCanvas.scaleFactor;
            float menuWidth = Mathf.Min(528, logicalWidth - 24);
            float menuHeight = Mathf.Max(162, Mathf.Min(210, logicalHeight - 145));
            menuGrid.offsetMin = new Vector2(-menuWidth / 2, -menuHeight / 2);
            menuGrid.offsetMax = new Vector2(menuWidth / 2, menuHeight / 2);
            menuGrid.GetComponent<GridLayoutGroup>().cellSize = new Vector2((menuWidth - 18) / 3, (menuHeight - 18) / 3);
        }
        private RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
        private RectTransform Panel(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent); var image = rect.gameObject.AddComponent<Image>(); image.color = color;
            return rect;
        }
        private Text Label(string name, Transform parent, int size, TextAnchor anchor)
        {
            var rect = Rect(name, parent); var text = rect.gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = size; text.color = new Color(.9f, .94f, .94f);
            text.alignment = anchor; text.raycastTarget = false; return text;
        }
        private Button Button(string name, Transform parent, string label, Action action)
        {
            var rect = Panel(name, parent, surface);
            var button = rect.gameObject.AddComponent<GuardedMobileButton>();
            button.Configure(CanAct, () => cancellationVersion == null ? 0 : cancellationVersion());
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(.6f, .88f, .95f); colors.pressedColor = accent; button.colors = colors;
            var text = Label("Text", rect, 16, TextAnchor.MiddleCenter); text.text = label;
            Stretch((RectTransform)text.transform, Vector2.zero, Vector2.one, new Vector2(5, 2), new Vector2(-5, -2));
            button.onClick.AddListener(() => action()); return button;
        }
        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax; }
    }
}
