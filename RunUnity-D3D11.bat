@echo off
REM Launches this Unity project forcing Direct3D 11 instead of D3D12.
REM Workaround for Intel integrated-GPU + D3D12 driver crashes in Unity 6000.7.0a6.
start "" "C:\Program Files\Unity\Hub\Editor\6000.7.0a6\Editor\Unity.exe" -projectPath "C:\Users\super\Paralysis" -force-d3d11
