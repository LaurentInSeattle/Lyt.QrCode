namespace Lyt.QrCode.Render;

internal sealed class BitmapBuilder
{
    const int imageHeaderSize = 54;

    private readonly int width;
    private readonly int height;
    private readonly byte[] pixelBytes;
    private readonly byte[] bitmapBytes;
    private readonly int foreground;
    private readonly int background;

    internal BitmapBuilder(int width, int height, int foreground, int background)
    {
        this.width = width;
        this.height = height;
        this.pixelBytes = new byte[width * height * 4];
        this.bitmapBytes = new byte[imageHeaderSize + this.pixelBytes.Length];
        this.foreground = foreground;
        this.background = background;
        this.GenerateHeader();
    }

    /// <summary>  Creates a BITMAP image of the provided Pixel Provider. (usually a QR code) </summary>
    /// <param name="pixelProvider">The pixelProvider</param>
    /// <param name="border">The border width, as a factor of the module (QR code pixel) size.</param>
    /// <param name="scale">The width and height, in pixels, of each module.</param>
    /// <param name="foreground">The foreground color (dark modules), in RGB value (little endian).</param>
    /// <param name="background">The background color (light modules), in RGB value (little endian).</param>
    /// <returns> A Bitmap image, as a byte array.</returns>
    internal static byte[] ToImage(IPixelProvider pixelProvider, int scale, int border, int foreground = 0, int background = 0xFFFFFF)
    {
        int width = pixelProvider.Width;
        int height = pixelProvider.Height;
        int imageWidth = (width + border * 2) * scale;
        int imageHeight = (height + border * 2) * scale;
        var builder = new BitmapBuilder(imageWidth, imageHeight, foreground, background);

        builder.CreateBitmap(pixelProvider, border, scale);
        builder.CopyPixelBytes();
        return builder.Bytes;
    }

    /// <summary>  Creates an uncompressed 32 bits per pixel bitmap of the provided Pixel Provider. (usually a QR code) </summary>
    /// <param name="pixelProvider">The pixelProvider</param>
    /// <param name="border">The border</param>
    /// <param name="scale">The scale</param>
    private void CreateBitmap(IPixelProvider pixelProvider, int border, int scale)
    {
        int sourceWidth = pixelProvider.Width;
        int sourceHeight = pixelProvider.Height;
        int bytesPerLine = this.width * 4;

        for (int y = 0; y < sourceHeight; y++)
        {
            int yOffset = (border + y) * scale * bytesPerLine;

            for (int x = 0; x < sourceWidth; x++)
            {
                int color = pixelProvider.GetPixel(x, y) ? this.foreground : this.background;
                int pos = (border + x) * scale;
                int end = pos + scale;

                // set pixels for module ('scale' times)
                for (; pos < end; pos++)
                {
                    this.SetPixel(pos, y, color);
                }
            }

            // replicate line 'scale' times
            for (int i = 1; i < scale; i++)
            {
                Array.Copy(this.pixelBytes, yOffset, this.pixelBytes, yOffset + i * bytesPerLine, bytesPerLine);
            }
        }
    }

    internal byte[] Bytes => this.bitmapBytes;

    // Note: Forces alpha to fully opaque, consider improving 
    internal void SetPixel(int x, int y, int color)
    {
        int offset = ((this.height - y - 1) * this.width + x) * 4;

        // BGRA 
        this.pixelBytes[offset + 0] = (byte)(color & 0xFF);
        this.pixelBytes[offset + 1] = (byte)((color >> 8) & 0xFF);
        this.pixelBytes[offset + 2] = (byte)((color >> 16) & 0xFF);
        this.pixelBytes[offset + 3] = 0xFF;
    }

    private void GenerateHeader()
    {
        this.bitmapBytes[0] = (byte)'B';
        this.bitmapBytes[1] = (byte)'M';
        this.bitmapBytes[14] = 40; // Basic bitmap header
        Array.Copy(BitConverter.GetBytes(this.bitmapBytes.Length), 0, this.bitmapBytes, 2, 4);
        Array.Copy(BitConverter.GetBytes(imageHeaderSize), 0, this.bitmapBytes, 10, 4);
        Array.Copy(BitConverter.GetBytes(this.width), 0, this.bitmapBytes, 18, 4);
        Array.Copy(BitConverter.GetBytes(this.height), 0, this.bitmapBytes, 22, 4);
        Array.Copy(BitConverter.GetBytes(32), 0, this.bitmapBytes, 28, 2);
    }

    internal void CopyPixelBytes()
        => Array.Copy(BitConverter.GetBytes(this.pixelBytes.Length), 0, this.bitmapBytes, 34, 4);
}
