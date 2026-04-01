namespace Lyt.Png.Internals; 

/// <summary> Used to construct PNG images. Call <see cref="Create"/> to make a new builder. </summary>
internal class PngBuilder
{
    private readonly byte[] rawData;
    private readonly bool hasAlphaChannel;
    private readonly int width;
    private readonly int height;
    private readonly int bytesPerPixel;

    private readonly int backgroundColorInt;
    private readonly Dictionary<int, int> colorCounts;
    private readonly List<(string keyword, byte[] data)> storedStrings = [];

    private bool hasTooManyColorsForPalette;

    /// <summary> Create a builder for a PNG with the given width and size. </summary>
    public static PngBuilder Create(int width, int height, bool hasAlphaChannel)
    {
        int bpp = hasAlphaChannel ? 4 : 3;
        int length = (height * width * bpp) + height;
        return new PngBuilder(new byte[length], hasAlphaChannel, width, height, bpp);
    }

    /// <summary> Create a builder from a <see cref="Png"/>. </summary>
    public static PngBuilder FromPng(PngImage png)
    {
        var result = Create(png.Width, png.Height, png.HasAlphaChannel);
        for (int y = 0; y < png.Height; y++)
        {
            for (int x = 0; x < png.Width; x++)
            {
                result.SetPixel(png.GetPixel(x, y), x, y);
            }
        }

        return result;
    }

    /// <summary>
    /// Create a builder from the bytes of the specified PNG image.
    /// </summary>
    public static PngBuilder FromPngBytes(byte[] png)
    {
        var pngActual = PngImage.Open(png);
        return FromPng(pngActual);
    }

    /// <summary>
    /// Create a builder from the bytes in the BGRA32 pixel format.
    /// https://docs.microsoft.com/en-us/dotnet/api/system.windows.media.pixelformats.bgra32
    /// </summary>
    /// <param name="data">The pixels in BGRA32 format.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="useAlphaChannel">Whether to include an alpha channel in the output.</param>
    public static PngBuilder FromBgra32Pixels(byte[] data, int width, int height, bool useAlphaChannel = true)
    {
        using var memoryStream = new MemoryStream(data);
        var builder = FromBgra32Pixels(memoryStream, width, height, useAlphaChannel);

        return builder;
    }

    /// <summary>
    /// Create a builder from the bytes in the BGRA32 pixel format.
    /// https://docs.microsoft.com/en-us/dotnet/api/system.windows.media.pixelformats.bgra32
    /// </summary>
    /// <param name="data">The pixels in BGRA32 format.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="useAlphaChannel">Whether to include an alpha channel in the output.</param>
    public static PngBuilder FromBgra32Pixels(Stream data, int width, int height, bool useAlphaChannel = true)
    {
        var bpp = useAlphaChannel ? 4 : 3;

        var length = (height * width * bpp) + height;

        var builder = new PngBuilder(new byte[length], useAlphaChannel, width, height, bpp);

        var buffer = new byte[4];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var read = data.Read(buffer, 0, buffer.Length);

                if (read != 4)
                {
                    throw new InvalidOperationException($"Unexpected end of stream, expected to read 4 bytes at offset {data.Position - read} for (x: {x}, y: {y}), instead got {read}.");
                }

                if (useAlphaChannel)
                {
                    builder.SetPixel(new Pixel(buffer[0], buffer[1], buffer[2], buffer[3], false), x, y);
                }
                else
                {
                    builder.SetPixel(buffer[0], buffer[1], buffer[2], x, y);
                }
            }
        }

        return builder;
    }

    private PngBuilder(byte[] rawData, bool hasAlphaChannel, int width, int height, int bytesPerPixel)
    {
        this.rawData = rawData;
        this.hasAlphaChannel = hasAlphaChannel;
        this.width = width;
        this.height = height;
        this.bytesPerPixel = bytesPerPixel;

        backgroundColorInt = Pixel.ToColorInt(0, 0, 0, hasAlphaChannel ? (byte)0 : byte.MaxValue);

        colorCounts = new Dictionary<int, int>()
        {
            { backgroundColorInt, (width * height)}
        };
    }

    /// <summary>
    /// Sets the RGB pixel value for the given column (x) and row (y).
    /// </summary>
    public PngBuilder SetPixel(byte r, byte g, byte b, int x, int y) => this.SetPixel(new Pixel(r, g, b), x, y);

    /// <summary>
    /// Set the pixel value for the given column (x) and row (y).
    /// </summary>
    public PngBuilder SetPixel(Pixel pixel, int x, int y)
    {
        if (!hasTooManyColorsForPalette)
        {
            var val = Pixel.ToColorInt(pixel);
            if (val != backgroundColorInt)
            {
                if (!colorCounts.ContainsKey(val))
                {
                    colorCounts[val] = 1;
                }
                else
                {
                    colorCounts[val]++;
                }

                colorCounts[backgroundColorInt]--;
                if (colorCounts[backgroundColorInt] == 0)
                {
                    colorCounts.Remove(backgroundColorInt);
                }
            }

            if (colorCounts.Count > 256)
            {
                hasTooManyColorsForPalette = true;
            }
        }

        var start = (y * ((width * bytesPerPixel) + 1)) + 1 + (x * bytesPerPixel);

        rawData[start++] = pixel.R;
        rawData[start++] = pixel.G;
        rawData[start++] = pixel.B;

        if (hasAlphaChannel)
        {
            rawData[start] = pixel.A;
        }

        return this;
    }

    /// <summary>
    /// Allows you to store arbitrary text data in the "iTXt" international textual data
    /// chunks of the generated PNG image.
    /// </summary>
    /// <param name="keyword">
    /// A keyword identifying the text data between 1-79 characters in length.
    /// Must not start with, end with or contain consecutive whitespace characters.
    /// Only characters in the range 32 - 126 and 161 - 255 are permitted.
    /// </param>
    /// <param name="text">
    /// The text data to store. Encoded as UTF-8 that may not contain zero (0) bytes but can be zero-length.
    /// </param>
    public PngBuilder StoreText(string keyword, string text)
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

        storedStrings.Add((keyword, bytes));

        return this;
    }

    /// <summary> Get the bytes of the PNG file for this builder. </summary>
    public byte[] Save()
    {
        using var memoryStream = new MemoryStream();
        Save(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary> Write the PNG file bytes to the provided stream. </summary>
    public void Save(Stream outputStream)
    {
        byte[]? palette = null;
        int dataLength = rawData.Length;
        int bitDepth = 8;

        if (!hasTooManyColorsForPalette && !hasAlphaChannel)
        {
            var paletteColors = colorCounts.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();
            bitDepth = paletteColors.Count > 16 ? 8 : 4;
            int samplesPerByte = bitDepth == 8 ? 1 : 2;
            bool applyShift = samplesPerByte == 2;

            palette = new byte[3 * paletteColors.Count];

            for (int i = 0; i < paletteColors.Count; i++)
            {
                var (r, g, b, a) = Pixel.FromColorInt(paletteColors[i]);
                int startIndex = i * 3;
                palette[startIndex++] = r;
                palette[startIndex++] = g;
                palette[startIndex] = b;
            }

            int rawDataIndex = 0;

            for (int y = 0; y < height; y++)
            {
                // None filter - we don't use filtering for palette images.
                rawData[rawDataIndex++] = 0;

                for (int x = 0; x < width; x++)
                {
                    int index = ((y * width * bytesPerPixel) + y + 1) + (x * bytesPerPixel);

                    byte r = rawData[index++];
                    byte g = rawData[index++];
                    byte b = rawData[index];

                    int colorInt = Pixel.ToColorInt(r, g, b);
                    byte value = (byte)paletteColors.IndexOf(colorInt);

                    if (applyShift)
                    {
                        // apply mask and shift
                        int withinByteIndex = x % 2;

                        if (withinByteIndex == 1)
                        {
                            rawData[rawDataIndex] = (byte)(rawData[rawDataIndex] + value);
                            rawDataIndex++;
                        }
                        else
                        {
                            rawData[rawDataIndex] = (byte)(value << 4);
                        }
                    }
                    else
                    {
                        rawData[rawDataIndex++] = value;
                    }
                }
            }

            dataLength = rawDataIndex;
        }
        else
        {
            AttemptCompressionOfRawData(rawData);
        }

        outputStream.Write(ImageHeader.ExpectedHeader, 0, ImageHeader.ExpectedHeader.Length);
        var stream = new PngStreamWriteHelper(outputStream);
        stream.WriteChunkLength(13);
        stream.WriteChunkHeader(ImageHeader.HeaderBytes);
        StreamHelper.WriteBigEndianInt32(stream, width);
        StreamHelper.WriteBigEndianInt32(stream, height);
        stream.WriteByte((byte)bitDepth);

        var colorType = ColorType.ColorUsed;
        if (hasAlphaChannel)
        {
            colorType |= ColorType.AlphaChannelUsed;
        }

        if (palette != null)
        {
            colorType |= ColorType.PaletteUsed;
        }

        stream.WriteByte((byte)colorType);
        stream.WriteByte((byte)CompressionMethod.DeflateWithSlidingWindow);
        stream.WriteByte((byte)FilterMethod.AdaptiveFiltering);
        stream.WriteByte((byte)InterlaceMethod.None);
        stream.WriteCrc();

        if (palette != null)
        {
            stream.WriteChunkLength(palette.Length);
            stream.WriteChunkHeader(Encoding.ASCII.GetBytes("PLTE"));
            stream.Write(palette, 0, palette.Length);
            stream.WriteCrc();
        }

        byte[] imageData = Compress(rawData, dataLength);
        stream.WriteChunkLength(imageData.Length);
        stream.WriteChunkHeader(Encoding.ASCII.GetBytes("IDAT"));
        stream.Write(imageData, 0, imageData.Length);
        stream.WriteCrc();

        foreach (var storedString in storedStrings)
        {
            byte[] keyword = Encoding.GetEncoding("iso-8859-1").GetBytes(storedString.keyword);
            int length = keyword.Length
                         + 1 // Null separator
                         + 1 // Compression flag
                         + 1 // Compression method
                         + 1 // Null separator
                         + 1 // Null separator
                         + storedString.data.Length;

            stream.WriteChunkLength(length);
            stream.WriteChunkHeader(Encoding.ASCII.GetBytes("iTXt"));
            stream.Write(keyword, 0, keyword.Length);            
            stream.WriteByte(0); // Null separator
            stream.WriteByte(0); // Compression flag (0 for uncompressed)
            stream.WriteByte(0); // Compression method (0, ignored since flag is zero)
            stream.WriteByte(0); // Null separator
            stream.WriteByte(0); // Null separator
            stream.Write(storedString.data, 0, storedString.data.Length);
            stream.WriteCrc();
        }

        stream.WriteChunkLength(0);
        stream.WriteChunkHeader(Encoding.ASCII.GetBytes("IEND"));
        stream.WriteCrc();
    }

    private static byte[] Compress(byte[] data, int dataLength)
    {
        const byte Deflate32KbWindow = 120;
        const byte ChecksumBits = 1;
        const int HeaderLength = 2;
        const int ChecksumLength = 4;

        using var compressStream = new MemoryStream();
        using var compressor = new DeflateStream(compressStream, CompressionLevel.Optimal, true);
        compressor.Write(data, 0, dataLength);
        compressor.Close();
        compressStream.Seek(0, SeekOrigin.Begin);
        byte[] result = new byte[HeaderLength + compressStream.Length + ChecksumLength];

        // Write the ZLib header.
        result[0] = Deflate32KbWindow;
        result[1] = ChecksumBits;

        // Write the compressed data.
        int streamValue;
        int i = 0;
        while ((streamValue = compressStream.ReadByte()) != -1)
        {
            result[HeaderLength + i] = (byte)streamValue;
            i++;
        }

        // Write Checksum of raw data.
        int checksum = Adler32Checksum.Calculate(data, dataLength);
        long offset = HeaderLength + compressStream.Length;
        result[offset++] = (byte)(checksum >> 24);
        result[offset++] = (byte)(checksum >> 16);
        result[offset++] = (byte)(checksum >> 8);
        result[offset] = (byte)(checksum >> 0);

        return result;
    }

    // WTH ? Should delete ? 
    /// <summary> Attempt to improve compressability of the raw data by using adaptive filtering. </summary>
    private void AttemptCompressionOfRawData(byte[] rawData)
    {
        int bytesPerScanline = 1 + (bytesPerPixel * width);
        int scanlineCount = rawData.Length / bytesPerScanline;
        byte[] scanData = new byte[bytesPerScanline - 1];

        for (int scanlineRowIndex = 0; scanlineRowIndex < scanlineCount; scanlineRowIndex++)
        {
            int sourceIndex = (scanlineRowIndex * bytesPerScanline) + 1;
            Array.Copy(rawData, sourceIndex, scanData, 0, bytesPerScanline - 1);
            int noneFilterSum = 0;
            for (int i = 0; i < scanData.Length; i++)
            {
                noneFilterSum += scanData[i];
            }

            int leftFilterSum = 0;
            for (int i = 0; i < scanData.Length; i++)
            {
                // WTH ? 
                // WAS EMPTY in orioginal code! 
            }

             // A heuristic approach is to use adaptive filtering as follows: 
             //     independently for each row, apply all five filters and select the filter that produces the
             //     smallest sum of absolute values per row. 
        }
    }
}
