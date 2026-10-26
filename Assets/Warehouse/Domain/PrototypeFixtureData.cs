using System;

namespace Warehouse.Domain
{
    // Serialization DTOs only. Never pass these mutable objects to presentation:
    // ReadOnlyMockWorld validates and deep-copies them before rendering begins.
    [Serializable]
    public sealed class PrototypeFixtureData
    {
        public string schemaVersion;
        public string description;
        public bool prototypeOnly;
        public bool promoteToProduction;
        public string sourceStatus;
        public bool mustNeverOverrideReferenceCatalog;
        public string gameplayAuthority;
        public WorldFixtureData world;
        public UiFixtureData ui;
        public StationFixtureData[] stations;
        public ParcelFixtureData[] packages;
        public TruckRunFixtureData[] mockTruckRuns;
        public ExportGateFixtureData fakeExportGate;
        public GhostFixtureData ghostConstruction;
        public string[] cameraMockDoesNotModify;
    }

    [Serializable]
    public sealed class WorldFixtureData
    {
        public string profileId;
        public int day;
        public string weekday;
        public string time;
        public long currencyUSD;
        public string currencySourceStatus;
        public int tier;
        public bool simulationClockResetOnCameraChange;
        public double tickSeconds;
        public string initialStation;
    }

    [Serializable]
    public sealed class UiFixtureData
    {
        public string orientation;
        public string mode;
        public string[] mainNavigation;
        public int stationTransitionMs;
        public string sourceStatus;
        public bool showPrototypeWatermark;
        public bool autoCompleteParcelByMenu;
    }

    [Serializable]
    public sealed class StationFixtureData
    {
        public string id;
        public string category;
        public string cameraAnchor;
        public string uiPanel;
        public bool enabled;
        public string lockedReason;
    }

    [Serializable]
    public sealed class ParcelFixtureData
    {
        public string id;
        public OwnerFixtureData owner;
        public VisibleLabelFixtureData visibleLabel;
        public InspectionTruthFixtureData hiddenTruth;
        public int massGrams;
        public string inspectionStatus;
        public bool onlyForTest;
    }

    [Serializable]
    public sealed class OwnerFixtureData
    {
        public string kind;
        public string entityId;
        public string slot;
    }

    [Serializable]
    public sealed class VisibleLabelFixtureData
    {
        public string cargo;
        public string countryName;
        public string flagId;
    }

    [Serializable]
    public sealed class InspectionTruthFixtureData
    {
        public bool cargoValid;
        public bool countryValid;
    }

    [Serializable]
    public sealed class TruckRunFixtureData
    {
        public string runId;
        public string dockId;
        public string state;
        public string[] cargoParcelIds;
        public string note;
    }

    [Serializable]
    public sealed class ExportGateFixtureData
    {
        public string gateId;
        public string cargoMode;
        public int maxCapacity;
        public int currentBufferedCount;
        public string capacityMeaning;
        public BatchSummaryFixtureData visualLastBatchSummary;
        public string visualLastBatchSummarySource;
        public bool actualSubmitAllowed;
        public bool actualMoneyMutationAllowed;
    }

    [Serializable]
    public sealed class BatchSummaryFixtureData
    {
        public int correctCount;
        public long correctParcelValueUSD;
        public int incorrectCount;
        public long incorrectParcelValueUSD;
        public long handlingFeeUSD;
        public long netUSD;
    }

    [Serializable]
    public sealed class GhostFixtureData
    {
        public string equipmentTypeId;
        public GridCellFixtureData gridPosition;
        public int rotationQuarterTurns;
        public bool flipped;
        public string validGhostColor;
        public string invalidGhostColor;
        public bool placementTransactionsEnabled;
        public bool noCostOrInventoryMutation;
        public GridCellFixtureData[] exampleOccupiedCells;
    }

    [Serializable]
    public sealed class GridCellFixtureData
    {
        public int floor;
        public int x;
        public int z;
    }
}
