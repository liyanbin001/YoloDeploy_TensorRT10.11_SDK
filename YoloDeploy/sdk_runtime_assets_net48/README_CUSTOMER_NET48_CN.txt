YoloDeploy.SDK.Net48 Runtime
============================

适用：
.NET Framework 4.8 / Windows x64

支持：
Detect
OBB
YOLO26 Instance Segmentation
Auto Task
ONNX -> Engine
工业相机 byte[] / IntPtr 内存帧
CPU Pinned Host Tensor + cudaMemcpyAsync

客户程序代码层引用：
YoloDeploy.SDK.Net48.dll

运行目录必须整体保留：
YoloDeploy.Native.dll
TensorRT DLL
ONNX Parser DLL
CUDA user-mode DLL
TestSDK.Net48.exe

客户电脑不需要：
.NET 8 Runtime
Visual Studio
CUDA Toolkit
TensorRT SDK
Python
PyTorch
Ultralytics
trtexec

客户电脑需要：
.NET Framework 4.8
NVIDIA GPU
兼容 NVIDIA Driver
Microsoft Visual C++ 2015-2022 Redistributable x64（建议/通常需要）

推荐模型：
best.onnx
classes.names

第一次初始化会在目标机器生成当前 GPU 专用 TensorRT Engine。
缓存目录：
%LOCALAPPDATA%\YoloDeploy\EngineCache

工业相机 BGR8：
detector.DetectFrame(
    buffer,
    width,
    height,
    stride,
    CameraPixelFormat.Bgr24);

工业相机 IntPtr：
detector.DetectFramePinned(
    pData,
    width,
    height,
    stride,
    CameraPixelFormat.Bgr24);

注意：
相机 IntPtr 必须保持有效直到 DetectFramePinned 返回。
