using System;

namespace YoloDeploy.SDK;

public abstract class DetectorOptionsBase
{
    public required string ModelPath { get; init; }
    public required string ClassNamesPath { get; init; }

    public int InputWidth { get; init; } = 1280;
    public int InputHeight { get; init; } = 512;

    public bool EnableFp16 { get; init; } = true;
    public int WorkspaceMiB { get; init; } = 1024;

    public float ConfidenceThreshold { get; init; } = 0.25f;
    public float NmsThreshold { get; init; } = 0.45f;
    public float MaskThreshold { get; init; } = 0.50f;

    public int MaxResults { get; init; } = 2048;
    public bool ForceRebuildEngine { get; init; } = false;
}

public sealed class YoloDetectorOptions : DetectorOptionsBase
{
    public YoloTask Task { get; init; } = YoloTask.Auto;
}

public sealed class DetectDetectorOptions : DetectorOptionsBase { }
public sealed class ObbDetectorOptions : DetectorOptionsBase { }
public sealed class SegDetectorOptions : DetectorOptionsBase { }
