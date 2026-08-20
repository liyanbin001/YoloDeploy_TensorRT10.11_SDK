# YoloDeploy MultiTask SDK 升级说明

目标仓库：

```text
https://github.com/liyanbin001/YoloDeploy_TensorRT10.11_WPF
```

本包按当前 `main` 的 Native API 设计。

## 结论

当前 Native 已经提供：

```text
YoloCreate
YoloGetTaskHint
YoloGetModelInfo
YoloDetectBgra
YoloDetectObbBgra
YoloDetectSegBgra
YoloDestroy
YoloGetGpuInfoJson
YoloBuildEngineFromOnnx
```

所以本升级：

**不要求修改 `YoloDeploy.Native` C++ 推理核心。**

主要新增：

```text
YoloDeploy.SDK
YoloDeploy.SDK.Test
sdk_runtime_assets
add_multitask_sdk_to_solution.bat
publish_multitask_sdk_runtime.ps1
publish_multitask_sdk_runtime.bat
```

---

## 1. 合并目录

把本 ZIP 的 `YoloDeploy\` 内容合并到原仓库：

```text
YoloDeploy_TensorRT10.11_WPF/
└─ YoloDeploy/
   ├─ YoloDeploy.App/
   ├─ YoloDeploy.Native/
   ├─ YoloDeploy.SDK/              NEW
   ├─ YoloDeploy.SDK.Test/         NEW
   ├─ docs/
   ├─ models/
   ├─ tools/
   ├─ sdk_runtime_assets/          NEW
   ├─ YoloDeploy.sln
   ├─ add_multitask_sdk_to_solution.bat
   ├─ publish_multitask_sdk_runtime.bat
   └─ publish_multitask_sdk_runtime.ps1
```

运行：

```text
add_multitask_sdk_to_solution.bat
```

然后 VS2022：

```text
Release | x64
```

---

## 2. 统一 Auto API

```csharp
using YoloDeploy.SDK;

using var detector = new YoloDetector(
    new YoloDetectorOptions
    {
        ModelPath = @"D:\Models\best.onnx",
        ClassNamesPath = @"D:\Models\classes.names",
        InputWidth = 1280,
        InputHeight = 512,
        Task = YoloTask.Auto
    });

YoloDetectionResponse result =
    detector.Detect(@"D:\Images", "001.jpg");

switch (result)
{
    case DetectResponse detect:
        break;

    case ObbResponse obb:
        break;

    case SegResponse seg:
        break;
}
```

Native task hint 映射：

```text
0 -> Detect
1 -> OBB
2 -> Seg
```

如果无法识别，SDK 不会猜测，而是抛出异常并附带 `YoloGetModelInfo()`。

---

## 3. Detect 强类型 API

```csharp
using var detector = new DetectDetector(
    new DetectDetectorOptions
    {
        ModelPath = @"D:\Models\detect.onnx",
        ClassNamesPath = @"D:\Models\classes.names",
        InputWidth = 1280,
        InputHeight = 512
    });

DetectResponse result =
    detector.Detect(@"D:\Images", "001.jpg");
```

返回：

```text
ClassId
ClassName
Confidence
X1 Y1 X2 Y2
Width Height CenterX CenterY
```

---

## 4. OBB 强类型 API

```csharp
using var detector = new ObbDetector(
    new ObbDetectorOptions
    {
        ModelPath = @"D:\Models\obb.onnx",
        ClassNamesPath = @"D:\Models\classes.names",
        InputWidth = 1280,
        InputHeight = 512
    });

ObbResponse result =
    detector.Detect(@"D:\Images", "001.jpg");
```

返回：

```text
ClassId
ClassName
Confidence
CenterX CenterY
Width Height
AngleRadians / AngleDegrees
P1 P2 P3 P4
```

---

## 5. Seg 强类型 API

```csharp
using var detector = new SegDetector(
    new SegDetectorOptions
    {
        ModelPath = @"D:\Models\seg.onnx",
        ClassNamesPath = @"D:\Models\classes.names",
        InputWidth = 1280,
        InputHeight = 512,
        MaskThreshold = 0.50f
    });

SegResponse result =
    detector.Detect(@"D:\Images", "001.jpg");
```

每个 `SegInstance`：

```text
ClassId
ClassName
Confidence

MaskId
MaskAreaPixels

mask-derived:
X1 Y1 X2 Y2

minimum-area rotated rectangle:
CenterX CenterY
RotatedWidth RotatedHeight
AngleRadians / AngleDegrees
P1 P2 P3 P4
```

另外：

```csharp
SegInstanceMask map =
    result.InstanceMask;
```

原图分辨率：

```text
ushort
0 = background
1..65535 = MaskId
```

某一个实例可直接生成二值 mask：

```csharp
byte[] mask =
    result.InstanceMask.CreateBinaryMask(
        instance.MaskId);
```

---

## 6. Engine Cache

SDK 与当前 WPF 对齐：

```text
%LOCALAPPDATA%\YoloDeploy\EngineCache
```

并保持：

```text
SchemaVersion = 2

ONNX SHA256
GPU name
Compute Capability
SM count
TensorRT version
FP16 / FP32
input width x height
workspace MiB
```

因此同一台机器的 WPF 和 SDK 可以复用有效 Engine。

---

## 7. 固定输入尺寸

初始化时固定：

```text
InputWidth
InputHeight
```

例如：

```text
1280 x 512
```

原始图片可为其他尺寸，由当前 Native LetterBox 处理。

固定 shape ONNX 必须匹配 W/H。

Dynamic ONNX 仍由当前 Builder 固化为：

```text
MIN = OPT = MAX = [1,3,H,W]
```

---

## 8. TestSDK

编译后：

```text
TestSDK.exe auto Models\best.onnx Models\classes.names D:\Images\001.jpg 1280 512

TestSDK.exe detect ...
TestSDK.exe obb ...
TestSDK.exe seg ...
```

Seg 可追加：

```text
confidence nms maskThreshold
```

例如：

```text
TestSDK.exe seg Models\seg.onnx Models\classes.names D:\Images\001.jpg 1280 512 0.25 0.45 0.50
```

---

## 9. 一键生成客户 Runtime ZIP

开发机需要原仓库同样的：

```text
VS2022
.NET 8 SDK
TENSORRT_ROOT
CUDA_PATH
```

执行：

```text
publish_multitask_sdk_runtime.bat
```

最终得到：

```text
dist\
└─ YoloDeploy.SDK.Runtime.MultiTask.zip
```

客户包包含：

```text
YoloDeploy.SDK.dll
YoloDeploy.Native.dll
TestSDK.exe
TensorRT DLL
nvonnxparser DLL
CUDA user-mode DLL
Examples
Models
README_CUSTOMER_CN.txt
verify_runtime.bat
runtime_manifest.json
SHA256SUMS.txt
```

发布脚本禁止把开发机 `.engine` 打进去。

客户只交付：

```text
ONNX
classes.names
Runtime ZIP
```

目标机第一次自动生成本机 Engine。

---

## 10. 支持边界

保持当前 GitHub Phase 6 的边界：

```text
Detect raw
OBB raw
YOLO26 instance Seg prediction + proto
batch = 1
one image input
Detect/OBB one output
Seg prediction + proto two outputs
FP32 / FP16
fixed rectangular W/H
LetterBox + RGB + /255 + NCHW
```

不扩展：

```text
Pose
whole-image Classification
Semantic Segmentation
INT8 calibration
batch > 1
multiple image inputs
arbitrary custom multi-output heads
```
