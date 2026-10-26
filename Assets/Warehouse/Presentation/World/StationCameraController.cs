using System.Collections.Generic;
using UnityEngine;

namespace Warehouse.Presentation
{
    /// <summary>One main camera, cached poses and instantaneous cuts. No scene loads or character movement.</summary>
    public sealed class StationCameraController : MonoBehaviour
    {
        private struct ViewPose { public Vector3 Position, Focus; public Quaternion Rotation; }
        private readonly Dictionary<string, ViewPose> poses = new Dictionary<string, ViewPose>();
        private Camera mainCamera;
        private WarehouseSceneBlockout world;
        private StationCameraAnchor profile;
        private Vector3 focus;
        public string ActiveStationId { get; private set; }
        public Camera MainCamera => mainCamera;
        public int SwitchCount { get; private set; }
        public bool CanZoom => profile != null && profile.AllowZoom;
        public void Initialize(Camera camera, WarehouseSceneBlockout scene)
        {
            mainCamera = camera; world = scene;
            mainCamera.nearClipPlane = 0.1f; mainCamera.farClipPlane = 90f;
            mainCamera.fieldOfView = 48f;
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.19f, 0.24f, 0.29f);
        }
        public bool TrySwitch(string stationId, string anchorId, out string error)
        {
            error = null;
            var anchor = world == null ? null : world.GetAnchor(anchorId);
            if (mainCamera == null || string.IsNullOrEmpty(stationId) || anchor == null)
            { error = "作業場所のカメラがありません"; return false; }
            if (!string.IsNullOrEmpty(ActiveStationId)) SavePose();
            profile = anchor.GetComponent<StationCameraAnchor>();
            ActiveStationId = stationId;
            if (poses.TryGetValue(stationId, out var pose))
            { mainCamera.transform.SetPositionAndRotation(pose.Position, pose.Rotation); focus = pose.Focus; }
            else
            {
                mainCamera.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
                focus = profile != null ? profile.FocusPosition : anchor.position + anchor.forward * 5f;
            }
            world.ShowStation(stationId);
            SwitchCount++;
            return true;
        }
        private void SavePose()
        {
            poses[ActiveStationId] = new ViewPose { Position = mainCamera.transform.position, Rotation = mainCamera.transform.rotation, Focus = focus };
        }
        public void Pan(Vector2 pixelDelta)
        {
            if (mainCamera == null || profile == null || !profile.AllowPan) return;
            var right = Vector3.ProjectOnPlane(mainCamera.transform.right, Vector3.up).normalized;
            var up = Vector3.ProjectOnPlane(mainCamera.transform.up, Vector3.up).normalized;
            var delta = (-right * pixelDelta.x - up * pixelDelta.y) * 0.014f;
            var next = focus + delta;
            next.x = Mathf.Clamp(next.x, -6f, 6f); next.z = Mathf.Clamp(next.z, -5f, 7f);
            mainCamera.transform.position += next - focus; focus = next;
            SavePose();
        }
        public void Zoom(float delta)
        {
            if (mainCamera == null || !CanZoom) return;
            var offset = mainCamera.transform.position - focus;
            var distance = Mathf.Clamp(offset.magnitude - delta * 0.025f, profile.MinDistance, profile.MaxDistance);
            mainCamera.transform.position = focus + offset.normalized * distance;
            SavePose();
        }
        public bool Focus(string entityId)
        {
            if (mainCamera == null || !world.TryGetEntity(entityId, out var entity)) return false;
            focus = entity.transform.position + Vector3.up * 0.35f;
            mainCamera.transform.LookAt(focus); SavePose(); return true;
        }
    }
}
