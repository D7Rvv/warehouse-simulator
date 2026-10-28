using System;
using System.Collections.Generic;

namespace Warehouse.Presentation
{
    public enum StationInputMode { Fixed, Inspection, Overview, Build }
    public enum StationPointerPhase { Down, Move, Up, Cancel }

    public readonly struct StationUiPressToken
    {
        public readonly bool EligibleAtDown;
        public readonly int CancellationVersion;
        internal StationUiPressToken(bool eligibleAtDown, int version)
        { EligibleAtDown = eligibleAtDown; CancellationVersion = version; }
    }

    /// <summary>
    /// Deterministic pointer capture, independent of Unity and frame frequency.
    /// Ownership is decided on Down and never handed from UI to the world.
    /// A multi-finger world gesture cannot become a tap when one finger lifts.
    /// </summary>
    public sealed class StationPointerOwnership
    {
        struct Pointer
        {
            public float StartX, StartY, X, Y;
            public bool Ui, Dragged, Grouped;
            public string EntityId, StationId;
        }

        readonly Dictionary<int, Pointer> pointers = new Dictionary<int, Pointer>(10);
        readonly float dragThresholdSquared;
        bool worldGroupSuppressed;
        StationInputMode mode;
        int cancellationVersion;

        public Action<string, string> OnEntityTap;
        public Action<float, float, float, float> OnDrag;
        public Action<float> OnPinch;
        public int Count { get { return pointers.Count; } }
        public int WorldCount
        {
            get
            {
                int count = 0;
                foreach (var item in pointers) if (!item.Value.Ui) count++;
                return count;
            }
        }
        public bool IsPinching { get { return AllowsPinch && WorldCount == 2; } }
        public bool CanInvokeUiAction { get { return !worldGroupSuppressed && WorldCount < 2; } }
        public int CancellationVersion { get { return cancellationVersion; } }
        bool AllowsPinch { get { return mode != StationInputMode.Fixed; } }

        public StationUiPressToken CaptureUiPressEligibility()
        {
            return new StationUiPressToken(CanInvokeUiAction, cancellationVersion);
        }

        public bool IsUiPressAllowed(StationUiPressToken token)
        {
            return token.EligibleAtDown && token.CancellationVersion == cancellationVersion && CanInvokeUiAction;
        }

        public StationPointerOwnership(float dragThresholdPixels = 12)
        {
            if (dragThresholdPixels <= 0 || float.IsNaN(dragThresholdPixels) || float.IsInfinity(dragThresholdPixels))
                throw new ArgumentOutOfRangeException(nameof(dragThresholdPixels));
            dragThresholdSquared = dragThresholdPixels * dragThresholdPixels;
        }

        public void SetMode(StationInputMode next)
        {
            CancelAll();
            mode = next;
        }

        public void CancelAll()
        {
            pointers.Clear();
            worldGroupSuppressed = false;
            unchecked { cancellationVersion++; }
        }

        public void ProcessPointer(int pointerId, StationPointerPhase phase,
            float x, float y, bool uiBlocked = false, string entityId = null, string stationId = null)
        {
            if (float.IsNaN(x) || float.IsNaN(y) || float.IsInfinity(x) || float.IsInfinity(y))
            {
                Remove(pointerId);
                return;
            }
            Pointer pointer;
            if (phase == StationPointerPhase.Down)
            {
                // A platform occasionally reuses an ID after a cancellation.
                Remove(pointerId);
                pointer = new Pointer {
                    StartX = x, StartY = y, X = x, Y = y,
                    Ui = uiBlocked, EntityId = entityId, StationId = stationId,
                    Grouped = !uiBlocked && worldGroupSuppressed
                };
                if (!uiBlocked && WorldCount > 0)
                {
                    if (!worldGroupSuppressed) unchecked { cancellationVersion++; }
                    worldGroupSuppressed = true;
                    pointer.Grouped = true;
                }
                pointers[pointerId] = pointer;
                return;
            }
            if (!pointers.TryGetValue(pointerId, out pointer)) return;
            if (phase == StationPointerPhase.Cancel)
            {
                // OS gesture cancellation must also invalidate a queued uGUI
                // release/click. Other pointers keep their captured ownership.
                unchecked { cancellationVersion++; }
                Remove(pointerId);
                return;
            }

            float oldDistance = PairDistance();
            float dx = x - pointer.X, dy = y - pointer.Y;
            pointer.X = x;
            pointer.Y = y;
            float movedX = x - pointer.StartX, movedY = y - pointer.StartY;
            if (movedX * movedX + movedY * movedY >= dragThresholdSquared) pointer.Dragged = true;
            if (!pointer.Ui && worldGroupSuppressed) pointer.Grouped = true;
            pointers[pointerId] = pointer;

            if (phase == StationPointerPhase.Up)
            {
                bool tap = !pointer.Ui && !pointer.Dragged && !pointer.Grouped &&
                           !worldGroupSuppressed && !string.IsNullOrEmpty(pointer.EntityId);
                // Remove before invoking callbacks: a station switch may cancel every pointer.
                Remove(pointerId);
                if (tap && OnEntityTap != null) OnEntityTap(pointer.EntityId, pointer.StationId);
                return;
            }
            if (phase != StationPointerPhase.Move || pointer.Ui) return;
            if (worldGroupSuppressed)
            {
                if (AllowsPinch && WorldCount == 2)
                {
                    float delta = PairDistance() - oldDistance;
                    if (Math.Abs(delta) > 0.001f && OnPinch != null) OnPinch(delta);
                }
                return;
            }
            if (pointer.Dragged && mode != StationInputMode.Fixed && OnDrag != null)
                OnDrag(dx, dy, x, y);
        }

        void Remove(int pointerId)
        {
            pointers.Remove(pointerId);
            if (WorldCount == 0) worldGroupSuppressed = false;
        }

        float PairDistance()
        {
            if (WorldCount != 2) return 0;
            bool found = false;
            Pointer first = default(Pointer);
            foreach (var pair in pointers)
            {
                var p = pair.Value;
                if (p.Ui) continue;
                if (!found) { first = p; found = true; }
                else
                {
                    float dx = first.X - p.X, dy = first.Y - p.Y;
                    return (float)Math.Sqrt(dx * dx + dy * dy);
                }
            }
            return 0;
        }
    }
}
