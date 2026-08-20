# YoloDeploy

Windows x64 下的 YOLO TensorRT 10.11 工业部署工程。

当前主线由以下部分组成：

- `YoloDeploy.App`：.NET 8 WPF 应用
- `YoloDeploy.Native`：C++ / TensorRT 10.11 / CUDA 推理核心
- `YoloDeploy.SDK`：.NET 8 SDK
- `YoloDeploy.SDK.Test`：.NET 8 SDK 测试程序
- `YoloDeploy.SDK.Net48`：.NET Framework 4.8 SDK
- `YoloDeploy.SDK.Net48.Test`：Net48 测试程序

## 1. 当前支持任务

- YOLO Detect
- YOLO OBB
- YOLO26 Instance Segmentation
- `YoloTask.Auto` 自动任务识别
- Batch = 1
- 单图像输入
- FP32 / FP16 TensorRT Builder
- 固定矩形输入，例如 `1280 x 512`
- Dynamic ONNX 可按指定 W/H 固化为 `MIN = OPT = MAX`
- 工业相机 `byte[]` / `IntPtr` 内存帧
- ONNX -> TensorRT Engine
- GPU / TensorRT / 模型配置绑定的 Engine Cache

Seg 可同时提供：

```text
classification : class id + confidence
segmentation   : instance mask
detection      : mask-derived horizontal AABB
rotated box    : mask-derived minimum-area rectangle
```

## 2. 当前模型输出约束

Detect / OBB：
- 一个 3D prediction 输出
- 标准 Ultralytics raw-output 布局

Seg：
- 一个 3D prediction
- 一个 4D prototype
- 推荐 `end2end=False, nms=False`

当前不以以下场景为目标：
- Pose
- 整图 Classification
- Semantic Segmentation
- INT8 calibration
- Batch > 1
- 多图像输入
- 任意自定义多输出 Head
- 与 LetterBox + RGB + `/255` + NCHW 不同的自定义预处理

## 3. 环境要求

开发机：

1. Windows x64
2. NVIDIA GPU + 兼容驱动
3. CUDA Toolkit 12.3
4. TensorRT 10.11.0.33
5. Visual Studio 2022
   - Desktop development with C++
   - .NET desktop development
   - Windows SDK
   - MSVC v143
6. .NET 8 SDK
7. 如构建 Net48：.NET Framework 4.8 Developer/Targeting Pack

环境变量：

```text
TENSORRT_ROOT=D:\TensorRT-10.11.0.33
CUDA_PATH=C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.3
```

本仓库保留 `release_env.bat` 供当前开发机使用，同时提供 `release_env.example.bat` 作为路径配置模板。

## 4. Engine Cache 当前规则

**WPF、.NET 8 SDK、Net48 SDK 统一使用同一策略：**

```text
ONNX 所在目录
├─ best.onnx
├─ classes.names
├─ <cache-key>.engine
└─ <cache-key>.engine.json
```

不再把新 Engine 统一写入：

```text
%LOCALAPPDATA%\YoloDeploy\EngineCache
```

Cache 身份包含：

- ONNX 内容 SHA-256
- GPU 型号
- Compute Capability
- SM 数
- TensorRT major/minor/patch/build
- FP16 / FP32
- 输入宽度 × 高度
- Workspace MiB

NVIDIA Driver / CUDA Runtime 版本记录在元数据中，但不直接进入 Cache Key。

### 路径注意事项

C# 到 Native 的模型/Engine 路径使用 Unicode 接口，当前工程可使用中文目录。

但是建议：
- 模型目录层级不要过深
- ONNX 文件名不要过长
- 客户部署使用较短稳定路径，例如 `D:\YoloModels\ProjectA\`

完整路径过长时，TensorRT / ONNX Parser / Windows 文件访问链路仍可能报错。

## 5. 构建

打开：

```text
YoloDeploy.sln
```

选择：

```text
Release | x64
```

建议先构建：

```text
YoloDeploy.Native
```

再构建整个 Solution。

## 6. WPF 使用

1. 选择 ONNX
2. 设置固定输入宽度/高度
3. 设置 FP32/FP16、Workspace、Confidence、NMS、MaskThreshold
4. 保持 Engine Cache 启用
5. 首次构建目标 GPU Engine
6. 后续相同模型/GPU/参数直接复用缓存
7. 加载 Engine 后执行 Detect / OBB / Seg

WPF 中“打开缓存目录”和“清理缓存”均针对**当前 ONNX 所在目录**：
- 清理只删除带对应 `.engine.json` 的 YoloDeploy 缓存 Engine
- 不删除 ONNX、图片、类别文件和普通手工 Engine

## 7. .NET 8 SDK

推荐使用统一入口：

```csharp
using YoloDeploy.SDK;

using var detector = new YoloDetector(
    new YoloDetectorOptions
    {
        ModelPath = @"D:\YoloModels\ProjectA\best.onnx",
        ClassNamesPath = @"D:\YoloModels\ProjectA\classes.names",
        InputWidth = 1280,
        InputHeight = 512,
        Task = YoloTask.Auto
    });

YoloDetectionResponse result =
    detector.Detect(@"D:\Images\001.jpg");
```

也可以使用强类型：
- `DetectDetector`
- `ObbDetector`
- `SegDetector`

详细说明见：

```text
SDK_INTEGRATION_CN.md
```

## 8. .NET Framework 4.8

Net48 与 .NET 8 SDK 并行存在。

客户项目：
- Target Framework = `.NET Framework 4.8`
- Platform target = `x64`
- Prefer 32-bit = `false`

引用：

```text
YoloDeploy.SDK.Net48.dll
```

命名空间仍为：

```csharp
using YoloDeploy.SDK;
```

详见：

```text
NET48_INTEGRATION_CN.md
```

## 9. 正式发布入口

### WPF Runtime

```text
publish_release.bat
```

作用：
- 构建 Native
- 发布 .NET 8 self-contained WPF
- 收集 TensorRT / CUDA Runtime
- 复制 ONNX/类别资源
- 生成 Runtime 校验脚本和 ZIP

### .NET 8 SDK Runtime

```text
publish_sdk_runtime.bat
```

作用：
- 构建 Native
- 构建当前 Detect / OBB / Seg SDK
- 发布 TestSDK
- 收集 TensorRT / CUDA Runtime
- 生成客户 Runtime ZIP
- 禁止把开发机 `.engine` 打入客户包

### .NET Framework 4.8 Runtime

```text
publish_net48_runtime.bat
```

作用：
- 构建 Native
- 构建 Net48 SDK/Test
- 收集运行时依赖
- 生成 Net48 客户 ZIP

## 10. 推荐客户交付

推荐：

```text
best.onnx
classes.names
YoloDeploy Runtime ZIP
```

不推荐把开发机生成的 `.engine` 当作跨 GPU 通用模型。

目标机首次运行：
1. 读取 ONNX
2. 在目标 GPU 上构建 Engine
3. Engine 与 `.engine.json` 写入 ONNX 同目录
4. 后续命中缓存

## 11. 工业相机

SDK 支持：
- BGR24
- RGB24
- Gray8
- BGRA32
- `byte[]`
- 非托管 `IntPtr`

`IntPtr` 缓冲区必须在同步推理调用返回前保持有效。

## 12. 常见问题

### Engine 无法加载

优先在目标 GPU 上使用 ONNX 重建，不建议跨机器硬拷 Engine。

### Native DLL 无法加载

检查完整 Runtime：
- `YoloDeploy.Native.dll`
- `nvinfer_10.dll`
- `nvinfer_plugin_10.dll`
- `nvonnxparser_10.dll`
- `cudart64_*.dll`
- 其他发布脚本收集的 CUDA Runtime

### 中文目录

当前路径接口支持 Unicode/中文目录。

若中文目录可用但某些长路径失败，请先把模型移到较短路径验证，例如：

```text
D:\YoloModels\A\
```

### 输出 shape 不支持

使用 `trtexec --dumpLayerInfo` 检查模型输出，确认符合当前 Detect/OBB 或 Seg prediction+proto 契约。

## 13. 仓库维护

当前正式脚本、历史脚本和文档的职责说明见：

```text
docs\REPOSITORY_MAINTENANCE_CN.md
```

Phase 1～5 的历史演进资料和旧迁移脚本不再作为当前使用入口。