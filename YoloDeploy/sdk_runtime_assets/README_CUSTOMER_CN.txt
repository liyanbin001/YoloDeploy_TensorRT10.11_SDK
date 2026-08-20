YoloDeploy.SDK Runtime
======================

适用：
- Windows x64
- .NET 8 Windows 客户程序

支持：
- Detect
- OBB
- YOLO26 Instance Segmentation
- Task = Auto
- ONNX -> TensorRT Engine
- 工业相机 byte[] / IntPtr 内存帧

推荐模型交付：
- best.onnx
- classes.names

Engine Cache：
- 第一次初始化：ONNX -> 当前 GPU TensorRT Engine -> 推理
- Engine 与 .engine.json 位于 ONNX 所在目录
- Cache 身份包含 ONNX SHA-256、GPU、TensorRT、FP16/FP32、输入宽高和 Workspace
- 支持中文模型目录
- 建议模型目录路径尽量短；完整路径过长时可能出现 Engine 构建、保存或加载失败
- 不建议把开发机 .engine 直接交付到不同 GPU 的客户机

客户代码层只引用：
YoloDeploy.SDK.dll

运行目录还必须整体包含：
YoloDeploy.Native.dll
TensorRT DLL
nvonnxparser DLL
CUDA 用户态 DLL

当前 SDK 使用 WPF BitmapDecoder 读取文件图片，因此文件图片接口的客户项目建议：
<TargetFramework>net8.0-windows</TargetFramework>
<PlatformTarget>x64</PlatformTarget>
<UseWPF>true</UseWPF>

工业相机直接帧接口不需要先保存 JPG/PNG。