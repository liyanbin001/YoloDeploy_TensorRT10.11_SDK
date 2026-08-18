using System;

namespace YoloDeploy.SDK;

public sealed class ObbDetector : IDisposable
{
    private readonly YoloDetector _inner;

    public ObbDetector(ObbDetectorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _inner = new YoloDetector(options, YoloTask.Obb);
    }

    public YoloDetectorInitializationInfo InitializationInfo =>
        _inner.InitializationInfo;

    public ObbDetectionResponse Detect(
        string imagePath,
        float? confidenceThreshold = null,
        float? nmsThreshold = null)
    {
        return (ObbDetectionResponse)_inner.Detect(
            imagePath,
            confidenceThreshold,
            nmsThreshold);
    }

    public ObbDetectionResponse Detect(
        string imageDirectory,
        string imageName,
        float? confidenceThreshold = null,
        float? nmsThreshold = null)
    {
        return (ObbDetectionResponse)_inner.Detect(
            imageDirectory,
            imageName,
            confidenceThreshold,
            nmsThreshold);
    }

    public void Dispose() => _inner.Dispose();
}
