using System;
using System.Collections.Generic;
using System.Text;

namespace YoloDeploy.SDK;

public sealed partial class YoloDetector
{
    /// <summary>
    /// Fast industrial-camera path:
    /// camera pixels -> Native CPU preprocess -> persistent pinned NCHW
    /// -> cudaMemcpyAsync -> TensorRT.
    ///
    /// No JPG/PNG and no managed BGR/RGB/Gray -> BGRA intermediate buffer.
    /// </summary>
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

        ResolvePinnedThresholds(
            confidenceThreshold,
            nmsThreshold,
            maskThreshold,
            out float confidence,
            out float nms,
            out float mask);

        string source =
            string.IsNullOrWhiteSpace(sourceName)
                ? "camera://cpu-pinned-managed"
                : sourceName;

        lock (_syncRoot)
        {
            ThrowIfDisposed();

            return Task switch
            {
                YoloTask.Detect =>
                    DetectPinnedManaged(
                        pixels,
                        width,
                        height,
                        stride,
                        pixelFormat,
                        source,
                        confidence,
                        nms),

                YoloTask.Obb =>
                    DetectObbPinnedManaged(
                        pixels,
                        width,
                        height,
                        stride,
                        pixelFormat,
                        source,
                        confidence,
                        nms),

                YoloTask.Seg =>
                    DetectSegPinnedManaged(
                        pixels,
                        width,
                        height,
                        stride,
                        pixelFormat,
                        source,
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
    /// Unmanaged industrial-camera frame path.
    /// The vendor pointer must remain valid until this synchronous call returns.
    /// CPU preprocessing reads directly from the vendor frame and writes the
    /// final NCHW tensor into Native's persistent pinned host buffer.
    /// </summary>
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

        ResolvePinnedThresholds(
            confidenceThreshold,
            nmsThreshold,
            maskThreshold,
            out float confidence,
            out float nms,
            out float mask);

        string source =
            string.IsNullOrWhiteSpace(sourceName)
                ? "camera://cpu-pinned-native"
                : sourceName;

        lock (_syncRoot)
        {
            ThrowIfDisposed();

            return Task switch
            {
                YoloTask.Detect =>
                    DetectPinnedPointer(
                        pixels,
                        width,
                        height,
                        stride,
                        pixelFormat,
                        source,
                        confidence,
                        nms),

                YoloTask.Obb =>
                    DetectObbPinnedPointer(
                        pixels,
                        width,
                        height,
                        stride,
                        pixelFormat,
                        source,
                        confidence,
                        nms),

                YoloTask.Seg =>
                    DetectSegPinnedPointer(
                        pixels,
                        width,
                        height,
                        stride,
                        pixelFormat,
                        source,
                        confidence,
                        nms,
                        mask),

                _ =>
                    throw new YoloSdkException(
                        "Detector task is unknown.")
            };
        }
    }

    private void ResolvePinnedThresholds(
        float? confidenceThreshold,
        float? nmsThreshold,
        float? maskThreshold,
        out float confidence,
        out float nms,
        out float mask)
    {
        confidence =
            confidenceThreshold
            ?? _options.ConfidenceThreshold;

        nms =
            nmsThreshold
            ?? _options.NmsThreshold;

        mask =
            maskThreshold
            ?? _options.MaskThreshold;

        EngineProvider.ValidateThreshold(
            confidence,
            nameof(confidenceThreshold));

        EngineProvider.ValidateThreshold(
            nms,
            nameof(nmsThreshold));

        EngineProvider.ValidateThreshold(
            mask,
            nameof(maskThreshold));
    }

    private static int PinnedBytesPerPixel(
        CameraPixelFormat pixelFormat)
    {
        return pixelFormat switch
        {
            CameraPixelFormat.Bgra32 => 4,
            CameraPixelFormat.Bgr24 => 3,
            CameraPixelFormat.Rgb24 => 3,
            CameraPixelFormat.Gray8 => 1,
            _ => throw new ArgumentOutOfRangeException(
                nameof(pixelFormat))
        };
    }

    private static void ValidateFrameShape(
        int width,
        int height,
        int stride,
        CameraPixelFormat pixelFormat)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        int minimumStride =
            checked(
                width
                * PinnedBytesPerPixel(pixelFormat));

        if (stride < minimumStride)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stride),
                $"Stride must be >= {minimumStride} for {pixelFormat}.");
        }
    }

    private static void ValidateManagedFrame(
        byte[] pixels,
        int width,
        int height,
        int stride,
        CameraPixelFormat pixelFormat)
    {
        ArgumentNullException.ThrowIfNull(pixels);

        ValidateFrameShape(
            width,
            height,
            stride,
            pixelFormat);

        long required =
            checked((long)stride * height);

        if (pixels.LongLength < required)
        {
            throw new ArgumentException(
                $"Camera buffer is too small. "
                + $"Need >= {required} bytes, actual={pixels.LongLength}.",
                nameof(pixels));
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
            throw new ArgumentException(
                "Camera frame pointer is null.",
                nameof(pixels));

        ValidateFrameShape(
            width,
            height,
            stride,
            pixelFormat);
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
        var native =
            new NativeMethods.YoloDetection[_options.MaxResults];

        var error = new StringBuilder(8192);

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
                out float inferenceMs,
                error,
                error.Capacity);

        return BuildPinnedDetectResponse(
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
        var native =
            new NativeMethods.YoloDetection[_options.MaxResults];

        var error = new StringBuilder(8192);

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
                out float inferenceMs,
                error,
                error.Capacity);

        return BuildPinnedDetectResponse(
            count,
            native,
            sourceName,
            width,
            height,
            inferenceMs,
            error);
    }

    private DetectResponse BuildPinnedDetectResponse(
        int count,
        NativeMethods.YoloDetection[] native,
        string sourceName,
        int width,
        int height,
        float inferenceMs,
        StringBuilder error)
    {
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
        var native =
            new NativeMethods.YoloObbDetection[_options.MaxResults];

        var error = new StringBuilder(8192);

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
                out float inferenceMs,
                error,
                error.Capacity);

        return BuildPinnedObbResponse(
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
        var native =
            new NativeMethods.YoloObbDetection[_options.MaxResults];

        var error = new StringBuilder(8192);

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
                out float inferenceMs,
                error,
                error.Capacity);

        return BuildPinnedObbResponse(
            count,
            native,
            sourceName,
            width,
            height,
            inferenceMs,
            error);
    }

    private ObbDetectionResponse BuildPinnedObbResponse(
        int count,
        NativeMethods.YoloObbDetection[] native,
        string sourceName,
        int width,
        int height,
        float inferenceMs,
        StringBuilder error)
    {
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
        var native =
            new NativeMethods.YoloSegDetection[_options.MaxResults];

        int maskStride = width;

        ushort[] instanceMask =
            new ushort[checked(maskStride * height)];

        var error = new StringBuilder(8192);

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
                out float inferenceMs,
                error,
                error.Capacity);

        return BuildPinnedSegResponse(
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
        var native =
            new NativeMethods.YoloSegDetection[_options.MaxResults];

        int maskStride = width;

        ushort[] instanceMask =
            new ushort[checked(maskStride * height)];

        var error = new StringBuilder(8192);

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
                out float inferenceMs,
                error,
                error.Capacity);

        return BuildPinnedSegResponse(
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

    private SegResponse BuildPinnedSegResponse(
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
            InstanceMask =
                new SegInstanceMask(
                    instanceMask,
                    width,
                    height,
                    maskStride)
        };
    }
}
