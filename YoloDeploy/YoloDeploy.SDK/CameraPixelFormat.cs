namespace YoloDeploy.SDK;

/// <summary>
/// Camera frame formats accepted by the managed-memory SDK entry point.
/// Bayer formats are intentionally not decoded here; use the camera vendor SDK
/// to convert Bayer to BGR/BGRA first.
/// </summary>
public enum CameraPixelFormat
{
    Bgra32 = 0,
    Bgr24 = 1,
    Rgb24 = 2,
    Gray8 = 3
}
