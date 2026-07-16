@echo off
setlocal

set "PROJECT_DIR=%~dp0"
for %%I in ("%PROJECT_DIR%..\..") do set "ROOT_DIR=%%~fI"
set "OUTPUT_DIR=%ROOT_DIR%\Tools\ControlCenter"

if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
dotnet publish "%PROJECT_DIR%Fantasy.ControlCenter.csproj" -c Release --no-self-contained -o "%OUTPUT_DIR%"
if errorlevel 1 exit /b %errorlevel%

echo Published to: %OUTPUT_DIR%
endlocal
