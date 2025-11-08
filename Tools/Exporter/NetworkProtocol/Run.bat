@echo off

echo Please select an option:
echo 1. Client
echo 2. Server
echo 3. All

set /p choice=Please select an option:

if "%choice%"=="1" (
    echo Client
    dotnet Fantasy.Tools.NetworkProtocol.dll --p 1
) else if "%choice%"=="2" (
    echo Server
    dotnet Fantasy.Tools.NetworkProtocol.dll --p 2
) else if "%choice%"=="3" (
    echo All
    dotnet Fantasy.Tools.NetworkProtocol.dll --p 3
) else (
    echo Invalid option
)