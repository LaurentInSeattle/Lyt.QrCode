namespace Lyt.QrCode.Image;

public sealed partial class SourceImage
{
    public int Width { get; }

    public int Stride { get; }

    public int Height { get; }

    public PixelFormat Format { get; }

    public byte[] Pixels { get; }

    /// <summary> Creates a SourceImage instance from the provided information</summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="stride"></param>
    /// <param name="format"></param>
    /// <param name="pixels"></param>
    /// <param name="isLocked"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public SourceImage(int width, int height, int stride, PixelFormat format, byte[] pixels)
    {
        if ((width <= 0) || (height <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width and Height must be positive.");
        }

        if ((width > 12 * 1024) || (height > 12 * 1024))
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width and Height are both limited to 12 K pixels.");
        }

        if ((stride * height > 32 * 1024 * 1024))
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Pixel count is limited to 32 M pixels.");
        }

        if (pixels.Length != height * stride)
        {
            throw new ArgumentNullException(nameof(pixels), "Pixel data length inconsistent with provided dimensions.");
        }

        this.Width = width;
        this.Height = height;
        this.Stride = stride;
        this.Format = format;
        this.Pixels = pixels;
    }

    internal GrayscaleImage ToGrayscale()
    {
        if (this.Format == PixelFormat.Gray8)
        {
            if (this.Stride != this.Width)
            {
                // Stride handling for Gray8 format 
                return new GrayscaleImage(this.Width, this.Height, this.Stride, this.Pixels);
            }
            else
            {
                return new GrayscaleImage(this.Width, this.Height, this.Pixels);
            }
        }

        // TODO: Handle stride properly, currently assuming tightly packed pixels
        byte[] grayscalePixels = new byte[this.Width * this.Height];
        for (int i = 0; i < this.Pixels.Length; i += this.Format.BytesPerPixel())
        {
            byte r, g, b;
            switch (this.Format)
            {
                case PixelFormat.RGBA32:
                case PixelFormat.RGB24:
                    r = this.Pixels[i];
                    g = this.Pixels[i + 1];
                    b = this.Pixels[i + 2];
                    break;

                case PixelFormat.BGRA32:
                case PixelFormat.BGR24:
                    b = this.Pixels[i];
                    g = this.Pixels[i + 1];
                    r = this.Pixels[i + 2];
                    break;

                case PixelFormat.ABGR32:
                    b = this.Pixels[i + 1];
                    g = this.Pixels[i + 2];
                    r = this.Pixels[i + 3];
                    break;

                case PixelFormat.ARGB32:
                    r = this.Pixels[i + 1];
                    g = this.Pixels[i + 2];
                    b = this.Pixels[i + 3];
                    break;

                case PixelFormat.RGB565:
                    ushort pixelData = BitConverter.ToUInt16(this.Pixels, i);
                    r = (byte)((pixelData >> 11) & 0x1F);
                    g = (byte)((pixelData >> 5) & 0x3F);
                    b = (byte)(pixelData & 0x1F);

                    // Scale to 8 bits
                    r = (byte)(r << 3);
                    g = (byte)(g << 2);
                    b = (byte)(b << 3);
                    break;

                default:
                    throw new NotSupportedException($"Unsupported pixel format: {this.Format}");
            }

            // Convert to grayscale using luminosity method
            grayscalePixels[i / this.Format.BytesPerPixel()] = (byte)(0.299 * r + 0.587 * g + 0.114 * b);
        }

        return new GrayscaleImage(this.Width, this.Height, grayscalePixels);
    }

    // RGBA32 format for output
    internal static SourceImage FromPixelProvider(
        IPixelProvider pixelProvider,
        int scale,
        int border,
        int foreground = 0,
        int background = 0xFFFFFF)
    {
        int sourceWidth = pixelProvider.Width;
        int sourceHeight = pixelProvider.Height;
        int imageWidth = (sourceWidth + border * 2) * scale;
        int imageHeight = (sourceHeight + border * 2) * scale;
        int imageStride = imageWidth * 4;
        byte[] pixelBytes = new byte[imageStride* imageHeight];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetPixel(int x, int y, int color)
        {
            // BGRA 
            int offset = ((imageHeight - y - 1) * imageWidth + x) * 4;
            pixelBytes[offset + 0] = (byte)(color & 0xFF);
            pixelBytes[offset + 1] = (byte)((color >> 8) & 0xFF);
            pixelBytes[offset + 2] = (byte)((color >> 16) & 0xFF);
            pixelBytes[offset + 3] = 0xFF;
        }

        for (int y = 0; y < sourceHeight; y++)
        {
            int yOffset = (border + y) * scale * imageStride;

            for (int x = 0; x < sourceWidth; x++)
            {
                int color = pixelProvider.GetPixel(x, y) ? foreground : background;
                int pos = (border + x) * scale;
                int end = pos + scale;

                // set pixels for module ('scale' times)
                for (; pos < end; pos++)
                {
                    SetPixel(pos, y, color);
                }
            }

            // replicate line 'scale' times
            for (int i = 1; i < scale; i++)
            {
                Array.Copy(pixelBytes, yOffset, pixelBytes, yOffset + i * imageStride, imageStride);
            }
        }

        return new SourceImage(imageWidth, imageHeight, imageStride, PixelFormat.RGBA32, pixelBytes);
    }
}
