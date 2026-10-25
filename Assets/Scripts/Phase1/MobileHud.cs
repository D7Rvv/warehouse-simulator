using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WarehouseSimulator.Phase1
{
    /// <summary>
    /// Runtime safe-area-aware touch UI, avoids scene setup and external dependencies.
    /// Movement stick and look drag handle separate pointer IDs.
    /// </summary>
    public sealed class MobileHud : MonoBehaviour
    {
        private MobilePlayerController _player;
        private AdaptiveMobileQuality _quality;
        private RectTransform _canvasRect;
        private RectTransform _stick;
        private RectTransform _knob;
        private RectTransform _lookZone;
        private Text _modeLabel;
        private Text _fpsLabel;
        private int _stickPointer = -1;
        private int _lookPointer = -1;
        private Vector2 _lookLast;
        private float _fpsElapsed;
        private int _fpsFrames;

        public static void Create(Transform parent, MobilePlayerController player, AdaptiveMobileQuality quality)
        {
            var obj = new GameObject("TouchHUD");
            obj.transform.SetParent(parent, false);
            var hud = obj.AddComponent<MobileHud>();
            hud._player = player;
            hud._quality = quality;
            hud.Build();
        }

        public static bool IsPointerOnHud(Vector2 screenPos)
        {
            // For mouse controls: keep the bottom on-screen controls available.
            return screenPos.y < Screen.height * .25f;
        }

        private void Build()
        {
            var canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform, false);
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasObj.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(960, 540);
            canvasObj.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasObj.GetComponent<CanvasScaler>().matchWidthOrHeight = .5f;
            _canvasRect = canvasObj.GetComponent<RectTransform>();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventObj = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_LEGACY_INPUT_MANAGER
                eventObj.AddComponent<StandaloneInputModule>();
#else
                // For Input System-only projects, install the Input System UI module in the scene.
                // Phase 1 recommends Player Settings > Active Input Handling = Both.
#endif
            }

            var safeArea = Panel("SafeArea", _canvasRect);
            Stretch(safeArea);
            var safeFitter = safeArea.gameObject.AddComponent<SafeAreaFitter>();

            var top = Label("ProjectTitle", safeArea, "WAREHOUSE SIMULATOR  /  PHASE 1", 19, TextAnchor.UpperLeft);
            Place(top.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -16f), new Vector2(460f, 42f));
            _fpsLabel = Label("FPS", safeArea, "30 FPS TARGET", 15, TextAnchor.UpperRight);
            Place(_fpsLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -14f), new Vector2(250f, 45f));

            var look = Panel("LookZone", safeArea);
            // Right half: swipe-to-look; under the view-mode button.
            look.anchorMin = new Vector2(.48f, 0f);
            look.anchorMax = new Vector2(1f, 1f);
            look.offsetMin = Vector2.zero;
            look.offsetMax = Vector2.zero;
            _lookZone = look;
            var lookImage = look.gameObject.AddComponent<Image>();
            lookImage.color = new Color(0f, 0f, 0f, 0f);
            lookImage.raycastTarget = true;
            var lookEvents = look.gameObject.AddComponent<EventTrigger>();
            AddTrigger(lookEvents, EventTriggerType.PointerDown, e => { var p = (PointerEventData)e; if (_lookPointer != -1) return; _lookPointer = p.pointerId; _lookLast = p.position; });
            AddTrigger(lookEvents, EventTriggerType.Drag, e => { var p = (PointerEventData)e; if (_lookPointer != p.pointerId) return; var delta = p.position - _lookLast; _lookLast = p.position; _player.LookDelta(delta * new Vector2(1f, 1f)); });
            AddTrigger(lookEvents, EventTriggerType.PointerUp, e => { var p = (PointerEventData)e; if (_lookPointer == p.pointerId) _lookPointer = -1; });
            AddTrigger(lookEvents, EventTriggerType.EndDrag, e => { var p = (PointerEventData)e; if (_lookPointer == p.pointerId) _lookPointer = -1; });

            _stick = Panel("MoveStick", safeArea);
            Place(_stick, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(110f, 113f), new Vector2(160f, 160f));
            var ring = _stick.gameObject.AddComponent<Image>();
            ring.color = new Color(1f, 1f, 1f, .17f);
            _knob = Panel("StickKnob", _stick);
            Place(_knob, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(67f, 67f));
            var knobImage = _knob.gameObject.AddComponent<Image>();
            knobImage.color = new Color(1f, 1f, 1f, .45f);
            var stickEvents = _stick.gameObject.AddComponent<EventTrigger>();
            AddTrigger(stickEvents, EventTriggerType.PointerDown, e => { var p = (PointerEventData)e; if (_stickPointer != -1) return; _stickPointer = p.pointerId; UpdateStick(p); });
            AddTrigger(stickEvents, EventTriggerType.Drag, e => { var p = (PointerEventData)e; if (_stickPointer == p.pointerId) UpdateStick(p); });
            AddTrigger(stickEvents, EventTriggerType.PointerUp, e => { var p = (PointerEventData)e; if (_stickPointer == p.pointerId) ReleaseStick(); });
            AddTrigger(stickEvents, EventTriggerType.EndDrag, e => { var p = (PointerEventData)e; if (_stickPointer == p.pointerId) ReleaseStick(); });

            var buttonObj = new GameObject("SwitchViewButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(safeArea, false);
            var rect = buttonObj.GetComponent<RectTransform>();
            Place(rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-118f, 62f), new Vector2(203f, 62f));
            buttonObj.GetComponent<Image>().color = new Color(.93f, .38f, .16f);
            var button = buttonObj.GetComponent<Button>();
            button.onClick.AddListener(() => { _player.ToggleView(); _modeLabel.text = _player.IsOverhead ? "一人称へ" : "俯瞰へ"; });
            _modeLabel = Label("ModeText", rect, "俯瞰へ", 21, TextAnchor.MiddleCenter);
            Stretch(_modeLabel.rectTransform);
            _modeLabel.raycastTarget = false;

            var hint = Label("Hint", safeArea, "左：移動    右：視点    ボタン：切替", 14, TextAnchor.MiddleCenter);
            Place(hint.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 18f), new Vector2(350f, 28f));
            hint.raycastTarget = false;
        }

        private void UpdateStick(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_stick, e.position, e.pressEventCamera, out var local);
            var delta = Vector2.ClampMagnitude(local / 60f, 1f);
            _knob.anchoredPosition = delta * 47f;
            _player.SetMove(delta);
        }

        private void ReleaseStick()
        {
            _stickPointer = -1;
            _knob.anchoredPosition = Vector2.zero;
            _player.SetMove(Vector2.zero);
        }

        private void Update()
        {
            _fpsElapsed += Time.unscaledDeltaTime;
            _fpsFrames++;
            if (_fpsElapsed < 1.5f) return;
            var fps = Mathf.RoundToInt(_fpsFrames / _fpsElapsed);
            _fpsLabel.text = fps.ToString() + " FPS  |  " + _quality.CurrentScale.ToString("0.00") + "x";
            _fpsElapsed = 0f;
            _fpsFrames = 0;
        }

        private static RectTransform Panel(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj.GetComponent<RectTransform>();
        }

        private static Text Label(string name, Transform parent, string content, int size, TextAnchor anchor)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var label = obj.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = content;
            label.fontSize = size;
            label.alignment = anchor;
            label.color = Color.white;
            return label;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = (anchorMin + anchorMax) / 2f;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private sealed class SafeAreaFitter : MonoBehaviour
        {
            private Rect _last;
            private RectTransform _rect;
            private void Awake() { _rect = GetComponent<RectTransform>(); Apply(); }
            private void Update() { if (_last != Screen.safeArea) Apply(); }
            private void Apply()
            {
                _last = Screen.safeArea;
                if (Screen.width < 1 || Screen.height < 1) return;
                _rect.anchorMin = new Vector2(_last.xMin / Screen.width, _last.yMin / Screen.height);
                _rect.anchorMax = new Vector2(_last.xMax / Screen.width, _last.yMax / Screen.height);
                _rect.offsetMin = Vector2.zero;
                _rect.offsetMax = Vector2.zero;
            }
        }
    }
}
