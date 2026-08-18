using System;

namespace YoloDeploy.SDK;

public sealed partial class ObbDetector
{
    public ObbDetectionResponse DetectFramePinned(
        byte[] pixels,
        int width,
        int height,
        int stride,
        CameraPixelFormat pixelFormat = CameraPixelFormat.Bgra32,
        float? confidenceThreshold = null,
        float? nmsThreshold = null,
        string sourceName = "camera://cpu-pinned-managed")
    {
        return (ObbDetectionResponse)_inner.DetectFramePinned(
            pixels,
            width,
            height,
            stride,
            pixelFormat,
            confidenceThreshold,
            nmsThreshold,
            null,
            sourceName);
    }

    public ObbDetectionResponse DetectFramePinned(
        IntPtr pixels,
        int width,
        int height,
        int stride,
        CameraPixelFormat pixelFormat = CameraPixelFormat.Bgra32,
        float? confidenceThreshold = null,
        float? nmsThreshold = null,
        string sourceName = "camera://cpu-pinned-native")
    {
        return (ObbDetectionResponse)_inner.DetectFramePinned(
            pixels,
            width,
            height,
            stride,
            pixelFormat,
            confidenceThreshold,
            nmsThreshold,
            null,
            sourceName);
    }
}
