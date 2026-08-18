using System;
using System.Buffers;

namespace YoloDeploy.SDK;

internal sealed class PreparedBgraFrame : IDisposable
{
    private readonly bool _returnToPool;

    internal PreparedBgraFrame(
        byte[] buffer,
        int width,
        int height,
        int stride,
        bool returnToPool)
    {
        Buffer = buffer;
        Width = width;
        Height = height;
        Stride = stride;
        _returnToPool = returnToPool;
    }

    internal byte[] Buffer { get; }
    internal int Width { get; }
    internal int Height { get; }
    internal int Stride { get; }

    public void Dispose()
    {
        if (_returnToPool)
            ArrayPool<byte>.Shared.Return(Buffer);
    }
}

internal static class CameraFrameConverter
{
    internal static PreparedBgraFrame Prepare(
        byte[] pixels,
        int width,
        int height,
        int stride,
        CameraPixelFormat pixelFormat)
    {
        ArgumentNullException.ThrowIfNull(pixels);

        Validate(
            pixels.Length,
            width,
            height,
            stride,
            pixelFormat);

        if (pixelFormat == CameraPixelFormat.Bgra32)
        {
            return new PreparedBgraFrame(
                pixels,
                width,
                height,
                stride,
                returnToPool: false);
        }

        int dstStride = checked(width * 4);
        int dstLength = checked(dstStride * height);

        byte[] dst =
            ArrayPool<byte>.Shared.Rent(dstLength);

        try
        {
            switch (pixelFormat)
            {
                case CameraPixelFormat.Bgr24:
                    ConvertBgr24(
                        pixels,
                        width,
                        height,
                        stride,
                        dst,
                        dstStride);
                    break;

                case CameraPixelFormat.Rgb24:
                    ConvertRgb24(
                        pixels,
                        width,
                        height,
                        stride,
                        dst,
                        dstStride);
                    break;

                case CameraPixelFormat.Gray8:
                    ConvertGray8(
                        pixels,
                        width,
                        height,
                        stride,
                        dst,
                        dstStride);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(pixelFormat));
            }

            return new PreparedBgraFrame(
                dst,
                width,
                height,
                dstStride,
                returnToPool: true);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(dst);
            throw;
        }
    }

    internal static void ValidatePointerFrame(
        IntPtr data,
        int width,
        int height,
        int stride)
    {
        if (data == IntPtr.Zero)
            throw new ArgumentException(
                "Camera frame pointer is null.",
                nameof(data));

        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        int minStride = checked(width * 4);

        if (stride < minStride)
            throw new ArgumentOutOfRangeException(
                nameof(stride),
                $"BGRA32 stride must be >= {minStride}.");
    }

    private static void Validate(
        int bufferLength,
        int width,
        int height,
        int stride,
        CameraPixelFormat format)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        int bytesPerPixel = format switch
        {
            CameraPixelFormat.Bgra32 => 4,
            CameraPixelFormat.Bgr24 => 3,
            CameraPixelFormat.Rgb24 => 3,
            CameraPixelFormat.Gray8 => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        int minStride =
            checked(width * bytesPerPixel);

        if (stride < minStride)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stride),
                $"Stride must be >= {minStride} for {format}.");
        }

        int required =
            checked(stride * height);

        if (bufferLength < required)
        {
            throw new ArgumentException(
                $"Camera buffer is too small. Need >= {required} bytes, actual={bufferLength}.",
                nameof(bufferLength));
        }
    }

    private static void ConvertBgr24(
        byte[] src,
        int width,
        int height,
        int srcStride,
        byte[] dst,
        int dstStride)
    {
        for (int y = 0; y < height; y++)
        {
            int s = y * srcStride;
            int d = y * dstStride;

            for (int x = 0; x < width; x++)
            {
                int si = s + x * 3;
                int di = d + x * 4;

                dst[di] = src[si];
                dst[di + 1] = src[si + 1];
                dst[di + 2] = src[si + 2];
                dst[di + 3] = 255;
            }
        }
    }

    private static void ConvertRgb24(
        byte[] src,
        int width,
        int height,
        int srcStride,
        byte[] dst,
        int dstStride)
    {
        for (int y = 0; y < height; y++)
        {
            int s = y * srcStride;
            int d = y * dstStride;

            for (int x = 0; x < width; x++)
            {
                int si = s + x * 3;
                int di = d + x * 4;

                dst[di] = src[si + 2];
                dst[di + 1] = src[si + 1];
                dst[di + 2] = src[si];
                dst[di + 3] = 255;
            }
        }
    }

    private static void ConvertGray8(
        byte[] src,
        int width,
        int height,
        int srcStride,
        byte[] dst,
        int dstStride)
    {
        for (int y = 0; y < height; y++)
        {
            int s = y * srcStride;
            int d = y * dstStride;

            for (int x = 0; x < width; x++)
            {
                byte gray = src[s + x];
                int di = d + x * 4;

                dst[di] = gray;
                dst[di + 1] = gray;
                dst[di + 2] = gray;
                dst[di + 3] = 255;
            }
        }
    }
}
