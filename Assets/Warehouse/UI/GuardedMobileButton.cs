using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Warehouse.UI
{
    /// <summary>A press rejected at Down, or canceled by a gesture/view/focus change, never becomes a click later.</summary>
    public sealed class GuardedMobileButton : Button, ICancelHandler
    {
        private Func<bool> canAct;
        private Func<int> cancellationVersion;
        private bool eligibleAtDown;
        private int versionAtDown;
        private int capturedPointer;
        public void Configure(Func<bool> eligibility, Func<int> version)
        { canAct = eligibility; cancellationVersion = version; }
        private bool Eligible() { return canAct == null || canAct(); }
        private int Version() { return cancellationVersion == null ? 0 : cancellationVersion(); }
        public override void OnPointerDown(PointerEventData eventData)
        {
            capturedPointer = eventData.pointerId;
            eligibleAtDown = eventData.button == PointerEventData.InputButton.Left && Eligible();
            versionAtDown = Version();
            if (eligibleAtDown) base.OnPointerDown(eventData);
        }
        public override void OnPointerClick(PointerEventData eventData)
        {
            bool accepted = eligibleAtDown && capturedPointer == eventData.pointerId &&
                            versionAtDown == Version() && Eligible();
            eligibleAtDown = false;
            if (accepted) base.OnPointerClick(eventData);
            else eventData.Use();
        }
        public override void OnSubmit(BaseEventData eventData)
        { if (Eligible()) base.OnSubmit(eventData); else eventData.Use(); }
        public void OnCancel(BaseEventData eventData) { eligibleAtDown = false; }
        protected override void OnDisable() { eligibleAtDown = false; base.OnDisable(); }
        private void OnApplicationFocus(bool focused) { if (!focused) eligibleAtDown = false; }
        private void OnApplicationPause(bool paused) { if (paused) eligibleAtDown = false; }
    }
}
