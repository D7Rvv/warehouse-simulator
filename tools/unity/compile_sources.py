#!/usr/bin/env python3
"""Additional real-reference C# API check. This is NOT a Unity import or player build.

The Editor ships cached template DLLs with older package versions. This check
catches source/type errors against real Unity assemblies, not stub APIs, but it
cannot certify the pinned URP/InputSystem versions or shader/asset serialization.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
PIN = "6000.3.27f1"

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--editor-root", type=Path, default=Path(os.environ.get("UNITY_EDITOR_ROOT", "/workspace/.warehouse-cloud/unity/" + PIN)))
    args = parser.parse_args()
    data = args.editor_root / "Editor/Data"
    cache = data / "Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.3d-cross-platform-17.0.14/ScriptAssemblies"
    output = ROOT / "Builds/Validation/csharp"
    output.mkdir(parents=True, exist_ok=True)
    mono = data / "MonoBleedingEdge/lib/mono/4.8-api"
    system = sorted(mono.glob("*.dll")) + [mono / "Facades/netstandard.dll"]
    engine = sorted((data / "Managed/UnityEngine").glob("UnityEngine*.dll"))
    editor = sorted((data / "Managed/UnityEngine").glob("UnityEditor*.dll"))
    packages = [cache / name for name in ["UnityEngine.UI.dll", "Unity.InputSystem.dll", "Unity.RenderPipelines.Core.Runtime.dll", "Unity.RenderPipelines.Universal.Runtime.dll", "Unity.Mathematics.dll", "Unity.Collections.dll"]]
    compiler = [str(data / "NetCoreRuntime/dotnet"), str(data / "DotNetSdkRoslyn/csc.dll")]
    required = compiler + [str(p) for p in system + engine + packages]
    if not all(Path(p).is_file() for p in required):
        print("NOT_RUN: required real Unity compiler/reference files are unavailable.")
        return 2
    defines = "ENABLE_INPUT_SYSTEM;UNITY_6000_0_OR_NEWER;UNITY_6000_3_OR_NEWER;UNITY_INCLUDE_TESTS"
    log = []
    compiled = []
    def compile_one(name, sources, references, editor_mode=False):
        if not sources:
            raise RuntimeError("No C# source for " + name)
        response = output / (name + ".rsp")
        args = ["-nostdlib+", "-target:library", "-langversion:9.0", "-nullable:disable", "-unsafe+", "-out:" + str(output / (name + ".dll")), "-define:" + defines + (";UNITY_EDITOR;UNITY_EDITOR_LINUX" if editor_mode else "")]
        args += ["-reference:" + str(p) for p in references]
        args += [str(p) for p in sources]
        response.write_text("\n".join('"' + arg + '"' for arg in args) + "\n")
        result = subprocess.run(compiler + ["-noconfig", "@" + str(response)], cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        log.append("===== " + name + " =====\n" + result.stdout)
        print(result.stdout, end="")
        if result.returncode != 0:
            raise RuntimeError(name + " C# source check failed")
        compiled.append({"assembly": name, "sources": {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in sources}})
    report = {"kind": "external_real_reference_csharp_check", "status": "FAIL", "unity_editor_import_executed": False, "native_build_executed": False,
              "reference_editor": PIN, "reference_package_versions": {"com.unity.inputsystem": "1.8.1", "com.unity.render-pipelines.universal": "17.0.1", "com.unity.ugui": "2.0.0"},
              "pinned_package_versions_validated": False,
              "limitation": "Cached official template package DLLs predate manifest pins (InputSystem1.17.0/URP17.3.0). This additional source/type check is not a licensed Unity compile, TestRunner, player build, or asset/shader validation."}
    try:
        compile_one("Warehouse.Domain", sorted((ROOT / "Assets/Warehouse/Domain").glob("*.cs")), system)
        domain = output / "Warehouse.Domain.dll"
        compile_one("Warehouse.UI", sorted((ROOT / "Assets/Warehouse/UI").rglob("*.cs")), system + engine + packages + [domain])
        ui = output / "Warehouse.UI.dll"
        compile_one("Warehouse.Presentation", sorted((ROOT / "Assets/Warehouse/Presentation").rglob("*.cs")), system + engine + packages + [domain, ui])
        presentation = output / "Warehouse.Presentation.dll"
        compile_one("Warehouse.Editor", sorted((ROOT / "Assets/Warehouse/Editor").rglob("*.cs")), system + engine + editor + packages + [domain, ui, presentation], True)
        test_refs = system + engine + editor + packages + [domain, ui, presentation, cache / "UnityEngine.TestRunner.dll", cache / "UnityEditor.TestRunner.dll", data / "Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net40/unity-custom/nunit.framework.dll"]
        for category in ["EditMode", "PlayMode"]:
            sources = sorted((ROOT / ("Assets/Warehouse/Tests/" + category)).rglob("*.cs"))
            if sources:
                compile_one("Warehouse.Tests." + category, sources, test_refs, True)
        report["status"] = "PASS"
    except RuntimeError as error:
        report["failure"] = str(error)
    report["assemblies"] = compiled
    report["reference_files"] = {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in engine + packages}
    (output / "compile.log").write_text("\n".join(log))
    (output / "report.json").write_text(json.dumps(report, indent=2) + "\n")
    print("EXTERNAL C# " + report["status"] + ": " + str(len(compiled)) + " assemblies; Unity import/native execution NOT_RUN; package-version limitation recorded.")
    return 0 if report["status"] == "PASS" else 1

if __name__ == "__main__":
    sys.exit(main())
