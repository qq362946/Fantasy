@echo off
setlocal

REM 获取脚本所在目录
set "ScriptDir=%~dp0"
REM 去掉末尾的反斜杠（可选）
if "%ScriptDir:~-1%"=="\" set "ScriptDir=%ScriptDir:~0,-1%"

echo Please select an option:
echo 1. Client
echo 2. Server
echo 3. All

set /p choice=Please select an option:

if "%choice%"=="1" (
    echo Client
    dotnet "%ScriptDir%\Fantasy.Tools.NetworkProtocol.dll" --p 1 --f "%ScriptDir%"
) else if "%choice%"=="2" (
    echo Server
    dotnet "%ScriptDir%\Fantasy.Tools.NetworkProtocol.dll" --p 2 --f "%ScriptDir%"
) else if "%choice%"=="3" (
    echo All
    dotnet "%ScriptDir%\Fantasy.Tools.NetworkProtocol.dll" --p 3 --f "%ScriptDir%"
) else (
    echo Invalid option
)

endlocal
