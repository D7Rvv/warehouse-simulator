using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WarehouseSimulator.Phase1
{
    /// <summary>
    /// Phase 1 scene builder: creates a playable mobile-first test scene at runtime.
    /// Create a NEW empty scene in a Unity URP project and press Play.
    /// No editor-only APIs or external art assets are required.
    /// </summary>
    public sealed class WarehouseBootstrap : MonoBehaviour
    {
        private const string RootName = "WarehousePrototypeRoot";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartPrototype()
        {
            if (FindFirstObjectByType<WarehouseBootstrap>() != null) return;
            var root = new GameObject(RootName);
            root.AddComponent<WarehouseBootstrap>();
            DontDestroyOnLoad(root);
        }

        private void Awake()
        {
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;

            foreach (var oldCamera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                oldCamera.enabled = false;

            WarehouseWorld.Build(transform);

            var player = new GameObject("Player");
            player.transform.SetParent(transform, false);
            player.transform.position = new Vector3(0f, 0.1f, 3f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.32f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.25f;

            var cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 65f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 75f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.62f, 0.74f, 0.81f);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.depth = 10f;
            cameraObject.AddComponent<AudioListener>();

            var motor = player.AddComponent<MobilePlayerController>();
            motor.Initialize(controller, camera);

            var performance = gameObject.AddComponent<AdaptiveMobileQuality>();
            MobileHud.Create(transform, motor, performance);
        }
    }
}
