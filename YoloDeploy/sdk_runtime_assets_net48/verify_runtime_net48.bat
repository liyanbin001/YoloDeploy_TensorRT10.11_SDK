@echo off
setlocal
cd /d "%~dp0"

set "FAIL=0"

echo ================================================================
echo YoloDeploy SDK .NET Framework 4.8 Runtime Verification
echo ================================================================
echo.

call :check_file "YoloDeploy.SDK.Net48.dll"
call :check_file "YoloDeploy.Native.dll"
call :check_file "nvinfer_10.dll"
call :check_file "nvinfer_plugin_10.dll"
call :check_file "nvonnxparser_10.dll"
call :check_file "TestSDK.Net48.exe"

echo.
echo --- CUDA Runtime ---
dir /b cudart64_*.dll >nul 2>nul
if errorlevel 1 (
  echo [ERROR] cudart64_*.dll missing
  set "FAIL=1"
) else (
  echo [OK] CUDA Runtime
)

echo.
echo --- NVIDIA GPU / Driver ---
where nvidia-smi.exe >nul 2>nul
if errorlevel 1 (
  echo [ERROR] nvidia-smi.exe not found.
  echo         Install/update an NVIDIA display driver.
  set "FAIL=1"
) else (
  nvidia-smi.exe --query-gpu=name,driver_version --format=csv,noheader
)

echo.
echo --- .NET Framework 4.8 ---
powershell.exe -NoProfile -Command ^
  "$r=(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -ErrorAction SilentlyContinue).Release; if($r -ge 528040){Write-Host '[OK] .NET Framework 4.8+ Release=' $r; exit 0}else{Write-Host '[ERROR] .NET Framework 4.8 not detected. Release=' $r; exit 1}"

if errorlevel 1 (
  set "FAIL=1"
)

echo.
if "%FAIL%"=="0" (
  echo ================================================================
  echo [OK] Runtime verification passed.
  echo ================================================================
) else (
  echo ================================================================
  echo [ERROR] Runtime verification failed.
  echo ================================================================
)

echo.
pause
exit /b %FAIL%

:check_file
if exist "%~1" (
  echo [OK] %~1
) else (
  echo [ERROR] %~1 missing
  set "FAIL=1"
)
exit /b 0
