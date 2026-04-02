namespace Lyt.Png;

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

    /// <summary> Create a new blank PNG image. </summary>
    public static PngImage CreateBlank(int width, int height, bool hasAlphaChannel)
    {
        int bytesPerPixel = hasAlphaChannel ? 4 : 3;
        int length = height * width * bytesPerPixel;
        return PngImage.FromBitmap(new byte[length], width, height, hasAlphaChannel);
    }

    /// <summary> Create a new PNG image using the provided RGBA or RGB pixel data. </summary>
    public static PngImage FromBitmap(byte[] pixels, int width, int height, bool hasAlphaChannel)
        => PngImage.FromBitmapInternal(pixels, width, height, hasAlphaChannel);

    /// <summary> Write the PNG file data bytes to a new byte array ready to be saved on disk, downloaded, etc... </summary>
    public byte[] Save()
    {
        using var pngMemoryStream = new PngMemoryStream();
        this.Save(pngMemoryStream);
        return pngMemoryStream.ToArray();
    }

    /// <summary> Creates a duplicate (Deep Copy) of this image. </summary>
    public PngImage Duplicate()
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

    /// <summary> Extract the bitmap data from this image, for example, using it in another application. </summary>
    /// <remarks> Valid ONLY for RGBA or RGB pixel data. </remarks>
    /// <remarks> Data format is independant from any UI or Image processing framework. </remarks>
    public (int Width, int Height, int Channels, byte[] Pixels) ToBitmap()
    {
        if (this.Header.IsRgba32)
        {
            return this.ToRgba32Bitmap();
        }
        else if (this.Header.IsRgb24)
        {
            return this.ToRgb24Bitmap();
        }

        var clone = this.Duplicate();
        if (clone.Header.IsRgba32)
        {
            return clone.ToRgba32Bitmap();
        }
        else if (clone.Header.IsRgb24)
        {
            return clone.ToRgb24Bitmap();
        }

        throw new Exception("Unable to convert image to bitmap format.");
    }

    /// <summary> The width of the image in pixels. </summary>
    public int Width => this.Header.Width;

    /// <summary> The height of the image in pixels. </summary>
    public int Height => this.Header.Height;

    /// <summary> The bit depth of the image, aka Bits per channel. </summary>
    public int BitDepth => this.Header.BitDepth;

    /// <summary> Whether the image is a BGRA 32 image. </summary>
    public bool IsBgra32 => this.Header.IsRgba32;

    /// <summary> Whether the image is a BGR 24 image. </summary>
    public bool IsBgr24 => this.Header.IsRgb24;

    /// <summary> Whether the image has an alpha (transparency) layer. </summary>
    public bool HasAlphaChannel => (this.Header.ColorType & ColorType.AlphaChannelUsed) != 0 || this.hasTransparencyChunk;

    /*
        Text data encoded in PNG files typically exists as metadata within tEXt, zTXt, or iTXt chunks to 
        describe image properties, authorship, and usage rights. 
        Common examples include image titles, creator names, copyright notices, creation times, software used, 
        and comments. These text chunks use keywords (e.g., "Description," "Author") and are often stored 
        in ISO 8859-1 (Latin-1) or UTF-8. 

        Examples of Text Data Metadata in PNG Chunks

            Title: A short description of the image.
            Author: The name of the creator.
            Description: A detailed explanation of the image content.
            Copyright: Legal notice regarding ownership.
            Creation Time: The date and time the image was created.
            Software: The software used to create or edit the image (e.g., "Adobe Photoshop").
            Comment: General user-supplied notes.
            Disclaimer: Legal warnings or disclaimers.
            Source: The device or source used to produce the image.
            Warning: Warnings regarding the nature of the content.
            URL: A link to the author or source.
    
     */

    /// <summary> Allows you to store arbitrary text data in the "iTXt" international textual data chunks of the generated PNG image. </summary>
    /// <param name="keyword">
    /// A keyword identifying the text data between 1-79 characters in length. Must not start with, end with or contain 
    /// consecutive whitespace characters. Only characters in the range 32 - 126 and 161 - 255 are permitted.
    /// </param>
    /// <param name="text"> The text data to store. Encoded as UTF-8 that may not contain zero (0) bytes but can be zero-length. </param>
    public void AddTextualMetadata(string keyword, string text)
    {
        if (keyword == string.Empty)
        {
            throw new ArgumentException("Keyword may not be empty.", nameof(keyword));
        }

        // trailing, leading and consecutive whitespaces are prohibited : Removing them
        keyword = keyword.Trim();
        keyword = keyword.Replace("  ", " ");
        if (keyword.Length > 79)
        {
            throw new ArgumentException(
                $"Keyword must be between 1 - 79 characters, provided keyword '{keyword}' has length of {keyword.Length} characters.",
                nameof(keyword));
        }

        for (int i = 0; i < keyword.Length; i++)
        {
            char c = keyword[i];
            bool isValid = (c >= 32 && c <= 126) || (c >= 161 && c <= 255);
            if (!isValid)
            {
                throw new ArgumentException(
                    "The keyword can only contain printable Latin 1 characters and spaces in the ranges 32 - 126 or 161 -255. " +
                    $"The provided keyword '{keyword}' contained an invalid character ({c}) at index {i}.",
                    nameof(keyword));
            }


        }

        byte[] bytes = Encoding.UTF8.GetBytes(text);
        for (int i = 0; i < bytes.Length; i++)
        {
            byte b = bytes[i];
            if (b == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(text),
                    "The provided text contained a null (0) byte when converted to UTF-8. Null bytes are not permitted. " +
                    $"Text was: '{text}'");
            }
        }

        // All checks passed, store the keyword and text data (as UTF-8 bytes) for later writing to the PNG file.
        this.textualMetadata.Add((keyword, bytes));
    }

    /// <summary> Get the pixel at the given column and row (x, y). </summary>
    /// <remarks>
    /// Pixel values are generated on demand from the underlying data to prevent holding many items in memory at once, 
    /// so consumers should cache values if they're going to be looped over many time.
    /// </remarks>
    /// <param name="x">The x coordinate (column).</param>
    /// <param name="y">The y coordinate (row).</param>
    /// <returns>The pixel at the coordinate.</returns>
    public Pixel GetPixel(int x, int y)
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

    /// <summary> Sets the RGB pixel value for the given column (x) and row (y). </summary>
    public void SetPixel(byte r, byte g, byte b, int x, int y) => this.SetPixel(new Pixel(r, g, b), x, y);

    /// <summary> Set the pixel value for the given column (x) and row (y). </summary>
    public void SetPixel(Pixel pixel, int x, int y)
    {
        if (!this.hasTooManyColorsForPalette)
        {
            int colorIntValue = Pixel.ToColorInt(pixel);
            if (colorIntValue != this.backgroundColorInt)
            {
                if (!this.colorCounts.TryGetValue(colorIntValue, out int value))
                {
                    this.colorCounts[colorIntValue] = 1;
                }
                else
                {
                    this.colorCounts[colorIntValue] = ++value;
                }

                this.colorCounts[backgroundColorInt]--;
                if (this.colorCounts[backgroundColorInt] == 0)
                {
                    this.colorCounts.Remove(backgroundColorInt);
                }
            }

            if (this.colorCounts.Count > 256)
            {
                this.hasTooManyColorsForPalette = true;
            }
        }

        int start = (y * ((this.width * this.bytesPerPixel) + 1)) + 1 + (x * this.bytesPerPixel);
        this.data[start++] = pixel.R;
        this.data[start++] = pixel.G;
        this.data[start++] = pixel.B;

        if (this.HasAlphaChannel)
        {
            this.data[start] = pixel.A;
        }
    }
}
