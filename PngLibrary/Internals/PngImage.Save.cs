namespace Lyt.Png;

public partial class PngImage
{
    internal (int Width, int Height, int Channels, byte[] Pixels) ToRgba32Bitmap()
    {
        byte[] pixels = new byte[this.Width * this.Height * 4];
        if (this.Header.IsRgba32)
        {
            int offset = 0;
            for (int y = 0; y < this.Height; y++)
            {
                for (int x = 0; x < this.Width; x++)
                {
                    Pixel pixel = this.GetPixel(x, y);
                    pixels[offset++] = pixel.R;
                    pixels[offset++] = pixel.G;
                    pixels[offset++] = pixel.B;
                    pixels[offset++] = pixel.A;
                }
            }

            return (this.Width, this.Height, 4, pixels);
        }

        throw new Exception("Not a RGBA 32 image ");
    }

    internal (int Width, int Height, int Channels, byte[] Pixels) ToRgb24Bitmap()
    {
        byte[] pixels = new byte[this.Width * this.Height * 3];
        if (this.Header.IsRgb24)
        {
            int offset = 0;
            for (int y = 0; y < this.Height; y++)
            {
                for (int x = 0; x < this.Width; x++)
                {
                    Pixel pixel = this.GetPixel(x, y);
                    pixels[offset++] = pixel.R;
                    pixels[offset++] = pixel.G;
                    pixels[offset++] = pixel.B;
                }
            }

            return (this.Width, this.Height, 3, pixels);
        }

        throw new Exception("Not a RGB 24 image ");
    }

    /// <summary> Write the PNG file bytes to the provided stream. </summary>
    internal void Save(PngMemoryStream stream)
    {
        byte[]? palette = null;
        int dataLength = this.data.Length;
        int bitDepth = 8;

        if (!this.hasTooManyColorsForPalette && !this.HasAlphaChannel)
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

            int dataIndex = 0;

            for (int y = 0; y < height; y++)
            {
                // None filter - we don't use filtering for palette images.
                this.data[dataIndex++] = 0;

                for (int x = 0; x < width; x++)
                {
                    int index = ((y * width * bytesPerPixel) + y + 1) + (x * bytesPerPixel);

                    byte r = this.data[index++];
                    byte g = this.data[index++];
                    byte b = this.data[index];

                    int colorInt = Pixel.ToColorInt(r, g, b);
                    byte value = (byte)paletteColors.IndexOf(colorInt);

                    if (applyShift)
                    {
                        // apply mask and shift
                        int withinByteIndex = x % 2;

                        if (withinByteIndex == 1)
                        {
                            this.data[dataIndex] = (byte)(this.data[dataIndex] + value);
                            dataIndex++;
                        }
                        else
                        {
                            this.data[dataIndex] = (byte)(value << 4);
                        }
                    }
                    else
                    {
                        this.data[dataIndex++] = value;
                    }
                }
            }

            dataLength = dataIndex;
        }
        else
        {
            // AttemptCompressionOfthis.data(this.data);
        }

        stream.Write(ImageHeader.ExpectedHeader, 0, ImageHeader.ExpectedHeader.Length);
        stream.WriteChunkLength(13);
        stream.WriteChunkHeader(ImageHeader.HeaderBytes);
        stream.WriteBigEndianInt32(this.width);
        stream.WriteBigEndianInt32(this.height);
        stream.WriteByte((byte)bitDepth);

        var colorType = ColorType.ColorUsed;
        if (this.HasAlphaChannel)
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

        byte[] imageData = Compress(this.data, dataLength);
        stream.WriteChunkLength(imageData.Length);
        stream.WriteChunkHeader(Encoding.ASCII.GetBytes("IDAT"));
        stream.Write(imageData, 0, imageData.Length);
        stream.WriteCrc();

        foreach (var storedString in textualMetadata)
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
}
