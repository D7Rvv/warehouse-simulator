using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Warehouse.Presentation
{
    public readonly struct StationEntityHit
    {
        public readonly string EntityId;
        public readonly string StationId;
        public StationEntityHit(string entityId, string stationId)
        { EntityId = entityId; StationId = stationId; }
    }

    /// <summary>
    /// The sole world-input adapter. uGUI remains on InputSystemUIInputModule.
    /// No walking, legacy InputManager or physical parcel command is emitted.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class MobileInputRouter : MonoBehaviour
    {
        const int MousePointerId = -1;
        readonly List<RaycastResult> uiHits = new List<RaycastResult>(24);
        readonly Dictionary<string, SelectableEntityBinding> capturedEntities =
            new Dictionary<string, SelectableEntityBinding>();
        StationPointerOwnership ownership;
        PointerEventData uiEvent;
        EventSystem cachedEventSystem;
        Camera stationCamera;
        bool mouseHeld;
        Vector2 mousePosition;
        bool initialized;
        StationInputMode currentMode;

        public Func<Vector2, bool> IsUiBlocked;
        public Func<Vector2, StationEntityHit?> HitTest;
        public Func<string, bool> EntityIsValid;
        public Action<string, string> OnEntityTap;
        public Action<Vector2, Vector2> OnDrag;
        public Action<float> OnPinch;
        public Action OnBack;
        public int ActivePointerCount { get { return Ownership.Count; } }
        public int CapturedWorldPointerCount { get { return Ownership.WorldCount; } }
        public bool IsPinching { get { return Ownership.IsPinching; } }
        public bool CanInvokeUiAction { get { return Ownership.CanInvokeUiAction; } }
        public int CancellationVersion { get { return Ownership.CancellationVersion; } }

        public StationUiPressToken CaptureUiPressEligibility() { return Ownership.CaptureUiPressEligibility(); }
        public bool IsUiPressAllowed(StationUiPressToken token) { return Ownership.IsUiPressAllowed(token); }

        StationPointerOwnership Ownership
        {
            get
            {
                if (ownership == null)
                {
                    float scale = Screen.dpi > 0 ? Mathf.Clamp(Screen.dpi / 160f, 1, 4) : 1;
                    ownership = new StationPointerOwnership(12 * scale);
                    ownership.OnEntityTap = DispatchEntityTap;
                    ownership.OnDrag = (dx, dy, x, y) => {
                        if (OnDrag != null) OnDrag(new Vector2(dx, dy), new Vector2(x, y));
                    };
                    ownership.OnPinch = delta => { if (OnPinch != null) OnPinch(delta); };
                }
                return ownership;
            }
        }

        public void Initialize(Camera camera)
        {
            stationCamera = camera;
            initialized = true;
            CancelAll();
        }

        public void SetMode(StationInputMode mode)
        {
            currentMode = mode;
            Ownership.SetMode(mode);
            capturedEntities.Clear();
            mouseHeld = false;
        }

        public void CancelAll()
        {
            Ownership.CancelAll();
            capturedEntities.Clear();
            mouseHeld = false;
        }

        /// <summary>Injectable phase entry point; uiBlocked is decided at Down only.</summary>
        public void ProcessPointer(int pointerId, StationPointerPhase phase,
            Vector2 position, bool uiBlocked = false)
        {
            string entity = null, station = null;
            if (phase == StationPointerPhase.Down && !uiBlocked)
            {
                StationEntityHit? hit = HitTest != null ? HitTest(position) : RaycastEntity(position);
                if (hit.HasValue) { entity = hit.Value.EntityId; station = hit.Value.StationId; }
            }
            Ownership.ProcessPointer(pointerId, phase, position.x, position.y, uiBlocked, entity, station);
            if (Ownership.Count == 0) capturedEntities.Clear();
        }

        void DispatchEntityTap(string entity, string station)
        {
            if (EntityIsValid != null && !EntityIsValid(entity)) return;
            SelectableEntityBinding binding;
            if (HitTest == null && (!capturedEntities.TryGetValue(entity, out binding) ||
                                   binding == null || !binding.isActiveAndEnabled)) return;
            if (OnEntityTap != null) OnEntityTap(entity, station);
        }

        StationEntityHit? RaycastEntity(Vector2 position)
        {
            if (stationCamera == null) return null;
            RaycastHit hit;
            if (!Physics.Raycast(stationCamera.ScreenPointToRay(position), out hit,
                                 stationCamera.farClipPlane, Physics.DefaultRaycastLayers,
                                 QueryTriggerInteraction.Ignore)) return null;
            var binding = hit.collider.GetComponentInParent<SelectableEntityBinding>();
            if (binding == null || !binding.isActiveAndEnabled || string.IsNullOrEmpty(binding.EntityId)) return null;
            capturedEntities[binding.EntityId] = binding;
            return new StationEntityHit(binding.EntityId, binding.StationId);
        }

        bool Blocked(Vector2 position)
        {
            if (!Screen.safeArea.Contains(position)) return true;
            if (IsUiBlocked != null && IsUiBlocked(position)) return true;
            var system = EventSystem.current;
            if (system == null) return false;
            if (cachedEventSystem != system || uiEvent == null)
            {
                cachedEventSystem = system;
                uiEvent = new PointerEventData(system);
            }
            uiEvent.Reset();
            uiEvent.position = position;
            uiHits.Clear();
            system.RaycastAll(uiEvent, uiHits);
            // A PhysicsRaycaster may also be installed by a host scene. Only
            // screen UI owns these hits; 3D physics remains below UI priority.
            for (int i = 0; i < uiHits.Count; i++)
                if (uiHits[i].module is UnityEngine.UI.GraphicRaycaster) return true;
            return false;
        }

        void Update()
        {
            if (!initialized) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                CancelAll();
                if (OnBack != null) OnBack();
            }
            bool anyTouch = false;
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (var touch in touchscreen.touches)
                {
                    var phase = touch.phase.ReadValue();
                    int id = touch.touchId.ReadValue();
                    var position = touch.position.ReadValue();
                    if (phase == InputTouchPhase.Began)
                    {
                        anyTouch = true;
                        ProcessPointer(id, StationPointerPhase.Down, position, Blocked(position));
                    }
                    else if (phase == InputTouchPhase.Moved)
                    {
                        anyTouch = true;
                        ProcessPointer(id, StationPointerPhase.Move, position);
                    }
                    else if (phase == InputTouchPhase.Stationary) anyTouch = true;
                    else if (phase == InputTouchPhase.Ended)
                    {
                        anyTouch = true;
                        ProcessPointer(id, StationPointerPhase.Up, position);
                    }
                    else if (phase == InputTouchPhase.Canceled)
                    {
                        anyTouch = true;
                        ProcessPointer(id, StationPointerPhase.Cancel, position);
                    }
                }
            }
            var mouse = Mouse.current;
            if (anyTouch)
            {
                // Prevent synthesized touch mouse events from duplicating a world action.
                if (mouseHeld) ProcessPointer(MousePointerId, StationPointerPhase.Cancel, mousePosition);
                mouseHeld = false;
                return;
            }
            if (mouse == null) return;
            Vector2 current = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                mouseHeld = true;
                mousePosition = current;
                ProcessPointer(MousePointerId, StationPointerPhase.Down, current, Blocked(current));
            }
            if (mouseHeld && current != mousePosition)
                ProcessPointer(MousePointerId, StationPointerPhase.Move, current);
            mousePosition = current;
            if (mouse.leftButton.wasReleasedThisFrame && mouseHeld)
            {
                ProcessPointer(MousePointerId, StationPointerPhase.Up, current);
                mouseHeld = false;
            }
            float scroll = mouse.scroll.ReadValue().y;
            if (currentMode != StationInputMode.Fixed && Mathf.Abs(scroll) > 0 &&
                !Blocked(current) && OnPinch != null)
                OnPinch(scroll * 0.1f);
        }

        void OnApplicationFocus(bool focused) { if (!focused) CancelAll(); }
        void OnApplicationPause(bool paused) { if (paused) CancelAll(); }
        void OnEnable() { InputSystem.onDeviceChange += OnDeviceChange; }
        void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            CancelAll();
        }
        void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is Pointer && (change == InputDeviceChange.Removed ||
                change == InputDeviceChange.Disconnected || change == InputDeviceChange.Disabled))
                CancelAll();
        }
    }
}
