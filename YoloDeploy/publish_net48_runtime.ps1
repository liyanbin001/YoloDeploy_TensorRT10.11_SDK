param(
    [string]$Configuration = "Release",
    [string]$PackageName = "YoloDeploy.SDK.Runtime.Net48"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path

$NativeProject =
    Join-Path $Root "YoloDeploy.Native\YoloDeploy.Native.vcxproj"

$SdkProject =
    Join-Path $Root "YoloDeploy.SDK.Net48\YoloDeploy.SDK.Net48.csproj"

$TestProject =
    Join-Path $Root "YoloDeploy.SDK.Net48.Test\YoloDeploy.SDK.Net48.Test.csproj"

$AssetsDir =
    Join-Path $Root "sdk_runtime_assets_net48"

$NativeDll =
    Join-Path $Root "artifacts\native\$Configuration\YoloDeploy.Native.dll"

$DistRoot =
    Join-Path $Root "dist"

$PackageDir =
    Join-Path $DistRoot $PackageName

$ZipPath =
    Join-Path $DistRoot ($PackageName + ".zip")

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
    $cmd =
        Get-Command msbuild.exe -ErrorAction SilentlyContinue

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

        $found =
            & $vswhere `
              -latest `
              -products * `
              -requires Microsoft.Component.MSBuild `
              -find "MSBuild\**\Bin\MSBuild.exe" |
            Select-Object -First 1

        if ($found -and
            (Test-Path $found)) {
            return $found
        }
    }

    throw "MSBuild.exe not found. Install Visual Studio 2022."
}

function Copy-DllPatterns {
    param(
        [string[]]$Directories,
        [string[]]$Patterns,
        [string]$Destination
    )

    $seen = @{}

    foreach ($directory in $Directories) {
        if ([string]::IsNullOrWhiteSpace($directory) -or
            -not (Test-Path $directory)) {
            continue
        }

        foreach ($pattern in $Patterns) {
            Get-ChildItem `
                -Path $directory `
                -Filter $pattern `
                -File `
                -ErrorAction SilentlyContinue |
            ForEach-Object {
                $key =
                    $_.Name.ToLowerInvariant()

                if (-not $seen.ContainsKey($key)) {
                    Copy-Item `
                        $_.FullName `
                        $Destination `
                        -Force

                    $seen[$key] = $true
                }
            }
        }
    }
}

function Find-BuiltFile {
    param(
        [string]$ProjectDirectory,
        [string]$FileName
    )

    $candidate =
        Get-ChildItem `
            -Path (Join-Path $ProjectDirectory "bin") `
            -Filter $FileName `
            -File `
            -Recurse `
            -ErrorAction SilentlyContinue |
        Where-Object {
            $_.FullName -match "\\$Configuration\\"
        } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if (-not $candidate) {
        throw "Build output not found: $FileName under $ProjectDirectory"
    }

    return $candidate.FullName
}

Step "1/8 Validate build environment"

Require $NativeProject "Native project"
Require $SdkProject "Net48 SDK project"
Require $TestProject "Net48 Test project"
Require $AssetsDir "Net48 runtime assets"

if ([string]::IsNullOrWhiteSpace($env:TENSORRT_ROOT)) {
    throw "TENSORRT_ROOT is not set."
}

if ([string]::IsNullOrWhiteSpace($env:CUDA_PATH)) {
    throw "CUDA_PATH is not set."
}

Require $env:TENSORRT_ROOT "TensorRT root"
Require $env:CUDA_PATH "CUDA path"

$MSBuild = Find-MSBuild

Write-Host "MSBuild       : $MSBuild"
Write-Host "TENSORRT_ROOT : $env:TENSORRT_ROOT"
Write-Host "CUDA_PATH     : $env:CUDA_PATH"

# .NET Framework 4.8 reference assemblies.
$Net48Ref =
    "${env:ProgramFiles(x86)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\mscorlib.dll"

if (-not (Test-Path $Net48Ref)) {
    throw ".NET Framework 4.8 Developer Pack / Targeting Pack was not found. Install it on the development PC."
}

Step "2/8 Build YoloDeploy.Native ($Configuration | x64)"

& $MSBuild `
    $NativeProject `
    "/t:Rebuild" `
    "/p:Configuration=$Configuration" `
    "/p:Platform=x64" `
    "/m" `
    "/nologo"

if ($LASTEXITCODE -ne 0) {
    throw "YoloDeploy.Native build failed."
}

Require $NativeDll "YoloDeploy.Native.dll"

Step "3/8 Build YoloDeploy.SDK.Net48"

& $MSBuild `
    $SdkProject `
    "/t:Restore;Rebuild" `
    "/p:Configuration=$Configuration" `
    "/p:Platform=x64" `
    "/m" `
    "/nologo"

if ($LASTEXITCODE -ne 0) {
    throw "YoloDeploy.SDK.Net48 build failed."
}

$SdkDll =
    Find-BuiltFile `
        (Join-Path $Root "YoloDeploy.SDK.Net48") `
        "YoloDeploy.SDK.Net48.dll"

Step "4/8 Build TestSDK.Net48"

& $MSBuild `
    $TestProject `
    "/t:Restore;Rebuild" `
    "/p:Configuration=$Configuration" `
    "/p:Platform=x64" `
    "/m" `
    "/nologo"

if ($LASTEXITCODE -ne 0) {
    throw "YoloDeploy.SDK.Net48.Test build failed."
}

$TestExe =
    Find-BuiltFile `
        (Join-Path $Root "YoloDeploy.SDK.Net48.Test") `
        "TestSDK.Net48.exe"

$TestDir =
    Split-Path -Parent $TestExe

Step "5/8 Assemble Net48 customer runtime"

if (Test-Path $PackageDir) {
    Remove-Item $PackageDir -Recurse -Force
}

New-Item `
    -ItemType Directory `
    -Path $PackageDir |
    Out-Null

Copy-Item $SdkDll $PackageDir -Force
Copy-Item $NativeDll $PackageDir -Force
Copy-Item $TestExe $PackageDir -Force

foreach ($name in @(
    "TestSDK.Net48.exe.config",
    "YoloDeploy.SDK.Net48.xml"
)) {
    $source = Join-Path $TestDir $name

    if (-not (Test-Path $source)) {
        $source =
            Join-Path (Split-Path -Parent $SdkDll) $name
    }

    if (Test-Path $source) {
        Copy-Item $source $PackageDir -Force
    }
}

Copy-Item `
    -Path (Join-Path $AssetsDir "*") `
    -Destination $PackageDir `
    -Recurse `
    -Force

Step "6/8 Copy TensorRT / ONNX Parser / CUDA user-mode DLLs"

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
    "YoloDeploy.SDK.Net48.dll",
    "YoloDeploy.Native.dll",
    "TestSDK.Net48.exe",
    "nvinfer_10.dll",
    "nvinfer_plugin_10.dll",
    "nvonnxparser_10.dll",
    "README_CUSTOMER_NET48_CN.txt",
    "verify_runtime_net48.bat"
)) {
    Require `
        (Join-Path $PackageDir $required) `
        "Required Net48 runtime file"
}

$Cudart =
    Get-ChildItem `
        $PackageDir `
        -Filter "cudart64_*.dll" `
        -File `
        -ErrorAction SilentlyContinue |
    Select-Object -First 1

if (-not $Cudart) {
    throw "cudart64_*.dll missing."
}

# Never ship a developer-machine engine.
$Engines =
    Get-ChildItem `
        $PackageDir `
        -Filter "*.engine" `
        -File `
        -Recurse `
        -ErrorAction SilentlyContinue

if ($Engines) {
    throw "Package unexpectedly contains .engine files. Distribute ONNX instead."
}

Step "7/8 Generate manifest + SHA256"

$Files =
    Get-ChildItem `
        $PackageDir `
        -File `
        -Recurse |
    Sort-Object FullName

$ManifestFiles = @()
$ShaLines = @()

foreach ($file in $Files) {
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

    $ManifestFiles +=
        [ordered]@{
            path = $relative
            bytes = $file.Length
            sha256 = $hash
        }

    $ShaLines +=
        "$hash  $relative"
}

$Manifest =
    [ordered]@{
        schemaVersion = 1
        package = $PackageName
        managedFramework = ".NET Framework 4.8"
        architecture = "x64"
        managedSdk = "YoloDeploy.SDK.Net48.dll"
        nativeBridge = "YoloDeploy.Native.dll"
        tasks = @("Detect", "OBB", "Seg")
        autoTask = $true
        cameraMemoryInput = $true
        modelDelivery = "ONNX"
        enginePolicy = "Build/cache on target GPU"
        engineCache = "%LOCALAPPDATA%\YoloDeploy\EngineCache"
        requiresDotNet8 = $false
        files = $ManifestFiles
    }

$Manifest |
    ConvertTo-Json -Depth 8 |
    Set-Content `
        (Join-Path $PackageDir "runtime_manifest.json") `
        -Encoding UTF8

$ShaLines |
    Set-Content `
        (Join-Path $PackageDir "SHA256SUMS.txt") `
        -Encoding ASCII

Step "8/8 Create Net48 ZIP"

if (-not (Test-Path $DistRoot)) {
    New-Item -ItemType Directory -Path $DistRoot | Out-Null
}

if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

Compress-Archive `
    -Path $PackageDir `
    -DestinationPath $ZipPath `
    -CompressionLevel Optimal `
    -Force

Write-Host ""
Write-Host "SUCCESS" -ForegroundColor Green
Write-Host "Net48 package:" -ForegroundColor Green
Write-Host $ZipPath -ForegroundColor Green
