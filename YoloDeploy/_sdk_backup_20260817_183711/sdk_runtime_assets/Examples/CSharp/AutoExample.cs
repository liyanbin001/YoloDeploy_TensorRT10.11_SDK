using YoloDeploy.SDK;

using var detector = new YoloDetector(
    new YoloDetectorOptions
    {
        ModelPath = @"D:\Models\best.onnx",
        ClassNamesPath = @"D:\Models\classes.names",
        InputWidth = 1280,
        InputHeight = 512,
        Task = YoloTask.Auto
    });

YoloDetectionResponse result =
    detector.Detect(@"D:\Images", "001.jpg");

switch (result)
{
    case DetectResponse detect:
        Console.WriteLine($"Detect: {detect.Detections.Count}");
        break;
    case ObbResponse obb:
        Console.WriteLine($"OBB: {obb.Detections.Count}");
        break;
    case SegResponse seg:
        Console.WriteLine($"Seg: {seg.Detections.Count}");
        break;
}
