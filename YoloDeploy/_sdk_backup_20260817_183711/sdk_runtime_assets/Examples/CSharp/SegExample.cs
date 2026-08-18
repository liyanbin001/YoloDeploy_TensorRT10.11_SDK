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

SegResponse result = detector.Detect(@"D:\Images", "001.jpg");

foreach (SegInstance instance in result.Detections)
{
    Console.WriteLine(
        $"{instance.ClassName}, mask={instance.MaskId}, "
        + $"area={instance.MaskAreaPixels:F0}");

    byte[] binary =
        result.InstanceMask.CreateBinaryMask(instance.MaskId);
}
