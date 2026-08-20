@echo off
rem ================================================================
rem YoloDeploy build/release environment example
rem Copy/adapt these values for your development PC.
rem ================================================================

set "TENSORRT_ROOT=D:\TensorRT-10.11.0.33"
set "CUDA_PATH=C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.3"
set "CUDNN_ROOT="

echo TENSORRT_ROOT=%TENSORRT_ROOT%
echo CUDA_PATH=%CUDA_PATH%
if not "%CUDNN_ROOT%"=="" echo CUDNN_ROOT=%CUDNN_ROOT%