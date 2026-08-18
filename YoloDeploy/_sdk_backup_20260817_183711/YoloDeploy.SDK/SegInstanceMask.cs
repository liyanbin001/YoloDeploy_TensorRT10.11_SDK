namespace YoloDeploy.SDK;

public sealed class SegInstanceMask
{
    private readonly ushort[] _data;

    internal SegInstanceMask(
        ushort[] data,
        int width,
        int height,
        int stride)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (width <= 0 || height <= 0 || stride < width)
            throw new ArgumentOutOfRangeException(nameof(width));

        if (data.Length < checked(stride * height))
            throw new ArgumentException(
                "Instance mask buffer is smaller than stride * height.",
                nameof(data));

        _data = data;
        Width = width;
        Height = height;
        Stride = stride;
    }

    public int Width { get; }
    public int Height { get; }

    /// <summary>Stride in UInt16 elements, not bytes.</summary>
    public int Stride { get; }

    public ReadOnlyMemory<ushort> Data => _data;

    public ushort this[int x, int y]
    {
        get
        {
            ValidateCoordinates(x, y);
            return _data[y * Stride + x];
        }
    }

    public bool BelongsTo(int x, int y, int maskId)
    {
        if (maskId <= 0 || maskId > ushort.MaxValue)
            return false;

        return this[x, y] == (ushort)maskId;
    }

    public byte[] CreateBinaryMask(int maskId)
    {
        if (maskId <= 0 || maskId > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(maskId));

        ushort target = (ushort)maskId;
        byte[] result = new byte[checked(Width * Height)];

        for (int y = 0; y < Height; y++)
        {
            int src = y * Stride;
            int dst = y * Width;

            for (int x = 0; x < Width; x++)
            {
                result[dst + x] =
                    _data[src + x] == target ? (byte)255 : (byte)0;
            }
        }

        return result;
    }

    public ushort[] ToArray() => (ushort[])_data.Clone();

    private void ValidateCoordinates(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
            throw new ArgumentOutOfRangeException(
                $"Pixel ({x},{y}) is outside {Width}x{Height}.");
    }
}
