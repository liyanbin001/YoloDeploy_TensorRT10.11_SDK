namespace YoloDeploy.SDK
{
    public abstract class DetectorOptionsBase
    {
        protected DetectorOptionsBase()
        {
            InputWidth = 1280;
            InputHeight = 512;
            EnableFp16 = true;
            WorkspaceMiB = 1024;
            ConfidenceThreshold = 0.25f;
            NmsThreshold = 0.45f;
            MaskThreshold = 0.50f;
            MaxResults = 2048;
            ForceRebuildEngine = false;
        }

        public string ModelPath { get; set; }
        public string ClassNamesPath { get; set; }
        public int InputWidth { get; set; }
        public int InputHeight { get; set; }
        public bool EnableFp16 { get; set; }
        public int WorkspaceMiB { get; set; }
        public float ConfidenceThreshold { get; set; }
        public float NmsThreshold { get; set; }
        public float MaskThreshold { get; set; }
        public int MaxResults { get; set; }
        public bool ForceRebuildEngine { get; set; }
    }

    public sealed class YoloDetectorOptions : DetectorOptionsBase
    {
        public YoloDetectorOptions()
        {
            Task = YoloTask.Auto;
        }

        public YoloTask Task { get; set; }
    }

    public sealed class DetectDetectorOptions : DetectorOptionsBase { }
    public sealed class ObbDetectorOptions : DetectorOptionsBase { }
    public sealed class SegDetectorOptions : DetectorOptionsBase { }
}
