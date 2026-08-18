using YoloDeploy.SDK;

// Example: the camera/vendor conversion API gives an unmanaged BGRA32 pointer.
IntPtr cameraBgra = GetCameraBgraPointerSomehow();
int width = 2448;
int height = 2048;
int stride = width * 4;

using var detector = new YoloDetector(
    new YoloDetectorOptions
    {
        ModelPath = @"D:\Models\best.onnx",
        ClassNamesPath = @"D:\Models\classes.names",
        InputWidth = 1280,
        InputHeight = 512,
        Task = YoloTask.Auto
    });

// IMPORTANT:
// cameraBgra MUST remain valid until DetectFrame returns.
// Do not release/requeue the vendor frame before this call finishes.
YoloDetectionResponse result =
    detector.DetectFrame(
        cameraBgra,
        width,
        height,
        stride,
        sourceName: "camera://station-a");

Console.WriteLine(
    $"Task={result.Task}, Inference={result.InferenceMilliseconds:F2} ms");

// Replace this with the industrial-camera SDK API.
static IntPtr GetCameraBgraPointerSomehow() =>
    throw new NotImplementedException();
