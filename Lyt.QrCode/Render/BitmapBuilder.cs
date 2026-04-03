namespace Lyt.QrCode.Render;

using System.Drawing;

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
        // Set all pixel bytes to background color 
        byte r = (byte ) ( this.background & 0xFF) ;
        byte g = (byte)((this.background >> 8) & 0xFF);
        byte b = (byte)((this.background >> 16 )& 0xFF);
        for (int yy = 0; yy < height; yy++)
        {
            for (int xx = 0; xx < width; xx++)
            {
                int offset = 4 * xx + yy * width * 4 ;
                this.pixelBytes[offset + 0] = r;
                this.pixelBytes[offset + 1] = g;
                this.pixelBytes[offset + 2] = b;
                this.pixelBytes[offset + 3] = 0xFF;
            }
        }

        int sourceWidth = pixelProvider.Width;
        int sourceHeight = pixelProvider.Height;
        int bytesPerLine = this.width * 4;

        r = (byte)(this.foreground & 0xFF);
        g = (byte)((this.foreground >> 8) & 0xFF);
        b = (byte)((this.foreground >> 16) & 0xFF);

        for (int y = 0; y < sourceHeight; y++)
        {
            int yOffset = (border + y) * scale * bytesPerLine;

            for (int x = 0; x < sourceWidth; x++)
            {
                if (!pixelProvider.GetPixel(x, sourceHeight - y - 1))
                {
                    // Background already set 
                    continue; 
                }
                
                int start = (border + x) * scale;
                int end = start + scale;

                // set pixels for module ('scale' times)
                for (int pos = start; pos < end; pos++)
                {
                    // BGRA 
                    int offset = yOffset + pos * 4;
                    this.pixelBytes[offset + 0] = r;
                    this.pixelBytes[offset + 1] = g;
                    this.pixelBytes[offset + 2] = b;
                    this.pixelBytes[offset + 3] = 0xFF;
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

    void SetPixel(int x, int y, int color)
    {
        // BGRA 
        int offset = y + x * 4 ; 
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
    {
        Array.Copy(BitConverter.GetBytes(this.pixelBytes.Length), 0, this.bitmapBytes, 34, 4);
        Array.Copy(this.pixelBytes, 0, this.bitmapBytes, imageHeaderSize, this.pixelBytes.Length);
    }
}
