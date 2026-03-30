namespace Lyt.Png; 

/// <summary> Represents a PNG image.</summary>
public class PngImage
{
    private readonly RawPngData data;
    private readonly bool hasTransparencyChunk;

    /// <summary> The header data from the PNG image. </summary>
    public ImageHeader Header { get; }

    /// <summary> The width of the image in pixels. </summary>
    public int Width => this.Header.Width;

    /// <summary> The height of the image in pixels. </summary>
    public int Height => this.Header.Height;

    /// <summary> Whether the image has an alpha (transparency) layer. </summary>
    public bool HasAlphaChannel => (this.Header.ColorType & ColorType.AlphaChannelUsed) != 0 || hasTransparencyChunk;

    internal PngImage(ImageHeader header, RawPngData data, bool hasTransparencyChunk)
    {
        this.Header = header;
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.hasTransparencyChunk = hasTransparencyChunk;
    }

    /// <summary> Get the pixel at the given column and row (x, y). </summary>
    /// <remarks>
    /// Pixel values are generated on demand from the underlying data to prevent holding many items in memory at once, 
    /// so consumers should cache values if they're going to be looped over many time.
    /// </remarks>
    /// <param name="x">The x coordinate (column).</param>
    /// <param name="y">The y coordinate (row).</param>
    /// <returns>The pixel at the coordinate.</returns>
    public Pixel GetPixel(int x, int y) => data.GetPixel(x, y);

    /// <summary> Reads a PNG image from a stream. </summary>
    /// <param name="stream">The stream containing PNG data to be read.</param>
    /// <returns>The <see cref="PngImage"/> data from the stream.</returns>
    public static PngImage Open(Stream stream) => PngOpener.Open(stream);

    /// <summary> Reads a PNG image from an array of bytes. </summary>
    /// <param name="bytes">The bytes of the PNG data to be read.</param>
    /// <returns>The <see cref="PngImage"/> data from the bytes.</returns>
    public static PngImage Open(byte[] bytes)
    {
        using var memoryStream = new MemoryStream(bytes);
        return PngOpener.Open(memoryStream);
    }
}
