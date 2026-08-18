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

    public void Dispose() => _inner.Dispose();
}
