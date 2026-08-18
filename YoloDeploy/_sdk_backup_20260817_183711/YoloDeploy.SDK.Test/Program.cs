using System.Globalization;
using YoloDeploy.SDK;

Console.OutputEncoding = System.Text.Encoding.UTF8;

static void Usage()
{
    Console.WriteLine("YoloDeploy MultiTask SDK Test");
    Console.WriteLine();
    Console.WriteLine(
        "TestSDK.exe <auto|detect|obb|seg> <model.onnx> <classes.names> <image> <W> <H> [conf] [nms] [mask]");
    Console.WriteLine();
    Console.WriteLine(
        @"TestSDK.exe auto Models\best.onnx Models\classes.names D:\Images\001.jpg 1280 512");
    Console.WriteLine(
        @"TestSDK.exe seg Models\best.onnx Models\classes.names D:\Images\001.jpg 1280 512 0.25 0.45 0.50");
}

if (args.Length < 6)
{
    Usage();
    return 2;
}

YoloTask task = args[0].Trim().ToLowerInvariant() switch
{
    "auto" => YoloTask.Auto,
    "detect" => YoloTask.Detect,
    "obb" => YoloTask.Obb,
    "seg" => YoloTask.Seg,
    _ => (YoloTask)(-999)
};

if ((int)task == -999)
{
    Console.Error.WriteLine("[ERROR] task must be auto/detect/obb/seg.");
    return 2;
}

string model = Path.GetFullPath(args[1]);
string names = Path.GetFullPath(args[2]);
string image = Path.GetFullPath(args[3]);

if (!int.TryParse(args[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width) ||
    !int.TryParse(args[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int height))
{
    Console.Error.WriteLine("[ERROR] W/H invalid.");
    return 2;
}

static float ParseOptional(string[] args, int index, float fallback)
{
    if (args.Length <= index)
        return fallback;

    return float.TryParse(
        args[index],
        NumberStyles.Float,
        CultureInfo.InvariantCulture,
        out float value)
            ? value
            : fallback;
}

float conf = ParseOptional(args, 6, 0.25f);
float nms = ParseOptional(args, 7, 0.45f);
float mask = ParseOptional(args, 8, 0.50f);

try
{
    using var detector = new YoloDetector(
        new YoloDetectorOptions
        {
            ModelPath = model,
            ClassNamesPath = names,
            InputWidth = width,
            InputHeight = height,
            EnableFp16 = true,
            WorkspaceMiB = 1024,
            ConfidenceThreshold = conf,
            NmsThreshold = nms,
            MaskThreshold = mask,
            Task = task
        });

    var init = detector.InitializationInfo;

    Console.WriteLine("=== Initialization ===");
    Console.WriteLine($"Requested : {init.RequestedTask}");
    Console.WriteLine($"Detected  : {init.DetectedTask}");
    Console.WriteLine($"GPU       : {init.Runtime.GpuName}");
    Console.WriteLine($"TensorRT  : {init.Runtime.TensorRtVersion}");
    Console.WriteLine($"Engine    : {init.EnginePath}");
    Console.WriteLine($"Cache hit : {init.EngineCacheHit}");
    Console.WriteLine($"Built now : {init.EngineBuiltNow}");
    Console.WriteLine();

    YoloDetectionResponse result = detector.Detect(image);

    Console.WriteLine("=== Result ===");
    Console.WriteLine($"Task      : {result.Task}");
    Console.WriteLine($"Image     : {result.ImageWidth}x{result.ImageHeight}");
    Console.WriteLine($"Inference : {result.InferenceMilliseconds:F2} ms");
    Console.WriteLine();

    switch (result)
    {
        case DetectResponse detect:
            Console.WriteLine($"Count: {detect.Detections.Count}");
            foreach (var d in detect.Detections)
            {
                Console.WriteLine(
                    $"{d.ClassName} {d.Confidence:F4} "
                    + $"[{d.X1:F1},{d.Y1:F1},{d.X2:F1},{d.Y2:F1}]");
            }
            break;

        case ObbResponse obb:
            Console.WriteLine($"Count: {obb.Detections.Count}");
            foreach (var d in obb.Detections)
            {
                Console.WriteLine(
                    $"{d.ClassName} {d.Confidence:F4} angle={d.AngleDegrees:F2}");
                Console.WriteLine(
                    $"  P1=({d.P1.X:F1},{d.P1.Y:F1}) "
                    + $"P2=({d.P2.X:F1},{d.P2.Y:F1}) "
                    + $"P3=({d.P3.X:F1},{d.P3.Y:F1}) "
                    + $"P4=({d.P4.X:F1},{d.P4.Y:F1})");
            }
            break;

        case SegResponse seg:
            Console.WriteLine($"Count: {seg.Detections.Count}");
            Console.WriteLine(
                $"Mask: {seg.InstanceMask.Width}x{seg.InstanceMask.Height}, stride={seg.InstanceMask.Stride}");

            foreach (var d in seg.Detections)
            {
                Console.WriteLine(
                    $"{d.ClassName} {d.Confidence:F4} "
                    + $"maskId={d.MaskId} area={d.MaskAreaPixels:F0}");
                Console.WriteLine(
                    $"  AABB=[{d.X1:F1},{d.Y1:F1},{d.X2:F1},{d.Y2:F1}] "
                    + $"Rot={d.RotatedWidth:F1}x{d.RotatedHeight:F1} "
                    + $"angle={d.AngleDegrees:F2}");
            }
            break;
    }

    Console.WriteLine();
    Console.WriteLine("[OK] completed.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("[ERROR]");
    Console.Error.WriteLine(ex);
    return 1;
}
