@echo off
setlocal
cd /d "%~dp0"

powershell.exe -NoProfile -ExecutionPolicy Bypass ^
  -File "%~dp0publish_net48_runtime.ps1"

if errorlevel 1 (
  echo.
  echo [ERROR] Net48 publish failed.
  pause
  exit /b 1
)

echo.
echo [OK] dist\YoloDeploy.SDK.Runtime.Net48.zip
pause
