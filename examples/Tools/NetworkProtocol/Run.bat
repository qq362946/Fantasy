@echo off

echo Please select an option:
echo 1. Client
echo 2. Server
echo 3. All

set /p choice=Please select an option:

if "%choice%"=="1" (
    echo Client
    dotnet Fantasy.Tools.NetworkProtocol.dll --ExportPlatform 1
) else if "%choice%"=="2" (
    echo Server
    dotnet Fantasy.Tools.NetworkProtocol.dll --ExportPlatform 2
) else if "%choice%"=="3" (
    echo All
    dotnet Fantasy.Tools.NetworkProtocol.dll --ExportPlatform 3
) else (
    echo Invalid option
)
