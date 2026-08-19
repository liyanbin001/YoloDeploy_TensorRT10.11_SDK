@echo off
setlocal
cd /d "%~dp0"

dotnet sln YoloDeploy.sln add YoloDeploy.SDK.Net48\YoloDeploy.SDK.Net48.csproj
if errorlevel 1 exit /b %errorlevel%

dotnet sln YoloDeploy.sln add YoloDeploy.SDK.Net48.Test\YoloDeploy.SDK.Net48.Test.csproj
if errorlevel 1 exit /b %errorlevel%

echo.
echo [OK] Net48 SDK projects added to YoloDeploy.sln.
pause
