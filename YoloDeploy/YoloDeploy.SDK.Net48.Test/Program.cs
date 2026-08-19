using System;
using System.Globalization;
using YoloDeploy.SDK;

namespace YoloDeploy.SDK.Net48.Test
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding =
                System.Text.Encoding.UTF8;

            if (args.Length < 6)
            {
                PrintUsage();
                return 2;
            }

            YoloTask task;

            switch (
                args[0]
                    .Trim()
                    .ToLowerInvariant())
            {
                case "auto":
                    task = YoloTask.Auto;
                    break;

                case "detect":
                    task = YoloTask.Detect;
                    break;

                case "obb":
                    task = YoloTask.Obb;
                    break;

                case "seg":
                    task = YoloTask.Seg;
                    break;

                default:
                    Console.Error.WriteLine(
                        "[ERROR] task must be auto/detect/obb/seg.");

                    return 2;
            }

            string model =
                System.IO.Path.GetFullPath(
                    args[1]);

            string names =
                System.IO.Path.GetFullPath(
                    args[2]);

            string image =
                System.IO.Path.GetFullPath(
                    args[3]);

            int width;
            int height;

            if (!int.TryParse(
                    args[4],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out width) ||
                !int.TryParse(
                    args[5],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out height))
            {
                Console.Error.WriteLine(
                    "[ERROR] W/H invalid.");

                return 2;
            }

            float confidence =
                ParseOptional(
                    args,
                    6,
                    0.25f);

            float nms =
                ParseOptional(
                    args,
                    7,
                    0.45f);

            float mask =
                ParseOptional(
                    args,
                    8,
                    0.50f);

            try
            {
                using (YoloDetector detector =
                    new YoloDetector(
                        new YoloDetectorOptions
                        {
                            ModelPath = model,
                            ClassNamesPath = names,
                            InputWidth = width,
                            InputHeight = height,
                            EnableFp16 = true,
                            WorkspaceMiB = 1024,
                            ConfidenceThreshold = confidence,
                            NmsThreshold = nms,
                            MaskThreshold = mask,
                            Task = task
                        }))
                {
                    YoloDetectorInitializationInfo init =
                        detector.InitializationInfo;

                    Console.WriteLine(
                        "=== .NET Framework 4.8 Initialization ===");

                    Console.WriteLine(
                        "Requested : " + init.RequestedTask);

                    Console.WriteLine(
                        "Detected  : " + init.DetectedTask);

                    Console.WriteLine(
                        "GPU       : " + init.Runtime.GpuName);

                    Console.WriteLine(
                        "TensorRT  : " + init.Runtime.TensorRtVersion);

                    Console.WriteLine(
                        "Engine    : " + init.EnginePath);

                    Console.WriteLine(
                        "Cache hit : " + init.EngineCacheHit);

                    Console.WriteLine(
                        "Built now : " + init.EngineBuiltNow);

                    Console.WriteLine();

                    YoloDetectionResponse result =
                        detector.Detect(
                            image,
                            confidence,
                            nms,
                            mask);

                    PrintResult(result);

                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("[ERROR]");
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static float ParseOptional(
            string[] args,
            int index,
            float fallback)
        {
            if (args.Length <= index)
                return fallback;

            float value;

            if (float.TryParse(
                    args[index],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value))
            {
                return value;
            }

            return fallback;
        }

        private static void PrintResult(
            YoloDetectionResponse result)
        {
            Console.WriteLine("=== Result ===");

            Console.WriteLine(
                "Task      : " + result.Task);

            Console.WriteLine(
                "Image     : "
                + result.ImageWidth
                + "x"
                + result.ImageHeight);

            Console.WriteLine(
                "Inference : "
                + result.InferenceMilliseconds.ToString("F2")
                + " ms");

            DetectResponse detect =
                result as DetectResponse;

            if (detect != null)
            {
                Console.WriteLine(
                    "Count     : "
                    + detect.Detections.Count);

                foreach (DetectResult item in detect.Detections)
                {
                    Console.WriteLine(
                        item.ClassName
                        + " "
                        + item.Confidence.ToString("F4")
                        + " ["
                        + item.X1.ToString("F1")
                        + ","
                        + item.Y1.ToString("F1")
                        + ","
                        + item.X2.ToString("F1")
                        + ","
                        + item.Y2.ToString("F1")
                        + "]");
                }

                return;
            }

            ObbDetectionResponse obb =
                result as ObbDetectionResponse;

            if (obb != null)
            {
                Console.WriteLine(
                    "Count     : "
                    + obb.Detections.Count);

                foreach (ObbResult item in obb.Detections)
                {
                    Console.WriteLine(
                        item.ClassName
                        + " "
                        + item.Confidence.ToString("F4")
                        + " angle="
                        + item.AngleDegrees.ToString("F2"));
                }

                return;
            }

            SegResponse seg =
                result as SegResponse;

            if (seg != null)
            {
                Console.WriteLine(
                    "Count     : "
                    + seg.Detections.Count);

                Console.WriteLine(
                    "Mask      : "
                    + seg.InstanceMask.Width
                    + "x"
                    + seg.InstanceMask.Height);

                foreach (SegResult item in seg.Detections)
                {
                    Console.WriteLine(
                        item.ClassName
                        + " "
                        + item.Confidence.ToString("F4")
                        + " maskId="
                        + item.MaskId
                        + " area="
                        + item.MaskAreaPixels.ToString("F0"));
                }
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine(
                "YoloDeploy .NET Framework 4.8 SDK Test");

            Console.WriteLine();

            Console.WriteLine(
                "TestSDK.Net48.exe <auto|detect|obb|seg> <model.onnx> <classes.names> <image> <W> <H> [conf] [nms] [mask]");
        }
    }
}
