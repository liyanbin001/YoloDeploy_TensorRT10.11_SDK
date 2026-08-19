using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace YoloDeploy.SDK
{
    public sealed class YoloDetector : IDisposable
    {
        private readonly object _syncRoot;
        private readonly DetectorOptionsBase _options;
        private readonly string[] _classNames;

        private IntPtr _handle;
        private bool _disposed;

        public YoloDetector(
            YoloDetectorOptions options)
            : this(
                options,
                options == null
                    ? YoloTask.Auto
                    : options.Task)
        {
        }

        internal YoloDetector(
            DetectorOptionsBase options,
            YoloTask requestedTask)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            if ((int)requestedTask < (int)YoloTask.Auto ||
                (int)requestedTask > (int)YoloTask.Seg)
            {
                throw new ArgumentOutOfRangeException(
                    "requestedTask");
            }

            _syncRoot = new object();

            EngineProvider.ValidateCommonOptions(
                options);

            _options = options;
            _classNames =
                LoadClassNames(
                    options.ClassNamesPath);

            EngineResolveResult engine =
                EngineProvider.Resolve(options);

            StringBuilder error =
                new StringBuilder(8192);

            try
            {
                _handle =
                    NativeMethods.YoloCreate(
                        engine.EnginePath,
                        options.InputWidth,
                        options.InputHeight,
                        error,
                        error.Capacity);
            }
            catch (DllNotFoundException ex)
            {
                throw new YoloSdkException(
                    "加载 YoloDeploy.Native.dll 失败。请使用完整 Net48 Runtime 目录。",
                    ex);
            }
            catch (BadImageFormatException ex)
            {
                throw new YoloSdkException(
                    "YoloDeploy.Native.dll 位数不匹配。当前 SDK 仅支持 Windows x64。",
                    ex);
            }

            if (_handle == IntPtr.Zero)
            {
                throw new YoloSdkException(
                    "加载 TensorRT Engine 失败："
                    + error);
            }

            string modelInfo =
                ReadModelInfo();

            int taskHint =
                NativeMethods.YoloGetTaskHint(
                    _handle,
                    _classNames.Length);

            YoloTask detectedTask;

            switch (taskHint)
            {
                case 0:
                    detectedTask = YoloTask.Detect;
                    break;

                case 1:
                    detectedTask = YoloTask.Obb;
                    break;

                case 2:
                    detectedTask = YoloTask.Seg;
                    break;

                default:
                    detectedTask = YoloTask.Auto;
                    break;
            }

            if (detectedTask == YoloTask.Auto)
            {
                ReleaseNativeHandle();

                throw new YoloSdkException(
                    "无法从 Engine 输出结构识别 Detect / OBB / Seg。"
                    + "请确认模型输出结构和 classes.names 类别数量。"
                    + Environment.NewLine
                    + modelInfo);
            }

            if (requestedTask != YoloTask.Auto &&
                requestedTask != detectedTask)
            {
                ReleaseNativeHandle();

                throw new YoloSdkException(
                    "模型任务不匹配。请求="
                    + requestedTask
                    + "，检测到="
                    + detectedTask
                    + "。"
                    + Environment.NewLine
                    + modelInfo);
            }

            InitializationInfo =
                new YoloDetectorInitializationInfo
                {
                    ModelPath =
                        Path.GetFullPath(
                            options.ModelPath),

                    EnginePath =
                        engine.EnginePath,

                    RequestedTask =
                        requestedTask,

                    DetectedTask =
                        detectedTask,

                    BuiltFromOnnx =
                        engine.BuiltFromOnnx,

                    EngineCacheHit =
                        engine.CacheHit,

                    EngineBuiltNow =
                        engine.BuiltNow,

                    ModelInfo =
                        modelInfo,

                    BuildLog =
                        engine.BuildLog,

                    Runtime =
                        engine.Gpu.ToPublic()
                };
        }

        public YoloDetectorInitializationInfo InitializationInfo
        {
            get;
            private set;
        }

        public YoloTask Task
        {
            get { return InitializationInfo.DetectedTask; }
        }

        public int InputWidth
        {
            get { return _options.InputWidth; }
        }

        public int InputHeight
        {
            get { return _options.InputHeight; }
        }

        public IReadOnlyList<string> ClassNames
        {
            get { return _classNames; }
        }

        public YoloDetectionResponse Detect(
            string imagePath,
            float? confidenceThreshold = null,
            float? nmsThreshold = null,
            float? maskThreshold = null)
        {
            ThrowIfDisposed();

            float confidence =
                confidenceThreshold
                ?? _options.ConfidenceThreshold;

            float nms =
                nmsThreshold
                ?? _options.NmsThreshold;

            float mask =
                maskThreshold
                ?? _options.MaskThreshold;

            ValidateThresholds(
                confidence,
                nms,
                mask);

            string fullPath =
                Path.GetFullPath(imagePath);

            BgraImage image =
                ImageLoader.LoadBgra32(
                    fullPath);

            return DetectFramePinned(
                image.Pixels,
                image.Width,
                image.Height,
                image.Stride,
                CameraPixelFormat.Bgra32,
                confidence,
                nms,
                mask,
                fullPath);
        }

        public YoloDetectionResponse Detect(
            string imagePath)
        {
            return Detect(
                imagePath,
                confidenceThreshold: null,
                nmsThreshold: null,
                maskThreshold: null);
        }

        public YoloDetectionResponse Detect(
            string imagePath,
            float? confidenceThreshold,
            float? nmsThreshold)
        {
            return Detect(
                imagePath,
                confidenceThreshold: confidenceThreshold,
                nmsThreshold: nmsThreshold,
                maskThreshold: null);
        }

        public YoloDetectionResponse Detect(
            string imageDirectory,
            string imageName,
            float? confidenceThreshold = null,
            float? nmsThreshold = null,
            float? maskThreshold = null)
        {
            if (string.IsNullOrWhiteSpace(imageDirectory))
            {
                throw new ArgumentException(
                    "图片目录不能为空。",
                    "imageDirectory");
            }

            if (string.IsNullOrWhiteSpace(imageName))
            {
                throw new ArgumentException(
                    "图片名称不能为空。",
                    "imageName");
            }

            return Detect(
                Path.Combine(
                    imageDirectory,
                    imageName),
                confidenceThreshold,
                nmsThreshold,
                maskThreshold);
        }

        public YoloDetectionResponse Detect(
            string imageDirectory,
            string imageName)
        {
            return Detect(
                imageDirectory,
                imageName,
                null,
                null,
                null);
        }

        public YoloDetectionResponse Detect(
            string imageDirectory,
            string imageName,
            float? confidenceThreshold,
            float? nmsThreshold)
        {
            return Detect(
                imageDirectory,
                imageName,
                confidenceThreshold,
                nmsThreshold,
                null);
        }

        // Existing managed camera API: now routes directly to Native CPU-pinned path.
        public YoloDetectionResponse DetectFrame(
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
            return DetectFramePinned(
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

        public YoloDetectionResponse DetectFrame(
            byte[] pixels,
            int width,
            int height,
            int stride,
            CameraPixelFormat pixelFormat)
        {
            return DetectFrame(
                pixels,
                width,
                height,
                stride,
                pixelFormat,
                null,
                null,
                null,
                "camera://managed");
        }

        // Backward-compatible pointer overload: BGRA32.
        public YoloDetectionResponse DetectFrame(
            IntPtr bgra,
            int width,
            int height,
            int stride,
            float? confidenceThreshold = null,
            float? nmsThreshold = null,
            float? maskThreshold = null,
            string sourceName = "camera://native")
        {
            return DetectFramePinned(
                bgra,
                width,
                height,
                stride,
                CameraPixelFormat.Bgra32,
                confidenceThreshold,
                nmsThreshold,
                maskThreshold,
                sourceName);
        }

        public YoloDetectionResponse DetectFrame(
            IntPtr bgra,
            int width,
            int height,
            int stride)
        {
            return DetectFrame(
                bgra,
                width,
                height,
                stride,
                null,
                null,
                null,
                "camera://native");
        }

        public YoloDetectionResponse DetectFramePinned(
            byte[] pixels,
            int width,
            int height,
            int stride,
            CameraPixelFormat pixelFormat = CameraPixelFormat.Bgra32,
            float? confidenceThreshold = null,
            float? nmsThreshold = null,
            float? maskThreshold = null,
            string sourceName = "camera://cpu-pinned-managed")
        {
            ThrowIfDisposed();

            ValidateManagedFrame(
                pixels,
                width,
                height,
                stride,
                pixelFormat);

            float confidence =
                confidenceThreshold
                ?? _options.ConfidenceThreshold;

            float nms =
                nmsThreshold
                ?? _options.NmsThreshold;

            float mask =
                maskThreshold
                ?? _options.MaskThreshold;

            ValidateThresholds(
                confidence,
                nms,
                mask);

            string source =
                string.IsNullOrWhiteSpace(sourceName)
                    ? "camera://cpu-pinned-managed"
                    : sourceName;

            lock (_syncRoot)
            {
                ThrowIfDisposed();

                switch (Task)
                {
                    case YoloTask.Detect:
                        return DetectPinnedManaged(
                            pixels,
                            width,
                            height,
                            stride,
                            pixelFormat,
                            source,
                            confidence,
                            nms);

                    case YoloTask.Obb:
                        return DetectObbPinnedManaged(
                            pixels,
                            width,
                            height,
                            stride,
                            pixelFormat,
                            source,
                            confidence,
                            nms);

                    case YoloTask.Seg:
                        return DetectSegPinnedManaged(
                            pixels,
                            width,
                            height,
                            stride,
                            pixelFormat,
                            source,
                            confidence,
                            nms,
                            mask);

                    default:
                        throw new YoloSdkException(
                            "Detector task is unknown.");
                }
            }
        }

        public YoloDetectionResponse DetectFramePinned(
            byte[] pixels,
            int width,
            int height,
            int stride,
            CameraPixelFormat pixelFormat)
        {
            return DetectFramePinned(
                pixels,
                width,
                height,
                stride,
                pixelFormat,
                null,
                null,
                null,
                "camera://cpu-pinned-managed");
        }

        public YoloDetectionResponse DetectFramePinned(
            IntPtr pixels,
            int width,
            int height,
            int stride,
            CameraPixelFormat pixelFormat = CameraPixelFormat.Bgra32,
            float? confidenceThreshold = null,
            float? nmsThreshold = null,
            float? maskThreshold = null,
            string sourceName = "camera://cpu-pinned-native")
        {
            ThrowIfDisposed();

            ValidatePointerFrame(
                pixels,
                width,
                height,
                stride,
                pixelFormat);

            float confidence =
                confidenceThreshold
                ?? _options.ConfidenceThreshold;

            float nms =
                nmsThreshold
                ?? _options.NmsThreshold;

            float mask =
                maskThreshold
                ?? _options.MaskThreshold;

            ValidateThresholds(
                confidence,
                nms,
                mask);

            string source =
                string.IsNullOrWhiteSpace(sourceName)
                    ? "camera://cpu-pinned-native"
                    : sourceName;

            lock (_syncRoot)
            {
                ThrowIfDisposed();

                switch (Task)
                {
                    case YoloTask.Detect:
                        return DetectPinnedPointer(
                            pixels,
                            width,
                            height,
                            stride,
                            pixelFormat,
                            source,
                            confidence,
                            nms);

                    case YoloTask.Obb:
                        return DetectObbPinnedPointer(
                            pixels,
                            width,
                            height,
                            stride,
                            pixelFormat,
                            source,
                            confidence,
                            nms);

                    case YoloTask.Seg:
                        return DetectSegPinnedPointer(
                            pixels,
                            width,
                            height,
                            stride,
                            pixelFormat,
                            source,
                            confidence,
                            nms,
                            mask);

                    default:
                        throw new YoloSdkException(
                            "Detector task is unknown.");
                }
            }
        }

        public YoloDetectionResponse DetectFramePinned(
            IntPtr pixels,
            int width,
            int height,
            int stride,
            CameraPixelFormat pixelFormat)
        {
            return DetectFramePinned(
                pixels,
                width,
                height,
                stride,
                pixelFormat,
                null,
                null,
                null,
                "camera://cpu-pinned-native");
        }

        private DetectResponse DetectPinnedManaged(
            byte[] pixels,
            int width,
            int height,
            int stride,
            CameraPixelFormat pixelFormat,
            string sourceName,
            float confidence,
            float nms)
        {
            NativeMethods.YoloDetection[] native =
                new NativeMethods.YoloDetection[
                    _options.MaxResults];

            StringBuilder error =
                new StringBuilder(8192);

            float inferenceMs;

            int count =
                NativeMethods.YoloDetectImage(
                    _handle,
                    pixels,
                    width,
                    height,
                    stride,
                    (int)pixelFormat,
                    confidence,
                    nms,
                    native,
                    native.Length,
                    out inferenceMs,
                    error,
                    error.Capacity);

            return BuildDetectResponse(
                count,
                native,
                sourceName,
                width,
                height,
                inferenceMs,
                error);
        }

        private DetectResponse DetectPinnedPointer(
            IntPtr pixels,
            int width,
            int height,
            int stride,
            CameraPixelFormat pixelFormat,
            string sourceName,
            float confidence,
            float nms)
        {
            NativeMethods.YoloDetection[] native =
                new NativeMethods.YoloDetection[
                    _options.MaxResults];

            StringBuilder error =
                new StringBuilder(8192);

            float inferenceMs;

            int count =
                NativeMethods.YoloDetectImagePointer(
                    _handle,
                    pixels,
                    width,
                    height,
                    stride,
                    (int)pixelFormat,
                    confidence,
                    nms,
                    native,
                    native.Length,
                    out inferenceMs,
                    error,
                    error.Capacity);

            return BuildDetectResponse(
                count,
                native,
                sourceName,
                width,
                height,
                inferenceMs,
                error);
        }

        private DetectResponse BuildDetectResponse(
            int count,
            NativeMethods.YoloDetection[] native,
            string sourceName,
            int width,
            int height,
            float inferenceMs,
            StringBuilder error)
        {
            if (count < 0)
            {
                throw new YoloSdkException(
                    "Detect 推理失败：" + error);
            }

            List<DetectResult> detections =
                new List<DetectResult>(count);

            for (int i = 0; i < count; i++)
            {
                NativeMethods.YoloDetection d =
                    native[i];

                detections.Add(
                    new DetectResult
                    {
                        ClassId = d.ClassId,
                        ClassName =
                            ResolveClassName(
                                d.ClassId),
                        Confidence = d.Score,
                        X1 = d.X1,
                        Y1 = d.Y1,
                        X2 = d.X2,
                        Y2 = d.Y2
                    });
            }

            return new DetectResponse
            {
                Task = YoloTask.Detect,
                ImagePath = sourceName,
                ImageWidth = width,
                ImageHeight = height,
                InferenceMilliseconds = inferenceMs,
                Detections = detections
            };
        }

        private ObbDetectionResponse DetectObbPinnedManaged(
            byte[] pixels,
            int width,
            int height,
            int stride,
            CameraPixelFormat pixelFormat,
            string sourceName,
            float confidence,
            float nms)
        {
            NativeMethods.YoloObbDetection[] native =
                new NativeMethods.YoloObbDetection[
                    _options.MaxResults];

            StringBuilder error =
                new StringBuilder(8192);

            float inferenceMs;

            int count =
                NativeMethods.YoloDetectObbImage(
                    _handle,
                    pixels,
                    width,
                    height,
                    stride,
                    (int)pixelFormat,
                    confidence,
                    nms,
                    _classNames.Length,
                    native,
                    native.Length,
                    out inferenceMs,
                    error,
                    error.Capacity);

            return BuildObbResponse(
                count,
                native,
                sourceName,
                width,
                height,
                inferenceMs,
                error);
        }

        private ObbDetectionResponse DetectObbPinnedPointer(
            IntPtr pixels,
            int width,
            int height,
            int stride,
            CameraPixelFormat pixelFormat,
            string sourceName,
            float confidence,
            float nms)
        {
            NativeMethods.YoloObbDetection[] native =
                new NativeMethods.YoloObbDetection[
                    _options.MaxResults];

            StringBuilder error =
                new StringBuilder(8192);

            float inferenceMs;

            int count =
                NativeMethods.YoloDetectObbImagePointer(
                    _handle,
                    pixels,
                    width,
                    height,
                    stride,
                    (int)pixelFormat,
                    confidence,
                    nms,
                    _classNames.Length,
                    native,
                    native.Length,
                    out inferenceMs,
                    error,
                    error.Capacity);

            return BuildObbResponse(
                count,
                native,
                sourceName,
                width,
                height,
                inferenceMs,
                error);
        }

        private ObbDetectionResponse BuildObbResponse(
            int count,
            NativeMethods.YoloObbDetection[] native,
            string sourceName,
            int width,
            int height,
            float inferenceMs,
            StringBuilder error)
        {
            if (count < 0)
            {
                throw new YoloSdkException(
                    "OBB 推理失败：" + error);
            }

            List<ObbResult> detections =
                new List<ObbResult>(count);

            for (int i = 0; i < count; i++)
            {
                NativeMethods.YoloObbDetection d =
                    native[i];

                detections.Add(
                    new ObbResult
                    {
                        ClassId = d.ClassId,
                        ClassName =
                            ResolveClassName(
                                d.ClassId),
                        Confidence = d.Score,
                        CenterX = d.CenterX,
                        CenterY = d.CenterY,
                        Width = d.Width,
                        Height = d.Height,
                        AngleRadians = d.AngleRadians,
                        P1 = new ObbPoint(
                            d.P1X,
                            d.P1Y),
                        P2 = new ObbPoint(
                            d.P2X,
                            d.P2Y),
                        P3 = new ObbPoint(
                            d.P3X,
                            d.P3Y),
                        P4 = new ObbPoint(
                            d.P4X,
                            d.P4Y)
                    });
            }

            return new ObbDetectionResponse
            {
                Task = YoloTask.Obb,
                ImagePath = sourceName,
                ImageWidth = width,
                ImageHeight = height,
                InferenceMilliseconds = inferenceMs,
                Detections = detections
            };
        }

        private SegResponse DetectSegPinnedManaged(
            byte[] pixels,
            int width,
            int height,
            int stride,
            CameraPixelFormat pixelFormat,
            string sourceName,
            float confidence,
            float nms,
            float maskThreshold)
        {
            NativeMethods.YoloSegDetection[] native =
                new NativeMethods.YoloSegDetection[
                    _options.MaxResults];

            int maskStride = width;

            ushort[] instanceMask =
                new ushort[
                    checked(
                        maskStride * height)];

            StringBuilder error =
                new StringBuilder(8192);

            float inferenceMs;

            int count =
                NativeMethods.YoloDetectSegImage(
                    _handle,
                    pixels,
                    width,
                    height,
                    stride,
                    (int)pixelFormat,
                    confidence,
                    nms,
                    maskThreshold,
                    _classNames.Length,
                    native,
                    native.Length,
                    instanceMask,
                    maskStride,
                    out inferenceMs,
                    error,
                    error.Capacity);

            return BuildSegResponse(
                count,
                native,
                instanceMask,
                sourceName,
                width,
                height,
                maskStride,
                inferenceMs,
                error);
        }

        private SegResponse DetectSegPinnedPointer(
            IntPtr pixels,
            int width,
            int height,
            int stride,
            CameraPixelFormat pixelFormat,
            string sourceName,
            float confidence,
            float nms,
            float maskThreshold)
        {
            NativeMethods.YoloSegDetection[] native =
                new NativeMethods.YoloSegDetection[
                    _options.MaxResults];

            int maskStride = width;

            ushort[] instanceMask =
                new ushort[
                    checked(
                        maskStride * height)];

            StringBuilder error =
                new StringBuilder(8192);

            float inferenceMs;

            int count =
                NativeMethods.YoloDetectSegImagePointer(
                    _handle,
                    pixels,
                    width,
                    height,
                    stride,
                    (int)pixelFormat,
                    confidence,
                    nms,
                    maskThreshold,
                    _classNames.Length,
                    native,
                    native.Length,
                    instanceMask,
                    maskStride,
                    out inferenceMs,
                    error,
                    error.Capacity);

            return BuildSegResponse(
                count,
                native,
                instanceMask,
                sourceName,
                width,
                height,
                maskStride,
                inferenceMs,
                error);
        }

        private SegResponse BuildSegResponse(
            int count,
            NativeMethods.YoloSegDetection[] native,
            ushort[] instanceMask,
            string sourceName,
            int width,
            int height,
            int maskStride,
            float inferenceMs,
            StringBuilder error)
        {
            if (count < 0)
            {
                throw new YoloSdkException(
                    "Seg 推理失败：" + error);
            }

            List<SegResult> detections =
                new List<SegResult>(count);

            for (int i = 0; i < count; i++)
            {
                NativeMethods.YoloSegDetection d =
                    native[i];

                detections.Add(
                    new SegResult
                    {
                        ClassId = d.ClassId,
                        ClassName =
                            ResolveClassName(
                                d.ClassId),
                        Confidence = d.Score,
                        MaskId = d.MaskId,
                        MaskAreaPixels =
                            d.MaskAreaPixels,
                        X1 = d.X1,
                        Y1 = d.Y1,
                        X2 = d.X2,
                        Y2 = d.Y2,
                        CenterX = d.CenterX,
                        CenterY = d.CenterY,
                        RotatedWidth =
                            d.RotatedWidth,
                        RotatedHeight =
                            d.RotatedHeight,
                        AngleRadians =
                            d.AngleRadians,
                        P1 = new YoloPoint(
                            d.P1X,
                            d.P1Y),
                        P2 = new YoloPoint(
                            d.P2X,
                            d.P2Y),
                        P3 = new YoloPoint(
                            d.P3X,
                            d.P3Y),
                        P4 = new YoloPoint(
                            d.P4X,
                            d.P4Y)
                    });
            }

            return new SegResponse
            {
                Task = YoloTask.Seg,
                ImagePath = sourceName,
                ImageWidth = width,
                ImageHeight = height,
                InferenceMilliseconds = inferenceMs,
                Detections = detections,
                InstanceMask =
                    new SegInstanceMask(
                        instanceMask,
                        width,
                        height,
                        maskStride)
            };
        }

        private static int BytesPerPixel(
            CameraPixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case CameraPixelFormat.Bgra32:
                    return 4;

                case CameraPixelFormat.Bgr24:
                case CameraPixelFormat.Rgb24:
                    return 3;

                case CameraPixelFormat.Gray8:
                    return 1;

                default:
                    throw new ArgumentOutOfRangeException(
                        "pixelFormat");
            }
        }

        private static void ValidateManagedFrame(
            byte[] pixels,
            int width,
            int height,
            int stride,
            CameraPixelFormat pixelFormat)
        {
            if (pixels == null)
                throw new ArgumentNullException("pixels");

            ValidateFrameShape(
                width,
                height,
                stride,
                pixelFormat);

            long requiredBytes =
                checked(
                    (long)stride * height);

            if (pixels.LongLength < requiredBytes)
            {
                throw new ArgumentException(
                    "Camera buffer is too small. Need >= "
                    + requiredBytes
                    + " bytes, actual="
                    + pixels.LongLength
                    + ".",
                    "pixels");
            }
        }

        private static void ValidatePointerFrame(
            IntPtr pixels,
            int width,
            int height,
            int stride,
            CameraPixelFormat pixelFormat)
        {
            if (pixels == IntPtr.Zero)
            {
                throw new ArgumentException(
                    "Camera frame pointer is null.",
                    "pixels");
            }

            ValidateFrameShape(
                width,
                height,
                stride,
                pixelFormat);
        }

        private static void ValidateFrameShape(
            int width,
            int height,
            int stride,
            CameraPixelFormat pixelFormat)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException("width");

            if (height <= 0)
                throw new ArgumentOutOfRangeException("height");

            int minimumStride =
                checked(
                    width
                    * BytesPerPixel(
                        pixelFormat));

            if (stride < minimumStride)
            {
                throw new ArgumentOutOfRangeException(
                    "stride",
                    "Stride must be >= "
                    + minimumStride
                    + " for "
                    + pixelFormat
                    + ".");
            }
        }

        private static void ValidateThresholds(
            float confidence,
            float nms,
            float mask)
        {
            EngineProvider.ValidateThreshold(
                confidence,
                "confidenceThreshold");

            EngineProvider.ValidateThreshold(
                nms,
                "nmsThreshold");

            EngineProvider.ValidateThreshold(
                mask,
                "maskThreshold");
        }

        private string ReadModelInfo()
        {
            StringBuilder info =
                new StringBuilder(16384);

            int code =
                NativeMethods.YoloGetModelInfo(
                    _handle,
                    info,
                    info.Capacity);

            if (code == 0)
                return info.ToString();

            return string.Empty;
        }

        private string ResolveClassName(
            int classId)
        {
            if (classId >= 0 &&
                classId < _classNames.Length)
            {
                return _classNames[classId];
            }

            return "class_" + classId;
        }

        private static string[] LoadClassNames(
            string path)
        {
            string[] names =
                File.ReadAllLines(path)
                    .Select(
                        delegate(string x)
                        {
                            return x.Trim();
                        })
                    .Where(
                        delegate(string x)
                        {
                            return !string.IsNullOrWhiteSpace(x);
                        })
                    .ToArray();

            if (names.Length == 0)
            {
                throw new YoloSdkException(
                    "类别文件为空。每行必须包含一个类别名称。");
            }

            return names;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    "YoloDetector");
            }
        }

        private void ReleaseNativeHandle()
        {
            if (_handle != IntPtr.Zero)
            {
                NativeMethods.YoloDestroy(
                    _handle);

                _handle = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed)
                    return;

                ReleaseNativeHandle();
                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}
