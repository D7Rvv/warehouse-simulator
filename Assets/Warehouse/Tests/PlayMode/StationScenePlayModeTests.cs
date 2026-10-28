using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Warehouse.Presentation;
using Warehouse.UI;
using Object = UnityEngine.Object;

namespace Warehouse.Tests
{
    // Authoring these tests is not evidence that Unity/Android/iOS ran them.
    // The Test Runner XML is the authority for actual PASS_EDITOR results.
    public sealed class StationScenePlayModeTests
    {
        private StationApp app;

        [UnitySetUp]
        public IEnumerator LoadCommittedScene()
        {
            yield return SceneManager.LoadSceneAsync("WarehouseStation", LoadSceneMode.Single);
            yield return null;
            app = Object.FindFirstObjectByType<StationApp>();
            Assert.That(app, Is.Not.Null, "Committed scene must instantiate the composition root");
            Assert.That(app.IsReady, Is.True, "Fixture, camera, world and UI initialization must succeed");
        }

        [UnityTest]
        public IEnumerator S0_03_OneWorldCameraAndUiWithoutLegacyPlayer()
        {
            Assert.That(Object.FindObjectsByType<StationApp>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<WarehouseSceneBlockout>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Camera.allCamerasCount, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Any(b =>
                b != null && (b.GetType().Name == "MobilePlayerController" || b.GetType().Name == "WarehouseBootstrap")), Is.False);
            Assert.That(app.Blockout.CargoVisualCount, Is.EqualTo(app.World.TruckRuns[0].CargoParcelIds.Count));
            Assert.That(Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator S1_02_ActualNavigationButtonsAndFiftySwitchesKeepTheWorld()
        {
            var before = Identity();
            var instance = app.Blockout.GetInstanceID();
            var scene = SceneManager.GetActiveScene().handle;
            var elapsed = app.DemoElapsedSeconds;
            var stations = new[] { "LOC-INBOUND-01", "LOC-INSPECTION-01", "LOC-EXPORT-AIR-01", "LOC-WAREHOUSE-OVERVIEW", "LOC-BUILD-01" };
            for (var i = 0; i < 50; i++)
            {
                var id = stations[i % stations.Length];
                var button = app.Hud.GetComponentsInChildren<Button>(true).Single(b => b.name == "Navigate-" + id);
                button.onClick.Invoke();
                Assert.That(app.Registry.Selection.ActiveStationId, Is.EqualTo(id));
                Assert.That(app.CameraController.ActiveStationId, Is.EqualTo(id));
                Assert.That(app.CameraController.MainCamera.enabled, Is.True);
                yield return null;
            }
            Assert.That(app.Blockout.GetInstanceID(), Is.EqualTo(instance));
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(scene));
            Assert.That(Identity(), Is.EqualTo(before), "A button/camera switch must not transfer boxes or reset fixture data");
            Assert.That(app.DemoElapsedSeconds, Is.GreaterThan(elapsed), "Display-only elapsed time should continue across navigation");
        }

        [UnityTest]
        public IEnumerator S1_03_ActualPhysicsRaycastSelectsEquipmentAndRetainsItsId()
        {
            Assert.That(app.TryNavigate("LOC-WAREHOUSE-OVERVIEW"), Is.True);
            SelectableEntityBinding belt;
            Assert.That(app.Blockout.TryGetEntity("DEV-BELT-0", out belt), Is.True);
            Physics.SyncTransforms();
            var collider = belt.GetComponentsInChildren<Collider>().First(c => c.enabled);
            var position = app.CameraController.MainCamera.WorldToScreenPoint(collider.bounds.center);
            Assert.That(position.z, Is.GreaterThan(0));
            app.Input.ProcessPointer(31, StationPointerPhase.Down, position);
            app.Input.ProcessPointer(31, StationPointerPhase.Up, position);
            Assert.That(app.Registry.Selection.ActiveStationId, Is.EqualTo("LOC-SORTING-01"));
            Assert.That(app.Registry.Selection.ActiveTargetId, Is.EqualTo("DEV-BELT-0"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator S1_05_ThirtyGhostOperationsCannotInstallOrCharge()
        {
            var before = Identity();
            var equipment = app.Blockout.PlacedEquipmentCount;
            var objects = app.Blockout.GetComponentsInChildren<Transform>(true).Length;
            Assert.That(app.TryNavigate("LOC-BUILD-01"), Is.True);
            for (var i = 0; i < 30; i++)
            {
                app.HandleAction("preview-reset");
                app.Preview.MoveTo(i % 5 + 1, i % 4 + 1);
                app.HandleAction("preview-rotate"); app.HandleAction("preview-flip");
                Assert.That(app.Ghost.IsVisible, Is.True);
                app.HandleAction("preview-confirm");
                app.HandleAction("preview-cancel");
                Assert.That(app.Ghost.IsVisible, Is.False);
                yield return null;
            }
            Assert.That(Identity(), Is.EqualTo(before));
            Assert.That(app.Blockout.PlacedEquipmentCount, Is.EqualTo(equipment));
            Assert.That(app.Blockout.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objects));
        }

        [UnityTest]
        public IEnumerator S1_06_MultiTouchBlocksUiAndFocusLossCancelsAllOwners()
        {
            app.TryNavigate("LOC-WAREHOUSE-OVERVIEW");
            var initial = app.Registry.Selection.ActiveStationId;
            app.Input.ProcessPointer(1, StationPointerPhase.Down, new Vector2(80, 80));
            app.Input.ProcessPointer(2, StationPointerPhase.Down, new Vector2(200, 80));
            Assert.That(app.Input.CanInvokeUiAction, Is.False);
            app.Hud.GetComponentsInChildren<Button>(true).Single(b => b.name == "Navigate-LOC-BUILD-01").onClick.Invoke();
            Assert.That(app.Registry.Selection.ActiveStationId, Is.EqualTo(initial), "UI may not execute while world pinch owns both touches");
            app.Input.SendMessage("OnApplicationFocus", false, SendMessageOptions.RequireReceiver);
            Assert.That(app.Input.ActivePointerCount, Is.Zero);
            Assert.That(app.Input.CanInvokeUiAction, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator S1_06_HeldUiPressCannotFireAfterPinchEnds()
        {
            app.TryNavigate("LOC-WAREHOUSE-OVERVIEW");
            var button = app.Hud.GetComponentsInChildren<GuardedMobileButton>(true)
                .Single(b => b.name == "Navigate-LOC-BUILD-01");
            var press = new PointerEventData(EventSystem.current) { pointerId = 7, button = PointerEventData.InputButton.Left };
            app.Input.ProcessPointer(1, StationPointerPhase.Down, new Vector2(80, 80));
            app.Input.ProcessPointer(2, StationPointerPhase.Down, new Vector2(200, 80));
            button.OnPointerDown(press);
            app.Input.ProcessPointer(1, StationPointerPhase.Up, new Vector2(80, 80));
            app.Input.ProcessPointer(2, StationPointerPhase.Up, new Vector2(200, 80));
            button.OnPointerUp(press); button.OnPointerClick(press);
            Assert.That(app.Registry.Selection.ActiveStationId, Is.EqualTo("LOC-WAREHOUSE-OVERVIEW"));

            var beforePair = new PointerEventData(EventSystem.current) { pointerId = 8, button = PointerEventData.InputButton.Left };
            button.OnPointerDown(beforePair);
            app.Input.ProcessPointer(1, StationPointerPhase.Down, new Vector2(80, 80));
            app.Input.ProcessPointer(2, StationPointerPhase.Down, new Vector2(200, 80));
            app.Input.CancelAll();
            button.OnPointerUp(beforePair); button.OnPointerClick(beforePair);
            Assert.That(app.Registry.Selection.ActiveStationId, Is.EqualTo("LOC-WAREHOUSE-OVERVIEW"), "Pre-pinch UI hold must also be canceled");

            var fresh = new PointerEventData(EventSystem.current) { pointerId = 9, button = PointerEventData.InputButton.Left };
            button.OnPointerDown(fresh); button.OnPointerUp(fresh); button.OnPointerClick(fresh);
            Assert.That(app.Registry.Selection.ActiveStationId, Is.EqualTo("LOC-BUILD-01"), "Fresh UI press after gesture should work");
            yield return null;
        }

        [UnityTest]
        public IEnumerator S1_06_FocusLossInvalidatesAnAlreadyHeldUiButton()
        {
            app.TryNavigate("LOC-WAREHOUSE-OVERVIEW");
            var button = app.Hud.GetComponentsInChildren<GuardedMobileButton>(true)
                .Single(b => b.name == "Navigate-LOC-BUILD-01");
            var press = new PointerEventData(EventSystem.current) { pointerId = 7, button = PointerEventData.InputButton.Left };
            button.OnPointerDown(press);
            app.Input.SendMessage("OnApplicationFocus", false, SendMessageOptions.RequireReceiver);
            button.OnPointerUp(press); button.OnPointerClick(press);
            Assert.That(app.Registry.Selection.ActiveStationId, Is.EqualTo("LOC-WAREHOUSE-OVERVIEW"));

            // An individual platform touchCancel must invalidate a held uGUI
            // press even while an unrelated world pointer remains captured.
            app.Input.ProcessPointer(1, StationPointerPhase.Down, new Vector2(80, 80));
            app.Input.ProcessPointer(7, StationPointerPhase.Down, new Vector2(200, 80), true);
            var canceledPress = new PointerEventData(EventSystem.current) { pointerId = 7, button = PointerEventData.InputButton.Left };
            button.OnPointerDown(canceledPress);
            app.Input.ProcessPointer(7, StationPointerPhase.Cancel, new Vector2(200, 80), true);
            button.OnPointerUp(canceledPress); button.OnPointerClick(canceledPress);
            Assert.That(app.Registry.Selection.ActiveStationId, Is.EqualTo("LOC-WAREHOUSE-OVERVIEW"), "Individual OS cancel must not fire a late UI click");
            Assert.That(app.Input.ActivePointerCount, Is.EqualTo(1));
            Assert.That(app.Input.CapturedWorldPointerCount, Is.EqualTo(1));
            app.Input.CancelAll();
            yield return null;
        }

        [UnityTest]
        public IEnumerator S1_07_SafeAreaContainsPrimaryButtonsAndBackClosesDetails()
        {
            Canvas.ForceUpdateCanvases();
            foreach (var button in app.Hud.GetComponentsInChildren<Button>().Where(b => b.name.StartsWith("Navigate-") || b.name == "Back" || b.name == "Menu"))
            {
                var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    Assert.That(corner.x, Is.InRange(Screen.safeArea.xMin - 1, Screen.safeArea.xMax + 1), button.name);
                    Assert.That(corner.y, Is.InRange(Screen.safeArea.yMin - 1, Screen.safeArea.yMax + 1), button.name);
                }
                Assert.That(((RectTransform)button.transform).rect.height, Is.GreaterThanOrEqualTo(44), button.name + " touch target");
            }
            app.TryNavigate("LOC-INSPECTION-01"); app.HandleAction("label");
            Assert.That(app.Hud.IsModalOpen, Is.True);
            app.Input.OnBack();
            Assert.That(app.Hud.IsModalOpen, Is.False);
            Assert.That(app.Registry.Selection.ActiveStationId, Is.EqualTo("LOC-INSPECTION-01"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator S1_08_InvalidStationMissingAnchorAndLockedViewKeepCamera()
        {
            var before = app.CameraController.MainCamera.transform.position;
            var selection = app.Registry.Selection.ActiveStationId;
            Assert.That(app.TryNavigate("NO-STATION"), Is.False);
            Assert.That(app.TryNavigate("LOC-CONTRACT"), Is.False);
            Assert.That(app.TryNavigate("LOC-INBOUND-01", "NO-TARGET"), Is.False);
            string reason;
            Assert.That(app.CameraController.TrySwitch("FAKE", "MISSING-ANCHOR", out reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
            Assert.That(app.CameraController.MainCamera.transform.position, Is.EqualTo(before));
            Assert.That(app.Registry.Selection.ActiveStationId, Is.EqualTo(selection));
            yield return null;
        }

        [UnityTest]
        public IEnumerator S1_08_DeletedVisualTargetFallsBackWithoutChangingDomain()
        {
            var before = Identity();
            app.TryNavigate("LOC-SORTING-01", "DEV-BELT-0");
            SelectableEntityBinding target;
            Assert.That(app.Blockout.TryGetEntity("DEV-BELT-0", out target), Is.True);
            Object.Destroy(target.gameObject);
            yield return new WaitForSecondsRealtime(.65f);
            Assert.That(app.Registry.Selection.ActiveTargetId, Is.Null.Or.Empty);
            Assert.That(app.Registry.Selection.ActiveStationId, Is.EqualTo("LOC-SORTING-01"));
            Assert.That(Identity(), Is.EqualTo(before), "Removing a visual cannot delete a fixture entity or transfer ownership");
        }

        [UnityTest]
        public IEnumerator S1_04_InspectionDraftAndViewOrientationSurviveNavigation()
        {
            var before = Identity();
            app.TryNavigate("LOC-INSPECTION-01");
            var draft = app.Registry.Selection.InspectionDraftId;
            app.HandleAction("rotate-parcel"); app.HandleAction("label");
            var angle = app.Registry.Selection.InspectionRotationDegrees;
            app.TryNavigate("LOC-BUILD-01"); app.TryNavigate("LOC-INSPECTION-01");
            Assert.That(app.Registry.Selection.InspectionDraftId, Is.EqualTo(draft));
            Assert.That(app.Registry.Selection.InspectionRotationDegrees, Is.EqualTo(angle));
            Assert.That(app.Hud.IsModalOpen && app.Registry.Selection.LabelExpanded, Is.True);
            Assert.That(Identity(), Is.EqualTo(before));
            yield return null;
        }

        [UnityTest]
        public IEnumerator S1_01_GhostFlowReversesAndMaterialsAreTransparent()
        {
            app.TryNavigate("LOC-BUILD-01"); app.HandleAction("preview-reset");
            var forward = app.Ghost.ArrowDirection;
            app.HandleAction("preview-flip");
            Assert.That(Vector3.Dot(forward, app.Ghost.ArrowDirection), Is.LessThan(-.99f));
            var proxy = app.Blockout.transform.Find("S1 preview only - no installed machine");
            Assert.That(proxy, Is.Not.Null);
            foreach (var renderer in proxy.GetComponentsInChildren<MeshRenderer>().Where(r => r.name != "Preview coordinate and reason"))
            {
                Assert.That(renderer.sharedMaterial.renderQueue, Is.GreaterThanOrEqualTo(3000));
                Assert.That(renderer.sharedMaterial.color.a, Is.InRange(.01f, .99f));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator S1_CameraZoomClampsAndPerStationViewReturns()
        {
            app.TryNavigate("LOC-WAREHOUSE-OVERVIEW");
            app.CameraController.Pan(new Vector2(40, 20)); app.CameraController.Zoom(100000);
            var clamped = app.CameraController.MainCamera.transform.position;
            app.CameraController.Zoom(100000);
            Assert.That(Vector3.Distance(clamped, app.CameraController.MainCamera.transform.position), Is.LessThan(.001f));
            app.TryNavigate("LOC-INBOUND-01"); app.TryNavigate("LOC-WAREHOUSE-OVERVIEW");
            Assert.That(Vector3.Distance(clamped, app.CameraController.MainCamera.transform.position), Is.LessThan(.001f));
            yield return null;
        }

        [Test]
        public void DiagnosticPercentilesUseMeasuredSamplesWithoutDeviceClaims()
        {
            var go = new GameObject("Synthetic diagnostic unit test");
            try
            {
                var diagnostics = go.AddComponent<MobileQualityDiagnostics>(); diagnostics.enabled = false;
                for (var i = 1; i <= 100; i++) diagnostics.RecordFrame(i);
                diagnostics.RecordFrame(double.NaN); diagnostics.RecordFrame(-1);
                diagnostics.RecordStationSwitch(3.5); diagnostics.RecordStationSwitch(1);
                var snapshot = diagnostics.Snapshot();
                Assert.That(snapshot.SampleCount, Is.EqualTo(100));
                Assert.That(snapshot.MeanFrameMs, Is.EqualTo(50.5));
                Assert.That(snapshot.P95FrameMs, Is.EqualTo(95)); Assert.That(snapshot.P99FrameMs, Is.EqualTo(99));
                Assert.That(snapshot.CameraSwitchCount, Is.EqualTo(2)); Assert.That(snapshot.MaxSwitchMs, Is.EqualTo(3.5));
            }
            finally { Object.DestroyImmediate(go); }
        }

        private string Identity()
        {
            var w = app.World;
            return w.Fingerprint + ":" + w.CurrencyUSD + ":" + w.Day + ":" + w.DisplayTime + ":" +
                string.Join("|", w.Parcels.Select(p => p.Id + "/" + p.OwnerKind + "/" + p.OwnerEntityId + "/" + p.OwnerSlot));
        }
    }
}
