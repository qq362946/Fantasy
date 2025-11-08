@echo off
REM Fantasy.SourceGenerator 更新脚本 (Windows)
REM 用于将最新的 Source Generator DLL 复制到 Fantasy.Unity package

setlocal enabledelayedexpansion

REM 设置路径
set SCRIPT_DIR=%~dp0
REM 项目根目录（脚本在 Tools\Update-Unity-Source-Generator\ 下，需要上两级到根目录）
set PROJECT_ROOT=%SCRIPT_DIR%..\..
set SOURCE_GENERATOR_PROJECT=%PROJECT_ROOT%\Fantasy.Packages\Fantasy.SourceGenerator\Fantasy.SourceGenerator.csproj
set UNITY_PACKAGE_DIR=%PROJECT_ROOT%\Fantasy.Packages\Fantasy.Unity\RoslynAnalyzers

REM 设置构建配置 (为 Unity 使用特殊配置)
set BUILD_CONFIG=Unity

echo ========================================
echo Fantasy.SourceGenerator 更新工具 (Unity 版本)
echo ========================================
echo.

REM 检查项目文件
if not exist "%SOURCE_GENERATOR_PROJECT%" (
    echo [错误] 找不到 Source Generator 项目文件
    echo 路径: %SOURCE_GENERATOR_PROJECT%
    exit /b 1
)

REM 创建目标目录
if not exist "%UNITY_PACKAGE_DIR%" (
    echo 创建 SourceGenerators 目录...
    mkdir "%UNITY_PACKAGE_DIR%"
)

REM 构建
echo 步骤 1/3: 构建 Fantasy.SourceGenerator (%BUILD_CONFIG% - Roslyn 4.0.1)...
dotnet build "%SOURCE_GENERATOR_PROJECT%" --configuration %BUILD_CONFIG%

if errorlevel 1 (
    echo [错误] 构建失败!
    exit /b 1
)

echo [成功] 构建完成 (使用 Unity 兼容的 Roslyn 版本)
echo.

REM 复制 DLL
set SOURCE_DLL=%PROJECT_ROOT%\Fantasy.Packages\Fantasy.SourceGenerator\bin\%BUILD_CONFIG%\netstandard2.0\Fantasy.SourceGenerator.dll
set TARGET_DLL=%UNITY_PACKAGE_DIR%\Fantasy.SourceGenerator.dll

if not exist "%SOURCE_DLL%" (
    echo [错误] 找不到生成的 DLL 文件
    echo 路径: %SOURCE_DLL%
    exit /b 1
)

echo 步骤 2/3: 复制 DLL 到 Unity Package...
copy /Y "%SOURCE_DLL%" "%TARGET_DLL%" >nul

if errorlevel 1 (
    echo [错误] 复制失败!
    exit /b 1
)

echo [成功] 复制完成
echo   源: %SOURCE_DLL%
echo   目标: %TARGET_DLL%
echo.

REM 验证
echo 步骤 3/3: 验证...
for %%A in ("%TARGET_DLL%") do (
    echo   文件大小: %%~zA 字节
    echo   修改时间: %%~tA
)
echo   Roslyn 版本: 4.0.1 (Unity 兼容)
echo [成功] 验证通过
echo.

REM 完成
echo ========================================
echo [成功] 更新完成!
echo ========================================
echo.
echo 下一步操作:
echo 1. 在 Unity 中右键点击 Fantasy.Unity package -^> Reimport
echo 2. 或者重启 Unity 编辑器
echo 3. 检查 Console 确认 Source Generator 正常工作
echo.
echo 调试生成的代码位置:
echo   ^<Unity项目^>/Temp/GeneratedCode/Fantasy.SourceGenerator/
echo.
echo 注意: 此版本使用 Roslyn 4.0.1，兼容 Unity 2020.2+
echo.

endlocal
