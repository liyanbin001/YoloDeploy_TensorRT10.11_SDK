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

    public void Dispose() => _inner.Dispose();
}
