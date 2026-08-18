@echo off
setlocal
cd /d "%~dp0"

dotnet sln YoloDeploy.sln add YoloDeploy.SDK\YoloDeploy.SDK.csproj
if errorlevel 1 exit /b %errorlevel%

dotnet sln YoloDeploy.sln add YoloDeploy.SDK.Test\YoloDeploy.SDK.Test.csproj
if errorlevel 1 exit /b %errorlevel%

echo.
echo MultiTask SDK projects added.
pause
