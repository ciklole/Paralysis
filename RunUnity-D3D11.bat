@echo off
REM Launches this Unity project forcing Direct3D 11 instead of D3D12.
REM Workaround for Intel integrated-GPU + D3D12 driver crashes in Unity 6000.7.0a6.
REM
REM The project path is taken from this script's own folder, so the repository
REM can be cloned anywhere. Edit UNITY_EXE below if your Unity install
REM lives in a different location.

setlocal

set "PROJECT_PATH=%~dp0"
if "%PROJECT_PATH:~-1%"=="\" set "PROJECT_PATH=%PROJECT_PATH:~0,-1%"

set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.7.0a6\Editor\Unity.exe"

if not exist "%UNITY_EXE%" (
    echo Unity editor not found at:
    echo   "%UNITY_EXE%"
    echo Edit UNITY_EXE in this script to match your Unity installation.
    pause
    exit /b 1
)

echo Starting Unity on "%PROJECT_PATH%" with Direct3D 11...
start "" "%UNITY_EXE%" -projectPath "%PROJECT_PATH%" -force-d3d11
endlocal
