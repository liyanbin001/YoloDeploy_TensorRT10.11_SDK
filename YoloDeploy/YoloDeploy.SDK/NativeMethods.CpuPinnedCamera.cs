using System;
using System.Runtime.InteropServices;
using System.Text;

namespace YoloDeploy.SDK;

internal static partial class NativeMethods
{
    [DllImport(
        DllName,
        CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Unicode)]
    internal static extern int YoloDetectImage(
        IntPtr handle,
        [In] byte[] pixels,
        int width,
        int height,
        int stride,
        int pixelFormat,
        float confidenceThreshold,
        float nmsThreshold,
        [Out] YoloDetection[] results,
        int resultCapacity,
        out float inferenceMilliseconds,
        StringBuilder errorBuffer,
        int errorCapacity);

    [DllImport(
        DllName,
        EntryPoint = "YoloDetectImage",
        CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Unicode)]
    internal static extern int YoloDetectImagePointer(
        IntPtr handle,
        IntPtr pixels,
        int width,
        int height,
        int stride,
        int pixelFormat,
        float confidenceThreshold,
        float nmsThreshold,
        [Out] YoloDetection[] results,
        int resultCapacity,
        out float inferenceMilliseconds,
        StringBuilder errorBuffer,
        int errorCapacity);

    [DllImport(
        DllName,
        CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Unicode)]
    internal static extern int YoloDetectObbImage(
        IntPtr handle,
        [In] byte[] pixels,
        int width,
        int height,
        int stride,
        int pixelFormat,
        float confidenceThreshold,
        float nmsThreshold,
        int expectedClassCount,
        [Out] YoloObbDetection[] results,
        int resultCapacity,
        out float inferenceMilliseconds,
        StringBuilder errorBuffer,
        int errorCapacity);

    [DllImport(
        DllName,
        EntryPoint = "YoloDetectObbImage",
        CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Unicode)]
    internal static extern int YoloDetectObbImagePointer(
        IntPtr handle,
        IntPtr pixels,
        int width,
        int height,
        int stride,
        int pixelFormat,
        float confidenceThreshold,
        float nmsThreshold,
        int expectedClassCount,
        [Out] YoloObbDetection[] results,
        int resultCapacity,
        out float inferenceMilliseconds,
        StringBuilder errorBuffer,
        int errorCapacity);

    [DllImport(
        DllName,
        CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Unicode)]
    internal static extern int YoloDetectSegImage(
        IntPtr handle,
        [In] byte[] pixels,
        int width,
        int height,
        int stride,
        int pixelFormat,
        float confidenceThreshold,
        float nmsThreshold,
        float maskThreshold,
        int expectedClassCount,
        [Out] YoloSegDetection[] results,
        int resultCapacity,
        [Out] ushort[] instanceMask,
        int maskStride,
        out float inferenceMilliseconds,
        StringBuilder errorBuffer,
        int errorCapacity);

    [DllImport(
        DllName,
        EntryPoint = "YoloDetectSegImage",
        CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Unicode)]
    internal static extern int YoloDetectSegImagePointer(
        IntPtr handle,
        IntPtr pixels,
        int width,
        int height,
        int stride,
        int pixelFormat,
        float confidenceThreshold,
        float nmsThreshold,
        float maskThreshold,
        int expectedClassCount,
        [Out] YoloSegDetection[] results,
        int resultCapacity,
        [Out] ushort[] instanceMask,
        int maskStride,
        out float inferenceMilliseconds,
        StringBuilder errorBuffer,
        int errorCapacity);
}
