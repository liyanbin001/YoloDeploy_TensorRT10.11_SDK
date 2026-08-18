using System;

namespace YoloDeploy.SDK;

public sealed class SegDetector : IDisposable
{
    private readonly YoloDetector _inner;

    public SegDetector(SegDetectorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _inner = new YoloDetector(options, YoloTask.Seg);
    }

    public YoloDetectorInitializationInfo InitializationInfo =>
        _inner.InitializationInfo;

    public SegResponse Detect(
        string imagePath,
        float? confidenceThreshold = null,
        float? nmsThreshold = null,
        float? maskThreshold = null)
    {
        return (SegResponse)_inner.Detect(
            imagePath,
            confidenceThreshold,
            nmsThreshold,
            maskThreshold);
    }

    public SegResponse Detect(
        string imageDirectory,
        string imageName,
        float? confidenceThreshold = null,
        float? nmsThreshold = null,
        float? maskThreshold = null)
    {
        return (SegResponse)_inner.Detect(
            imageDirectory,
            imageName,
            confidenceThreshold,
            nmsThreshold,
            maskThreshold);
    }


    public SegResponse DetectFrame(
        byte[] pixels,
        int width,
        int height,
        int stride,
        CameraPixelFormat pixelFormat = CameraPixelFormat.Bgra32,
        float? confidenceThreshold = null,
        float? nmsThreshold = null,
        float? maskThreshold = null,
        string sourceName = "camera://managed")
    {
        return (SegResponse)_inner.DetectFrame(
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

    /// <summary>
    /// Direct unmanaged BGRA32 frame input. The camera buffer must remain valid
    /// until this synchronous call returns.
    /// </summary>
    public SegResponse DetectFrame(
        IntPtr bgra,
        int width,
        int height,
        int stride,
        float? confidenceThreshold = null,
        float? nmsThreshold = null,
        float? maskThreshold = null,
        string sourceName = "camera://native")
    {
        return (SegResponse)_inner.DetectFrame(
            bgra,
            width,
            height,
            stride,
            confidenceThreshold,
            nmsThreshold,
            maskThreshold,
            sourceName);
    }

    public void Dispose() => _inner.Dispose();
}
