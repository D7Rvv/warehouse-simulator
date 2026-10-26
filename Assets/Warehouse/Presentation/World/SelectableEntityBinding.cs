using UnityEngine;

namespace Warehouse.Presentation
{
    /// <summary>Presentation-only target; selecting never transfers parcel ownership.</summary>
    public sealed class SelectableEntityBinding : MonoBehaviour
    {
        [SerializeField] private string entityId;
        [SerializeField] private string stationId;
        [SerializeField] private string interactionKind;
        public string EntityId => entityId;
        public string StationId => stationId;
        public string InteractionKind => interactionKind;
        public void Configure(string id, string station, string kind)
        {
            entityId = id; stationId = station; interactionKind = kind;
        }
    }
}
