#!/usr/bin/env python3
"""Run the pinned licensed Unity Editor; distinguish NOT_RUN from a test failure."""
import argparse
import datetime
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
PIN = "6000.3.27f1"

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("task", choices=["validate", "editmode", "playmode", "android", "ios"])
    parser.add_argument("--editor", default=os.environ.get("UNITY_EDITOR"))
    parser.add_argument("--timeout", type=int, default=180)
    args = parser.parse_args()
    evidence = ROOT / "Builds/Validation"
    evidence.mkdir(parents=True, exist_ok=True)
    log = evidence / (args.task + ".log")
    report = {"task": args.task, "editor": PIN, "status": "NOT_RUN", "log": str(log.relative_to(ROOT)),
              "timestamp": datetime.datetime.now(datetime.timezone.utc).isoformat(),
              "native_device_tested": False}
    candidates = [args.editor, shutil.which("Unity"),
                  "/workspace/.warehouse-cloud/unity/" + PIN + "/Editor/Unity"]
    editor = next((Path(p).resolve() for p in candidates if p and Path(p).is_file()), None)
    if editor is None:
        report["reason"] = "Set UNITY_EDITOR to the licensed Unity " + PIN + " executable."
        print(report["reason"])
        (evidence / (args.task + ".json")).write_text(json.dumps(report, indent=2) + "\n")
        return 2
    env = os.environ.copy()
    state = ROOT / "Temp/EditorState"
    for key, directory in [("XDG_CACHE_HOME", state / "cache"), ("XDG_CONFIG_HOME", state / "config")]:
        directory.mkdir(parents=True, exist_ok=True)
        env[key] = str(directory)
    # HOME, licensing selectors, inherited proxies and CA trust are preserved.
    command = [str(editor), "-batchmode", "-nographics", "-projectPath", str(ROOT), "-logFile", str(log)]
    if args.task in ("editmode", "playmode"):
        result_xml = evidence / (args.task + "-results.xml")
        if result_xml.is_symlink():
            raise RuntimeError("Refusing a symlink TestRunner results path.")
        if result_xml.exists():
            # Unity may exit before producing XML. Preserve earlier evidence and
            # require this invocation to create its own results before claiming PASS.
            import uuid
            previous = evidence / (args.task + "-results.previous." + uuid.uuid4().hex + ".xml")
            result_xml.rename(previous)
            report["previous_results"] = str(previous.relative_to(ROOT))
        command += ["-runTests", "-testPlatform", "EditMode" if args.task == "editmode" else "PlayMode",
                    "-testResults", str(result_xml)]
    else:
        methods = {"validate": "Warehouse.Editor.ProjectValidation.Validate",
                   "android": "Warehouse.Editor.MobileBuilds.AndroidDevelopment",
                   "ios": "Warehouse.Editor.MobileBuilds.IosDevelopment"}
        command += ["-executeMethod", methods[args.task], "-quit"]
    try:
        result = subprocess.run(command, cwd=ROOT, env=env, stdout=subprocess.PIPE,
                                stderr=subprocess.STDOUT, text=True, timeout=args.timeout)
        report["exit_code"] = result.returncode
        contents = log.read_text(errors="replace") if log.exists() else result.stdout
        license_errors = ("No valid Unity Editor license", "No valid license", "Failed to activate", "com.unity.editor.headless")
        if any(message in contents for message in license_errors):
            report["reason"] = "Unity Editor license unavailable; compilation/TestRunner/native export did not execute."
        elif result.returncode != 0:
            report.update(status="FAIL", reason="Unity returned a nonzero exit code; inspect the log.")
        elif args.task in ("editmode", "playmode"):
            from xml.etree import ElementTree
            xml = evidence / (args.task + "-results.xml")
            if not xml.is_file():
                report.update(status="FAIL", reason="Unity exited without a TestRunner results file.")
            else:
                tree = ElementTree.parse(xml).getroot()
                report["tests"] = tree.attrib
                report["status"] = "PASS" if tree.attrib.get("result") == "Passed" else "FAIL"
        else:
            report["status"] = "PASS"
    except subprocess.TimeoutExpired:
        report["reason"] = "Unity did not finish within the bounded timeout; no successful execution claimed."
    except OSError as error:
        report["reason"] = type(error).__name__ + ": " + str(error)
    (evidence / (args.task + ".json")).write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, ensure_ascii=False))
    return 0 if report["status"] == "PASS" else 2 if report["status"] == "NOT_RUN" else 1

if __name__ == "__main__":
    sys.exit(main())
