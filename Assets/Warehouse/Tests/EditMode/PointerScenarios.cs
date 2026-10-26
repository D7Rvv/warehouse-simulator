using System;
using Warehouse.Presentation;

namespace Warehouse.Tests
{
    public static class PointerScenarios
    {
        public static readonly string[] Names = {
            "INPUT-001 UiCaptureNeverLeaksToWorld", "INPUT-002 CancelNeverTaps",
            "INPUT-003 ModeChangeCancelsCapture", "INPUT-004 PinchSuppressesBothTaps",
            "INPUT-005 PinchRemainderCannotDragOrTap", "INPUT-006 DragDoesNotSelect",
            "INPUT-007 FixedCameraIgnoresPanPinch", "INPUT-008 UiDoesNotJoinWorldPinch",
            "INPUT-009 PinchBlocksUiCommands", "INPUT-010 NonFinitePointerCancels",
            "INPUT-011 UiReleaseCannotClearWorldGestureGuard", "INPUT-012 UiPressStartedDuringPinchStaysRejected",
            "INPUT-013 UiPressBeforePinchCannotFireAfterPairReleased", "INPUT-014 CancelInvalidatesHeldUiPress",
            "INPUT-015 FreshUiPressWorksAfterGestureEnds", "INPUT-016 IndividualUiCancelInvalidatesPressWithoutDroppingOtherFinger"
        };

        public static void Run(string name)
        {
            var ownership = new StationPointerOwnership();
            ownership.SetMode(StationInputMode.Overview);
            var taps = 0; var drags = 0; var pinches = 0;
            ownership.OnEntityTap = (e, s) => taps++;
            ownership.OnDrag = (dx, dy, x, y) => drags++;
            ownership.OnPinch = d => pinches++;
            switch (name)
            {
                case "INPUT-001 UiCaptureNeverLeaksToWorld":
                    ownership.ProcessPointer(1, StationPointerPhase.Down, 10, 10, true, "BELT", "BUILD");
                    ownership.ProcessPointer(1, StationPointerPhase.Move, 80, 80, false, "BELT", "BUILD");
                    ownership.ProcessPointer(1, StationPointerPhase.Up, 80, 80);
                    DomainScenarios.Equal(0, taps + drags + pinches, "UI capture escaped into world"); break;
                case "INPUT-002 CancelNeverTaps":
                    Down(ownership, 1, 0, 0);
                    ownership.ProcessPointer(1, StationPointerPhase.Cancel, 0, 0);
                    ownership.ProcessPointer(1, StationPointerPhase.Up, 0, 0);
                    DomainScenarios.Equal(0, taps, "canceled finger selected a target");
                    DomainScenarios.Equal(0, ownership.Count, "cancel leaked pointer ownership"); break;
                case "INPUT-003 ModeChangeCancelsCapture":
                    Down(ownership, 1, 0, 0);
                    ownership.SetMode(StationInputMode.Build);
                    ownership.ProcessPointer(1, StationPointerPhase.Up, 0, 0);
                    DomainScenarios.Equal(0, taps, "view switch reused old touch release");
                    DomainScenarios.Equal(0, ownership.Count, "mode change leaked capture"); break;
                case "INPUT-004 PinchSuppressesBothTaps":
                    Down(ownership, 1, 0, 0); Down(ownership, 2, 100, 0);
                    ownership.ProcessPointer(2, StationPointerPhase.Move, 120, 0);
                    ownership.ProcessPointer(1, StationPointerPhase.Up, 0, 0);
                    ownership.ProcessPointer(2, StationPointerPhase.Up, 120, 0);
                    DomainScenarios.Equal(1, pinches, "two world fingers did not zoom");
                    DomainScenarios.Equal(0, taps + drags, "pinch also selected or dragged"); break;
                case "INPUT-005 PinchRemainderCannotDragOrTap":
                    Down(ownership, 1, 0, 0); Down(ownership, 2, 100, 0);
                    ownership.ProcessPointer(2, StationPointerPhase.Cancel, 100, 0);
                    ownership.ProcessPointer(1, StationPointerPhase.Move, 100, 100);
                    ownership.ProcessPointer(1, StationPointerPhase.Up, 100, 100);
                    DomainScenarios.Equal(0, taps + drags, "remaining pinch finger became another command");
                    Down(ownership, 3, 0, 0); ownership.ProcessPointer(3, StationPointerPhase.Up, 0, 0);
                    DomainScenarios.Equal(1, taps, "fresh single tap failed after group ended"); break;
                case "INPUT-006 DragDoesNotSelect":
                    Down(ownership, 1, 0, 0);
                    ownership.ProcessPointer(1, StationPointerPhase.Move, 40, 0);
                    ownership.ProcessPointer(1, StationPointerPhase.Up, 40, 0);
                    DomainScenarios.Equal(1, drags, "single captured drag did not execute");
                    DomainScenarios.Equal(0, taps, "drag release selected target"); break;
                case "INPUT-007 FixedCameraIgnoresPanPinch":
                    ownership.SetMode(StationInputMode.Fixed);
                    Down(ownership, 1, 0, 0); Down(ownership, 2, 100, 0);
                    ownership.ProcessPointer(2, StationPointerPhase.Move, 150, 0);
                    ownership.CancelAll();
                    Down(ownership, 3, 0, 0); ownership.ProcessPointer(3, StationPointerPhase.Up, 0, 0);
                    DomainScenarios.Equal(0, pinches + drags, "fixed gate/yard camera accepted gesture");
                    DomainScenarios.Equal(1, taps, "fixed view lost equipment selection"); break;
                case "INPUT-008 UiDoesNotJoinWorldPinch":
                    ownership.ProcessPointer(1, StationPointerPhase.Down, 0, 0, true);
                    Down(ownership, 2, 100, 0);
                    ownership.ProcessPointer(2, StationPointerPhase.Move, 150, 0);
                    DomainScenarios.Equal(0, pinches, "UI finger participated in world pinch");
                    DomainScenarios.Equal(1, ownership.WorldCount, "UI included in world count");
                    ownership.CancelAll(); break;
                case "INPUT-009 PinchBlocksUiCommands":
                    Down(ownership, 1, 0, 0); Down(ownership, 2, 100, 0);
                    DomainScenarios.Check(!ownership.CanInvokeUiAction, "UI command accepted during pinch");
                    ownership.ProcessPointer(1, StationPointerPhase.Up, 0, 0);
                    DomainScenarios.Check(!ownership.CanInvokeUiAction, "UI command accepted before gesture group ended");
                    ownership.ProcessPointer(2, StationPointerPhase.Up, 100, 0);
                    DomainScenarios.Check(ownership.CanInvokeUiAction, "UI remained blocked after all touches released"); break;
                case "INPUT-010 NonFinitePointerCancels":
                    Down(ownership, 1, 0, 0);
                    ownership.ProcessPointer(1, StationPointerPhase.Move, float.NaN, 0);
                    DomainScenarios.Equal(0, ownership.Count, "invalid coordinate kept a captured pointer");
                    DomainScenarios.Equal(0, taps + drags + pinches, "invalid coordinate emitted an action"); break;
                case "INPUT-011 UiReleaseCannotClearWorldGestureGuard":
                    Down(ownership, 1, 0, 0);
                    DomainScenarios.Check(ownership.CanInvokeUiAction, "single world finger unexpectedly blocks UI");
                    Down(ownership, 2, 100, 0);
                    ownership.ProcessPointer(3, StationPointerPhase.Down, 200, 0, true);
                    ownership.ProcessPointer(3, StationPointerPhase.Up, 200, 0, true);
                    DomainScenarios.Check(!ownership.CanInvokeUiAction, "UI release cleared current multi-world gesture guard");
                    ownership.ProcessPointer(1, StationPointerPhase.Up, 0, 0);
                    DomainScenarios.Check(!ownership.CanInvokeUiAction, "first world release cleared pinch guard too early");
                    ownership.CancelAll();
                    DomainScenarios.Check(ownership.CanInvokeUiAction && ownership.Count == 0, "cancellation did not release guard"); break;
                case "INPUT-012 UiPressStartedDuringPinchStaysRejected":
                    Down(ownership, 1, 0, 0); Down(ownership, 2, 100, 0);
                    var during = ownership.CaptureUiPressEligibility();
                    ownership.ProcessPointer(1, StationPointerPhase.Up, 0, 0);
                    ownership.ProcessPointer(2, StationPointerPhase.Up, 100, 0);
                    DomainScenarios.Check(!ownership.IsUiPressAllowed(during), "UI held during pinch fired after fingers lifted"); break;
                case "INPUT-013 UiPressBeforePinchCannotFireAfterPairReleased":
                    var beforePair = ownership.CaptureUiPressEligibility();
                    Down(ownership, 1, 0, 0); Down(ownership, 2, 100, 0);
                    ownership.ProcessPointer(1, StationPointerPhase.Up, 0, 0);
                    ownership.ProcessPointer(2, StationPointerPhase.Up, 100, 0);
                    DomainScenarios.Check(!ownership.IsUiPressAllowed(beforePair), "pre-pinch UI hold survived gesture cancellation generation"); break;
                case "INPUT-014 CancelInvalidatesHeldUiPress":
                    var canceled = ownership.CaptureUiPressEligibility();
                    ownership.CancelAll();
                    DomainScenarios.Check(!ownership.IsUiPressAllowed(canceled), "CancelAll did not invalidate held UI action");
                    var switched = ownership.CaptureUiPressEligibility();
                    ownership.SetMode(StationInputMode.Build);
                    DomainScenarios.Check(!ownership.IsUiPressAllowed(switched), "view switch did not invalidate held UI action"); break;
                case "INPUT-015 FreshUiPressWorksAfterGestureEnds":
                    Down(ownership, 1, 0, 0); Down(ownership, 2, 100, 0);
                    ownership.CancelAll();
                    DomainScenarios.Check(ownership.IsUiPressAllowed(ownership.CaptureUiPressEligibility()), "fresh eligible UI press rejected after cancel"); break;
                case "INPUT-016 IndividualUiCancelInvalidatesPressWithoutDroppingOtherFinger":
                    Down(ownership, 1, 0, 0);
                    ownership.ProcessPointer(7, StationPointerPhase.Down, 200, 0, true);
                    var individuallyCanceled = ownership.CaptureUiPressEligibility();
                    DomainScenarios.Check(ownership.IsUiPressAllowed(individuallyCanceled), "UI press should start eligible beside one world finger");
                    ownership.ProcessPointer(7, StationPointerPhase.Cancel, 200, 0, true);
                    DomainScenarios.Check(!ownership.IsUiPressAllowed(individuallyCanceled), "individual OS cancel left held UI press eligible");
                    DomainScenarios.Equal(1, ownership.Count, "canceling UI pointer incorrectly discarded the other captured finger");
                    DomainScenarios.Equal(1, ownership.WorldCount, "remaining world pointer lost ownership");
                    ownership.ProcessPointer(1, StationPointerPhase.Up, 0, 0);
                    DomainScenarios.Equal(1, taps, "remaining independent world capture stopped working after UI cancel"); break;
                default: throw new ArgumentException("Unknown pointer scenario " + name);
            }
        }

        private static void Down(StationPointerOwnership o, int id, float x, float y)
        { o.ProcessPointer(id, StationPointerPhase.Down, x, y, false, "ENTITY", "STATION"); }
    }
}
