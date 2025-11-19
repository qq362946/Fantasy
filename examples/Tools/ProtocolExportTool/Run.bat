@echo off
chcp 65001 >nul
setlocal

echo ==========================================
echo Fantasy Protocol Export Tool
echo ==========================================
echo.

REM 获取脚本所在目录
set "SCRIPT_DIR=%~dp0"
set "APP_DLL=%SCRIPT_DIR%Fantasy.ProtocolExportTool.dll"

REM 检查 dotnet 是否安装
where dotnet >nul 2>nul
if errorlevel 1 (
    echo.
    echo ==========================================
    echo 错误：未检测到 .NET 运行时
    echo ==========================================
    echo.
    echo 请先安装 .NET 8.0 SDK 或 Runtime
    echo.
    echo 下载地址：
    echo   https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

REM 检查 .NET 版本
for /f "tokens=1 delims=." %%v in ('dotnet --version 2^>nul') do set MAJOR_VERSION=%%v

if %MAJOR_VERSION% LSS 8 (
    echo.
    echo ==========================================
    echo 错误：.NET 版本过低
    echo ==========================================
    echo.
    dotnet --version
    echo 需要版本: 8.0 或更高
    echo.
    echo 请升级到 .NET 8.0 或更高版本
    echo.
    echo 下载地址：
    echo   https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

dotnet --version
echo.
echo 正在启动导出工具...
echo.

REM 运行导出工具
dotnet "%APP_DLL%" export --silent

if errorlevel 1 (
    echo.
    echo ==========================================
    echo × 导出失败
    echo ==========================================
    echo.
    echo 提示：请检查 ExporterSettings.json 配置文件是否正确
    echo.
    pause
    exit /b 1
)

echo.
echo ==========================================
echo √ 导出完成！
echo ==========================================
echo.
pause
