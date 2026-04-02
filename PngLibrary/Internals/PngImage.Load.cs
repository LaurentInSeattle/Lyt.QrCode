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
}
