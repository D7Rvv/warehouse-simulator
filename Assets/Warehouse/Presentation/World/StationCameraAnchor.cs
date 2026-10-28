using UnityEngine;

namespace Warehouse.Presentation
{
    /// <summary>[P] Camera-only constraints; unrelated to parcel or clock authority.</summary>
    public sealed class StationCameraAnchor : MonoBehaviour
    {
        public Vector3 FocusPosition { get; private set; }
        public float MinDistance { get; private set; }
        public float MaxDistance { get; private set; }
        public bool AllowPan { get; private set; }
        public bool AllowZoom { get; private set; }
        public void Configure(Vector3 focus, float min, float max, bool pan, bool zoom)
        {
            FocusPosition = focus; MinDistance = min; MaxDistance = max;
            AllowPan = pan; AllowZoom = zoom;
            transform.LookAt(focus);
        }
    }
}
