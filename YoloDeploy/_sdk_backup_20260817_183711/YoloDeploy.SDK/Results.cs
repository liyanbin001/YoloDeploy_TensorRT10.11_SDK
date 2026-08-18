namespace YoloDeploy.SDK;

public readonly record struct YoloPoint(float X, float Y);

public sealed record DetectBox
{
    public required int ClassId { get; init; }
    public required string ClassName { get; init; }
    public required float Confidence { get; init; }

    public required float X1 { get; init; }
    public required float Y1 { get; init; }
    public required float X2 { get; init; }
    public required float Y2 { get; init; }

    public float Width => MathF.Max(0, X2 - X1);
    public float Height => MathF.Max(0, Y2 - Y1);
    public float CenterX => (X1 + X2) * 0.5f;
    public float CenterY => (Y1 + Y2) * 0.5f;
}

public readonly record struct ObbPoint(float X, float Y);

public record ObbResult
{
    public required int ClassId { get; init; }
    public required string ClassName { get; init; }
    public required float Confidence { get; init; }

    public required float CenterX { get; init; }
    public required float CenterY { get; init; }
    public required float Width { get; init; }
    public required float Height { get; init; }

    public required float AngleRadians { get; init; }
    public float AngleDegrees => AngleRadians * 180.0f / MathF.PI;

    public required ObbPoint P1 { get; init; }
    public required ObbPoint P2 { get; init; }
    public required ObbPoint P3 { get; init; }
    public required ObbPoint P4 { get; init; }
}

/// <summary>
/// MultiTask naming alias. Inherits the original first-version OBB result contract.
/// </summary>
public sealed record ObbBox : ObbResult
{
}

public sealed record SegInstance
{
    public required int ClassId { get; init; }
    public required string ClassName { get; init; }
    public required float Confidence { get; init; }

    public required int MaskId { get; init; }
    public required float MaskAreaPixels { get; init; }

    public required float X1 { get; init; }
    public required float Y1 { get; init; }
    public required float X2 { get; init; }
    public required float Y2 { get; init; }

    public required float CenterX { get; init; }
    public required float CenterY { get; init; }
    public required float RotatedWidth { get; init; }
    public required float RotatedHeight { get; init; }

    public required float AngleRadians { get; init; }
    public float AngleDegrees => AngleRadians * 180.0f / MathF.PI;

    public required YoloPoint P1 { get; init; }
    public required YoloPoint P2 { get; init; }
    public required YoloPoint P3 { get; init; }
    public required YoloPoint P4 { get; init; }
}

public abstract record YoloDetectionResponse
{
    public required YoloTask Task { get; init; }
    public required string ImagePath { get; init; }
    public required int ImageWidth { get; init; }
    public required int ImageHeight { get; init; }
    public required float InferenceMilliseconds { get; init; }
}

public sealed record DetectResponse : YoloDetectionResponse
{
    public required IReadOnlyList<DetectBox> Detections { get; init; }
}

public record ObbResponse : YoloDetectionResponse
{
    public required IReadOnlyList<ObbBox> Detections { get; init; }
}

/// <summary>
/// Backward-compatible response name from the OBB-only SDK first version.
/// </summary>
public sealed record ObbDetectionResponse : ObbResponse
{
}

public sealed record SegResponse : YoloDetectionResponse
{
    public required IReadOnlyList<SegInstance> Detections { get; init; }
    public required SegInstanceMask InstanceMask { get; init; }
}

public sealed record YoloRuntimeInfo
{
    public required string GpuName { get; init; }
    public required int ComputeCapabilityMajor { get; init; }
    public required int ComputeCapabilityMinor { get; init; }
    public required ulong TotalGlobalMemoryBytes { get; init; }
    public required int MultiProcessorCount { get; init; }
    public required int CudaRuntimeVersion { get; init; }
    public required int CudaDriverVersion { get; init; }
    public required int TensorRtMajor { get; init; }
    public required int TensorRtMinor { get; init; }
    public required int TensorRtPatch { get; init; }
    public required int TensorRtBuild { get; init; }

    public string ComputeCapability =>
        $"{ComputeCapabilityMajor}.{ComputeCapabilityMinor}";

    public string TensorRtVersion =>
        $"{TensorRtMajor}.{TensorRtMinor}.{TensorRtPatch}.{TensorRtBuild}";

    public double TotalMemoryGiB =>
        TotalGlobalMemoryBytes / 1024.0 / 1024.0 / 1024.0;
}

public sealed record YoloDetectorInitializationInfo
{
    public required string ModelPath { get; init; }
    public required string EnginePath { get; init; }
    public required YoloTask RequestedTask { get; init; }
    public required YoloTask DetectedTask { get; init; }

    public required bool BuiltFromOnnx { get; init; }
    public required bool EngineCacheHit { get; init; }
    public required bool EngineBuiltNow { get; init; }

    public required string ModelInfo { get; init; }
    public required string BuildLog { get; init; }
    public required YoloRuntimeInfo Runtime { get; init; }
}
