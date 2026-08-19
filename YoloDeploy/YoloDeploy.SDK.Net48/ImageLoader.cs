using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace YoloDeploy.SDK
{
    internal sealed class BgraImage
    {
        internal BgraImage(
            byte[] pixels,
            int width,
            int height,
            int stride)
        {
            Pixels = pixels;
            Width = width;
            Height = height;
            Stride = stride;
        }

        internal byte[] Pixels { get; private set; }
        internal int Width { get; private set; }
        internal int Height { get; private set; }
        internal int Stride { get; private set; }
    }

    internal static class ImageLoader
    {
        internal static BgraImage LoadBgra32(
            string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException(
                    "待检测图片不存在。",
                    imagePath);
            }

            using (FileStream stream = new FileStream(
                imagePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite))
            {
                BitmapDecoder decoder =
                    BitmapDecoder.Create(
                        stream,
                        BitmapCreateOptions.PreservePixelFormat,
                        BitmapCacheOption.OnLoad);

                if (decoder.Frames.Count == 0)
                {
                    throw new YoloSdkException(
                        "无法读取图片：" + imagePath);
                }

                BitmapSource source =
                    decoder.Frames[0];

                FormatConvertedBitmap converted =
                    new FormatConvertedBitmap();

                converted.BeginInit();
                converted.Source = source;
                converted.DestinationFormat =
                    PixelFormats.Bgra32;
                converted.EndInit();
                converted.Freeze();

                int width =
                    converted.PixelWidth;

                int height =
                    converted.PixelHeight;

                if (width <= 0 ||
                    height <= 0)
                {
                    throw new YoloSdkException(
                        "图片尺寸无效："
                        + width
                        + "x"
                        + height);
                }

                int stride =
                    checked(width * 4);

                byte[] pixels =
                    new byte[
                        checked(
                            stride * height)];

                converted.CopyPixels(
                    pixels,
                    stride,
                    0);

                return new BgraImage(
                    pixels,
                    width,
                    height,
                    stride);
            }
        }
    }
}
