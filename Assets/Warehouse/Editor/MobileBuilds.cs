using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Warehouse.Editor
{
    /// <summary>Separate development export commands; never contains signing credentials.</summary>
    public static class MobileBuilds
    {
        private const string Identifier = "org.d7rvv.warehousesimulator";
        [MenuItem("Warehouse/Build/Android development APK")]
        public static void AndroidDevelopment()
        {
            ProjectValidation.Validate();
            RequireModule(BuildTarget.Android);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, Identifier);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.Android.useCustomKeystore = false;
            EditorUserBuildSettings.buildAppBundle = false;
            Export(BuildTarget.Android, "Builds/Android/WarehouseSimulator-development.apk");
        }

        [MenuItem("Warehouse/Build/iOS development Xcode project")]
        public static void IosDevelopment()
        {
            ProjectValidation.Validate();
            RequireModule(BuildTarget.iOS);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, Identifier);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            // This exports an Xcode project, not a signed IPA. Configure the real Team ID in Xcode.
            Export(BuildTarget.iOS, "Builds/iOS/Xcode");
        }

        private static void RequireModule(BuildTarget target)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
                throw new InvalidOperationException(target + " Build Support is not installed; native export NOT_RUN.");
        }
        private static void Export(BuildTarget target, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                target = target, locationPathName = path, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(target + " export failed: " + report.summary.result);
            Debug.Log(target + " development export completed; physical-device and thermal tests remain NOT_RUN.");
        }
    }
}
