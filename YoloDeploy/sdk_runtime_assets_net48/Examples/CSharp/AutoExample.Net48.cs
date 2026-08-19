using System;
using YoloDeploy.SDK;

public static class AutoExample
{
    public static void Run()
    {
        using (YoloDetector detector =
            new YoloDetector(
                new YoloDetectorOptions
                {
                    ModelPath = @"D:\Model\best.onnx",
                    ClassNamesPath = @"D:\Model\classes.names",
                    InputWidth = 1280,
                    InputHeight = 512,
                    Task = YoloTask.Auto
                }))
        {
            YoloDetectionResponse result =
                detector.Detect(
                    @"D:\Images\001.jpg");

            DetectResponse detect =
                result as DetectResponse;

            ObbDetectionResponse obb =
                result as ObbDetectionResponse;

            SegResponse seg =
                result as SegResponse;

            if (detect != null)
                Console.WriteLine("Detect: " + detect.Detections.Count);
            else if (obb != null)
                Console.WriteLine("OBB: " + obb.Detections.Count);
            else if (seg != null)
                Console.WriteLine("Seg: " + seg.Detections.Count);
        }
    }
}
