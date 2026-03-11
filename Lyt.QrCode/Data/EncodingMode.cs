namespace Lyt.QrCode.Data;
/// <summary>
/// <p>See ISO 18004:2006, 6.4.1, Tables 2 and 3. This enum encapsulates the various modes in which
/// data can be encoded to bits in the QR code standard.</p>
/// </summary>
public sealed class EncodingMode
{
    /// <summary> Gets the mode's name. </summary>
    public Names Name { get; private set; }

    /// <summary> Enumeration for encoding modes </summary>
    public enum Names
    {
        Terminator,

        /// <summary> numeric encoding </summary>
        Numeric,
        /// <summary> alpha-numeric encoding </summary>
        Alphanumeric,
        /// <summary> structured append, not supported  </summary>
        StructuredAppend,
        /// <summary> byte mode encoding </summary>
        Byte,
        /// <summary> ECI segment </summary>
        Eci,
        /// <summary> Kanji mode </summary>
        Kanji,
        /// <summary> FNC1 char, first position </summary>
        Fnc1FirstPosition,
        /// <summary> FNC1 char, second position </summary>
        Fnc1SecondPosition,
        /// <summary> Hanzi mode </summary>
        Hanzi
    }

    /// <summary> Not supported </summary>
    public static readonly EncodingMode StructuredAppend = new([0, 0, 0], 0x03, Names.StructuredAppend);

    /// <summary> Not really a mode... character counts don't apply: all zeroes </summary>
    public static readonly EncodingMode Terminator = new([0, 0, 0], 0x00, Names.Terminator);
    public static readonly EncodingMode Fnc1FirstPosition = new([0, 0, 0], 0x05, Names.Fnc1FirstPosition);
    public static readonly EncodingMode Fnc1SecondPosition = new([0, 0, 0], 0x09, Names.Fnc1SecondPosition);

    /// <summary> Actual modes... </summary>
    public static readonly EncodingMode Numeric = new([10, 12, 14], 0x01, Names.Numeric);
    public static readonly EncodingMode Alphanumeric = new([9, 11, 13], 0x02, Names.Alphanumeric);
    public static readonly EncodingMode Byte = new([8, 16, 16], 0x04, Names.Byte);    
    public static readonly EncodingMode Eci = new([0, 0, 0], 0x07, Names.Eci); // character counts don't apply 
    public static readonly EncodingMode Kanji = new([8, 10, 12], 0x08, Names.Kanji);

    /// <summary> See GBT 18284-2000; "Hanzi" is a transliteration of this mode name. </summary>
    public static readonly EncodingMode Hanzi = new([8, 10, 12], 0x0D, Names.Hanzi);

    private readonly int[] characterCountBitsForVersions;

    private EncodingMode(int[] characterCountBitsForVersions, int bits, Names name)
    {
        this.characterCountBitsForVersions = characterCountBitsForVersions;
        this.Bits = bits;
        this.Name = name;
    }

    /// Returns the Mode encoded by these bits
    /// <exception cref="ArgumentException">if bits do not correspond to a known mode</exception>
    public static EncodingMode FromBits(int bits)
    {
        return bits switch
        {
            0x0 => Terminator,

            0x1 => Numeric,
            0x2 => Alphanumeric,
            0x3 => StructuredAppend, // Not supported 
            0x4 => Byte,
            0x5 => Fnc1FirstPosition,
            0x7 => Eci,
            0x8 => Kanji,
            0x9 => Fnc1SecondPosition,
            0xD => Hanzi, // 0xD is defined in GBT 18284-2000, may not be supported in foreign country

            _ => throw new ArgumentException("Undefined bits", nameof(bits))
        };
    }

    /// <returns> number of bits used, in this QR Code symbol {@link Version}, to encode the
    /// count of characters that will follow encoded in this {@link Mode}
    /// </returns>
    /// <param name="version"> QR version </param>
    public int GetCharacterCountBits(QrVersion version)
    {
        if (this.characterCountBitsForVersions == null)
        {
            throw new ArgumentException("Character count doesn't apply to this mode");
        }

        int number = version.VersionNumber;
        int offset;
        if (number <= 9)
        {
            offset = 0;
        }
        else if (number <= 26)
        {
            offset = 1;
        }
        else
        {
            offset = 2;
        }

        return this.characterCountBitsForVersions[offset];
    }

    /// <summary> Gets the mode bits. </summary>
    public int Bits { get; private set; }

    /// <summary>
    /// Returns the bit length of the character count in the QR segment header
    /// for the specified QR code version. The result is in the range [0, 16].
    /// </summary>
    /// <param name="versionNumber">the QR code version (between 1 and 40)</param>
    internal int NumCharCountBits(int versionNumber)
    {
        Debug.Assert(Lyt.QrCode.QrCode.MinVersion <= versionNumber && versionNumber <= Lyt.QrCode.QrCode.MaxVersion);
        return this.characterCountBitsForVersions[(versionNumber + 7) / 17];
    }

    /// <summary> Returns a <see cref="string"/> that represents this instance. </summary>
    public override string ToString() => this.Name.ToString();
}