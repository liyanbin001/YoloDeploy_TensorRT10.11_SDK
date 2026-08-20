[CmdletBinding()]
param(
    [string]$Root = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# Capture the script directory at script scope.
# Do NOT use $MyInvocation.MyCommand.Path inside Resolve-Root:
# inside a function MyInvocation refers to the function invocation and
# MyCommand may be a FunctionInfo without a Path property.
$script:VerifierToolsDir = $PSScriptRoot

function Resolve-Root {
    param([string]$Requested)

    $candidates = @()

    if (-not [string]::IsNullOrWhiteSpace($Requested)) {
        $candidates += $Requested
    }

    if (-not [string]::IsNullOrWhiteSpace($script:VerifierToolsDir)) {
        $candidates += (Split-Path -Parent $script:VerifierToolsDir)
    }

    $candidates += (Get-Location).Path

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        try {
            $full = [IO.Path]::GetFullPath($candidate)
        }
        catch {
            continue
        }

        if (Test-Path (Join-Path $full "YoloDeploy.sln")) {
            return $full
        }
    }

    throw "Cannot find YoloDeploy.sln. Use -Root."
}

function Read-Text {
    param([string]$Path)
    return [IO.File]::ReadAllText($Path)
}

function Need {
    param(
        [string]$RelativePath,
        [string]$Pattern,
        [string]$Label
    )

    $path = Join-Path $script:RootDir $RelativePath

    if (-not (Test-Path $path -PathType Leaf)) {
        throw "[FAIL] $Label : missing $RelativePath"
    }

    $text = Read-Text $path

    if (-not [Text.RegularExpressions.Regex]::IsMatch(
            $text,
            $Pattern,
            [Text.RegularExpressions.RegexOptions]::Singleline)) {
        throw "[FAIL] $Label : pattern mismatch in $RelativePath"
    }

    Write-Host "[OK] $Label" -ForegroundColor Green
}

function MustNotContain {
    param(
        [string]$RelativePath,
        [string]$Text,
        [string]$Label
    )

    $path = Join-Path $script:RootDir $RelativePath

    if (-not (Test-Path $path -PathType Leaf)) {
        throw "[FAIL] $Label : missing $RelativePath"
    }

    if ((Read-Text $path).Contains($Text)) {
        throw "[FAIL] $Label : forbidden text found in $RelativePath"
    }

    Write-Host "[OK] $Label" -ForegroundColor Green
}

$script:RootDir = Resolve-Root $Root

Write-Host ""
Write-Host "YoloDeploy repository verification" -ForegroundColor Cyan
Write-Host "Root: $script:RootDir"
Write-Host ""

Need `
    "YoloDeploy.App\EngineCacheManager.cs" `
    'Path\.Combine\s*\(\s*modelDirectory\s*,\s*cacheKey\s*\+\s*"\.engine"\s*\)' `
    "WPF Engine cache beside ONNX"

MustNotContain `
    "YoloDeploy.App\EngineCacheManager.cs" `
    "Environment.SpecialFolder.LocalApplicationData" `
    "WPF has no LocalApplicationData cache"

Need `
    "YoloDeploy.SDK\EngineCacheManager.cs" `
    'Path\.Combine\s*\(\s*modelDirectory\s*,\s*cacheKey\s*\+\s*"\.engine"\s*\)' `
    ".NET 8 SDK Engine cache beside ONNX"

MustNotContain `
    "YoloDeploy.SDK\EngineCacheManager.cs" `
    "Environment.SpecialFolder.LocalApplicationData" `
    ".NET 8 SDK has no LocalApplicationData cache"

Need `
    "YoloDeploy.SDK.Net48\EngineCacheManager.cs" `
    'Path\.Combine\s*\(\s*modelDirectory\s*,\s*cacheKey\s*\+\s*"\.engine"\s*\)' `
    "Net48 Engine cache beside ONNX"

MustNotContain `
    "YoloDeploy.SDK.Net48\EngineCacheManager.cs" `
    "Environment.SpecialFolder.LocalApplicationData" `
    "Net48 has no LocalApplicationData cache"

Need `
    "YoloDeploy.App\MainWindow.xaml.cs" `
    'EngineCacheManager\.GetStats\s*\(\s*OnnxPathTextBox\.Text\.Trim\(\)\s*\)' `
    "WPF cache statistics use current ONNX"

Need `
    "YoloDeploy.App\MainWindow.xaml.cs" `
    'EngineCacheManager\.OpenCacheFolder\s*\(\s*OnnxPathTextBox\.Text\.Trim\(\)\s*\)' `
    "WPF opens current ONNX/cache directory"

Need `
    "YoloDeploy.App\MainWindow.xaml.cs" `
    'EngineCacheManager\.ClearAll\s*\(\s*onnxPath\s*\)' `
    "WPF clears cache for current ONNX directory"

Need `
    "publish_sdk_runtime.ps1" `
    'PackageName\s*=\s*"YoloDeploy\.SDK\.Runtime"' `
    "Official .NET 8 SDK publisher"

Need `
    "publish_sdk_runtime.ps1" `
    'tasks\s*=\s*@\(\s*"Detect"\s*,\s*"OBB"\s*,\s*"Seg"\s*\)' `
    ".NET 8 runtime declares Detect/OBB/Seg"

Need `
    "publish_sdk_runtime.ps1" `
    'engineCache\s*=\s*"Beside ONNX:' `
    ".NET 8 runtime manifest cache policy"

Need `
    "publish_release.bat" `
    'YoloDeploy - One-click WPF Release' `
    "Current WPF release title"

MustNotContain `
    "publish_release.ps1" `
    '%LOCALAPPDATA%\YoloDeploy\EngineCache' `
    "WPF release docs have no old cache path"

MustNotContain `
    "publish_net48_runtime.ps1" `
    '%LOCALAPPDATA%\YoloDeploy\EngineCache' `
    "Net48 release docs have no old cache path"

$forbiddenRoot = @(
    "publish_multitask_sdk_runtime.bat",
    "publish_multitask_sdk_runtime.ps1",
    "add_sdk_to_solution.bat",
    "add_multitask_sdk_to_solution.bat",
    "add_net48_sdk_to_solution.bat",
    "publish_sdk.ps1",
    "setup_env_example.bat",
    "setup_env_example.txt",
    "verify_runtime.bat",
    "MULTITASK_SDK_UPGRADE_CN.md",
    "RUNTIME_PUBLISH_INTEGRATION_CN.md"
)

foreach ($file in $forbiddenRoot) {
    if (Test-Path (Join-Path $script:RootDir $file)) {
        throw "[FAIL] historical file still exists in repository root: $file"
    }
}

Write-Host "[OK] Repository root has no legacy publisher/migration files." -ForegroundColor Green

foreach ($required in @(
    "publish_release.bat",
    "publish_release.ps1",
    "publish_sdk_runtime.bat",
    "publish_sdk_runtime.ps1",
    "publish_net48_runtime.bat",
    "publish_net48_runtime.ps1",
    "README.md",
    "SDK_INTEGRATION_CN.md",
    "NET48_INTEGRATION_CN.md",
    "release_env.example.bat",
    "docs\REPOSITORY_MAINTENANCE_CN.md"
)) {
    if (-not (Test-Path (Join-Path $script:RootDir $required))) {
        throw "[FAIL] required current file missing: $required"
    }
}

Write-Host "[OK] Official root files are present." -ForegroundColor Green
Write-Host ""
Write-Host "VERIFY SUCCESS" -ForegroundColor Green