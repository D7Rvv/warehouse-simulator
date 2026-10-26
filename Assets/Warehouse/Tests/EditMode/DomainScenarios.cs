using System;
using System.Linq;
using Warehouse.Domain;

namespace Warehouse.Tests
{
    // The same scenarios run under NUnit/JsonUtility and under a real standalone
    // C# runtime. A standalone pass is PASS_LOGIC, never PASS_EDITOR or DEVICE.
    public static class DomainScenarios
    {
        public static readonly string[] Names = {
            "FIXTURE-001 PrototypeBoundary", "FIXTURE-002 NoProductionPromotion",
            "FIXTURE-003 NoReferenceCatalogOverride", "FIXTURE-004 NoNavigationAutomation",
            "FIXTURE-005 NoRealSubmission", "FIXTURE-006 NoMoneyMutation",
            "FIXTURE-007 NoPlacementTransaction", "FIXTURE-008 UniqueParcelIds",
            "FIXTURE-009 UniqueOwnerSlots", "FIXTURE-010 ExactTruckManifest",
            "FIXTURE-011 DeepCopyIsolation", "FIXTURE-012 CurrentAndPreviousBatchSeparate",
            "FIXTURE-013 NoInspectionTruthInPresentation", "NAV-001 FiftySwitchesPreserveWorld",
            "NAV-002 InvalidStationKeepsSelection", "NAV-003 LockedStationHasReason",
            "NAV-004 HandAndDraftSurviveSwitch", "NAV-005 SelectedEntityRetained",
            "NAV-006 RemovedTargetFallsBack", "NAV-007 UnknownTargetRejected", "NAV-008 MissingAnchorKeepsSelection",
            "GHOST-001 ThirtyPreviewCyclesDoNotMutateWorld", "GHOST-002 OccupiedCellRejected",
            "GHOST-003 BoundsRejected", "GHOST-004 RotationAndFlipRoundTrip",
            "GHOST-005 CancelClearsPreviewOnly", "GHOST-006 InvalidFloorRejected"
        };

        public static void Run(string name, Func<PrototypeFixtureData> fixture)
        {
            var d = fixture();
            switch (name)
            {
                case "FIXTURE-001 PrototypeBoundary":
                    d.prototypeOnly = false; Reject(d); return;
                case "FIXTURE-002 NoProductionPromotion":
                    d.promoteToProduction = true; Reject(d); return;
                case "FIXTURE-003 NoReferenceCatalogOverride":
                    d.mustNeverOverrideReferenceCatalog = false; Reject(d); return;
                case "FIXTURE-004 NoNavigationAutomation":
                    d.ui.autoCompleteParcelByMenu = true; Reject(d); return;
                case "FIXTURE-005 NoRealSubmission":
                    d.fakeExportGate.actualSubmitAllowed = true; Reject(d); return;
                case "FIXTURE-006 NoMoneyMutation":
                    d.fakeExportGate.actualMoneyMutationAllowed = true; Reject(d); return;
                case "FIXTURE-007 NoPlacementTransaction":
                    d.ghostConstruction.placementTransactionsEnabled = true; Reject(d); return;
                case "FIXTURE-008 UniqueParcelIds":
                    d.packages[1].id = d.packages[0].id; Reject(d); return;
                case "FIXTURE-009 UniqueOwnerSlots":
                    d.packages[1].owner.slot = d.packages[0].owner.slot; Reject(d); return;
                case "FIXTURE-010 ExactTruckManifest":
                    d.mockTruckRuns[0].cargoParcelIds[0] = "MISSING-PARCEL"; Reject(d); return;
                case "FIXTURE-011 DeepCopyIsolation":
                {
                    var world = new ReadOnlyMockWorld(d);
                    var before = WorldIdentity(world);
                    d.world.currencyUSD = 0;
                    d.packages[0].visibleLabel.countryName = "MUTATED";
                    d.packages[0].owner.slot = "MUTATED";
                    d.mockTruckRuns[0].cargoParcelIds[0] = "MUTATED";
                    d.fakeExportGate.currentBufferedCount = 0;
                    d.ghostConstruction.exampleOccupiedCells[0].x = 123;
                    Equal(before, WorldIdentity(world), "DTO mutation leaked into immutable world");
                    return;
                }
                case "FIXTURE-012 CurrentAndPreviousBatchSeparate":
                {
                    var world = new ReadOnlyMockWorld(d);
                    Equal(1, world.Gate.CurrentBufferedCount, "current gate demonstration count");
                    Equal(16, world.Gate.LastBatchSummary.CorrectCount, "previous batch count");
                    Equal(1003m, world.Gate.LastBatchSummary.NetUSD, "previous net is not multiplied by current count");
                    return;
                }
                case "FIXTURE-013 NoInspectionTruthInPresentation":
                    Check(typeof(ParcelSnapshot).GetProperties().All(p =>
                        p.Name != "HiddenTruth" && p.Name != "CargoValid" && p.Name != "CountryValid"),
                        "presentation must read the visible label, not hidden answers"); return;
            }

            var w = new ReadOnlyMockWorld(d);
            var r = new MobileStationRegistry(w.Stations, w.InitialStationId);
            var preview = new PlacementPreview(w.Ghost);
            string reason;
            switch (name)
            {
                case "NAV-001 FiftySwitchesPreserveWorld":
                {
                    var before = WorldIdentity(w);
                    var stations = w.Stations.Where(s => s.Enabled).ToArray();
                    for (var i = 0; i < 50; i++)
                        Check(r.TrySelect(stations[i % stations.Length].Id, null, out reason), reason);
                    Equal(before, WorldIdentity(w), "camera navigation changed parcels, owners, money, gate or clock");
                    return;
                }
                case "NAV-002 InvalidStationKeepsSelection":
                {
                    var previous = r.Selection.ActiveStationId;
                    Check(!r.TrySelect("UNKNOWN-STATION", null, out reason), "unknown station accepted");
                    Equal(previous, r.Selection.ActiveStationId, "invalid navigation moved camera selection");
                    Check(!string.IsNullOrWhiteSpace(reason), "invalid station must explain rejection"); return;
                }
                case "NAV-003 LockedStationHasReason":
                    Check(!r.TrySelect("LOC-CONTRACT", null, out reason), "locked contract accepted");
                    Check(!string.IsNullOrWhiteSpace(reason), "locked view must explain condition"); return;
                case "NAV-004 HandAndDraftSurviveSwitch":
                    r.Selection.HandSlotParcelId = "RESERVED-HAND";
                    r.Selection.InspectionDraftId = "DRAFT-1";
                    r.Selection.InspectionRotationDegrees = 137;
                    r.Selection.LabelExpanded = true;
                    Check(r.TrySelect("LOC-BUILD-01", null, out reason), reason);
                    Check(r.TrySelect("LOC-INSPECTION-01", null, out reason), reason);
                    Equal("RESERVED-HAND", r.Selection.HandSlotParcelId, "hand slot reset");
                    Equal("DRAFT-1", r.Selection.InspectionDraftId, "inspection draft reset");
                    Equal(137f, r.Selection.InspectionRotationDegrees, "inspection orientation reset");
                    Check(r.Selection.LabelExpanded, "label draft reset"); return;
                case "NAV-005 SelectedEntityRetained":
                    r.RegisterTarget("DEV-DOCK-01", "LOC-INBOUND-01");
                    Check(r.TrySelect("LOC-INBOUND-01", "DEV-DOCK-01", out reason), reason);
                    Equal("DEV-DOCK-01", r.Selection.ActiveTargetId, "target lost at selection");
                    Check(r.TrySelect("LOC-INBOUND-01", null, out reason), reason);
                    Equal("DEV-DOCK-01", r.Selection.ActiveTargetId, "same-station menu lost active target"); return;
                case "NAV-006 RemovedTargetFallsBack":
                    r.RegisterTarget("DEV-DOCK-01", "LOC-INBOUND-01");
                    Check(r.TrySelect("LOC-INBOUND-01", "DEV-DOCK-01", out reason), reason);
                    r.TargetRemoved("DEV-DOCK-01");
                    Equal("LOC-INBOUND-01", r.Selection.ActiveStationId, "removed target changed station");
                    Check(string.IsNullOrEmpty(r.Selection.ActiveTargetId), "stale target retained"); return;
                case "NAV-007 UnknownTargetRejected":
                    Check(!r.TrySelect("LOC-INBOUND-01", "NONEXISTENT-ENTITY", out reason), "nonexistent target accepted");
                    Equal(w.InitialStationId, r.Selection.ActiveStationId, "bad target changed station"); return;
                case "NAV-008 MissingAnchorKeepsSelection":
                    r.SetAnchorValidator(id => false);
                    Check(!r.TrySelect("LOC-INBOUND-01", null, out reason), "station without a camera anchor accepted");
                    Equal(w.InitialStationId, r.Selection.ActiveStationId, "missing anchor changed station");
                    Check(!string.IsNullOrWhiteSpace(reason), "missing anchor needs a visible error"); return;
                case "GHOST-001 ThirtyPreviewCyclesDoNotMutateWorld":
                {
                    var before = WorldIdentity(w);
                    var confirmed = w.ConfirmedEquipmentCount;
                    for (var i = 0; i < 30; i++)
                    {
                        preview.Reset(); preview.MoveTo(i % 5 + 1, i % 4 + 1);
                        preview.Rotate(); preview.Flip(); preview.Cancel();
                    }
                    Equal(before, WorldIdentity(w), "preview changed read-only world");
                    Equal(confirmed, w.ConfirmedEquipmentCount, "preview committed actual equipment"); return;
                }
                case "GHOST-002 OccupiedCellRejected":
                    var cell = w.Ghost.OccupiedCells[0];
                    preview.MoveTo(cell.X, cell.Z, cell.Floor);
                    Check(!preview.IsValid && !string.IsNullOrEmpty(preview.InvalidReason), "occupied preview accepted or no reason"); return;
                case "GHOST-003 BoundsRejected":
                    preview.MoveTo(999, -999);
                    Check(!preview.IsValid && !string.IsNullOrEmpty(preview.InvalidReason), "out-of-bounds preview accepted"); return;
                case "GHOST-004 RotationAndFlipRoundTrip":
                    var quarter = preview.RotationQuarterTurns;
                    var flip = preview.Flipped;
                    for (var i = 0; i < 4; i++) preview.Rotate();
                    preview.Flip(); preview.Flip();
                    Equal(quarter, preview.RotationQuarterTurns, "four quarter turns must return original orientation");
                    Equal(flip, preview.Flipped, "double flip must return original flow"); return;
                case "GHOST-005 CancelClearsPreviewOnly":
                    preview.MoveTo(4, 4); preview.Rotate(); preview.Flip(); preview.Cancel();
                    Check(!preview.Active, "cancel left preview active");
                    Equal(w.Ghost.InitialPosition.X, preview.X, "cancel did not reset draft coordinates");
                    Equal(w.Ghost.InitialPosition.Z, preview.Z, "cancel did not reset draft coordinates");
                    preview.Reset(); Check(preview.Active, "new preview cannot restart after cancel"); return;
                case "GHOST-006 InvalidFloorRejected":
                    preview.MoveTo(2, 3, 3);
                    Check(!preview.IsValid && !string.IsNullOrEmpty(preview.InvalidReason), "unsupported floor accepted"); return;
                default: throw new ArgumentException("Unknown scenario " + name);
            }
        }

        public static string WorldIdentity(ReadOnlyMockWorld w)
        {
            return w.Fingerprint + ":" + w.CurrencyUSD + ":" + w.Day + ":" + w.Weekday + ":" + w.DisplayTime + ":" +
                w.ConfirmedEquipmentCount + ":" + w.Gate.CurrentBufferedCount + ":" +
                string.Join("|", w.Parcels.Select(p => p.Id + "/" + p.OwnerKind + "/" + p.OwnerEntityId + "/" + p.OwnerSlot + "/" + p.CountryName)) +
                ":" + string.Join("|", w.TruckRuns.Select(t => string.Join(",", t.CargoParcelIds)));
        }

        private static void Reject(PrototypeFixtureData data)
        {
            try { new ReadOnlyMockWorld(data); }
            catch (ArgumentException) { return; }
            throw new InvalidOperationException("Unsafe/invalid fixture was accepted");
        }
        public static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
        public static void Equal<T>(T expected, T actual, string message)
        { if (!Equals(expected, actual)) throw new InvalidOperationException(message + ": expected " + expected + ", actual " + actual); }
    }
}
