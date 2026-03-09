namespace Lyt.QrCode.Utilities;

/// <summary> 
/// This class provides an easy abstraction to read bits at a time from a sequence of bytes, where the
/// number of bits read is not often a multiple of 8.
/// </summary>
/// <param name="bytes">
/// bytes from which this will read bits. Bits will be read from the first byte first.
/// Bits are read within a byte from most-significant to least-significant bit.
/// </param>
internal sealed class BitSource(byte[] bytes)
{
    private readonly byte[] bytes = bytes;

    /// <summary> index of next bit in current byte which would be read by the next call to {@link #readBits(int)}. </summary>
    public int BitOffset { get; private set; }

    /// <summary> index of next byte in input byte array which would be read by the next call to {@link #readBits(int)}. </summary>
    public int ByteOffset { get; private set; }

    /// Returns an integer representing the bits read. The bits will appear as the least-significant bits of the returned value
    /// <param name="numBits">Count of bits to read </param>
    /// <exception cref="ArgumentException">if numBits isn't in [1,32] or more than is available</exception>
    public int ReadBits(int numBits)
    {
        if (numBits < 1 || numBits > 32 || numBits > this.Available)
        {
            throw new ArgumentException(numBits.ToString(), nameof(numBits));
        }

        int result = 0;

        // First, read remainder from current byte
        if (this.BitOffset > 0)
        {
            int bitsLeft = 8 - this.BitOffset;
            int toRead = numBits < bitsLeft ? numBits : bitsLeft;
            int bitsToNotRead = bitsLeft - toRead;
            int mask = (0xFF >> (8 - toRead)) << bitsToNotRead;
            result = (bytes[this.ByteOffset] & mask) >> bitsToNotRead;
            numBits -= toRead;
            this.BitOffset += toRead;
            if (this.BitOffset == 8)
            {
                this.BitOffset = 0;
                this.ByteOffset++;
            }
        }

        // Next read whole bytes
        if (numBits > 0)
        {
            while (numBits >= 8)
            {
                result = (result << 8) | (bytes[this.ByteOffset] & 0xFF);
                this.ByteOffset++;
                numBits -= 8;
            }

            // Finally read a partial byte
            if (numBits > 0)
            {
                int bitsToNotRead = 8 - numBits;
                int mask = (0xFF >> bitsToNotRead) << bitsToNotRead;
                result = (result << numBits) | ((bytes[this.ByteOffset] & mask) >> bitsToNotRead);
                this.BitOffset += numBits;
            }
        }

        return result;
    }

    /// The count of bits that can be read successfully 
    public int Available => 8 * (this.bytes.Length - this.ByteOffset) - this.BitOffset;
}