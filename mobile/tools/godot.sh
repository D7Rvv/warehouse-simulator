#!/usr/bin/env bash
set -euo pipefail
mobile_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
state_root="$mobile_root/.godot/cloud-state"
mkdir -p "$state_root/cache" "$state_root/config" "$state_root/data"
export XDG_CACHE_HOME="$state_root/cache"
export XDG_CONFIG_HOME="$state_root/config"
export XDG_DATA_HOME="$state_root/data"
godot_bin="${GODOT_BIN:-godot}"
exec "$godot_bin" --path "$mobile_root" "$@"
