using System;
using YoloDeploy.SDK;

public static class ObbFileExample
{
    public static void Run()
    {
        ObbDetectorOptions options =
            new ObbDetectorOptions
            {
                ModelPath = @"D:\Model\best.onnx",
                ClassNamesPath = @"D:\Model\classes.names",
                InputWidth = 1280,
                InputHeight = 512,
                EnableFp16 = true
            };

        using (ObbDetector detector =
            new ObbDetector(options))
        {
            ObbDetectionResponse result =
                detector.Detect(
                    @"D:\Images",
                    "001.jpg");

            foreach (ObbResult item in result.Detections)
            {
                Console.WriteLine(
                    item.ClassName
                    + " "
                    + item.Confidence.ToString("F3")
                    + " P1=("
                    + item.P1.X.ToString("F1")
                    + ","
                    + item.P1.Y.ToString("F1")
                    + ")");
            }
        }
    }
}
