using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Warehouse.Editor
{
    /// <summary>Licensed Editor/CI entry point. No project state is generated during validation.</summary>
    public static class ProjectValidation
    {
        [MenuItem("Warehouse/Validate S0 project")]
        public static void Validate()
        {
            if (Application.unityVersion != "6000.3.27f1")
                throw new InvalidOperationException("Use the pinned Unity Editor 6000.3.27f1.");
            if (GraphicsSettings.defaultRenderPipeline == null)
                throw new InvalidOperationException("The mobile URP pipeline is not assigned.");
            if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == "Assets/Scenes/WarehouseStation.unity"))
                throw new InvalidOperationException("WarehouseStation must be enabled in build settings.");
            if (!File.Exists("Assets/Resources/Prototype/prototype_station_fixture_v0_1.json"))
                throw new InvalidOperationException("The explicit prototype fixture is missing.");
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetType("WarehouseSimulator.Phase1.WarehouseBootstrap", false) != null ||
                    assembly.GetType("WarehouseSimulator.Phase1.MobilePlayerController", false) != null)
                    throw new InvalidOperationException("Legacy free-walk code must not be compiled.");
            Debug.Log("S0 PROJECT VALIDATION PASS: pinned Editor, URP, scene, fixture and disabled legacy.");
        }
    }
}
