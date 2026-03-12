#!/bin/bash

SCRIPT_DIR=$(cd $(dirname "$0") && pwd)

echo "=========================================="
echo "Fantasy Protocol Export Tool 2026.0.1002"
echo "=========================================="
echo ""

dotnet "$SCRIPT_DIR/Fantasy.ProtocolExportTool.dll" export --silent

echo "按回车键退出..."
read -r
