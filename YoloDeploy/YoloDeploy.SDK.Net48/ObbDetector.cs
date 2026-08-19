using System;

namespace YoloDeploy.SDK
{
    public sealed class ObbDetector : IDisposable
    {
        private readonly YoloDetector _inner;

        public ObbDetector(ObbDetectorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            _inner = new YoloDetector(options, YoloTask.Obb);
        }

        public YoloDetectorInitializationInfo InitializationInfo
        {
            get { return _inner.InitializationInfo; }
        }

        public ObbDetectionResponse Detect(
            string imagePath,
            float? confidenceThreshold = null,
            float? nmsThreshold = null)
        {
            return (ObbDetectionResponse)_inner.Detect(
                imagePath,
                confidenceThreshold,
                nmsThreshold);
        }

        public ObbDetectionResponse Detect(
            string imageDirectory,
            string imageName,
            float? confidenceThreshold = null,
            float? nmsThreshold = null)
        {
            return (ObbDetectionResponse)_inner.Detect(
                imageDirectory,
                imageName,
                confidenceThreshold,
                nmsThreshold);
        }

        public ObbDetectionResponse DetectFrame(
            byte[] pixels,
            int width,
            int height,
            int stride,
            CameraPixelFormat pixelFormat = CameraPixelFormat.Bgra32,
            float? confidenceThreshold = null,
            float? nmsThreshold = null,
            float? maskThreshold = null,
            string sourceName = "camera://managed")
        {
            return (ObbDetectionResponse)_inner.DetectFrame(
                pixels,
                width,
                height,
                stride,
                pixelFormat,
                confidenceThreshold,
                nmsThreshold,
                maskThreshold,
                sourceName);
        }

        public ObbDetectionResponse DetectFrame(
            IntPtr bgra,
            int width,
            int height,
            int stride,
            float? confidenceThreshold = null,
            float? nmsThreshold = null,
            float? maskThreshold = null,
            string sourceName = "camera://native")
        {
            return (ObbDetectionResponse)_inner.DetectFrame(
                bgra,
                width,
                height,
                stride,
                confidenceThreshold,
                nmsThreshold,
                maskThreshold,
                sourceName);
        }

        public ObbDetectionResponse DetectFramePinned(
            byte[] pixels,
            int width,
            int height,
            int stride,
            CameraPixelFormat pixelFormat = CameraPixelFormat.Bgra32,
            float? confidenceThreshold = null,
            float? nmsThreshold = null,
            string sourceName = "camera://cpu-pinned-managed")
        {
            return (ObbDetectionResponse)_inner.DetectFramePinned(
                pixels,
                width,
                height,
                stride,
                pixelFormat,
                confidenceThreshold,
                nmsThreshold,
                null,
                sourceName);
        }

        public ObbDetectionResponse DetectFramePinned(
            IntPtr pixels,
            int width,
            int height,
            int stride,
            CameraPixelFormat pixelFormat = CameraPixelFormat.Bgra32,
            float? confidenceThreshold = null,
            float? nmsThreshold = null,
            string sourceName = "camera://cpu-pinned-native")
        {
            return (ObbDetectionResponse)_inner.DetectFramePinned(
                pixels,
                width,
                height,
                stride,
                pixelFormat,
                confidenceThreshold,
                nmsThreshold,
                null,
                sourceName);
        }

        public void Dispose()
        {
            _inner.Dispose();
        }
    }
}
