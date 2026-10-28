using System;
using System.Collections.Generic;

namespace Warehouse.Domain
{
    /// <summary>View state only: these fields never act as parcel ownership.</summary>
    public sealed class StationSelection
    {
        public string ActiveStationId { get; internal set; }
        public string ActiveTargetId { get; internal set; }
        public string PreviousStationId { get; internal set; }
        public string SelectedParcelId { get; set; }
        public string HandSlotParcelId { get; set; }
        public string InspectionDraftId { get; set; }
        public float InspectionRotationDegrees { get; set; }
        public bool LabelExpanded { get; set; }
        public string ConstructionPreviewId { get; set; }
    }

    /// <summary>
    /// Resolves nav and entity taps through one path. Does not own a clock,
    /// ledger or transfer service, so navigation cannot write gameplay data.
    /// </summary>
    public sealed class MobileStationRegistry
    {
        private readonly Dictionary<string, StationDefinition> definitions =
            new Dictionary<string, StationDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> targets =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> rememberedTargets =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private Func<string, bool> anchorExists;
        public StationSelection Selection { get; } = new StationSelection();
        public StationDefinition Current => Find(Selection.ActiveStationId);

        public MobileStationRegistry(IEnumerable<StationDefinition> stations, string initialStationId,
                                     Func<string, bool> anchorExists = null)
        {
            if (stations == null) throw new ArgumentNullException(nameof(stations));
            foreach (var station in stations)
            {
                if (station == null || definitions.ContainsKey(station.Id))
                    throw new ArgumentException("Null/duplicate station definition.");
                definitions.Add(station.Id, station);
            }
            this.anchorExists = anchorExists;
            string reason;
            if (!TrySelect(initialStationId, null, out reason)) throw new ArgumentException(reason);
        }

        public StationDefinition Find(string stationId)
        {
            StationDefinition definition;
            return stationId != null && definitions.TryGetValue(stationId, out definition) ? definition : null;
        }

        public void SetAnchorValidator(Func<string, bool> exists) { anchorExists = exists; }

        public void RegisterTarget(string targetId, string stationId)
        {
            if (string.IsNullOrWhiteSpace(targetId) || Find(stationId) == null)
                throw new ArgumentException("A target must reference an existing station.");
            string previous;
            if (targets.TryGetValue(targetId, out previous) && previous != stationId)
                throw new ArgumentException("A target cannot belong to two stations.");
            targets[targetId] = stationId;
        }

        public bool TrySelect(string stationId, string targetId, out string reason)
        {
            var station = Find(stationId);
            if (station == null) { reason = "作業場所が見つかりません。現在の画面を維持します。"; return false; }
            if (!station.Enabled) { reason = station.LockedReason; return false; }
            if (anchorExists != null && !anchorExists(station.CameraAnchor))
            { reason = "カメラ位置が見つかりません。現在の画面を維持します。"; return false; }

            string remembered;
            if (string.IsNullOrEmpty(targetId) && rememberedTargets.TryGetValue(stationId, out remembered))
                targetId = remembered;
            if (!string.IsNullOrEmpty(targetId))
            {
                string belongsTo;
                if (!targets.TryGetValue(targetId, out belongsTo) || belongsTo != stationId)
                { reason = "選択対象が存在しないか、この作業場所に属していません。"; return false; }
            }
            if (Selection.ActiveStationId != stationId)
                Selection.PreviousStationId = Selection.ActiveStationId;
            Selection.ActiveStationId = stationId;
            Selection.ActiveTargetId = targetId;
            if (!string.IsNullOrEmpty(targetId)) rememberedTargets[stationId] = targetId;
            reason = "";
            return true;
        }

        public void TargetRemoved(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            targets.Remove(targetId);
            // Enumerate into a small list, never mutate a dictionary mid-iteration.
            var clear = new List<string>();
            foreach (var pair in rememberedTargets)
                if (pair.Value == targetId) clear.Add(pair.Key);
            foreach (var stationId in clear) rememberedTargets.Remove(stationId);
            if (Selection.ActiveTargetId == targetId) Selection.ActiveTargetId = null;
            if (Selection.SelectedParcelId == targetId) Selection.SelectedParcelId = null;
            // Presentation target deletion cannot empty HandSlot or delete a draft.
        }
    }
}
