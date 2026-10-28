#!/usr/bin/env python3
"""Check S0/S1 configuration and execute pure C# fixture/input scenarios.

This command does not open Unity, render a frame, build a native app, or measure
phone FPS/temperature. Those results remain NOT_RUN until actually executed.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
from datetime import datetime, timezone

ROOT = Path(__file__).resolve().parents[2]
FIXTURE = ROOT / "docs/data/prototype_station_fixture_v0_1.json"
RESOURCE = ROOT / "Assets/Resources/Prototype/prototype_station_fixture_v0_1.json"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def static_checks() -> list[dict]:
    checks = []

    def check(name, operation):
        try:
            operation()
            checks.append({"id": name, "status": "PASS", "scope": "static_configuration"})
        except (OSError, ValueError, KeyError, TypeError) as error:
            checks.append({"id": name, "status": "FAIL", "detail": str(error), "scope": "static_configuration"})

    def versions():
        text = (ROOT / "ProjectSettings/ProjectVersion.txt").read_text()
        require(re.search(r"^m_EditorVersion: 6000\.3\.27f1$", text, re.M) is not None, "Editor version must be exactly pinned")
        dependencies = json.loads((ROOT / "Packages/manifest.json").read_text())["dependencies"]
        for package in ("com.unity.inputsystem", "com.unity.render-pipelines.universal", "com.unity.ugui", "com.unity.test-framework"):
            require(re.fullmatch(r"\d+\.\d+\.\d+", dependencies[package]) is not None, "Dependency not exact: " + package)
        require(dependencies["com.unity.render-pipelines.universal"].startswith("17.3."), "Unity6.3 requires matching URP17.3")
    check("STATIC-001 EditorAndPackagesPinned", versions)

    def project():
        settings = (ROOT / "ProjectSettings/ProjectSettings.asset").read_text()
        require(re.search(r"activeInputHandler: 1\b", settings) is not None, "New Input System only must be selected")
        require(re.search(r"defaultScreenOrientation: (3|4)\b", settings) is not None, "Landscape orientation not configured")
        build = (ROOT / "ProjectSettings/EditorBuildSettings.asset").read_text()
        require("Assets/Scenes/WarehouseStation.unity" in build, "Startup scene not in build")
        scene = ROOT / "Assets/Scenes/WarehouseStation.unity"
        require(scene.is_file() and scene.with_suffix(".unity.meta").is_file(), "Scene/meta missing")
        appmeta = (ROOT / "Assets/Warehouse/Presentation/StationApp.cs.meta").read_text()
        guid = re.search(r"^guid: ([a-f0-9]{32})$", appmeta, re.M).group(1)
        require(guid in scene.read_text(), "Scene does not reference real StationApp script GUID")
    check("STATIC-002 NativeProjectAndStartupScene", project)

    def legacy():
        paths = sorted((ROOT / "Assets/Scripts/Phase1").glob("*.cs"))
        require(len(paths) == 5, "Legacy source inventory changed unexpectedly")
        for path in paths:
            text = path.read_text().strip()
            require(text.startswith("#if WAREHOUSE_LEGACY_FREE_WALK") and text.endswith("#endif"), "Legacy not fully compile-guarded: " + path.name)
        settings = (ROOT / "ProjectSettings/ProjectSettings.asset").read_text()
        # A guarded source is unsafe if any active build target defines its guard.
        require("WAREHOUSE_LEGACY_FREE_WALK" not in settings, "Legacy guard enabled in player defines")
    check("STATIC-003 LegacyFreeWalkInactive", legacy)

    def fixture():
        require(RESOURCE.read_bytes() == FIXTURE.read_bytes(), "Runtime fixture differs from canonical docs fixture")
        d = json.loads(FIXTURE.read_text())
        require(d["prototypeOnly"] and not d["promoteToProduction"] and d["sourceStatus"] == "P", "Prototype isolation missing")
        require(not d["fakeExportGate"]["actualSubmitAllowed"] and not d["fakeExportGate"]["actualMoneyMutationAllowed"], "Real export writes enabled")
        require(not d["ghostConstruction"]["placementTransactionsEnabled"], "Actual placement enabled")
        ids = {p["id"] for p in d["packages"]}
        require(len(ids) == len(d["packages"]), "Duplicate parcel IDs")
        require(ids == {i for r in d["mockTruckRuns"] for i in r["cargoParcelIds"]}, "Truck mock does not match parcels")
    check("STATIC-004 CanonicalFixtureParity", fixture)

    def assemblies():
        domain = json.loads((ROOT / "Assets/Warehouse/Domain/Warehouse.Domain.asmdef").read_text())
        require(domain.get("noEngineReferences") is True, "Domain depends on UnityEngine")
        require(not domain.get("references"), "Domain imports presentation/UI")
        ui = json.loads((ROOT / "Assets/Warehouse/UI/Warehouse.UI.asmdef").read_text())
        require("Warehouse.Presentation" not in ui.get("references", []), "UI creates presentation dependency cycle")
        for name in ("EditMode", "PlayMode"):
            p = ROOT / f"Assets/Warehouse/Tests/{name}/Warehouse.Tests.{name}.asmdef"
            config = json.loads(p.read_text())
            require("TestAssemblies" in config.get("optionalUnityReferences", []), "Tests leak into normal player assembly")
    check("STATIC-005 AssemblyAndTestBoundaries", assemblies)

    def urp():
        asset = ROOT / "Assets/Warehouse/Settings/Mobile_RPAsset.asset"
        guid = re.search(r"^guid: ([a-f0-9]{32})$", asset.with_suffix(".asset.meta").read_text(), re.M).group(1)
        require(guid in (ROOT / "ProjectSettings/GraphicsSettings.asset").read_text(), "URP asset not bound in GraphicsSettings")
        require(guid in (ROOT / "ProjectSettings/QualitySettings.asset").read_text(), "URP asset not bound in QualitySettings")
        require((ROOT / "Assets/Warehouse/Settings/Mobile_Renderer.asset").is_file(), "URP renderer asset missing")
    check("STATIC-006 UrpAssetsBound", urp)

    def profiles():
        for platform in ("Android", "iOS"):
            profile = json.loads((ROOT / f"Assets/Warehouse/Settings/Profiles/{platform}Development.json").read_text())
            require(profile["platform"] == platform and profile["architecture"] == "ARM64", "Wrong native platform profile")
            require(profile["buildStatus"] == "NOT_RUN" and profile["deviceStatus"] == "NOT_RUN", "Unexecuted native build/device marked successful")
            require(profile["thermal30Minutes"] == "NOT_RUN" and not profile["signingConfigured"], "Unverified thermal/signing status")
    check("STATIC-007 HonestNativeProfiles", profiles)

    def metas():
        guids = {}
        for meta in (ROOT / "Assets").rglob("*.meta"):
            match = re.search(r"^guid: ([a-f0-9]{32})$", meta.read_text(), re.M)
            require(match is not None, "Invalid GUID in " + str(meta.relative_to(ROOT)))
            guid = match.group(1)
            require(guid not in guids, "Duplicate asset GUID: " + str(meta))
            guids[guid] = meta
        for directory in (ROOT / "Assets/Warehouse", ROOT / "Assets/Scenes"):
            for asset in directory.rglob("*"):
                if asset.is_file() and asset.suffix != ".meta":
                    require(Path(str(asset) + ".meta").is_file(), "Missing asset meta: " + str(asset.relative_to(ROOT)))
    check("STATIC-008 StableAssetGuids", metas)
    return checks


def run_logic(editor: Path, output: Path) -> dict:
    data = editor.parent / "Data/MonoBleedingEdge"
    runtime = data / "bin-linux64/mono"
    compiler = data / "lib/mono/4.5/csc.exe"
    web = data / "lib/mono/4.5/System.Web.Extensions.dll"
    if not runtime.is_file() or not compiler.is_file() or not web.is_file():
        return {"status": "NOT_RUN", "reason": "Unity-bundled Mono/Roslyn/System.Web.Extensions not available", "scope": "pure_csharp"}
    sources = sorted((ROOT / "Assets/Warehouse/Domain").glob("*.cs")) + [
        ROOT / "Assets/Warehouse/Presentation/Input/StationPointerOwnership.cs",
        ROOT / "Assets/Warehouse/Tests/EditMode/DomainScenarios.cs",
        ROOT / "Assets/Warehouse/Tests/EditMode/PointerScenarios.cs",
        ROOT / "Assets/Warehouse/Tests/Standalone/RunDomainTests.cs",
    ]
    executable = output / "domain-pointer-tests.exe"
    command = [str(runtime), str(compiler), "-nologo", "-langversion:9", "-define:WAREHOUSE_STANDALONE_TESTS", "-r:" + str(web), "-out:" + str(executable)] + [str(p) for p in sources]
    compiled = subprocess.run(command, cwd=ROOT, text=True, capture_output=True, timeout=60)
    (output / "logic-compile.log").write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode:
        return {"status": "FAIL", "stage": "pure_csharp_compile", "exit_code": compiled.returncode, "report": str(output / "logic-compile.log")}
    result = subprocess.run([str(runtime), str(executable), str(FIXTURE)], cwd=ROOT, text=True, capture_output=True, timeout=60)
    log = result.stdout + result.stderr
    (output / "logic-tests.log").write_text(log)
    checks = [{"id": line.split(" ", 2)[1], "label": line[5:], "status": "PASS_LOGIC"} for line in log.splitlines() if line.startswith("PASS ")]
    return {"status": "PASS_LOGIC" if result.returncode == 0 else "FAIL", "scope": "actual_pure_csharp_no_unity_engine",
            "exit_code": result.returncode, "passed": len(checks), "checks": checks,
            "fixture_sha256": sha(FIXTURE),
            "compiled_source_sha256": {str(p.relative_to(ROOT)): sha(p) for p in sources},
            "log": str((output / "logic-tests.log").relative_to(ROOT))}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--editor", type=Path, default=Path(os.environ.get("UNITY_EDITOR", "/workspace/.warehouse-cloud/unity/6000.3.27f1/Editor/Unity")))
    parser.add_argument("--output", type=Path, default=ROOT / "builds/s01")
    parser.add_argument("--logic-only", action="store_true")
    parser.add_argument("--static-only", action="store_true")
    args = parser.parse_args()
    require(not(args.logic_only and args.static_only), "Choose one restricted mode")
    output = args.output.resolve()
    require(output.is_relative_to(ROOT / "builds"), "Evidence output must stay under ignored builds/")
    output.mkdir(parents=True, exist_ok=True)
    static = [] if args.logic_only else static_checks()
    logic = {"status": "NOT_RUN", "reason": "--static-only"} if args.static_only else run_logic(args.editor.resolve(), output)
    report = {"generated_at": datetime.now(timezone.utc).isoformat(), "static_checks": static, "pure_csharp": logic,
              "unity_editor": "NOT_RUN_BY_THIS_COMMAND", "unity_playmode": "NOT_RUN_BY_THIS_COMMAND",
              "android_device": "NOT_RUN", "ios_device": "NOT_RUN", "thermal_30_minutes": "NOT_RUN"}
    (output / "validation.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n")
    for item in static:
        print(item["status"] + " " + item["id"] + (": " + item["detail"] if "detail" in item else ""))
    print("Pure C# " + logic["status"] + ": " + str(logic.get("passed", 0)) + " scenarios")
    print("Unity rendering/native phone/thermal results: NOT_RUN by this validator")
    return 1 if any(c["status"] == "FAIL" for c in static) or logic["status"] == "FAIL" else 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError, subprocess.TimeoutExpired) as error:
        raise SystemExit("VALIDATION BLOCKED: " + str(error))
