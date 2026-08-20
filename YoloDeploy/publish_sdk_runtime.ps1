param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$PackageName = "YoloDeploy.SDK.Runtime"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path

$NativeProject = Join-Path $Root "YoloDeploy.Native\YoloDeploy.Native.vcxproj"
$SdkProject = Join-Path $Root "YoloDeploy.SDK\YoloDeploy.SDK.csproj"
$TestProject = Join-Path $Root "YoloDeploy.SDK.Test\YoloDeploy.SDK.Test.csproj"
$AssetsDir = Join-Path $Root "sdk_runtime_assets"

$NativeDll = Join-Path $Root "artifacts\native\$Configuration\YoloDeploy.Native.dll"

$DistRoot = Join-Path $Root "dist"
$TestPublish = Join-Path $DistRoot "_sdk_test_publish"
$PackageDir = Join-Path $DistRoot $PackageName
$ZipPath = Join-Path $DistRoot ($PackageName + ".zip")

function Step([string]$Text) {
    Write-Host ""
    Write-Host "================================================================" -ForegroundColor Cyan
    Write-Host $Text -ForegroundColor Cyan
    Write-Host "================================================================" -ForegroundColor Cyan
}

function Require([string]$Path, [string]$Description) {
    if (-not (Test-Path $Path)) {
        throw "$Description not found: $Path"
    }
}

function Find-MSBuild {
    $cmd = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }

    $vswhereCandidates = @(
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\Installer\vswhere.exe"
    )

    foreach ($vswhere in $vswhereCandidates) {
        if (-not (Test-Path $vswhere)) {
            continue
        }

        $found = & $vswhere `
            -latest `
            -products * `
            -requires Microsoft.Component.MSBuild `
            -find "MSBuild\**\Bin\MSBuild.exe" |
            Select-Object -First 1

        if ($found -and (Test-Path $found)) {
            return $found
        }
    }

    throw "MSBuild.exe not found. Install VS2022 Desktop development with C++."
}

function Copy-DllPatterns {
    param(
        [string[]]$Directories,
        [string[]]$Patterns,
        [string]$Destination
    )

    $seen = @{}

    foreach ($dir in $Directories) {
        if ([string]::IsNullOrWhiteSpace($dir) -or -not (Test-Path $dir)) {
            continue
        }

        foreach ($pattern in $Patterns) {
            Get-ChildItem `
                -Path $dir `
                -Filter $pattern `
                -File `
                -ErrorAction SilentlyContinue |
            ForEach-Object {
                $key = $_.Name.ToLowerInvariant()

                if (-not $seen.ContainsKey($key)) {
                    Copy-Item $_.FullName $Destination -Force
                    $seen[$key] = $true
                }
            }
        }
    }
}

Step "1/8 Validate environment"

Require $NativeProject "Native project"
Require $SdkProject "SDK project"
Require $TestProject "Test project"
Require $AssetsDir "Runtime assets"

if ([string]::IsNullOrWhiteSpace($env:TENSORRT_ROOT)) {
    throw "TENSORRT_ROOT is not set."
}

if ([string]::IsNullOrWhiteSpace($env:CUDA_PATH)) {
    throw "CUDA_PATH is not set."
}

Require $env:TENSORRT_ROOT "TensorRT root"
Require $env:CUDA_PATH "CUDA path"

$MSBuild = Find-MSBuild
$DotNet = (Get-Command dotnet.exe -ErrorAction Stop).Source

Write-Host "MSBuild       : $MSBuild"
Write-Host "dotnet        : $DotNet"
Write-Host "TENSORRT_ROOT : $env:TENSORRT_ROOT"
Write-Host "CUDA_PATH     : $env:CUDA_PATH"

Step "2/8 Build Native Release|x64"

& $MSBuild `
    $NativeProject `
    "/t:Rebuild" `
    "/p:Configuration=$Configuration" `
    "/p:Platform=x64" `
    "/m" `
    "/nologo"

if ($LASTEXITCODE -ne 0) {
    throw "Native build failed."
}

Require $NativeDll "YoloDeploy.Native.dll"

Step "3/8 Build YoloDeploy.SDK (Detect / OBB / Seg)"

& $DotNet build `
    $SdkProject `
    -c $Configuration `
    -p:Platform=x64 `
    --nologo

if ($LASTEXITCODE -ne 0) {
    throw "SDK build failed."
}

Step "4/8 Publish TestSDK"

if (Test-Path $TestPublish) {
    Remove-Item $TestPublish -Recurse -Force
}

& $DotNet publish `
    $TestProject `
    -c $Configuration `
    -r $RuntimeIdentifier `
    --self-contained false `
    -p:Platform=x64 `
    -o $TestPublish `
    --nologo

if ($LASTEXITCODE -ne 0) {
    throw "TestSDK publish failed."
}

Require (Join-Path $TestPublish "YoloDeploy.SDK.dll") "YoloDeploy.SDK.dll"
Require (Join-Path $TestPublish "TestSDK.exe") "TestSDK.exe"

Step "5/8 Assemble customer runtime"

if (Test-Path $PackageDir) {
    Remove-Item $PackageDir -Recurse -Force
}

New-Item -ItemType Directory -Path $PackageDir | Out-Null

foreach ($name in @(
    "YoloDeploy.SDK.dll",
    "YoloDeploy.SDK.xml",
    "TestSDK.exe",
    "TestSDK.dll",
    "TestSDK.deps.json",
    "TestSDK.runtimeconfig.json"
)) {
    $src = Join-Path $TestPublish $name
    if (Test-Path $src) {
        Copy-Item $src $PackageDir -Force
    }
}

Copy-Item $NativeDll $PackageDir -Force

Copy-Item `
    -Path (Join-Path $AssetsDir "*") `
    -Destination $PackageDir `
    -Recurse `
    -Force

Step "6/8 Copy TensorRT / ONNX parser / CUDA user-mode DLLs"

Copy-DllPatterns `
    -Directories @(
        (Join-Path $env:TENSORRT_ROOT "lib"),
        (Join-Path $env:TENSORRT_ROOT "bin")
    ) `
    -Patterns @(
        "nvinfer*.dll",
        "nvonnxparser*.dll"
    ) `
    -Destination $PackageDir

Copy-DllPatterns `
    -Directories @(
        (Join-Path $env:CUDA_PATH "bin")
    ) `
    -Patterns @(
        "cudart64_*.dll",
        "cublas64_*.dll",
        "cublasLt64_*.dll",
        "nvrtc64_*.dll",
        "nvrtc-builtins64_*.dll",
        "cufft64_*.dll",
        "curand64_*.dll"
    ) `
    -Destination $PackageDir

foreach ($required in @(
    "YoloDeploy.SDK.dll",
    "YoloDeploy.Native.dll",
    "TestSDK.exe",
    "TestSDK.runtimeconfig.json",
    "nvinfer_10.dll",
    "nvinfer_plugin_10.dll",
    "nvonnxparser_10.dll",
    "README_CUSTOMER_CN.txt",
    "verify_runtime.bat"
)) {
    Require (Join-Path $PackageDir $required) "Required runtime file"
}

$Cudart = Get-ChildItem `
    $PackageDir `
    -Filter "cudart64_*.dll" `
    -File `
    -ErrorAction SilentlyContinue |
    Select-Object -First 1

if (-not $Cudart) {
    throw "cudart64_*.dll missing."
}

# Do not distribute a developer-machine TensorRT engine.
$engines = Get-ChildItem `
    $PackageDir `
    -Filter "*.engine" `
    -File `
    -Recurse `
    -ErrorAction SilentlyContinue

if ($engines) {
    throw "Package unexpectedly contains .engine files."
}

Step "7/8 Generate runtime manifest + SHA256"

$files = Get-ChildItem `
    $PackageDir `
    -File `
    -Recurse |
    Sort-Object FullName

$manifestFiles = @()
$sha = @()

foreach ($file in $files) {
    $relative =
        [IO.Path]::GetRelativePath(
            $PackageDir,
            $file.FullName
        ).Replace("\", "/")

    $hash =
        (Get-FileHash `
            $file.FullName `
            -Algorithm SHA256
        ).Hash.ToLowerInvariant()

    $manifestFiles += [ordered]@{
        path = $relative
        bytes = $file.Length
        sha256 = $hash
    }

    $sha += "$hash  $relative"
}

$manifest = [ordered]@{
    schemaVersion = 1
    package = $PackageName
    tasks = @("Detect", "OBB", "Seg")
    autoTask = $true
    modelDelivery = "ONNX"
    enginePolicy = "Build/cache on target GPU"
    engineCache = "Beside ONNX: <model>.engine + <model>.engine.json"
    fixedInput = $true
    files = $manifestFiles
}

$manifest |
    ConvertTo-Json -Depth 8 |
    Set-Content `
        (Join-Path $PackageDir "runtime_manifest.json") `
        -Encoding UTF8

$sha |
    Set-Content `
        (Join-Path $PackageDir "SHA256SUMS.txt") `
        -Encoding ASCII

Step "8/8 Create ZIP"

if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

Compress-Archive `
    -Path $PackageDir `
    -DestinationPath $ZipPath `
    -CompressionLevel Optimal `
    -Force

if (Test-Path $TestPublish) {
    Remove-Item $TestPublish -Recurse -Force
}

Write-Host ""
Write-Host "SUCCESS" -ForegroundColor Green
Write-Host $ZipPath -ForegroundColor Green
