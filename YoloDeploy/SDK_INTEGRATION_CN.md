# YoloDeploy .NET 8 SDK 集成说明

## 1. SDK 定位

`YoloDeploy.SDK` 是当前 .NET 8 Windows SDK，统一支持：

- Detect
- OBB
- YOLO26 Instance Segmentation
- `YoloTask.Auto`
- ONNX -> TensorRT Engine
- GPU/模型/配置绑定的 Engine Cache
- 文件图片
- 工业相机 `byte[]` / `IntPtr`

客户代码层引用：

```text
YoloDeploy.SDK.dll
```

部署时必须整体保留 Runtime 中的 Native、TensorRT、ONNX Parser 和 CUDA 用户态 DLL。

## 2. 客户项目配置

推荐：

```xml
<TargetFramework>net8.0-windows</TargetFramework>
<PlatformTarget>x64</PlatformTarget>
<UseWPF>true</UseWPF>
```

当前文件图片接口使用 WPF BitmapDecoder，因此使用文件图片检测时建议启用 `UseWPF`。

## 3. Auto API

```csharp
using YoloDeploy.SDK;

using var detector =
    new YoloDetector(
        new YoloDetectorOptions
        {
            ModelPath =
                @"D:\YoloModels\ProjectA\best.onnx",

            ClassNamesPath =
                @"D:\YoloModels\ProjectA\classes.names",

            InputWidth = 1280,
            InputHeight = 512,
            EnableFp16 = true,
            WorkspaceMiB = 1024,
            ConfidenceThreshold = 0.25f,
            NmsThreshold = 0.45f,
            MaskThreshold = 0.50f,
            Task = YoloTask.Auto
        });

YoloDetectionResponse response =
    detector.Detect(
        @"D:\Images\001.jpg");
```

Auto 任务提示：
- `0` = Detect
- `1` = OBB
- `2` = Seg
- 无法识别时抛出异常，不自动猜测

## 4. 强类型 API

可直接使用：
- `DetectDetector`
- `ObbDetector`
- `SegDetector`

适合业务代码明确知道模型类型的场景。

## 5. classes.names

每行一个类别，数量与顺序必须和训练模型一致。

示例：

```text
defect_A
defect_B
defect_C
```

## 6. Engine Cache

当前 SDK 将 Engine 放在 ONNX 所在目录：

```text
D:\YoloModels\ProjectA\
├─ best.onnx
├─ classes.names
├─ <cache-key>.engine
└─ <cache-key>.engine.json
```

Cache 身份包括：
- ONNX SHA-256
- GPU
- Compute Capability
- SM 数
- TensorRT 版本
- FP16 / FP32
- 输入 W/H
- Workspace

Driver / CUDA Runtime 记录在元数据中但不直接进入 Cache Key。

路径支持中文，但建议避免过深目录和过长完整路径。

## 7. 工业相机 byte[]

```csharp
var result =
    detector.DetectFrame(
        cameraBuffer,
        width,
        height,
        stride,
        CameraPixelFormat.Bgr24);
```

`stride` 必须使用相机实际行跨度。

## 8. 工业相机 IntPtr

```csharp
var result =
    detector.DetectFramePinned(
        pData,
        width,
        height,
        stride,
        CameraPixelFormat.Bgr24);
```

`pData` 必须保持有效直到同步调用返回。

## 9. Runtime 发布

开发机运行：

```text
publish_sdk_runtime.bat
```

输出：

```text
dist\YoloDeploy.SDK.Runtime.zip
```

正式 Runtime 会包含：
- `YoloDeploy.SDK.dll`
- `YoloDeploy.Native.dll`
- TestSDK
- TensorRT DLL
- ONNX Parser DLL
- CUDA 用户态 DLL
- 示例/说明
- runtime manifest
- SHA256 校验文件

发布脚本禁止携带开发机 `.engine`。

## 10. TestSDK

示例：

```text
TestSDK.exe auto   Models\best.onnx Models\classes.names D:\Images\001.jpg 1280 512
TestSDK.exe detect Models\best.onnx Models\classes.names D:\Images\001.jpg 1280 512
TestSDK.exe obb    Models\best.onnx Models\classes.names D:\Images\001.jpg 1280 512
TestSDK.exe seg    Models\seg.onnx  Models\classes.names D:\Images\001.jpg 1280 512 0.25 0.45 0.50
```

建议在客户机先用 TestSDK 验证环境和模型，再接入正式业务程序。

## 11. 注意事项

- x64 only
- batch=1
- 不支持任意自定义输出 Head
- Engine 是 GPU/TensorRT 相关资产
- 推荐客户交付 ONNX，不交付开发机 Engine
- 模型目录建议短路径