@echo off

echo Please select an option:
echo 1. Client Increment
echo 2. Client all
echo 3. Server Increment
echo 4. Server all
echo 5. Client and Server Increment
echo 6. Client and Server all

set /p choice=Please select an option:

if "%choice%"=="1" (
    echo Client Increment
    dotnet Fantasy.Tools.ConfigTable.dll --p 1 --e 1
) else if "%choice%"=="2" (
    echo Client all
    dotnet Fantasy.Tools.ConfigTable.dll --p 1 --e 2
) else if "%choice%"=="3" (
    echo Server Increment
    dotnet Fantasy.Tools.ConfigTable.dll --p 2 --e 1
) else if "%choice%"=="4" (
    echo Server all
    dotnet Fantasy.Tools.ConfigTable.dll --p 2 --e 2
) else if "%choice%"=="5" (
    echo Client and Server Increment
    dotnet Fantasy.Tools.ConfigTable.dll --p 3 --e 1
) else if "%choice%"=="6" (
    echo Client and Server all
    dotnet Fantasy.Tools.ConfigTable.dll --p 3 --e 2
) else (
    echo Invalid option
)
