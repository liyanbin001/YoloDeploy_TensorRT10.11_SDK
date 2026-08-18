using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace YoloDeploy.SDK;

public sealed partial class YoloDetector : IDisposable
{
    private readonly object _syncRoot = new();
    private readonly DetectorOptionsBase _options;
    private readonly string[] _classNames;

    private IntPtr _handle;
    private bool _disposed;

    public YoloDetectorInitializationInfo InitializationInfo { get; }

    public YoloTask Task => InitializationInfo.DetectedTask;
    public int InputWidth => _options.InputWidth;
    public int InputHeight => _options.InputHeight;
    public IReadOnlyList<string> ClassNames => _classNames;

    public YoloDetector(YoloDetectorOptions options)
        : this(options, options?.Task ?? YoloTask.Auto)
    {
    }

    internal YoloDetector(
        DetectorOptionsBase options,
        YoloTask requestedTask)
    {
        ArgumentNullException.ThrowIfNull(options);

        if ((int)requestedTask < (int)YoloTask.Auto ||
            (int)requestedTask > (int)YoloTask.Seg)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedTask));
        }

        EngineProvider.ValidateCommonOptions(options);

        _options = options;
        _classNames = LoadClassNames(options.ClassNamesPath);

        EngineResolveResult engine =
            EngineProvider.Resolve(options);

        var error = new StringBuilder(8192);

        try
        {
            _handle = NativeMethods.YoloCreate(
                engine.EnginePath,
                options.InputWidth,
                options.InputHeight,
                error,
                error.Capacity);
        }
        catch (DllNotFoundException ex)
        {
            throw new YoloSdkException(
                "加载 YoloDeploy.Native.dll 失败。请使用完整 SDK Runtime 目录。",
                ex);
        }
        catch (BadImageFormatException ex)
        {
            throw new YoloSdkException(
                "YoloDeploy.Native.dll 位数不匹配。当前 SDK 仅支持 Windows x64。",
                ex);
        }

        if (_handle == IntPtr.Zero)
            throw new YoloSdkException(
                $"加载 TensorRT Engine 失败：{error}");

        string modelInfo = ReadModelInfo();

        int taskHint = NativeMethods.YoloGetTaskHint(
            _handle,
            _classNames.Length);

        YoloTask detectedTask = taskHint switch
        {
            0 => YoloTask.Detect,
            1 => YoloTask.Obb,
            2 => YoloTask.Seg,
            _ => YoloTask.Auto
        };

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
                $"模型任务不匹配。请求={requestedTask}，检测到={detectedTask}。"
                + Environment.NewLine
                + modelInfo);
        }

        InitializationInfo = new YoloDetectorInitializationInfo
        {
            ModelPath = Path.GetFullPath(options.ModelPath),
            EnginePath = engine.EnginePath,
            RequestedTask = requestedTask,
            DetectedTask = detectedTask,
            BuiltFromOnnx = engine.BuiltFromOnnx,
            EngineCacheHit = engine.CacheHit,
            EngineBuiltNow = engine.BuiltNow,
            ModelInfo = modelInfo,
            BuildLog = engine.BuildLog,
            Runtime = engine.Gpu.ToPublic()
        };
    }

    public YoloDetectionResponse Detect(
        string imagePath,
        float? confidenceThreshold = null,
        float? nmsThreshold = null,
        float? maskThreshold = null)
    {
        ThrowIfDisposed();

        float confidence =
            confidenceThreshold ?? _options.ConfidenceThreshold;

        float nms =
            nmsThreshold ?? _options.NmsThreshold;

        float mask =
            maskThreshold ?? _options.MaskThreshold;

        EngineProvider.ValidateThreshold(
            confidence,
            nameof(confidenceThreshold));

        EngineProvider.ValidateThreshold(
            nms,
            nameof(nmsThreshold));

        EngineProvider.ValidateThreshold(
            mask,
            nameof(maskThreshold));

        string fullPath = Path.GetFullPath(imagePath);
        BgraImage image = ImageLoader.LoadBgra32(fullPath);

        lock (_syncRoot)
        {
            ThrowIfDisposed();

            return Task switch
            {
                YoloTask.Detect =>
                    DetectAxisAligned(
                        image,
                        fullPath,
                        confidence,
                        nms),

                YoloTask.Obb =>
                    DetectObb(
                        image,
                        fullPath,
                        confidence,
                        nms),

                YoloTask.Seg =>
                    DetectSeg(
                        image,
                        fullPath,
                        confidence,
                        nms,
                        mask),

                _ =>
                    throw new YoloSdkException(
                        "Detector task is unknown.")
            };
        }
    }


    /// <summary>
    /// Detect an industrial-camera frame directly from managed memory.
    /// BGR/RGB/Gray/BGRA are preprocessed in Native directly into the
    /// persistent pinned TensorRT input buffer.
    /// </summary>
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

    /// <summary>
    /// Backward-compatible unmanaged BGRA32 camera API.
    /// The camera buffer must remain valid until this synchronous call returns.
    /// </summary>
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

    public YoloDetectionResponse Detect(
        string imageDirectory,
        string imageName,
        float? confidenceThreshold = null,
        float? nmsThreshold = null,
        float? maskThreshold = null)
    {
        if (string.IsNullOrWhiteSpace(imageDirectory))
            throw new ArgumentException(
                "图片目录不能为空。",
                nameof(imageDirectory));

        if (string.IsNullOrWhiteSpace(imageName))
            throw new ArgumentException(
                "图片名称不能为空。",
                nameof(imageName));

        return Detect(
            Path.Combine(imageDirectory, imageName),
            confidenceThreshold,
            nmsThreshold,
            maskThreshold);
    }

    private DetectResponse DetectAxisAligned(
        BgraImage image,
        string imagePath,
        float confidence,
        float nms)
    {
        var native =
            new NativeMethods.YoloDetection[_options.MaxResults];

        var error = new StringBuilder(8192);

        int count = NativeMethods.YoloDetectBgra(
            _handle,
            image.Pixels,
            image.Width,
            image.Height,
            image.Stride,
            confidence,
            nms,
            native,
            native.Length,
            out float inferenceMs,
            error,
            error.Capacity);

        if (count < 0)
            throw new YoloSdkException(
                $"Detect 推理失败：{error}");

        var detections =
            new List<DetectResult>(count);

        for (int i = 0; i < count; i++)
        {
            NativeMethods.YoloDetection d = native[i];

            detections.Add(new DetectResult
            {
                ClassId = d.ClassId,
                ClassName = ResolveClassName(d.ClassId),
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
            ImagePath = imagePath,
            ImageWidth = image.Width,
            ImageHeight = image.Height,
            InferenceMilliseconds = inferenceMs,
            Detections = detections
        };
    }

    private ObbDetectionResponse DetectObb(
        BgraImage image,
        string imagePath,
        float confidence,
        float nms)
    {
        var native =
            new NativeMethods.YoloObbDetection[_options.MaxResults];

        var error = new StringBuilder(8192);

        int count = NativeMethods.YoloDetectObbBgra(
            _handle,
            image.Pixels,
            image.Width,
            image.Height,
            image.Stride,
            confidence,
            nms,
            _classNames.Length,
            native,
            native.Length,
            out float inferenceMs,
            error,
            error.Capacity);

        if (count < 0)
            throw new YoloSdkException(
                $"OBB 推理失败：{error}");

        var detections =
            new List<ObbResult>(count);

        for (int i = 0; i < count; i++)
        {
            NativeMethods.YoloObbDetection d = native[i];

            detections.Add(new ObbResult
            {
                ClassId = d.ClassId,
                ClassName = ResolveClassName(d.ClassId),
                Confidence = d.Score,
                CenterX = d.CenterX,
                CenterY = d.CenterY,
                Width = d.Width,
                Height = d.Height,
                AngleRadians = d.AngleRadians,
                P1 = new ObbPoint(d.P1X, d.P1Y),
                P2 = new ObbPoint(d.P2X, d.P2Y),
                P3 = new ObbPoint(d.P3X, d.P3Y),
                P4 = new ObbPoint(d.P4X, d.P4Y)
            });
        }

        return new ObbDetectionResponse
        {
            Task = YoloTask.Obb,
            ImagePath = imagePath,
            ImageWidth = image.Width,
            ImageHeight = image.Height,
            InferenceMilliseconds = inferenceMs,
            Detections = detections
        };
    }

    private SegResponse DetectSeg(
        BgraImage image,
        string imagePath,
        float confidence,
        float nms,
        float maskThreshold)
    {
        var native =
            new NativeMethods.YoloSegDetection[_options.MaxResults];

        int maskStride = image.Width;

        ushort[] instanceMask =
            new ushort[checked(maskStride * image.Height)];

        var error = new StringBuilder(8192);

        int count = NativeMethods.YoloDetectSegBgra(
            _handle,
            image.Pixels,
            image.Width,
            image.Height,
            image.Stride,
            confidence,
            nms,
            maskThreshold,
            _classNames.Length,
            native,
            native.Length,
            instanceMask,
            maskStride,
            out float inferenceMs,
            error,
            error.Capacity);

        if (count < 0)
            throw new YoloSdkException(
                $"Seg 推理失败：{error}");

        var detections =
            new List<SegResult>(count);

        for (int i = 0; i < count; i++)
        {
            NativeMethods.YoloSegDetection d = native[i];

            detections.Add(new SegResult
            {
                ClassId = d.ClassId,
                ClassName = ResolveClassName(d.ClassId),
                Confidence = d.Score,
                MaskId = d.MaskId,
                MaskAreaPixels = d.MaskAreaPixels,
                X1 = d.X1,
                Y1 = d.Y1,
                X2 = d.X2,
                Y2 = d.Y2,
                CenterX = d.CenterX,
                CenterY = d.CenterY,
                RotatedWidth = d.RotatedWidth,
                RotatedHeight = d.RotatedHeight,
                AngleRadians = d.AngleRadians,
                P1 = new YoloPoint(d.P1X, d.P1Y),
                P2 = new YoloPoint(d.P2X, d.P2Y),
                P3 = new YoloPoint(d.P3X, d.P3Y),
                P4 = new YoloPoint(d.P4X, d.P4Y)
            });
        }

        return new SegResponse
        {
            Task = YoloTask.Seg,
            ImagePath = imagePath,
            ImageWidth = image.Width,
            ImageHeight = image.Height,
            InferenceMilliseconds = inferenceMs,
            Detections = detections,
            InstanceMask = new SegInstanceMask(
                instanceMask,
                image.Width,
                image.Height,
                maskStride)
        };
    }


    private DetectResponse DetectAxisAlignedPointer(
        IntPtr bgra,
        int width,
        int height,
        int stride,
        string sourceName,
        float confidence,
        float nms)
    {
        var native =
            new NativeMethods.YoloDetection[_options.MaxResults];

        var error = new StringBuilder(8192);

        int count =
            NativeMethods.YoloDetectBgraPointer(
                _handle,
                bgra,
                width,
                height,
                stride,
                confidence,
                nms,
                native,
                native.Length,
                out float inferenceMs,
                error,
                error.Capacity);

        if (count < 0)
            throw new YoloSdkException(
                $"Detect 推理失败：{error}");

        var detections =
            new List<DetectResult>(count);

        for (int i = 0; i < count; i++)
        {
            NativeMethods.YoloDetection d =
                native[i];

            detections.Add(new DetectResult
            {
                ClassId = d.ClassId,
                ClassName = ResolveClassName(d.ClassId),
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

    private ObbDetectionResponse DetectObbPointer(
        IntPtr bgra,
        int width,
        int height,
        int stride,
        string sourceName,
        float confidence,
        float nms)
    {
        var native =
            new NativeMethods.YoloObbDetection[_options.MaxResults];

        var error = new StringBuilder(8192);

        int count =
            NativeMethods.YoloDetectObbBgraPointer(
                _handle,
                bgra,
                width,
                height,
                stride,
                confidence,
                nms,
                _classNames.Length,
                native,
                native.Length,
                out float inferenceMs,
                error,
                error.Capacity);

        if (count < 0)
            throw new YoloSdkException(
                $"OBB 推理失败：{error}");

        var detections =
            new List<ObbResult>(count);

        for (int i = 0; i < count; i++)
        {
            NativeMethods.YoloObbDetection d =
                native[i];

            detections.Add(new ObbResult
            {
                ClassId = d.ClassId,
                ClassName = ResolveClassName(d.ClassId),
                Confidence = d.Score,
                CenterX = d.CenterX,
                CenterY = d.CenterY,
                Width = d.Width,
                Height = d.Height,
                AngleRadians = d.AngleRadians,
                P1 = new ObbPoint(d.P1X, d.P1Y),
                P2 = new ObbPoint(d.P2X, d.P2Y),
                P3 = new ObbPoint(d.P3X, d.P3Y),
                P4 = new ObbPoint(d.P4X, d.P4Y)
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

    private SegResponse DetectSegPointer(
        IntPtr bgra,
        int width,
        int height,
        int stride,
        string sourceName,
        float confidence,
        float nms,
        float maskThreshold)
    {
        var native =
            new NativeMethods.YoloSegDetection[_options.MaxResults];

        int maskStride = width;

        ushort[] instanceMask =
            new ushort[checked(maskStride * height)];

        var error = new StringBuilder(8192);

        int count =
            NativeMethods.YoloDetectSegBgraPointer(
                _handle,
                bgra,
                width,
                height,
                stride,
                confidence,
                nms,
                maskThreshold,
                _classNames.Length,
                native,
                native.Length,
                instanceMask,
                maskStride,
                out float inferenceMs,
                error,
                error.Capacity);

        if (count < 0)
            throw new YoloSdkException(
                $"Seg 推理失败：{error}");

        var detections =
            new List<SegResult>(count);

        for (int i = 0; i < count; i++)
        {
            NativeMethods.YoloSegDetection d =
                native[i];

            detections.Add(new SegResult
            {
                ClassId = d.ClassId,
                ClassName = ResolveClassName(d.ClassId),
                Confidence = d.Score,
                MaskId = d.MaskId,
                MaskAreaPixels = d.MaskAreaPixels,
                X1 = d.X1,
                Y1 = d.Y1,
                X2 = d.X2,
                Y2 = d.Y2,
                CenterX = d.CenterX,
                CenterY = d.CenterY,
                RotatedWidth = d.RotatedWidth,
                RotatedHeight = d.RotatedHeight,
                AngleRadians = d.AngleRadians,
                P1 = new YoloPoint(d.P1X, d.P1Y),
                P2 = new YoloPoint(d.P2X, d.P2Y),
                P3 = new YoloPoint(d.P3X, d.P3Y),
                P4 = new YoloPoint(d.P4X, d.P4Y)
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
            InstanceMask = new SegInstanceMask(
                instanceMask,
                width,
                height,
                maskStride)
        };
    }

    private string ReadModelInfo()
    {
        var info = new StringBuilder(16384);

        int code = NativeMethods.YoloGetModelInfo(
            _handle,
            info,
            info.Capacity);

        return code == 0
            ? info.ToString()
            : "";
    }

    private string ResolveClassName(int classId)
    {
        return classId >= 0 &&
               classId < _classNames.Length
            ? _classNames[classId]
            : $"class_{classId}";
    }

    private static string[] LoadClassNames(string path)
    {
        string[] names =
            File.ReadAllLines(path)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

        if (names.Length == 0)
            throw new YoloSdkException(
                "类别文件为空。每行必须包含一个类别名称。");

        return names;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(
                nameof(YoloDetector));
    }

    private void ReleaseNativeHandle()
    {
        if (_handle != IntPtr.Zero)
        {
            NativeMethods.YoloDestroy(_handle);
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


