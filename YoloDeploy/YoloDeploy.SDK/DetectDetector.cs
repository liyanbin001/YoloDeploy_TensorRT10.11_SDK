using System;

namespace YoloDeploy.SDK;

public sealed class DetectDetector : IDisposable
{
    private readonly YoloDetector _inner;

    public DetectDetector(DetectDetectorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _inner = new YoloDetector(options, YoloTask.Detect);
    }

    public YoloDetectorInitializationInfo InitializationInfo =>
        _inner.InitializationInfo;

    public DetectResponse Detect(
        string imagePath,
        float? confidenceThreshold = null,
        float? nmsThreshold = null)
    {
        return (DetectResponse)_inner.Detect(
            imagePath,
            confidenceThreshold,
            nmsThreshold);
    }

    public DetectResponse Detect(
        string imageDirectory,
        string imageName,
        float? confidenceThreshold = null,
        float? nmsThreshold = null)
    {
        return (DetectResponse)_inner.Detect(
            imageDirectory,
            imageName,
            confidenceThreshold,
            nmsThreshold);
    }


    public DetectResponse DetectFrame(
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
        return (DetectResponse)_inner.DetectFrame(
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
    public DetectResponse DetectFrame(
        IntPtr bgra,
        int width,
        int height,
        int stride,
        float? confidenceThreshold = null,
        float? nmsThreshold = null,
        float? maskThreshold = null,
        string sourceName = "camera://native")
    {
        return (DetectResponse)_inner.DetectFrame(
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
