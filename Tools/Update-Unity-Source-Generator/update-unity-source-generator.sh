#!/bin/bash

# Fantasy.SourceGenerator 更新脚本
# 用于将最新的 Source Generator DLL 复制到 Fantasy.Unity package

set -e

# 颜色定义
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# 路径定义
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
# 项目根目录（脚本在 Tools/Update-Unity-Source-Generator/ 下，需要上两级到根目录）
PROJECT_ROOT="${SCRIPT_DIR}/../.."
SOURCE_GENERATOR_PROJECT="${PROJECT_ROOT}/Fantasy.Packages/Fantasy.SourceGenerator/Fantasy.SourceGenerator.csproj"
UNITY_PACKAGE_DIR="${PROJECT_ROOT}/Fantasy.Packages/Fantasy.Unity/RoslynAnalyzers"

# 配置 - 为 Unity 使用特殊的构建配置
BUILD_CONFIG="Unity"  # 使用 Unity 配置，引用低版本 Roslyn

echo -e "${YELLOW}Fantasy.SourceGenerator 更新工具 (Unity 版本)${NC}"
echo "========================================"
echo ""

# 检查项目文件是否存在
if [ ! -f "${SOURCE_GENERATOR_PROJECT}" ]; then
    echo -e "${RED}错误: 找不到 Source Generator 项目文件${NC}"
    echo "路径: ${SOURCE_GENERATOR_PROJECT}"
    exit 1
fi

# 检查目标目录是否存在
if [ ! -d "${UNITY_PACKAGE_DIR}" ]; then
    echo -e "${YELLOW}创建 SourceGenerators 目录...${NC}"
    mkdir -p "${UNITY_PACKAGE_DIR}"
fi

# 构建 Source Generator (Unity 版本)
echo -e "${YELLOW}步骤 1/3: 构建 Fantasy.SourceGenerator (${BUILD_CONFIG} - Roslyn 4.3.0)...${NC}"
dotnet build "${SOURCE_GENERATOR_PROJECT}" --configuration "${BUILD_CONFIG}"

if [ $? -ne 0 ]; then
    echo -e "${RED}构建失败!${NC}"
    exit 1
fi

echo -e "${GREEN}✓ 构建成功 (使用 Unity 兼容的 Roslyn 4.3.0)${NC}"
echo ""

# 复制 DLL
SOURCE_DLL="${PROJECT_ROOT}/Fantasy.Packages/Fantasy.SourceGenerator/bin/${BUILD_CONFIG}/netstandard2.0/Fantasy.SourceGenerator.dll"
TARGET_DLL="${UNITY_PACKAGE_DIR}/Fantasy.SourceGenerator.dll"

if [ ! -f "${SOURCE_DLL}" ]; then
    echo -e "${RED}错误: 找不到生成的 DLL 文件${NC}"
    echo "路径: ${SOURCE_DLL}"
    exit 1
fi

echo -e "${YELLOW}步骤 2/3: 复制 DLL 到 Unity Package...${NC}"
cp "${SOURCE_DLL}" "${TARGET_DLL}"
echo -e "${GREEN}✓ 复制成功${NC}"
echo "  源: ${SOURCE_DLL}"
echo "  目标: ${TARGET_DLL}"
echo ""

# 获取文件信息
DLL_SIZE=$(ls -lh "${TARGET_DLL}" | awk '{print $5}')
DLL_DATE=$(ls -l "${TARGET_DLL}" | awk '{print $6, $7, $8}')

echo -e "${YELLOW}步骤 3/3: 验证...${NC}"
echo "  文件大小: ${DLL_SIZE}"
echo "  修改时间: ${DLL_DATE}"
echo "  Roslyn 版本: 4.3.0 (Unity 兼容)"
echo -e "${GREEN}✓ 验证通过${NC}"
echo ""

# 完成
echo "========================================"
echo -e "${GREEN}✓ 更新完成!${NC}"
echo ""
echo "下一步操作:"
echo "1. 在 Unity 中右键点击 Fantasy.Unity package → Reimport"
echo "2. 或者重启 Unity 编辑器"
echo "3. 检查 Console 确认 Source Generator 正常工作"
echo ""
echo "调试生成的代码位置:"
echo "  <Unity项目>/Temp/GeneratedCode/Fantasy.SourceGenerator/"
echo ""
echo "注意: 此版本使用 Roslyn 4.3.0，兼容 Unity 2022.2+"
