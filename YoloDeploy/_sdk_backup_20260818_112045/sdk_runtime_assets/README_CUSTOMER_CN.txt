YoloDeploy.SDK MultiTask
========================

支持：
- Detect
- OBB
- YOLO26 Instance Segmentation
- Task=Auto

推荐模型交付：
- best.onnx
- classes.names

首次启动：
ONNX -> 当前 GPU TensorRT Engine -> Engine Cache -> 推理

缓存：
%LOCALAPPDATA%\YoloDeploy\EngineCache

客户代码层只引用：
YoloDeploy.SDK.dll

但运行目录还必须包含：
YoloDeploy.Native.dll
TensorRT DLL
nvonnxparser DLL
CUDA 用户态 DLL

当前 SDK 使用 WPF BitmapDecoder 读取图片，因此客户项目建议：
<TargetFramework>net8.0-windows</TargetFramework>
<PlatformTarget>x64</PlatformTarget>
<UseWPF>true</UseWPF>
