using YoloDeploy.SDK;

// Example: the industrial-camera SDK already gives you one BGR24 frame.
// No image file is written to disk.
byte[] cameraBuffer = GetCameraBufferSomehow();
int width = 2448;
int height = 2048;
int stride = width * 3;

using var detector = new ObbDetector(
    new ObbDetectorOptions
    {
        ModelPath = @"D:\Models\obb.onnx",
        ClassNamesPath = @"D:\Models\classes.names",
        InputWidth = 1280,
        InputHeight = 512
    });

ObbDetectionResponse result =
    detector.DetectFrame(
        cameraBuffer,
        width,
        height,
        stride,
        CameraPixelFormat.Bgr24,
        sourceName: "camera://line1");

foreach (ObbResult item in result.Detections)
{
    Console.WriteLine(
        $"{item.ClassName} {item.Confidence:F3} "
        + $"P1=({item.P1.X:F1},{item.P1.Y:F1}) "
        + $"P2=({item.P2.X:F1},{item.P2.Y:F1}) "
        + $"P3=({item.P3.X:F1},{item.P3.Y:F1}) "
        + $"P4=({item.P4.X:F1},{item.P4.Y:F1})");
}

// Replace this with the API of Hikrobot/Basler/Daheng/FLIR/etc.
static byte[] GetCameraBufferSomehow() =>
    throw new NotImplementedException();
