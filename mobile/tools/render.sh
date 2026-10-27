#!/usr/bin/env bash
set -euo pipefail
mobile_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
output_root="${1:-$mobile_root/../builds/godot-mobile/render}"
mkdir -p "$output_root"
if [[ -n "${DISPLAY:-}" ]]; then
 "$mobile_root/tools/godot.sh" --rendering-method gl_compatibility --audio-driver Dummy --script res://tests/render_smoke.gd -- --output "$output_root"
elif command -v xvfb-run >/dev/null 2>&1; then
 LIBGL_ALWAYS_SOFTWARE=1 xvfb-run -a -s '-screen 0 1280x720x24' "$mobile_root/tools/godot.sh" --rendering-method gl_compatibility --audio-driver Dummy --script res://tests/render_smoke.gd -- --output "$output_root"
else
 echo 'NOT_RUN: a real display or Xvfb+Mesa is needed; headless dummy rendering cannot validate pixels.' >&2
 exit 2
fi
