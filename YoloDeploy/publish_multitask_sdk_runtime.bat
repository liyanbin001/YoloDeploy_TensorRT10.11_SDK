@echo off
setlocal
cd /d "%~dp0"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish_multitask_sdk_runtime.ps1"

if errorlevel 1 (
  echo.
  echo [ERROR] Publish failed.
  pause
  exit /b 1
)

echo.
echo [OK] dist\YoloDeploy.SDK.Runtime.MultiTask.zip
pause
