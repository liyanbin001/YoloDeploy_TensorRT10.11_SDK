using YoloDeploy.SDK;

using var detector = new DetectDetector(
    new DetectDetectorOptions
    {
        ModelPath = @"D:\Models\detect.onnx",
        ClassNamesPath = @"D:\Models\classes.names",
        InputWidth = 1280,
        InputHeight = 512
    });

DetectResponse result = detector.Detect(@"D:\Images", "001.jpg");

foreach (DetectBox box in result.Detections)
{
    Console.WriteLine(
        $"{box.ClassName}, {box.Confidence:F3}, "
        + $"({box.X1:F1},{box.Y1:F1})-({box.X2:F1},{box.Y2:F1})");
}
