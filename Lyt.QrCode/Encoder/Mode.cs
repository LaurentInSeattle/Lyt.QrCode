namespace Lyt.QrCode.Encoder;

/// <summary> Segment encoding mode. Describes how text or binary data is encoded into bits. </summary>
internal sealed class Mode
{
    /// <summary> Numeric encoding mode. </summary>
    internal static readonly Mode Numeric = new (0x1, 10, 12, 14);

    /// <summary> Alphanumeric encoding mode. </summary>
    internal static readonly Mode Alphanumeric = new (0x2, 9, 11, 13);

    /// <summary> Byte encoding mode. </summary>
    internal static readonly Mode Byte = new (0x4, 8, 16, 16);

    /// <summary> Kanji encoding mode. </summary>
    internal static readonly Mode Kanji = new (0x8, 8, 10, 12);

    /// <summary> ECI encoding mode. </summary>
    internal static readonly Mode Eci = new (0x7, 0, 0, 0);

    /// <summary> Structured append encoding mode. </summary>
    internal static readonly Mode StructuredAppend = new (0x3, 0, 0, 0);

    /// <summary> Mode indicator value. 4 bit value in the QR segment header indicating the encoding mode. </summary>
    internal uint ModeBits { get; }

    /// <summary> 
    /// Array of character count bit length. Number of bits for character count in QR segment header.
    /// The three array values apply to versions 0 to 9, 10 to 26 and 27 to 40
    /// respectively. All array values are in the range [0, 16].
    /// </summary>
    private int[] NumBitsCharCount { get; }

    /// <summary>
    /// Returns the bit length of the character count in the QR segment header
    /// for the specified QR code version. The result is in the range [0, 16].
    /// </summary>
    /// <param name="version">the QR code version (between 1 and 40)</param>
    internal int NumCharCountBits(int version)
    {
        Debug.Assert(Lyt.QrCode.QrCode.MinVersion <= version && version <= Lyt.QrCode.QrCode.MaxVersion);
        return this.NumBitsCharCount[(version + 7) / 17];
    }

    // private constructor to initializes the constants
    private Mode(uint modeBits, params int[] numBitsCharCount)
    {
        this.ModeBits = modeBits;
        this.NumBitsCharCount = numBitsCharCount;
    }
}