@echo off
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe"
echo Opening NinjaStrike in Unity...
start "" %UNITY_PATH% -projectPath "%~dp0."
exit
