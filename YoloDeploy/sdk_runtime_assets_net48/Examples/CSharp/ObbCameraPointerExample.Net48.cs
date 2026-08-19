using System;
using YoloDeploy.SDK;

public static class ObbCameraPointerExample
{
    // Call this from the camera SDK callback.
    public static void DetectOneFrame(
        ObbDetector detector,
        IntPtr pData,
        int width,
        int height,
        int stride)
    {
        // Example assumes BGR8 packed camera memory.
        // pData must stay valid until DetectFramePinned returns.
        ObbDetectionResponse result =
            detector.DetectFramePinned(
                pData,
                width,
                height,
                stride,
                CameraPixelFormat.Bgr24);

        foreach (ObbResult item in result.Detections)
        {
            Console.WriteLine(
                item.ClassName
                + ": center=("
                + item.CenterX.ToString("F1")
                + ","
                + item.CenterY.ToString("F1")
                + ")");
        }
    }
}
