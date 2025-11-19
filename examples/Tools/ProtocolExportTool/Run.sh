#!/bin/bash
# Fantasy Protocol Export Tool - 运行脚本
# 此脚本用于快速运行协议导出工具

set -e

# 获取脚本所在目录
SCRIPT_DIR=$(cd $(dirname "$0") && pwd)
APP_DLL="$SCRIPT_DIR/Fantasy.ProtocolExportTool.dll"

##############################################
# 检测 .NET 运行时
##############################################
check_dotnet() {
    # 检查是否安装了 dotnet
    if ! command -v dotnet &> /dev/null; then
        echo ""
        echo "=========================================="
        echo "错误：未检测到 .NET 运行时"
        echo "=========================================="
        echo ""
        echo "请先安装 .NET 8.0 SDK 或 Runtime"
        echo ""
        echo "下载地址："
        echo "  https://dotnet.microsoft.com/download/dotnet/8.0"
        echo ""
        echo "安装说明："
        echo "  macOS:   brew install dotnet@8"
        echo "  Linux:   参考官方文档安装对应发行版的包"
        echo ""
        exit 1
    fi

    # 检查 .NET 版本
    DOTNET_VERSION=$(dotnet --version 2>/dev/null || echo "0.0.0")
    MAJOR_VERSION=$(echo "$DOTNET_VERSION" | cut -d'.' -f1)

    if [ "$MAJOR_VERSION" -lt 8 ]; then
        echo ""
        echo "=========================================="
        echo "错误：.NET 版本过低"
        echo "=========================================="
        echo ""
        echo "当前版本: $DOTNET_VERSION"
        echo "需要版本: 8.0 或更高"
        echo ""
        echo "请升级到 .NET 8.0 或更高版本"
        echo ""
        echo "下载地址："
        echo "  https://dotnet.microsoft.com/download/dotnet/8.0"
        echo ""
        exit 1
    fi

    echo "✓ 检测到 .NET $DOTNET_VERSION"
}

##############################################
# 主程序
##############################################

echo "=========================================="
echo "Fantasy Protocol Export Tool"
echo "=========================================="
echo ""

# 检测 .NET
check_dotnet

echo ""
echo "正在启动导出工具..."
echo ""

# 使用静默模式（从 ExporterSettings.json 读取配置）
dotnet "$APP_DLL" export --silent

if [ $? -eq 0 ]; then
    echo ""
    echo "=========================================="
    echo "✓ 导出完成！"
    echo "=========================================="
    echo ""
else
    echo ""
    echo "=========================================="
    echo "✗ 导出失败"
    echo "=========================================="
    echo ""
    echo "提示：请检查 ExporterSettings.json 配置文件是否正确"
    echo ""
    exit 1
fi
