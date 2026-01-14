@echo off
REM Fantasy ProtocolExportTool - Build and Package Script (Windows)
REM This script builds the protocol export tool and packages it for distribution
REM Note: Uses framework-dependent deployment (requires .NET runtime installed)

setlocal enabledelayedexpansion

set APP_NAME=Fantasy.ProtocolExportTool
set PROJECT_DIR=%~dp0
set PROJECT_DIR=%PROJECT_DIR:~0,-1%
for %%I in ("%PROJECT_DIR%\..\..") do set SOLUTION_ROOT=%%~fI
set OUTPUT_DIR=%SOLUTION_ROOT%\Tools\ProtocolExportTool

echo ==========================================
echo Fantasy Protocol Export Tool - Build and Package
echo ==========================================
echo.
echo Project Directory: %PROJECT_DIR%
echo Output Directory: %OUTPUT_DIR%
echo Deployment Mode: Framework-dependent (.NET runtime required)
echo.

REM ##############################################
REM 1. 清理旧的输出
REM ##############################################
echo ==========================================
echo [1/3] Cleaning old build artifacts...
echo ==========================================
echo.

if exist "%PROJECT_DIR%\bin" (
    echo   Removing: %PROJECT_DIR%\bin
    rmdir /s /q "%PROJECT_DIR%\bin"
)

if exist "%PROJECT_DIR%\obj" (
    echo   Removing: %PROJECT_DIR%\obj
    rmdir /s /q "%PROJECT_DIR%\obj"
)

if exist "%OUTPUT_DIR%" (
    echo   Removing: %OUTPUT_DIR%
    rmdir /s /q "%OUTPUT_DIR%"
)

echo.
echo √ Cleanup completed!
echo.

REM ##############################################
REM 2. 恢复依赖包
REM ##############################################
echo ==========================================
echo [2/3] Restoring NuGet packages...
echo ==========================================
echo.

dotnet restore "%PROJECT_DIR%\%APP_NAME%.csproj"

echo.
echo √ Restore completed!
echo.

REM ##############################################
REM 3. 发布应用程序（平台无关）
REM ##############################################
echo ==========================================
echo [3/3] Publishing application...
echo ==========================================
echo.

REM 发布为平台无关的框架依赖模式
dotnet publish "%PROJECT_DIR%\%APP_NAME%.csproj" ^
    -c Release ^
    --no-self-contained ^
    -o "%OUTPUT_DIR%"

echo.
echo √ Build completed!
echo.

REM ##############################################
REM 复制额外文件
REM ##############################################
echo Copying additional files...

REM 复制配置文件（如果不在输出目录）
if exist "%PROJECT_DIR%\ExporterSettings.json" (
    if not exist "%OUTPUT_DIR%\ExporterSettings.json" (
        copy /Y "%PROJECT_DIR%\ExporterSettings.json" "%OUTPUT_DIR%\" >nul
        echo   √ Copied: ExporterSettings.json
    )
)

REM 复制运行脚本
if exist "%PROJECT_DIR%\export-protocol.sh" (
    copy /Y "%PROJECT_DIR%\export-protocol.sh" "%OUTPUT_DIR%\" >nul
    echo   √ Copied: export-protocol.sh
)

if exist "%PROJECT_DIR%\export-protocol.bat" (
    copy /Y "%PROJECT_DIR%\export-protocol.bat" "%OUTPUT_DIR%\" >nul
    echo   √ Copied: export-protocol.bat
)

echo.
echo √ Packaging completed!
echo.

REM ##############################################
REM 总结
REM ##############################################
echo ==========================================
echo √ Build and Package Completed Successfully!
echo ==========================================
echo.
echo Output Directory:
echo   %OUTPUT_DIR%
echo.
echo Packaged Files:
echo   - %APP_NAME%.dll (main assembly)
echo   - %APP_NAME%.deps.json
echo   - %APP_NAME%.runtimeconfig.json
echo   - ExporterSettings.json
echo   - export-protocol.sh
echo   - export-protocol.bat
echo   - All dependency DLLs
echo.
echo Requirements:
echo   - .NET 8.0 Runtime or SDK must be installed
echo.
echo Usage:
echo   # Windows:
echo   cd "%OUTPUT_DIR%"
echo   export-protocol.bat
echo.
echo   # macOS/Linux:
echo   cd "%OUTPUT_DIR%"
echo   ./export-protocol.sh
echo.
echo   # Direct execution:
echo   dotnet "%OUTPUT_DIR%\%APP_NAME%.dll" export --help
echo.

endlocal
pause
