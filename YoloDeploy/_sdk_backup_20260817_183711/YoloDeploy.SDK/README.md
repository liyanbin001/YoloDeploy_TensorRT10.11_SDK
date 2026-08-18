# YoloDeploy.SDK MultiTask

基于当前 GitHub `YoloDeploy_TensorRT10.11_WPF` Native API 的托管 SDK。

支持：

- Detect
- OBB
- YOLO26 instance Seg
- `YoloTask.Auto`
- ONNX -> TensorRT Engine 自动构建
- 与现有 WPF 相同的 Engine Cache
- 固定矩形 W/H
- FP32 / FP16

## Auto

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
    detector.Detect(@"D:\Images\001.jpg");

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

## Detect

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

## OBB

```csharp
using var detector = new ObbDetector(
    new ObbDetectorOptions
    {
        ModelPath = @"D:\Models\obb.onnx",
        ClassNamesPath = @"D:\Models\classes.names",
        InputWidth = 1280,
        InputHeight = 512
    });

ObbDetectionResponse result =
    detector.Detect(@"D:\Images", "001.jpg");
```

## Seg

```csharp
using var detector = new SegDetector(
    new SegDetectorOptions
    {
        ModelPath = @"D:\Models\seg.onnx",
        ClassNamesPath = @"D:\Models\classes.names",
        InputWidth = 1280,
        InputHeight = 512,
        MaskThreshold = 0.5f
    });

SegResponse result =
    detector.Detect(@"D:\Images", "001.jpg");

foreach (SegInstance instance in result.Detections)
{
    byte[] oneMask =
        result.InstanceMask.CreateBinaryMask(instance.MaskId);
}
```

Seg 返回：

- 类别 / 置信度
- MaskId
- MaskAreaPixels
- mask 派生 AABB
- mask 派生 minimum-area rotated rectangle
- P1/P2/P3/P4
- 原图分辨率 UInt16 InstanceMask

## Cache

复用当前 WPF 的：

```text
%LOCALAPPDATA%\YoloDeploy\EngineCache
```

SchemaVersion 与 cache key 保持一致，因此同机 WPF/SDK 可复用有效 Engine Cache。
