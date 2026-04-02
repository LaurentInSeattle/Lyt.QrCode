namespace Lyt.Png.Internals;
/// <summary> The high level information about the image. </summary>
internal readonly struct ImageHeader
{
    internal static readonly byte[] ExpectedHeader = [137, 80, 78, 71, 13, 10, 26, 10];

    internal static readonly byte[] HeaderBytes = [73, 72, 68, 82];

    private static readonly Dictionary<ColorType, HashSet<byte>> PermittedBitDepths = new()
    {
        {ColorType.None, new HashSet<byte> {1, 2, 4, 8, 16}},
        {ColorType.ColorUsed, new HashSet<byte> {8, 16}},
        {ColorType.PaletteUsed | ColorType.ColorUsed, new HashSet<byte> {1, 2, 4, 8}},
        {ColorType.AlphaChannelUsed, new HashSet<byte> {8, 16}},
        {ColorType.AlphaChannelUsed | ColorType.ColorUsed, new HashSet<byte> {8, 16}},
    };

    /// <summary> Create a new <see cref="ImageHeader"/>. </summary>
    internal ImageHeader(int width, int height, byte bitDepth, ColorType colorType, CompressionMethod compressionMethod, FilterMethod filterMethod, InterlaceMethod interlaceMethod)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Invalid width (<= 0) for image.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Invalid height (<= 0) for image.");
        }

        if (!PermittedBitDepths.TryGetValue(colorType, out var permitted)
            || !permitted.Contains(bitDepth))
        {
            throw new ArgumentException($"The bit depth {bitDepth} is not permitted for color type {colorType}.");
        }

        this.Width = width;
        this.Height = height;
        this.BitDepth = bitDepth;
        this.ColorType = colorType;
        this.CompressionMethod = compressionMethod;
        this.FilterMethod = filterMethod;
        this.InterlaceMethod = interlaceMethod;
    }

    /// <summary> The width of the image in pixels. </summary>
    internal int Width { get; }

    /// <summary> The height of the image in pixels. </summary>
    internal int Height { get; }

    /// <summary> The bit depth of the image. </summary>
    /// <remarks> Bits per channel </remarks>
    internal byte BitDepth { get; }

    /// <summary> The color type of the image. </summary>
    internal ColorType ColorType { get; }

    /// <summary> The compression method used for the image. </summary>
    internal CompressionMethod CompressionMethod { get; }

    /// <summary> The filter method used for the image. </summary>
    internal FilterMethod FilterMethod { get; }

    /// <summary> The interlace method used by the image. </summary>
    internal InterlaceMethod InterlaceMethod { get; }

    internal bool IsRgba32 =>
        this.BitDepth == 8 &&
        this.ColorType.HasFlag(ColorType.ColorUsed) &&
        this.ColorType.HasFlag(ColorType.AlphaChannelUsed);

    internal bool IsRgb24 =>
        this.BitDepth == 8 &&
        this.ColorType.HasFlag(ColorType.ColorUsed) &&
        !this.ColorType.HasFlag(ColorType.AlphaChannelUsed);

    internal (byte bytesPerPixel, byte samplesPerPixel) GetBytesAndSamplesPerPixel()
    {
        int bitDepthCorrected = (this.BitDepth + 7) / 8;
        byte samplesPerPixel = this.SamplesPerPixel;
        return ((byte)(samplesPerPixel * bitDepthCorrected), samplesPerPixel);
    }

    internal byte SamplesPerPixel
        => this.ColorType switch
        {
            ColorType.None => 1,
            ColorType.PaletteUsed => 1,
            ColorType.ColorUsed => 3,
            ColorType.AlphaChannelUsed => 2,
            ColorType.ColorUsed | ColorType.AlphaChannelUsed => 4,
            _ => 0,
        };

    internal int BytesPerScanline(byte samplesPerPixel)
    {
        int width = this.Width;
        return this.BitDepth switch
        {
            1 => (width + 7) / 8,
            2 => (width + 3) / 4,
            4 => (width + 1) / 2,
            8 or 16 => width * samplesPerPixel * (this.BitDepth / 8),
            _ => 0,
        };
    }

    /// <inheritdoc />
    public override string ToString()
        => $"w: {this.Width}, h: {this.Height}, bitDepth: {this.BitDepth}, colorType: {this.ColorType}, " +
            $"compression: {this.CompressionMethod}, filter: {this.FilterMethod}, interlace: {this.InterlaceMethod}.";
}