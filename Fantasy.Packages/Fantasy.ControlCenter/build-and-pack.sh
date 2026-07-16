#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(cd "$PROJECT_DIR/../.." && pwd)"
OUTPUT_DIR="$ROOT_DIR/Tools/ControlCenter"

rm -rf "$OUTPUT_DIR"
dotnet publish "$PROJECT_DIR/Fantasy.ControlCenter.csproj" \
    -c Release \
    --no-self-contained \
    -o "$OUTPUT_DIR"

echo "Published to: $OUTPUT_DIR"
