YoloDeploy.SDK Runtime — Detect / OBB / Seg
===========================================

支持：
1. 普通目标检测 Detect
2. 旋转框检测 OBB
3. YOLO26 实例分割 Seg
4. Auto 自动任务识别

模型建议：
best.onnx
classes.names

客户不需要手工生成 Engine。

第一次运行：
ONNX -> 查询 GPU/TensorRT -> 生成目标电脑专用 Engine -> 缓存 -> 推理

后续运行：
命中 Engine Cache -> 直接推理

Engine Cache：
%LOCALAPPDATA%\YoloDeploy\EngineCache


一、客户程序依赖
----------------
Windows x64
NVIDIA GPU
兼容 NVIDIA Driver
.NET 8 Windows Desktop Runtime x64

当前 SDK 使用 WPF BitmapDecoder 读取图片，
客户 .NET 项目建议：

<TargetFramework>net8.0-windows</TargetFramework>
<PlatformTarget>x64</PlatformTarget>
<UseWPF>true</UseWPF>


二、不要只复制 YoloDeploy.SDK.dll
---------------------------------
代码层只引用：
YoloDeploy.SDK.dll

运行时必须保留 Runtime ZIP 中：
YoloDeploy.Native.dll
TensorRT DLL
ONNX Parser DLL
CUDA 用户态 DLL


三、快速测试
------------
TestSDK.exe auto Models\best.onnx Models\classes.names D:\Images\001.jpg 1280 512

指定任务：
TestSDK.exe detect ...
TestSDK.exe obb ...
TestSDK.exe seg ...


四、Detect 返回
---------------
ClassId
ClassName
Confidence
X1 Y1 X2 Y2


五、OBB 返回
------------
ClassId
ClassName
Confidence
CenterX CenterY
Width Height
AngleRadians / AngleDegrees
P1 P2 P3 P4


六、Seg 返回
------------
每个实例：
ClassId
ClassName
Confidence
MaskId
MaskAreaPixels
X1 Y1 X2 Y2
CenterX CenterY
RotatedWidth RotatedHeight
AngleRadians / AngleDegrees
P1 P2 P3 P4

另外：
SegResponse.InstanceMask

它是原图同分辨率的 UInt16 实例 ID 图：
0 = background
1..65535 = MaskId

InstanceMask.CreateBinaryMask(maskId)
可得到某个实例的 0/255 二值 mask。


七、当前 Seg 边界
-----------------
与当前 GitHub main 的 Phase 6 保持一致：
YOLO26 instance segmentation prediction + proto。

不是通用语义分割，不承诺任意自定义多输出 head。
