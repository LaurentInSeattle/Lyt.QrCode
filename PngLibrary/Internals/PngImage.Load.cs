namespace Lyt.Png;

public partial class PngImage
{
    private readonly RawPngData data;
    private readonly bool hasTransparencyChunk;

    internal PngImage(ImageHeader header, RawPngData data, bool hasTransparencyChunk)
    {
        this.Header = header;
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.hasTransparencyChunk = hasTransparencyChunk;
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
            if (!StreamHelper.TryReadHeaderBytes(stream, out var headerBytes))
            {
                return false;
            }

            int length = StreamHelper.ReadBigEndianInt32(headerBytes, 0);
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

            int width = StreamHelper.ReadBigEndianInt32(ihdrBytes, 0);
            int height = StreamHelper.ReadBigEndianInt32(ihdrBytes, 4);
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
                imageHeader,
                new RawPngData(bytesOut, bytesPerPixel, palette, imageHeader),
                palette?.HasAlphaValues ?? false);
    }
}
