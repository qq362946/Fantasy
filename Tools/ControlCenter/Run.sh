#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
exec dotnet "$SCRIPT_DIR/Fantasy.ControlCenter.dll" "$@"
