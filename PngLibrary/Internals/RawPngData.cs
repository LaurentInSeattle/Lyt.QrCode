namespace Lyt.Png.Internals;

/// <summary> Provides convenience methods for indexing into a raw byte array to extract pixel values. </summary>
internal class RawPngData
{
    private readonly byte[] data;
    private readonly int bytesPerPixel;
    private readonly int width;
    private readonly Palette? palette;
    private readonly ColorType colorType;
    private readonly int rowOffset;
    private readonly int bitDepth;

    /// <summary> Create a new <see cref="RawPngData"/>. </summary>
    /// <param name="data">The decoded pixel data as bytes.</param>
    /// <param name="bytesPerPixel">The number of bytes in each pixel.</param>
    /// <param name="palette">The palette for the image.</param>
    /// <param name="imageHeader">The image header.</param>
    internal RawPngData(byte[] data, int bytesPerPixel, Palette? palette, ImageHeader imageHeader)
    {

        this.data = data; 
        this.bytesPerPixel = bytesPerPixel;
        this.palette = palette;
        this.width = imageHeader.Width;
        this.colorType = imageHeader.ColorType;
        this.rowOffset = imageHeader.InterlaceMethod == InterlaceMethod.Adam7 ? 0 : 1;
        this.bitDepth = imageHeader.BitDepth;
    }

    internal Pixel GetPixel(int x, int y)
    {
        if (palette != null)
        {
            int pixelsPerByte = (8 / bitDepth);
            int bytesInRow = (1 + (width / pixelsPerByte));
            int byteIndexInRow = x / pixelsPerByte;
            int paletteIndex = (1 + (y * bytesInRow)) + byteIndexInRow;
            byte b = data[paletteIndex];

            if (bitDepth == 8)
            {
                return palette.GetPixel(b);
            }

            int withinByteIndex = x % pixelsPerByte;
            int rightShift = 8 - ((withinByteIndex + 1) * bitDepth);
            int indexActual = (b >> rightShift) & ((1 << bitDepth) - 1);

            return palette.GetPixel(indexActual);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte ToSingleByte(byte first, byte second)
        {
            int us = (first << 8) + second;
            return (byte)Math.Round((255 * us) / (double)ushort.MaxValue);
        }

        int rowStartPixel = (rowOffset + (rowOffset * y)) + (bytesPerPixel * width * y);
        int pixelStartIndex = rowStartPixel + (bytesPerPixel * x);
        byte first = data[pixelStartIndex];

        switch (bytesPerPixel)
        {
            case 1:
                return new Pixel(first, first, first, 255, true);

            case 2:
                switch (colorType)
                {
                    case ColorType.None:
                        {
                            byte second = data[pixelStartIndex + 1];
                            byte value = ToSingleByte(first, second);
                            return new Pixel(value, value, value, 255, true);
                        }

                    default:
                        return new Pixel(first, first, first, data[pixelStartIndex + 1], true);
                }

            case 3:
                return new Pixel(first, data[pixelStartIndex + 1], data[pixelStartIndex + 2], 255, false);

            case 4:
                switch (colorType)
                {
                    case ColorType.None | ColorType.AlphaChannelUsed:
                        {
                            byte second = data[pixelStartIndex + 1];
                            byte firstAlpha = data[pixelStartIndex + 2];
                            byte secondAlpha = data[pixelStartIndex + 3];
                            byte gray = ToSingleByte(first, second);
                            byte alpha = ToSingleByte(firstAlpha, secondAlpha);
                            return new Pixel(gray, gray, gray, alpha, true);
                        }

                    default:
                        return new Pixel(first, data[pixelStartIndex + 1], data[pixelStartIndex + 2], data[pixelStartIndex + 3], false);
                }
            case 6:
                return new Pixel(first, data[pixelStartIndex + 2], data[pixelStartIndex + 4], 255, false);

            case 8:
                return new Pixel(
                    first, data[pixelStartIndex + 2], data[pixelStartIndex + 4], data[pixelStartIndex + 6], false);

            default:
                throw new InvalidOperationException($"Unreconized number of bytes per pixel: {bytesPerPixel}.");
        }
    }
}