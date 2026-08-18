using YoloDeploy.SDK;

using var detector = new SegDetector(
    new SegDetectorOptions
    {
        ModelPath = @"D:\Models\seg.onnx",
        ClassNamesPath = @"D:\Models\classes.names",
        InputWidth = 1280,
        InputHeight = 512,
        MaskThreshold = 0.50f
    });

SegResponse result =
    detector.Detect(@"D:\Images", "001.jpg");

foreach (SegResult item in result.Detections)
{
    Console.WriteLine(
        $"{item.ClassName} {item.Confidence:F3} "
        + $"mask={item.MaskId} area={item.MaskAreaPixels:F0}");

    byte[] binaryMask =
        result.InstanceMask.CreateBinaryMask(item.MaskId);
}
