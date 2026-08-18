using YoloDeploy.SDK;

using var detector = new ObbDetector(
    new ObbDetectorOptions
    {
        ModelPath = @"D:\Models\obb.onnx",
        ClassNamesPath = @"D:\Models\classes.names",
        InputWidth = 1280,
        InputHeight = 512
    });

ObbDetectionResponse result = detector.Detect(@"D:\Images", "001.jpg");

foreach (ObbBox box in result.Detections)
{
    Console.WriteLine(
        $"{box.ClassName}, {box.Confidence:F3}, "
        + $"P1=({box.P1.X:F1},{box.P1.Y:F1}), "
        + $"P2=({box.P2.X:F1},{box.P2.Y:F1}), "
        + $"P3=({box.P3.X:F1},{box.P3.Y:F1}), "
        + $"P4=({box.P4.X:F1},{box.P4.Y:F1})");
}
