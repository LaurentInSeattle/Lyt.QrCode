namespace Lyt.QrCode.Decoder;

/// <summary>
/// <p>See ISO 18004:2006, 6.4.1, Tables 2 and 3. This enum encapsulates the various modes in which
/// data can be encoded to bits in the QR code standard.</p>
/// </summary>
public sealed class Mode
{
    /// <summary> Gets the mode's name. </summary>
    public Names Name { get; private set; }

    /// <summary> Enumeration for encoding modes </summary>
    public enum Names
    {
        TERMINATOR,

        /// <summary> numeric encoding </summary>
        NUMERIC,
        /// <summary> alpha-numeric encoding </summary>
        ALPHANUMERIC,
        /// <summary> structured append, not supported  </summary>
        STRUCTURED_APPEND,
        /// <summary> byte mode encoding </summary>
        BYTE,
        /// <summary> ECI segment </summary>
        ECI,
        /// <summary> Kanji mode </summary>
        KANJI,
        /// <summary> FNC1 char, first position </summary>
        FNC1_FIRST_POSITION,
        /// <summary> FNC1 char, second position </summary>
        FNC1_SECOND_POSITION,
        /// <summary> Hanzi mode </summary>
        HANZI
    }

    /// <summary> Not really a mode... </summary>
    public static readonly Mode TERMINATOR = new([0, 0, 0], 0x00, Names.TERMINATOR);

    public static readonly Mode NUMERIC = new([10, 12, 14], 0x01, Names.NUMERIC);
    public static readonly Mode ALPHANUMERIC = new([9, 11, 13], 0x02, Names.ALPHANUMERIC);
    /// <summary> Not supported </summary>
    public static readonly Mode STRUCTURED_APPEND = new([0, 0, 0], 0x03, Names.STRUCTURED_APPEND);
    public static readonly Mode BYTE = new([8, 16, 16], 0x04, Names.BYTE);
    /// <summary> character counts don't apply </summary>
    public static readonly Mode ECI = new([0, 0, 0], 0x07, Names.ECI);
    public static readonly Mode KANJI = new([8, 10, 12], 0x08, Names.KANJI);
    public static readonly Mode FNC1_FIRST_POSITION = new([0, 0, 0], 0x05, Names.FNC1_FIRST_POSITION);
    public static readonly Mode FNC1_SECOND_POSITION = new([0, 0, 0], 0x09, Names.FNC1_SECOND_POSITION);

    /// <summary> See GBT 18284-2000; "Hanzi" is a transliteration of this mode name. </summary>
    public static readonly Mode HANZI = new([8, 10, 12], 0x0D, Names.HANZI);

    private readonly int[] characterCountBitsForVersions;

    private Mode(int[] characterCountBitsForVersions, int bits, Names name)
    {
        this.characterCountBitsForVersions = characterCountBitsForVersions;
        this.Bits = bits;
        this.Name = name;
    }

    /// Returns the Mode encoded by these bits
    /// <exception cref="ArgumentException">if bits do not correspond to a known mode</exception>
    public static Mode FromBits(int bits)
    {
        return bits switch
        {
            0x0 => TERMINATOR,

            0x1 => NUMERIC,
            0x2 => ALPHANUMERIC,
            0x3 => STRUCTURED_APPEND, // Not supported 
            0x4 => BYTE,
            0x5 => FNC1_FIRST_POSITION,
            0x7 => ECI,
            0x8 => KANJI,
            0x9 => FNC1_SECOND_POSITION,
            0xD => HANZI,// 0xD is defined in GBT 18284-2000, may not be supported in foreign country

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

        return characterCountBitsForVersions[offset];
    }

    /// <summary> Gets the bits. </summary>
    public int Bits { get; private set; }

    /// <summary> Returns a <see cref="System.String"/> that represents this instance. </summary>
    public override String ToString() => this.Name.ToString();
}