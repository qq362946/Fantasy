#!/bin/bash
# Fantasy ProtocolExportTool - Quick Build Script
# This script builds the tool quickly for development iterations
# Note: Uses framework-dependent deployment (requires .NET runtime installed)

set -e

APP_NAME="Fantasy.ProtocolExportTool"
PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
SOLUTION_ROOT="$(cd "$PROJECT_DIR/../.." && pwd)"
OUTPUT_DIR="$SOLUTION_ROOT/Tools/ProtocolExportTool"

echo "=========================================="
echo "Fantasy Protocol Export Tool - Quick Build"
echo "=========================================="
echo ""
echo "Deployment Mode: Framework-dependent"
echo "Output: $OUTPUT_DIR"
echo ""

##############################################
# 1. 清理
##############################################
echo "[1/3] Cleaning..."
rm -rf "$PROJECT_DIR/bin/Release"
rm -rf "$PROJECT_DIR/obj/Release"
echo "✓ Cleanup completed!"
echo ""

##############################################
# 2. 发布
##############################################
echo "[2/3] Publishing..."
dotnet publish "$PROJECT_DIR/$APP_NAME.csproj" \
    -c Release \
    --no-self-contained \
    -o "$OUTPUT_DIR"

echo ""
echo "✓ Build completed!"
echo ""

##############################################
# 3. 复制额外文件
##############################################
echo "[3/3] Copying additional files..."

# 复制配置文件（如果不在输出目录）
if [ -f "$PROJECT_DIR/ExporterSettings.json" ] && [ ! -f "$OUTPUT_DIR/ExporterSettings.json" ]; then
    cp "$PROJECT_DIR/ExporterSettings.json" "$OUTPUT_DIR/"
    echo "  ✓ Copied: ExporterSettings.json"
fi

# 复制运行脚本
if [ -f "$PROJECT_DIR/export-protocol.sh" ]; then
    cp "$PROJECT_DIR/export-protocol.sh" "$OUTPUT_DIR/"
    chmod +x "$OUTPUT_DIR/export-protocol.sh"
    echo "  ✓ Copied: export-protocol.sh"
fi

if [ -f "$PROJECT_DIR/export-protocol.bat" ]; then
    cp "$PROJECT_DIR/export-protocol.bat" "$OUTPUT_DIR/"
    echo "  ✓ Copied: export-protocol.bat"
fi

echo ""
echo "✓ Packaging completed!"
echo ""

##############################################
# 总结
##############################################
echo "=========================================="
echo "✓ Quick Build Completed!"
echo "=========================================="
echo ""
echo "Output: $OUTPUT_DIR"
echo ""
echo "Requirements:"
echo "  - .NET 8.0 Runtime or SDK must be installed"
echo ""
echo "Run:"
echo "  cd \"$OUTPUT_DIR\""
echo "  ./export-protocol.sh"
echo ""
echo "Or directly:"
echo "  dotnet \"$OUTPUT_DIR/$APP_NAME.dll\" export --help"
echo ""
