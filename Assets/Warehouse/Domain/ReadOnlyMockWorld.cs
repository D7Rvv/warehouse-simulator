using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Warehouse.Domain
{
    /// <summary>
    /// S1 presentation fixture, not a save, catalog, clock or financial service.
    /// No operations in this type can transfer parcels or post revenue.
    /// </summary>
    public sealed class ReadOnlyMockWorld
    {
        public decimal CurrencyUSD { get; }
        public int Day { get; }
        public string Weekday { get; }
        public string DisplayTime { get; }
        public int Tier { get; }
        public string ProfileId { get; }
        public string InitialStationId { get; }
        public string SourceStatus => "P";
        public bool PrototypeOnly => true;
        public bool PromoteToProduction => false;
        public IReadOnlyList<ParcelSnapshot> Parcels { get; }
        public IReadOnlyList<TruckRunSnapshot> TruckRuns { get; }
        public IReadOnlyList<StationDefinition> Stations { get; }
        public ExportGateSnapshot Gate { get; }
        public GhostFixtureSnapshot Ghost { get; }
        public string Fingerprint { get; }
        public int ConfirmedEquipmentCount => Ghost.OccupiedCells.Count;
        private readonly Dictionary<string, ParcelSnapshot> parcelsById;

        public ReadOnlyMockWorld(PrototypeFixtureData data)
        {
            Require(data != null, "Fixture is missing.");
            Require(data.schemaVersion == "prototype_station_fixture_v0.1", "Unsupported prototype fixture schema.");
            Require(data.prototypeOnly && !data.promoteToProduction && data.sourceStatus == "P" &&
                    data.mustNeverOverrideReferenceCatalog, "Only an isolated P-status prototype fixture is allowed.");
            Require(data.world != null && data.ui != null, "Fixture world/UI is missing.");
            Require(!data.world.simulationClockResetOnCameraChange && !data.ui.autoCompleteParcelByMenu,
                    "Navigation must not reset the world or complete parcels.");
            Require(data.ui.showPrototypeWatermark, "Prototype watermark is required.");
            Require(data.world.currencySourceStatus == "P_DEMO_ONLY", "Currency must be marked demo only.");
            Require(data.world.day > 0 && data.world.tier >= 0, "Invalid fixture day/tier.");
            CurrencyUSD = data.world.currencyUSD;
            Day = data.world.day;
            Weekday = Text(data.world.weekday, "weekday");
            DisplayTime = Text(data.world.time, "display time");
            ProfileId = Text(data.world.profileId, "profile ID");
            Tier = data.world.tier;
            InitialStationId = Text(data.world.initialStation, "initial station");

            Require(data.stations != null && data.stations.Length > 0, "Stations are missing.");
            var stations = new List<StationDefinition>();
            var stationIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var station in data.stations)
            {
                Require(station != null, "Null station.");
                var snapshot = new StationDefinition(station);
                Require(stationIds.Add(snapshot.Id), "Duplicate station ID: " + snapshot.Id);
                stations.Add(snapshot);
            }
            Require(stationIds.Contains(InitialStationId), "Initial station does not exist.");
            Require(stations.Find(s => s.Id == InitialStationId).Enabled, "Initial station is locked.");
            Stations = stations.AsReadOnly();

            Require(data.packages != null, "Parcels are missing.");
            var parcels = new List<ParcelSnapshot>();
            parcelsById = new Dictionary<string, ParcelSnapshot>(StringComparer.Ordinal);
            var ownerSlots = new HashSet<string>(StringComparer.Ordinal);
            foreach (var parcel in data.packages)
            {
                Require(parcel != null && parcel.onlyForTest, "Every parcel must be test-only.");
                var snapshot = new ParcelSnapshot(parcel);
                Require(!parcelsById.ContainsKey(snapshot.Id), "Duplicate parcel ID: " + snapshot.Id);
                Require(ownerSlots.Add(snapshot.OwnerKey), "Two parcels occupy the same owner slot.");
                parcelsById.Add(snapshot.Id, snapshot);
                parcels.Add(snapshot);
            }
            Parcels = parcels.AsReadOnly();

            Require(data.mockTruckRuns != null, "Truck runs are missing.");
            var runs = new List<TruckRunSnapshot>();
            var runIds = new HashSet<string>(StringComparer.Ordinal);
            var cargoIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var run in data.mockTruckRuns)
            {
                Require(run != null, "Null truck run.");
                var snapshot = new TruckRunSnapshot(run);
                Require(runIds.Add(snapshot.RunId), "Duplicate run ID.");
                foreach (var id in snapshot.CargoParcelIds)
                {
                    ParcelSnapshot parcel;
                    Require(cargoIds.Add(id) && parcelsById.TryGetValue(id, out parcel), "Duplicate or unknown truck parcel.");
                    parcel = parcelsById[id];
                    Require(parcel.OwnerKind == "TRUCK_CARGO" && parcel.OwnerEntityId == snapshot.RunId,
                            "Truck manifest and parcel owner differ.");
                }
                runs.Add(snapshot);
            }
            foreach (var parcel in parcels)
                if (parcel.OwnerKind == "TRUCK_CARGO")
                    Require(cargoIds.Contains(parcel.Id), "Truck parcel missing from manifest.");
            TruckRuns = runs.AsReadOnly();
            Gate = new ExportGateSnapshot(data.fakeExportGate);
            Ghost = new GhostFixtureSnapshot(data.ghostConstruction);
            Fingerprint = CalculateFingerprint();
        }

        public ParcelSnapshot GetParcel(string id)
        {
            ParcelSnapshot value;
            return id != null && parcelsById.TryGetValue(id, out value) ? value : null;
        }

        private string CalculateFingerprint()
        {
            var b = new StringBuilder();
            Add(b, ProfileId); Add(b, Day); Add(b, Weekday); Add(b, DisplayTime); Add(b, CurrencyUSD); Add(b, Tier);
            foreach (var p in Parcels)
            {
                Add(b, p.Id); Add(b, p.OwnerKey); Add(b, p.Cargo); Add(b, p.CountryName); Add(b, p.FlagId);
                Add(b, p.MassGrams); Add(b, p.InspectionStatus);
                Add(b, p.TruthCargoValid); Add(b, p.TruthCountryValid);
            }
            foreach (var r in TruckRuns)
            {
                Add(b, r.RunId); Add(b, r.DockId); Add(b, r.State);
                foreach (var id in r.CargoParcelIds) Add(b, id);
            }
            Add(b, Gate.GateId); Add(b, Gate.CargoMode); Add(b, Gate.MaxCapacity); Add(b, Gate.CurrentBufferedCount);
            Add(b, Gate.LastBatchSummary.CorrectCount); Add(b, Gate.LastBatchSummary.CorrectParcelValueUSD);
            Add(b, Gate.LastBatchSummary.IncorrectCount); Add(b, Gate.LastBatchSummary.IncorrectParcelValueUSD);
            Add(b, Gate.LastBatchSummary.HandlingFeeUSD); Add(b, Gate.LastBatchSummary.NetUSD);
            foreach (var c in Ghost.OccupiedCells) { Add(b, c.Floor); Add(b, c.X); Add(b, c.Z); }
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(b.ToString()));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private static void Add(StringBuilder b, object value)
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
            b.Append(text.Length).Append(':').Append(text).Append(';');
        }

        internal static string Text(string value, string name)
        {
            Require(!string.IsNullOrWhiteSpace(value), "Missing " + name + ".");
            return value;
        }

        internal static void Require(bool condition, string message)
        {
            if (!condition) throw new ArgumentException(message);
        }
    }

    public sealed class StationDefinition
    {
        public string Id { get; }
        public string Category { get; }
        public string CameraAnchor { get; }
        public string UiPanel { get; }
        public bool Enabled { get; }
        public string LockedReason { get; }
        public StationDefinition(StationFixtureData source)
        {
            ReadOnlyMockWorld.Require(source != null, "Null station.");
            Id = ReadOnlyMockWorld.Text(source.id, "station ID");
            Category = ReadOnlyMockWorld.Text(source.category, "station category");
            CameraAnchor = ReadOnlyMockWorld.Text(source.cameraAnchor, "camera anchor");
            UiPanel = ReadOnlyMockWorld.Text(source.uiPanel, "UI panel");
            Enabled = source.enabled;
            LockedReason = Enabled ? "" : string.IsNullOrEmpty(source.lockedReason) ? "未解放（条件未設定）" : source.lockedReason;
        }
    }

    public sealed class ParcelSnapshot
    {
        public string Id { get; }
        public string OwnerKind { get; }
        public string OwnerEntityId { get; }
        public string OwnerSlot { get; }
        public string Cargo { get; }
        public string CountryName { get; }
        public string FlagId { get; }
        public int MassGrams { get; }
        public string InspectionStatus { get; }
        // Preserved for domain invariants/fingerprint, inaccessible to the UI assembly.
        internal bool TruthCargoValid { get; }
        internal bool TruthCountryValid { get; }
        internal string OwnerKey => OwnerKind + "\u001f" + OwnerEntityId + "\u001f" + OwnerSlot;
        internal ParcelSnapshot(ParcelFixtureData source)
        {
            ReadOnlyMockWorld.Require(source.owner != null && source.visibleLabel != null && source.hiddenTruth != null,
                                     "Parcel owner/label/truth missing.");
            Id = ReadOnlyMockWorld.Text(source.id, "parcel ID");
            OwnerKind = ReadOnlyMockWorld.Text(source.owner.kind, "owner kind");
            OwnerEntityId = ReadOnlyMockWorld.Text(source.owner.entityId, "owner entity ID");
            OwnerSlot = ReadOnlyMockWorld.Text(source.owner.slot, "owner slot");
            Cargo = ReadOnlyMockWorld.Text(source.visibleLabel.cargo, "visible cargo");
            CountryName = ReadOnlyMockWorld.Text(source.visibleLabel.countryName, "visible country");
            FlagId = ReadOnlyMockWorld.Text(source.visibleLabel.flagId, "visible flag");
            ReadOnlyMockWorld.Require(source.massGrams >= 0, "Negative parcel mass.");
            MassGrams = source.massGrams;
            InspectionStatus = source.inspectionStatus ?? "NotStarted";
            TruthCargoValid = source.hiddenTruth.cargoValid;
            TruthCountryValid = source.hiddenTruth.countryValid;
            // InspectionTruth is deliberately not exposed to presentation.
        }
    }

    public sealed class TruckRunSnapshot
    {
        public string RunId { get; }
        public string DockId { get; }
        public string State { get; }
        public IReadOnlyList<string> CargoParcelIds { get; }
        internal TruckRunSnapshot(TruckRunFixtureData source)
        {
            RunId = ReadOnlyMockWorld.Text(source.runId, "run ID");
            DockId = ReadOnlyMockWorld.Text(source.dockId, "dock ID");
            State = ReadOnlyMockWorld.Text(source.state, "truck state");
            ReadOnlyMockWorld.Require(source.cargoParcelIds != null, "Missing truck manifest.");
            CargoParcelIds = Array.AsReadOnly((string[])source.cargoParcelIds.Clone());
        }
    }

    public sealed class ExportGateSnapshot
    {
        public string GateId { get; }
        public string CargoMode { get; }
        public int MaxCapacity { get; }
        public int CurrentBufferedCount { get; }
        public string CapacityMeaning { get; }
        public string LastBatchSummarySource { get; }
        public BatchSummarySnapshot LastBatchSummary { get; }
        internal ExportGateSnapshot(ExportGateFixtureData source)
        {
            ReadOnlyMockWorld.Require(source != null && !source.actualSubmitAllowed && !source.actualMoneyMutationAllowed,
                "S1 export must be display-only.");
            GateId = ReadOnlyMockWorld.Text(source.gateId, "gate ID");
            CargoMode = ReadOnlyMockWorld.Text(source.cargoMode, "gate cargo mode");
            ReadOnlyMockWorld.Require(source.maxCapacity >= 0 && source.currentBufferedCount >= 0 &&
                                     source.currentBufferedCount <= source.maxCapacity, "Invalid demo gate count.");
            MaxCapacity = source.maxCapacity;
            CurrentBufferedCount = source.currentBufferedCount;
            CapacityMeaning = source.capacityMeaning ?? "試作表示のみ";
            LastBatchSummarySource = source.visualLastBatchSummarySource ?? "P";
            LastBatchSummary = new BatchSummarySnapshot(source.visualLastBatchSummary);
        }
    }

    public sealed class BatchSummarySnapshot
    {
        public int CorrectCount { get; }
        public decimal CorrectParcelValueUSD { get; }
        public int IncorrectCount { get; }
        public decimal IncorrectParcelValueUSD { get; }
        public decimal HandlingFeeUSD { get; }
        public decimal NetUSD { get; }
        internal BatchSummarySnapshot(BatchSummaryFixtureData source)
        {
            ReadOnlyMockWorld.Require(source != null && source.correctCount >= 0 && source.incorrectCount >= 0,
                                     "Missing/invalid previous batch display.");
            CorrectCount = source.correctCount; CorrectParcelValueUSD = source.correctParcelValueUSD;
            IncorrectCount = source.incorrectCount; IncorrectParcelValueUSD = source.incorrectParcelValueUSD;
            HandlingFeeUSD = source.handlingFeeUSD; NetUSD = source.netUSD;
        }
    }

    public readonly struct GridCell : IEquatable<GridCell>
    {
        public int Floor { get; }
        public int X { get; }
        public int Z { get; }
        public GridCell(int x, int z, int floor = 0) { Floor = floor; X = x; Z = z; }
        public bool Equals(GridCell other) => Floor == other.Floor && X == other.X && Z == other.Z;
        public override bool Equals(object obj) => obj is GridCell && Equals((GridCell)obj);
        public override int GetHashCode() => unchecked((Floor * 397 ^ X) * 397 ^ Z);
    }

    public sealed class GhostFixtureSnapshot
    {
        public string EquipmentTypeId { get; }
        public GridCell InitialPosition { get; }
        public int InitialRotationQuarterTurns { get; }
        public bool InitialFlipped { get; }
        public IReadOnlyList<GridCell> OccupiedCells { get; }
        internal GhostFixtureSnapshot(GhostFixtureData source)
        {
            ReadOnlyMockWorld.Require(source != null && !source.placementTransactionsEnabled && source.noCostOrInventoryMutation,
                                     "S1 ghost cannot enable placement transactions.");
            EquipmentTypeId = ReadOnlyMockWorld.Text(source.equipmentTypeId, "ghost type ID");
            ReadOnlyMockWorld.Require(source.gridPosition != null && source.exampleOccupiedCells != null, "Missing ghost cells.");
            InitialPosition = new GridCell(source.gridPosition.x, source.gridPosition.z, source.gridPosition.floor);
            InitialRotationQuarterTurns = ((source.rotationQuarterTurns % 4) + 4) % 4;
            InitialFlipped = source.flipped;
            var occupied = new List<GridCell>();
            foreach (var cell in source.exampleOccupiedCells)
            {
                ReadOnlyMockWorld.Require(cell != null, "Null occupied cell.");
                occupied.Add(new GridCell(cell.x, cell.z, cell.floor));
            }
            OccupiedCells = occupied.AsReadOnly();
        }
    }
}
