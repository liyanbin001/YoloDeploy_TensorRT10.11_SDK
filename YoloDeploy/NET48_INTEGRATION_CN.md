# YoloDeploy SDK .NET Framework 4.8 集成

## 1. 定位

`YoloDeploy.SDK.Net48` 用于已有的 .NET Framework 4.8 Windows 工业项目。

它与 `.NET 8 YoloDeploy.SDK` 并行存在，不替换主 SDK。

当前 Solution 已经包含：
- `YoloDeploy.SDK.Net48`
- `YoloDeploy.SDK.Net48.Test`

不需要再运行 `add_net48_sdk_to_solution.bat`。

## 2. 开发机要求

- Visual Studio 2022
- .NET Framework 4.8 Developer/Targeting Pack
- TensorRT / CUDA 开发环境
- `TENSORRT_ROOT`
- `CUDA_PATH`

构建配置：

```text
Release | x64
```

建议：
1. Rebuild `YoloDeploy.Native`
2. Rebuild `YoloDeploy.SDK.Net48`
3. Rebuild `YoloDeploy.SDK.Net48.Test`

## 3. 客户项目

必须配置：

```text
Target Framework: .NET Framework 4.8
Platform target: x64
Prefer 32-bit: false
```

引用：

```text
YoloDeploy.SDK.Net48.dll
```

命名空间：

```csharp
using YoloDeploy.SDK;
```

## 4. 模型与 Engine

推荐客户交付：
- `best.onnx`
- `classes.names`
- Net48 Runtime ZIP

第一次初始化会在目标 GPU 上构建 Engine。

当前缓存位置：

```text
ONNX 模型所在目录
```

例如：

```text
D:\YoloModels\ProjectA\
├─ best.onnx
├─ classes.names
├─ <cache-key>.engine
└─ <cache-key>.engine.json
```

支持中文目录，但建议完整路径保持较短。

Cache Key 与 .NET 8 主线一致。

Net48 元数据使用 .NET Framework 自带序列化实现；旧元数据解析失败时会判为无效缓存并重建。

## 5. OBB 示例

```csharp
ObbDetectorOptions options =
    new ObbDetectorOptions
    {
        ModelPath =
            @"D:\YoloModels\ProjectA\best.onnx",

        ClassNamesPath =
            @"D:\YoloModels\ProjectA\classes.names",

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

输出：

```text
dist\YoloDeploy.SDK.Runtime.Net48.zip
```

Net48 客户机不需要 .NET 8 Runtime，但需要：
- .NET Framework 4.8
- NVIDIA GPU + 兼容驱动
- 通常建议安装 VC++ 2015-2022 Redistributable x64

## 9. 注意事项

Native/TensorRT 为 x64：
- AnyCPU 项目建议明确改为 x64
- x86 项目不能加载当前 `YoloDeploy.Native.dll`
- 不建议跨 GPU 交付 `.engine`
- 建议在目标机从 ONNX 首次构建