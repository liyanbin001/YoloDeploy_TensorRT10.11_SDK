using System;

namespace YoloDeploy.SDK;

public sealed partial class SegDetector
{
    public SegResponse DetectFramePinned(
        byte[] pixels,
        int width,
        int height,
        int stride,
        CameraPixelFormat pixelFormat = CameraPixelFormat.Bgra32,
        float? confidenceThreshold = null,
        float? nmsThreshold = null,
        float? maskThreshold = null,
        string sourceName = "camera://cpu-pinned-managed")
    {
        return (SegResponse)_inner.DetectFramePinned(
            pixels,
            width,
            height,
            stride,
            pixelFormat,
            confidenceThreshold,
            nmsThreshold,
            maskThreshold,
            sourceName);
    }

    public SegResponse DetectFramePinned(
        IntPtr pixels,
        int width,
        int height,
        int stride,
        CameraPixelFormat pixelFormat = CameraPixelFormat.Bgra32,
        float? confidenceThreshold = null,
        float? nmsThreshold = null,
        float? maskThreshold = null,
        string sourceName = "camera://cpu-pinned-native")
    {
        return (SegResponse)_inner.DetectFramePinned(
            pixels,
            width,
            height,
            stride,
            pixelFormat,
            confidenceThreshold,
            nmsThreshold,
            maskThreshold,
            sourceName);
    }
}
