namespace Lyt.Png;

using System.Net.NetworkInformation;

// See: https://github.com/EliotJones/BigGustave

/// <summary> Represents a PNG image.</summary>
public partial class PngImage
{
    /// <summary> Reads a PNG image from an array of bytes. </summary>
    /// <remarks> Usually result of File.ReadAllBytes() on a PNG file on disk. </remarks>
    /// <param name="bytes"> The bytes of the PNG data to be read.</param>
    /// <returns>A new <see cref="PngImage"/>.</returns>
    public static PngImage Open(byte[] bytes)
    {
        using var memoryStream = new MemoryStream(bytes);
        return PngImage.OpenInternal(memoryStream);
    }

    /// <summary> Reads a PNG image from a stream. </summary>
    /// <param name="stream">The stream containing PNG data to be read.</param>
    /// <returns>A new <see cref="PngImage"/>.</returns>
    public static PngImage Open(Stream stream) => PngImage.OpenInternal(stream);

    public PngImage Clone()
    {
        var newImage = PngImage.CreateBlank(this.Width, this.Height, this.HasAlphaChannel);
        for (int y = 0; y < this.Height; y++)
        {
            for (int x = 0; x < this.Width; x++)
            {
                newImage.SetPixel(this.GetPixel(x, y), x, y);
            }
        }

        return newImage;
    }

    public (int Width, int Height, byte[] Pixels) ToBgraBitmap  ()
    {
        byte[] pixels = new byte[this.Width * this.Height * 4];
        // TODO !
        return (this.Width, this.Height, pixels);
    }
    
    public static PngImage CreateBlank(int width, int height, bool hasAlphaChannel)
    {
        int bpp = hasAlphaChannel ? 4 : 3;
        int length = (height * width * bpp) + height;
        return PngImage.FromPixels(new byte[length], width, height, hasAlphaChannel);
    }

    public static PngImage FromPixels(byte[] pixels, int width, int height, bool hasAlphaChannel)
    {
        throw new NotImplementedException();
    }

    /// <summary> The width of the image in pixels. </summary>
    public int Width => this.Header.Width;

    /// <summary> The height of the image in pixels. </summary>
    public int Height => this.Header.Height;

    /// <summary> The bit depth of the image. </summary>
    public int BitDepth => this.Header.BitDepth;

    /// <summary> Whether the image has an alpha (transparency) layer. </summary>
    public bool HasAlphaChannel => (this.Header.ColorType & ColorType.AlphaChannelUsed) != 0 || this.hasTransparencyChunk;
}
