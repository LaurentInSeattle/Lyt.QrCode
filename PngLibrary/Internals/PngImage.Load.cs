namespace Lyt.Png;

public partial class PngImage
{
    private readonly byte[] data;
    private readonly int bytesPerPixel;
    private readonly Palette? palette;
    private readonly bool hasTransparencyChunk;

    // These fields are cached from the header for faster access when manipulating pixels.
    private readonly int height;
    private readonly int width;
    private readonly ColorType colorType;
    private readonly int rowOffset;
    private readonly int bitDepth;

    // These fields are used when saving the image
    // AI Generated : 
    // ??? - to determine whether we can use a palette and if so, how many colors it should contain.
    private bool hasTooManyColorsForPalette;

    private readonly int backgroundColorInt;
    private readonly Dictionary<int, int> colorCounts = [];
    private readonly List<(string keyword, byte[] data)> textualMetadata = [];

    internal PngImage(
        ImageHeader imageHeader, byte[] data, int bytesPerPixel, Palette? palette, bool hasTransparencyChunk)
    {
        this.Header = imageHeader;
        this.data = data;
        this.bytesPerPixel = bytesPerPixel;
        this.palette = palette;
        this.hasTransparencyChunk = hasTransparencyChunk;

        // These fields are cached from the header for faster access when manipulating pixels.
        this.height = imageHeader.Height;
        this.width = imageHeader.Width;
        this.colorType = imageHeader.ColorType;
        this.rowOffset = imageHeader.InterlaceMethod == InterlaceMethod.Adam7 ? 0 : 1;
        this.bitDepth = imageHeader.BitDepth;

        // Initialize palette related fields
        this.backgroundColorInt = Pixel.ToColorInt(0, 0, 0, this.HasAlphaChannel ? (byte)0 : byte.MaxValue);
        if (this.colorCounts.Count == 0)
        {
            this.colorCounts = new Dictionary<int, int>()
            {
                { this.backgroundColorInt, this.width * this.height }
            };
        } 
    }

    /// <summary> The header data from the PNG image. </summary>
    internal ImageHeader Header { get; }

    internal static PngImage FromBitmapInternal(byte[] sourcePixels, int width, int height, bool hasAlphaChannel)
    {
        const int bitDepthPerChannel = 8;
        int bytesPerPixel = hasAlphaChannel ? 4 : 3;
        var header = new ImageHeader(
            width, height,
            bitDepthPerChannel,
            hasAlphaChannel ? ColorType.ColorUsed | ColorType.AlphaChannelUsed : ColorType.ColorUsed,
            CompressionMethod.DeflateWithSlidingWindow,
            FilterMethod.AdaptiveFiltering,
            InterlaceMethod.None);
        int length = height * width * bytesPerPixel + height;
        byte[] imagePixels = new byte[length];
        var newImage = new PngImage(header, imagePixels, bytesPerPixel, null, hasTransparencyChunk: false);

        // CONSIDER: Speed up by using unsafe code and pointers to copy the pixel data
        int offset = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte r = sourcePixels[offset];
                byte g = sourcePixels[offset + 1];
                byte b = sourcePixels[offset + 2];
                Pixel pixel =
                    hasAlphaChannel ?
                        new Pixel(r, g, b, sourcePixels[offset + 3], false) :
                        new Pixel(r, g, b);
                newImage.SetPixel(pixel, x, y);
                offset += bytesPerPixel;
            }
        }

        return newImage;
    }

    internal static PngImage OpenInternal(Stream stream)
    {
        if (!stream.CanRead)
        {
            throw new ArgumentException($"The provided stream of type {stream.GetType().FullName} is not readable.");
        }

        bool HasValidPngHeader()
        {
            for (int i = 0; i < ImageHeader.ExpectedHeader.Length; i++)
            {
                if (stream.ReadByte() != ImageHeader.ExpectedHeader[i])
                {
                    return false;
                }
            }

            return true;
        }

        if (!HasValidPngHeader())
        {
            throw new ArgumentException($"The provided stream did not start with the PNG header.");
        }

        bool TryReadChunkHeader([NotNullWhen(true)] out ChunkHeader? chunkHeader)
        {
            chunkHeader = null;
            long position = stream.Position;
            if (!stream.TryReadHeaderBytes(out byte[] headerBytes))
            {
                return false;
            }

            int length = headerBytes.ReadBigEndianInt32(0);
            string name = Encoding.ASCII.GetString(headerBytes, 4, 4);
            chunkHeader = new ChunkHeader(position, length, name);
            return true;
        }

        ImageHeader ReadImageHeader(byte[] crc)
        {
            if (!TryReadChunkHeader(out var header))
            {
                throw new ArgumentException("The provided stream did not contain a single chunk.");
            }

            if (header.Name != "IHDR")
            {
                throw new ArgumentException($"The first chunk was not the IHDR chunk: {header}.");
            }

            if (header.Length != 13)
            {
                throw new ArgumentException($"The first chunk did not have a length of 13 bytes: {header}.");
            }

            byte[] ihdrBytes = new byte[13];
            int read = stream.Read(ihdrBytes, 0, ihdrBytes.Length);
            if (read != 13)
            {
                throw new InvalidOperationException($"Did not read 13 bytes for the IHDR, only found: {read}.");
            }

            read = stream.Read(crc, 0, crc.Length);
            if (read != 4)
            {
                throw new InvalidOperationException($"Did not read 4 bytes for the CRC, only found: {read}.");
            }

            int width = ihdrBytes.ReadBigEndianInt32(0);
            int height = ihdrBytes.ReadBigEndianInt32(4);
            byte bitDepth = ihdrBytes[8];
            byte colorType = ihdrBytes[9];
            byte compressionMethod = ihdrBytes[10];
            byte filterMethod = ihdrBytes[11];
            byte interlaceMethod = ihdrBytes[12];

            return new ImageHeader(
                width,
                height,
                bitDepth,
                (ColorType)colorType,
                (CompressionMethod)compressionMethod,
                (FilterMethod)filterMethod,
                (InterlaceMethod)interlaceMethod);
        }

        byte[] crc = new byte[4];
        var imageHeader = ReadImageHeader(crc);
        bool hasEncounteredImageEnd = false;

        Palette? palette = null;

        using var output = new MemoryStream();
        using (var memoryStream = new MemoryStream())
        {
            while (TryReadChunkHeader(out var header))
            {
                if (hasEncounteredImageEnd)
                {
                    throw new InvalidOperationException($"Found another chunk {header} after already reading the IEND chunk.");
                }

                byte[] bytes = new byte[header.Length];
                int read = stream.Read(bytes, 0, bytes.Length);
                if (read != bytes.Length)
                {
                    throw new InvalidOperationException($"Did not read {header.Length} bytes for the {header} header, only found: {read}.");
                }

                if (header.IsCritical)
                {
                    switch (header.Name)
                    {
                        case "PLTE":
                            if (header.Length % 3 != 0)
                            {
                                throw new InvalidOperationException($"Palette data must be multiple of 3, got {header.Length}.");
                            }

                            // Ignore palette data unless the header.ColorType indicates that the image is paletted.
                            if (imageHeader.ColorType.HasFlag(ColorType.PaletteUsed))
                            {
                                palette = new Palette(bytes);
                            }

                            break;

                        case "IDAT":
                            memoryStream.Write(bytes, 0, bytes.Length);
                            break;

                        case "IEND":
                            hasEncounteredImageEnd = true;
                            break;

                        default:
                            throw new NotSupportedException($"Encountered critical header {header} which was not recognised.");
                    }
                }
                else
                {
                    switch (header.Name)
                    {
                        case "tRNS":
                            // Add transparency to palette, if the PLTE chunk has been read.
                            palette?.SetAlphaValues(bytes);
                            break;
                    }
                }

                read = stream.Read(crc, 0, crc.Length);
                if (read != 4)
                {
                    throw new InvalidOperationException($"Did not read 4 bytes for the CRC, only found: {read}.");
                }

                // Why casting to int ?
                // Because Crc32.Calculate returns a uint, but the CRC in the file is 4 bytes which we read into a byte
                // array. We need to compare these two values, and since the CRC in the file is effectively an
                // unsigned 32-bit integer, we can safely cast the result of Crc32.
                int result = (int)Crc32.Calculate(Encoding.ASCII.GetBytes(header.Name), bytes);
                int crcActual = (crc[0] << 24) + (crc[1] << 16) + (crc[2] << 8) + crc[3];
                if (result != crcActual)
                {
                    throw new InvalidOperationException($"CRC calculated {result} did not match file {crcActual} for chunk: {header.Name}.");
                }
            }

            memoryStream.Flush();
            memoryStream.Seek(2, SeekOrigin.Begin);

            using var deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress);
            deflateStream.CopyTo(output);
            deflateStream.Close();
        }

        byte[] bytesOut = output.ToArray();
        var (bytesPerPixel, samplesPerPixel) = imageHeader.GetBytesAndSamplesPerPixel();
        bytesOut = PngImage.Decode(bytesOut, imageHeader, bytesPerPixel, samplesPerPixel);

        return
            new PngImage(
                imageHeader, bytesOut, bytesPerPixel, palette, palette?.HasAlphaValues ?? false);
    }

    internal static byte[] Decode(byte[] decompressedData, ImageHeader header, byte bytesPerPixel, byte samplesPerPixel)
    {
        void ReverseFilter(FilterType type, int previousRowStartByteAbsolute, int rowStartByteAbsolute, int byteAbsolute, int rowByteIndex, int bytesPerPixel)
        {
            byte GetLeftByteValue()
            {
                int leftIndex = rowByteIndex - bytesPerPixel;
                byte leftValue = leftIndex >= 0 ? decompressedData[rowStartByteAbsolute + leftIndex] : (byte)0;
                return leftValue;
            }

            byte GetAboveByteValue()
            {
                int upIndex = previousRowStartByteAbsolute + rowByteIndex;
                return upIndex >= 0 ? decompressedData[upIndex] : (byte)0;
            }

            byte GetAboveLeftByteValue()
            {
                int index = previousRowStartByteAbsolute + rowByteIndex - bytesPerPixel;
                return
                    index < previousRowStartByteAbsolute || previousRowStartByteAbsolute < 0 ?
                        (byte)0 :
                        decompressedData[index];
            }

            // Moved out of the switch for performance.
            if (type == FilterType.Up)
            {
                int above = previousRowStartByteAbsolute + rowByteIndex;
                if (above < 0)
                {
                    return;
                }

                decompressedData[byteAbsolute] += decompressedData[above];
                return;
            }

            if (type == FilterType.Sub)
            {
                int leftIndex = rowByteIndex - bytesPerPixel;
                if (leftIndex < 0)
                {
                    return;
                }

                decompressedData[byteAbsolute] += decompressedData[rowStartByteAbsolute + leftIndex];
                return;
            }

            /// <summary>
            /// Computes a simple linear function of the three neighboring pixels (left, above, upper left),
            /// then chooses as predictor the neighboring pixel closest to the computed value.
            /// </summary>
            static byte GetPaethValue(byte a, byte b, byte c)
            {
                int p = a + b - c;
                int pa = Math.Abs(p - a);
                int pb = Math.Abs(p - b);
                int pc = Math.Abs(p - c);

                if (pa <= pb && pa <= pc)
                {
                    return a;
                }

                return pb <= pc ? b : c;
            }

            switch (type)
            {
                case FilterType.None:
                    return;

                case FilterType.Average:
                    decompressedData[byteAbsolute] += (byte)((GetLeftByteValue() + GetAboveByteValue()) / 2);
                    break;

                case FilterType.Paeth:
                    byte a = GetLeftByteValue();
                    byte b = GetAboveByteValue();
                    byte c = GetAboveLeftByteValue();
                    decompressedData[byteAbsolute] += GetPaethValue(a, b, c);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        switch (header.InterlaceMethod)
        {
            case InterlaceMethod.None:
                {
                    int bytesPerScanline = header.BytesPerScanline(samplesPerPixel);
                    int currentRowStartByteAbsolute = 1;
                    for (int rowIndex = 0; rowIndex < header.Height; rowIndex++)
                    {
                        var filterType = (FilterType)decompressedData[currentRowStartByteAbsolute - 1];
                        int previousRowStartByteAbsolute = (rowIndex) + (bytesPerScanline * (rowIndex - 1));
                        int end = currentRowStartByteAbsolute + bytesPerScanline;
                        for (int currentByteAbsolute = currentRowStartByteAbsolute; currentByteAbsolute < end; currentByteAbsolute++)
                        {
                            ReverseFilter(filterType, previousRowStartByteAbsolute, currentRowStartByteAbsolute, currentByteAbsolute, currentByteAbsolute - currentRowStartByteAbsolute, bytesPerPixel);
                        }

                        currentRowStartByteAbsolute += bytesPerScanline + 1;
                    }

                    return decompressedData;
                }

            case InterlaceMethod.Adam7:
                {
                    int pixelsPerRow = header.Width * bytesPerPixel;
                    byte[] newBytes = new byte[header.Height * pixelsPerRow];
                    int i = 0;
                    int previousStartRowByteAbsolute = -1;

                    // 7 passes
                    for (int pass = 0; pass < 7; pass++)
                    {
                        int numberOfScanlines = Adam7.GetNumberOfScanlinesInPass(header, pass);
                        int numberOfPixelsPerScanline = Adam7.GetPixelsPerScanlineInPass(header, pass);
                        if (numberOfScanlines <= 0 || numberOfPixelsPerScanline <= 0)
                        {
                            continue;
                        }

                        for (int scanlineIndex = 0; scanlineIndex < numberOfScanlines; scanlineIndex++)
                        {
                            var filterType = (FilterType)decompressedData[i++];
                            int rowStartByte = i;
                            for (int j = 0; j < numberOfPixelsPerScanline; j++)
                            {
                                var (x, y) = Adam7.GetPixelIndexForScanlineInPass(pass, scanlineIndex, j);
                                for (int k = 0; k < bytesPerPixel; k++)
                                {
                                    int byteLineNumber = (j * bytesPerPixel) + k;
                                    ReverseFilter(filterType, previousStartRowByteAbsolute, rowStartByte, i, byteLineNumber, bytesPerPixel);
                                    i++;
                                }

                                int start = pixelsPerRow * y + x * bytesPerPixel;
                                Array.ConstrainedCopy(
                                    decompressedData, rowStartByte + j * bytesPerPixel, newBytes, start, bytesPerPixel);
                            }

                            previousStartRowByteAbsolute = rowStartByte;
                        }
                    }

                    return newBytes;
                }

            default:
                throw new ArgumentOutOfRangeException($"Invalid interlace method: {header.InterlaceMethod}.");
        }
    }
}
