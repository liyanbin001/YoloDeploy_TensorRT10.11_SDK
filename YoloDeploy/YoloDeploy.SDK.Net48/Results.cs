using System;
using System.Collections.Generic;

namespace YoloDeploy.SDK
{
    public struct YoloPoint
    {
        public YoloPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; private set; }
        public float Y { get; private set; }
    }

    public struct ObbPoint
    {
        public ObbPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; private set; }
        public float Y { get; private set; }
    }

    public sealed class DetectResult
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public float Confidence { get; set; }
        public float X1 { get; set; }
        public float Y1 { get; set; }
        public float X2 { get; set; }
        public float Y2 { get; set; }

        public float Width
        {
            get { return Math.Max(0.0f, X2 - X1); }
        }

        public float Height
        {
            get { return Math.Max(0.0f, Y2 - Y1); }
        }

        public float CenterX
        {
            get { return (X1 + X2) * 0.5f; }
        }

        public float CenterY
        {
            get { return (Y1 + Y2) * 0.5f; }
        }
    }

    public sealed class ObbResult
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public float Confidence { get; set; }

        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public float AngleRadians { get; set; }

        public float AngleDegrees
        {
            get
            {
                return (float)(AngleRadians * 180.0 / Math.PI);
            }
        }

        public ObbPoint P1 { get; set; }
        public ObbPoint P2 { get; set; }
        public ObbPoint P3 { get; set; }
        public ObbPoint P4 { get; set; }
    }

    public sealed class SegResult
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public float Confidence { get; set; }

        public int MaskId { get; set; }
        public float MaskAreaPixels { get; set; }

        public float X1 { get; set; }
        public float Y1 { get; set; }
        public float X2 { get; set; }
        public float Y2 { get; set; }

        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public float RotatedWidth { get; set; }
        public float RotatedHeight { get; set; }

        public float AngleRadians { get; set; }

        public float AngleDegrees
        {
            get
            {
                return (float)(AngleRadians * 180.0 / Math.PI);
            }
        }

        public YoloPoint P1 { get; set; }
        public YoloPoint P2 { get; set; }
        public YoloPoint P3 { get; set; }
        public YoloPoint P4 { get; set; }
    }

    public abstract class YoloDetectionResponse
    {
        public YoloTask Task { get; set; }
        public string ImagePath { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public float InferenceMilliseconds { get; set; }
    }

    public sealed class DetectResponse : YoloDetectionResponse
    {
        public IReadOnlyList<DetectResult> Detections { get; set; }
    }

    public sealed class ObbDetectionResponse : YoloDetectionResponse
    {
        public IReadOnlyList<ObbResult> Detections { get; set; }
    }

    public sealed class SegResponse : YoloDetectionResponse
    {
        public IReadOnlyList<SegResult> Detections { get; set; }
        public SegInstanceMask InstanceMask { get; set; }
    }

    public sealed class YoloRuntimeInfo
    {
        public string GpuName { get; set; }
        public int ComputeCapabilityMajor { get; set; }
        public int ComputeCapabilityMinor { get; set; }
        public ulong TotalGlobalMemoryBytes { get; set; }
        public int MultiProcessorCount { get; set; }
        public int CudaRuntimeVersion { get; set; }
        public int CudaDriverVersion { get; set; }
        public int TensorRtMajor { get; set; }
        public int TensorRtMinor { get; set; }
        public int TensorRtPatch { get; set; }
        public int TensorRtBuild { get; set; }

        public string ComputeCapability
        {
            get
            {
                return ComputeCapabilityMajor + "." + ComputeCapabilityMinor;
            }
        }

        public string TensorRtVersion
        {
            get
            {
                return TensorRtMajor + "."
                    + TensorRtMinor + "."
                    + TensorRtPatch + "."
                    + TensorRtBuild;
            }
        }

        public double TotalMemoryGiB
        {
            get
            {
                return TotalGlobalMemoryBytes / 1024.0 / 1024.0 / 1024.0;
            }
        }
    }

    public sealed class YoloDetectorInitializationInfo
    {
        public string ModelPath { get; set; }
        public string EnginePath { get; set; }
        public YoloTask RequestedTask { get; set; }
        public YoloTask DetectedTask { get; set; }
        public bool BuiltFromOnnx { get; set; }
        public bool EngineCacheHit { get; set; }
        public bool EngineBuiltNow { get; set; }
        public string ModelInfo { get; set; }
        public string BuildLog { get; set; }
        public YoloRuntimeInfo Runtime { get; set; }
    }
}
