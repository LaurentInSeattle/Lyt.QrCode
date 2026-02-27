namespace Lyt.QrCode.Render;

/// <summary> Creates a PNG file from a given QR code. </summary>
internal sealed class PngBuilder
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] IHDR = [73, 72, 68, 82];
    private static readonly byte[] PLTE = [80, 76, 84, 69];
    private static readonly byte[] IDAT = [73, 68, 65, 84];
    private static readonly byte[] IEND = [73, 69, 78, 68];

    /// <summary> 
    /// Creates a PNG image for the given QR code.
    /// The PNG image uses indexed colors, 1 bit per pixel and a palette with entries for the foreground and background colors.
    /// </summary>
    /// <param name="qrCode">The QR code.</param>
    /// <param name="border">The border width, as a factor of the module (QR code pixel) size.</param>
    /// <param name="scale">The width and height, in pixels, of each module.</param>
    /// <param name="foreground">The foreground color (dark modules), in RGB value (little endian).</param>
    /// <param name="background">The background color (light modules), in RGB value (little endian).</param>
    /// <returns>A PNG image, as a byte array.</returns>
    internal static byte[] ToPngImage(QrCode qrCode, int scale, int border, int foreground = 0, int background = 0xFFFFFF)
    {
        int imageSize = (qrCode.Size + border * 2) * scale;
        var builder = new PngBuilder();
        builder.WriteHeader(imageSize, imageSize, 1, 3);
        builder.WritePalette([background, foreground]);
        builder.WriteData(CreateBitmap(qrCode, border, scale));
        builder.WriteEnd();
        return builder.GetBytes();
    }

    /// <summary> Creates an uncompressed 1-bit per pixel bitmap of the QR code. </summary>
    /// <param name="qrCode">The QR code</param>
    /// <param name="border">The border</param>
    /// <param name="scale">The scale</param>
    /// <returns>Bitmap, as a byte array</returns>
    private static byte[] CreateBitmap(QrCode qrCode, int border, int scale)
    {
        int size = qrCode.Size;
        int imageSize = (size + border * 2) * scale;

        // additional byte at the start for filter type
        int bytesPerLine = (imageSize + 7) / 8 + 1; 
        byte[] data = new byte[bytesPerLine * imageSize];

        for (int y = 0; y < size; y++)
        {
            int offset = (border + y) * scale * bytesPerLine;

            for (int x = 0; x < size; x++)
            {
                if (!qrCode.GetModule(x, y))
                {
                    continue;
                }

                int pos = (border + x) * scale;
                int end = pos + scale;

                // set pixels for module ('scale' times)
                for (; pos < end; pos++)
                {
                    int index = offset + pos / 8 + 1;
                    data[index] |= (byte)(0x80U >> (pos % 8));
                }
            }

            // replicate line 'scale' times
            for (int i = 1; i < scale; i++)
            {
                Array.Copy(data, offset, data, offset + i * bytesPerLine, bytesPerLine);
            }
        }

        return data;
    }

    private readonly MemoryStream stream = new();

    /// <summary> Returns the resulting PNG bytes. </summary>
    /// <returns>PNG file, as a byte array.</returns>
    private byte[] GetBytes()
    {
        byte[] bytes = stream.ToArray();
        SetCRC(bytes);
        return bytes;
    }

    /// <summary> Writes the PNG header (IHDR chunk). </summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="bitDepth">The bits per pixel.</param>
    /// <param name="colorType">The color type (see PNG specification).</param>
    private void WriteHeader(int width, int height, byte bitDepth, byte colorType)
    {
        this.stream.Write(Signature, 0, Signature.Length);
        this.WriteChunkStart(IHDR, 13);
        this.WriteIntBigEndian((uint)width);
        this.WriteIntBigEndian((uint)height);
        this.stream.WriteByte(bitDepth);
        this.stream.WriteByte(colorType);
        this.stream.WriteByte(0);
        this.stream.WriteByte(0);
        this.stream.WriteByte(0);
        this.WriteChunkEnd();
    }

    /// <summary> Writes the palette (PLTE chunk). </summary>
    /// <param name="palette">The color palettes as an array of RGB values.</param>
    private void WritePalette(int[] palette)
    {
        this.WriteChunkStart(PLTE, palette.Length * 3);
        foreach (int color in palette)
        {
            this.stream.WriteByte((byte)((color >> 16) & 0xFF));
            this.stream.WriteByte((byte)((color >> 8) & 0xFF));
            this.stream.WriteByte((byte)(color & 0xFF));
        }

        this.WriteChunkEnd();
    }

    /// <summary> Writes the pixel data (IDAT chunk). </summary>
    /// <param name="data">The pixel data.</param>
    private void WriteData(byte[] data)
    {
        byte[] compressedData = Deflate(data);
        this.WriteChunkStart(IDAT, compressedData.Length + 6);
        this.stream.WriteByte(0x78);
        this.stream.WriteByte(0x9C);
        this.stream.Write(compressedData, 0, compressedData.Length);
        uint adler = data.CalcAdler32(0, data.Length);
        this.WriteIntBigEndian(adler);
        this.WriteChunkEnd();
    }

    /// <summary> Writes the end chunk (IEND). </summary>
    private void WriteEnd()
    {
        this.WriteChunkStart(IEND, 0);
        this.WriteChunkEnd();
    }

    private static void SetCRC(byte[] bytes)
    {
        int chunkOffset = Signature.Length;
        while (chunkOffset < bytes.Length)
        {
            // calculate CRC
            int dataLength = 
                (bytes[chunkOffset] << 24) | 
                (bytes[chunkOffset + 1] << 16) | 
                (bytes[chunkOffset + 2] << 8) | 
                bytes[chunkOffset + 3];
            uint crc = bytes.CalcCrc32(chunkOffset + 4, dataLength + 4);
            int crcOffset = chunkOffset + 8 + dataLength;

            // set CRC
            bytes[crcOffset + 0] = (byte)(crc >> 24);
            bytes[crcOffset + 1] = (byte)(crc >> 16);
            bytes[crcOffset + 2] = (byte)(crc >> 8);
            bytes[crcOffset + 3] = (byte)crc;

            chunkOffset = crcOffset + 4;
        }
    }

    private void WriteChunkStart(byte[] type, int length)
    {
        this.WriteIntBigEndian((uint)length);
        this.stream.Write(type, 0, 4);
    }

    private void WriteChunkEnd()
    {
        this.stream.SetLength(stream.Length + 4);
        this.stream.Position += 4;
    }

    private void WriteIntBigEndian(uint value)
    {
        this.stream.WriteByte((byte)(value >> 24));
        this.stream.WriteByte((byte)(value >> 16));
        this.stream.WriteByte((byte)(value >> 8));
        this.stream.WriteByte((byte)value);
    }

    private static byte[] Deflate(byte[] data)
    {
        var output = new MemoryStream();
        using (var deflater = new DeflateStream(output, CompressionLevel.Optimal))
        {
            deflater.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }

}
