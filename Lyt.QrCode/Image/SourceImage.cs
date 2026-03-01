namespace Lyt.QrCode.Image;

public sealed class SourceImage
{
    public int Width { get; }

    public int Stride { get; }

    public int Height { get; }

    public PixelFormat Format { get; }

    public byte[] Pixels { get; }

    public bool IsLocked { get; }

    public SourceImage(int width, int height, int stride, PixelFormat format, byte[] pixels, bool isLocked = true)
    {
        if ((width <= 0) || (height <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width and Height must be positive.");
        }

        if ((width > 8 * 1024) || (height > 8 * 1024))
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width and Height are limited to 8K pixels.");
        }

        if (pixels.Length != height * stride * format.BytesPerPixel())
        {
            throw new ArgumentNullException(nameof(pixels), "Pixel data inconsistent with provided dimensions.");
        }

        this.Width = width;
        this.Height = height;
        this.Stride = stride;
        this.Format = format;
        this.Pixels = pixels;
        this.IsLocked = isLocked;
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
                return new GrayscaleImage(this.Width, this.Height, this.Pixels, this.IsLocked);
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
}
