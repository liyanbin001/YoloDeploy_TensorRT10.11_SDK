using YoloDeploy.SDK;

using var detector = new DetectDetector(
    new DetectDetectorOptions
    {
        ModelPath = @"D:\Models\detect.onnx",
        ClassNamesPath = @"D:\Models\classes.names",
        InputWidth = 1280,
        InputHeight = 512
    });

DetectResponse result =
    detector.Detect(@"D:\Images", "001.jpg");

foreach (DetectResult item in result.Detections)
{
    Console.WriteLine(
        $"{item.ClassName} {item.Confidence:F3} "
        + $"[{item.X1:F1},{item.Y1:F1},{item.X2:F1},{item.Y2:F1}]");
}
