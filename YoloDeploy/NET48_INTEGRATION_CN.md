# YoloDeploy SDK .NET Framework 4.8 集成

## 1. 合并目录

将本包 `YoloDeploy\` 目录内容合并到原仓库：

```text
YoloDeploy_TensorRT10.11_SDK/
└─ YoloDeploy/
```

不要删除原来的 `YoloDeploy.SDK`，Net48 与 Net8 并行存在。

## 2. 加入 VS2022 Solution

运行：

```text
add_net48_sdk_to_solution.bat
```

或者手工：

```powershell
dotnet sln YoloDeploy.sln add YoloDeploy.SDK.Net48\YoloDeploy.SDK.Net48.csproj
dotnet sln YoloDeploy.sln add YoloDeploy.SDK.Net48.Test\YoloDeploy.SDK.Net48.Test.csproj
```

## 3. 开发机要求

需要安装：

- Visual Studio 2022
- .NET Framework 4.8 Developer Pack / Targeting Pack
- .NET SDK（用于 SDK-style csproj）
- 原项目所需 TensorRT / CUDA 开发环境
- TENSORRT_ROOT
- CUDA_PATH

选择：

```text
Release | x64
```

建议顺序：

1. Rebuild YoloDeploy.Native
2. Rebuild YoloDeploy.SDK.Net48
3. Rebuild YoloDeploy.SDK.Net48.Test

## 4. 客户旧项目引用

客户项目必须：

```text
Target Framework: .NET Framework 4.8
Platform target: x64
Prefer 32-bit: false
```

添加引用：

```text
YoloDeploy.SDK.Net48.dll
```

命名空间不变：

```csharp
using YoloDeploy.SDK;
```

## 5. OBB 文件检测

```csharp
ObbDetectorOptions options =
    new ObbDetectorOptions
    {
        ModelPath = @"D:\Model\best.onnx",
        ClassNamesPath = @"D:\Model\classes.names",
        InputWidth = 1280,
        InputHeight = 512,
        EnableFp16 = true
    };

using (ObbDetector detector =
    new ObbDetector(options))
{
    ObbDetectionResponse result =
        detector.Detect(
            @"D:\Images",
            "001.jpg");
}
```

## 6. 工业相机 BGR24

```csharp
ObbDetectionResponse result =
    detector.DetectFrame(
        cameraBuffer,
        width,
        height,
        stride,
        CameraPixelFormat.Bgr24);
```

## 7. 工业相机 IntPtr

```csharp
ObbDetectionResponse result =
    detector.DetectFramePinned(
        pData,
        width,
        height,
        stride,
        CameraPixelFormat.Bgr24);
```

`pData` 必须保持有效直到调用返回。

## 8. Runtime 发布

运行：

```text
publish_net48_runtime.bat
```

最终：

```text
dist\
└─ YoloDeploy.SDK.Runtime.Net48.zip
```

Net48 客户不需要安装 .NET 8 Runtime。

## 9. Engine Cache

保持：

```text
%LOCALAPPDATA%\YoloDeploy\EngineCache
```

Cache Key 与主线一致。

Net48 使用 .NET Framework 自带 `JavaScriptSerializer`，
CreatedUtc 写成 ISO-8601 字符串，使元数据尽量保持与主线互操作。

如果旧缓存元数据无法解析，SDK 会把缓存判定为无效并自动重建 Engine，
不会继续加载不可信缓存。

## 10. 注意

Native/TensorRT 仍然是 x64，因此：
- AnyCPU 项目建议明确改 x64。
- x86 项目无法直接加载当前 YoloDeploy.Native.dll。
