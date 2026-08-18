using YoloDeploy.SDK;

using var detector = new ObbDetector(
    new ObbDetectorOptions
    {
        ModelPath = @"D:\Models\obb.onnx",
        ClassNamesPath = @"D:\Models\classes.names",
        InputWidth = 1280,
        InputHeight = 512
    });

ObbDetectionResponse result =
    detector.Detect(@"D:\Images", "001.jpg");

foreach (ObbResult item in result.Detections)
{
    Console.WriteLine(
        $"{item.ClassName} {item.Confidence:F3} "
        + $"P1=({item.P1.X:F1},{item.P1.Y:F1}) "
        + $"P2=({item.P2.X:F1},{item.P2.Y:F1}) "
        + $"P3=({item.P3.X:F1},{item.P3.Y:F1}) "
        + $"P4=({item.P4.X:F1},{item.P4.Y:F1})");
}
