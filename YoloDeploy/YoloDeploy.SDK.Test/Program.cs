using System;
using System.IO;
using System.Globalization;
using YoloDeploy.SDK;

Console.OutputEncoding = System.Text.Encoding.UTF8;

static void Usage()
{
    Console.WriteLine("YoloDeploy MultiTask SDK Test");
    Console.WriteLine();
    Console.WriteLine(
        "TestSDK.exe <auto|detect|obb|seg> <model.onnx> <classes.names> <image> <W> <H> [conf] [nms] [mask]");
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

static float ParseOptional(string[] values, int index, float fallback)
{
    if (values.Length <= index)
        return fallback;

    return float.TryParse(
        values[index],
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
            ConfidenceThreshold = conf,
            NmsThreshold = nms,
            MaskThreshold = mask,
            Task = task
        });

    Console.WriteLine($"Task: {detector.Task}");
    Console.WriteLine($"GPU : {detector.InitializationInfo.Runtime.GpuName}");

    YoloDetectionResponse result = detector.Detect(image);

    switch (result)
    {
        case DetectResponse d:
            Console.WriteLine($"Detect count: {d.Detections.Count}");
            foreach (DetectResult item in d.Detections)
                Console.WriteLine(
                    $"{item.ClassName} {item.Confidence:F3} "
                    + $"[{item.X1:F1},{item.Y1:F1},{item.X2:F1},{item.Y2:F1}]");
            break;

        case ObbDetectionResponse o:
            Console.WriteLine($"OBB count: {o.Detections.Count}");
            foreach (ObbResult item in o.Detections)
                Console.WriteLine(
                    $"{item.ClassName} {item.Confidence:F3} "
                    + $"P1=({item.P1.X:F1},{item.P1.Y:F1}) "
                    + $"P2=({item.P2.X:F1},{item.P2.Y:F1}) "
                    + $"P3=({item.P3.X:F1},{item.P3.Y:F1}) "
                    + $"P4=({item.P4.X:F1},{item.P4.Y:F1})");
            break;

        case SegResponse s:
            Console.WriteLine($"Seg count: {s.Detections.Count}");
            Console.WriteLine(
                $"Mask: {s.InstanceMask.Width}x{s.InstanceMask.Height}");

            foreach (SegResult item in s.Detections)
                Console.WriteLine(
                    $"{item.ClassName} {item.Confidence:F3} "
                    + $"mask={item.MaskId} area={item.MaskAreaPixels:F0}");
            break;
    }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}
