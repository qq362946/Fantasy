#!/bin/bash
# Fantasy ProtocolExportTool - Build and Package Script
# This script builds the protocol export tool and packages it for distribution
# Note: Uses framework-dependent deployment (requires .NET runtime installed)

set -e

APP_NAME="Fantasy.ProtocolExportTool"
PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"
SOLUTION_ROOT="$(cd "$PROJECT_DIR/../.." && pwd)"
OUTPUT_DIR="$SOLUTION_ROOT/Tools/ProtocolExportTool"

echo "=========================================="
echo "Fantasy Protocol Export Tool - Build and Package"
echo "=========================================="
echo ""
echo "Project Directory: $PROJECT_DIR"
echo "Output Directory: $OUTPUT_DIR"
echo "Deployment Mode: Framework-dependent (.NET runtime required)"
echo ""

##############################################
# 1. 清理旧的输出
##############################################
echo "=========================================="
echo "[1/3] Cleaning old build artifacts..."
echo "=========================================="
echo ""

# 清理项目的 bin/obj 目录
if [ -d "$PROJECT_DIR/bin" ]; then
    echo "  Removing: $PROJECT_DIR/bin"
    rm -rf "$PROJECT_DIR/bin"
fi

if [ -d "$PROJECT_DIR/obj" ]; then
    echo "  Removing: $PROJECT_DIR/obj"
    rm -rf "$PROJECT_DIR/obj"
fi

# 清理输出目录
if [ -d "$OUTPUT_DIR" ]; then
    echo "  Removing: $OUTPUT_DIR"
    rm -rf "$OUTPUT_DIR"
fi

echo ""
echo "✓ Cleanup completed!"
echo ""

##############################################
# 2. 恢复依赖包
##############################################
echo "=========================================="
echo "[2/3] Restoring NuGet packages..."
echo "=========================================="
echo ""

dotnet restore "$PROJECT_DIR/$APP_NAME.csproj"

echo ""
echo "✓ Restore completed!"
echo ""

##############################################
# 3. 发布应用程序（平台无关）
##############################################
echo "=========================================="
echo "[3/3] Publishing application..."
echo "=========================================="
echo ""

# 发布为平台无关的框架依赖模式
dotnet publish "$PROJECT_DIR/$APP_NAME.csproj" \
    -c Release \
    --no-self-contained \
    -o "$OUTPUT_DIR"

echo ""
echo "✓ Build completed!"
echo ""

##############################################
# 复制额外文件
##############################################
echo "Copying additional files..."

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
echo "✓ Build and Package Completed Successfully!"
echo "=========================================="
echo ""
echo "Output Directory:"
echo "  $OUTPUT_DIR"
echo ""
echo "Packaged Files:"
echo "  - $APP_NAME.dll (main assembly)"
echo "  - $APP_NAME.deps.json"
echo "  - $APP_NAME.runtimeconfig.json"
echo "  - ExporterSettings.json"
echo "  - export-protocol.sh"
echo "  - export-protocol.bat"
echo "  - All dependency DLLs"
echo ""
echo "Requirements:"
echo "  - .NET 8.0 Runtime or SDK must be installed"
echo ""
echo "Usage:"
echo "  # macOS/Linux:"
echo "  cd \"$OUTPUT_DIR\""
echo "  ./export-protocol.sh"
echo ""
echo "  # Windows:"
echo "  cd \"$OUTPUT_DIR\""
echo "  export-protocol.bat"
echo ""
echo "  # Direct execution:"
echo "  dotnet \"$OUTPUT_DIR/$APP_NAME.dll\" export --help"
echo ""
