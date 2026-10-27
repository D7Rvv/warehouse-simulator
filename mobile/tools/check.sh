#!/usr/bin/env bash
set -euo pipefail
mobile_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
engine_version="$("$mobile_root/tools/godot.sh" --version)"
case "$engine_version" in
 4.6.3.*) ;;
 *) echo "Expected Godot4.6.3, found $engine_version" >&2; exit 1 ;;
esac
"$mobile_root/tools/godot.sh" --headless --editor --quit
"$mobile_root/tools/godot.sh" --headless --script res://tests/headless_suite.gd
python3 "$mobile_root/tools/quality_audit.py"
