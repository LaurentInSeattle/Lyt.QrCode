namespace Lyt.QrCode.Encoder;

internal class QrSegment
{
    /// <summary>
    /// Initializes a QR code segment with the specified attributes and data.
    /// The character count <paramref name="numChars"/> must agree with the mode and the bit array length,
    /// but the constraint isn't checked. The specified bit array is cloned.
    /// </summary>
    /// <param name="mode">The segment mode used to encode this segment.</param>
    /// <param name="numChars">The data length in characters or bytes (depending on the segment mode).</param>
    /// <param name="data">The data bits.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="numChars"/> is negative.</exception>
    internal QrSegment(Mode mode, int numChars, BitArray data)
    {
        if (numChars < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numChars), "Invalid value, cannot be negative.");
        }

        this.EncodingMode = mode;
        this.NumChars = numChars;

        // WHY ?? Make defensive copy
        this.data = (BitArray)data.Clone();  
    }

    /// <summary>The encoding mode of this segment.</summary>
    /// <value>Encoding mode.</value>
    internal Mode EncodingMode { get; }

    /// <summary> 
    /// The length of this segment's unencoded data, measured in characters for numeric/alphanumeric/kanji mode,
    /// bytes for byte mode, and 0 for ECI mode. : Different from the data's bit length.
    /// </summary>
    /// <value>Length of the segment's unencoded data.</value>
    internal int NumChars { get; }

    // The data bits of this segment. Accessed through GetData().
    private readonly BitArray data;

    /// <summary> Returns a copy of this segment's data bits. </summary>
    public BitArray GetData()
        => (BitArray) this.data.Clone();  // Make defensive copy => WHY ??? NeCESSARY ? 
    
    /// <summary>
    /// Creates a list of zero or more segments representing the specified text string.
    /// The text may contain the full range of Unicode characters.
    /// The result may consist of multiple segments with various encoding modes in order to minimize the length of the bit stream.
    /// </summary>
    /// <param name="text">The text to be encoded.</param>
    /// <returns>The created mutable list of segments representing the specified text.</returns>
    /// <exception cref="ArgumentNullException"><c>text</c> is <c>null</c>.</exception>
    /// TODO: 
    /// <remarks> The current implementation does not create multiple segments. </remarks>
    internal static List<QrSegment> MakeSegments(string text)
    {
        // Select the most efficient segment encoding automatically
        var result = new List<QrSegment>();
        if (text.IsNumeric())
        {
            result.Add(QrSegment.MakeNumeric(text));
        }
        else if (text.IsAlphanumeric())
        {
            result.Add(MakeAlphanumeric(text));
        }
        else
        {
            result.Add(MakeBytes(Encoding.UTF8.GetBytes(text)));
        }

        return result;
    }

    /// <summary>
    /// Creates a segment representing the specified binary data
    /// encoded in byte mode. All input byte arrays are acceptable.
    /// Any text string can be converted to UTF-8 bytes (using <c>Encoding.UTF8.GetBytes(str)</c>)
    /// and encoded as a byte mode segment.
    /// </summary>
    /// <param name="data">The binary data to encode.</param>
    /// <returns>The created segment containing the specified data.</returns>
    public static QrSegment MakeBytes(byte[] data)
    {
        var bitArray = new BitArray(0);
        foreach (byte b in data)
        {
            bitArray.AppendBits(b, 8);
        }

        return new QrSegment(Mode.Byte, data.Length, bitArray);
    }

    /// <summary>
    /// Creates a segment representing the specified string of decimal digits.
    /// The segment is encoded in numeric mode.
    /// </summary>
    /// <param name="digits">The text to encode, consisting of digits from 0 to 9 only.</param>
    /// <returns>The created segment containing the text.</returns>
    /// <exception cref="ArgumentNullException"><c>digits</c> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><c>digits</c> contains non-digit characters</exception>
    private static QrSegment MakeNumeric(string digits)
    {
        var bitArray = new BitArray(0);
        for (int i = 0; i < digits.Length;)
        {
            // Consume up to 3 digits per iteration
            int n = Math.Min(digits.Length - i, 3);
            bitArray.AppendBits(uint.Parse(digits.Substring(i, n)), n * 3 + 1);
            i += n;
        }

        return new QrSegment(Mode.Numeric, digits.Length, bitArray);
    }

    /// <summary>
    /// Creates a segment representing the specified text string. The segment is encoded in alphanumeric mode.
    /// Allowed characters are: 0 to 9, A to Z (uppercase only), space,
    /// dollar, percent, asterisk, plus, hyphen, period, slash, colon.
    /// </summary>
    /// <param name="text">The text to encode, consisting of allowed characters only.</param>
    /// <exception cref="ArgumentOutOfRangeException"><c>text</c> contains non-encodable characters.</exception>
    private static QrSegment MakeAlphanumeric(string text)
    {
        var bitArray = new BitArray(0);
        int i;
        for (i = 0; i <= text.Length - 2; i += 2)
        {
            // Process groups of 2
            uint temp = text[i].IndexOfAlphanumeric() * 45;
            temp += text[i + 1].IndexOfAlphanumeric();
            bitArray.AppendBits(temp, 11);
        }
        if (i < text.Length)  // 1 character remaining
        {
            bitArray.AppendBits(text[i].IndexOfAlphanumeric(), 6);
        }

        return new QrSegment(Mode.Alphanumeric, text.Length, bitArray);
    }

    /// <summary>
    /// Calculates the number of bits needed to encode the given segments.
    /// <para>
    /// Returns a non-negative number if successful. Otherwise returns -1 if a segment has too
    /// many characters to fit its length field, or the total bits exceeds int.MaxValue.
    /// </para>
    /// </summary>
    /// <param name="segments">The segements.</param>
    /// <param name="version">The version number.</param>
    /// <returns>The number of bits, or -1.</returns>
    internal static int GetTotalBits(List<QrSegment> segments, int version)
    {
        long result = 0;
        foreach (var seg in segments)
        {
            int ccBits = seg.EncodingMode.NumCharCountBits(version);
            if (seg.NumChars >= 1 << ccBits)
            {
                return -1;  // The segment's length doesn't fit the field's bit width
            }

            result += 4L + ccBits + seg.data.Length;
            if (result > int.MaxValue)
            {
                return -1;  // The sum will overflow an int type
            }
        }

        return (int)result;
    }

    /// <summary> Calculates the total number of bits required to encode a data segment in a QR code. </summary>
    /// <param name="numChars">The number of characters in the data segment to be encoded. For byte mode, it is the number of bytes.</param>
    /// <param name="mode">The encoding mode used for the data segment.</param>
    /// <param name="version">The QR code version.</param>
    /// <returns>The total number of bits required to encode the segment, including the mode indicator, character count
    /// indicator, and data bits. Returns -1 if the number of characters exceeds the maximum allowed for the
    /// specified mode and version.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the specified mode is not supported for this calculation.</exception>
    internal static int GetTotalBits(int numChars, Mode mode, int version)
    {
        int ccBits = mode.NumCharCountBits(version);
        if (numChars >= 1 << ccBits)
        {
            return -1;  // The segment's length doesn't fit the field's bit width
        }

        int dataBits;
        if (mode == Mode.Numeric)
        {
            dataBits = numChars / 3 * 10 + (numChars % 3 == 0 ? 0 : (numChars % 3 * 3 + 1));
        }
        else if (mode == Mode.Alphanumeric)
        {
            dataBits = numChars / 2 * 11 + (numChars % 2 == 0 ? 0 : 6);
        }
        else if (mode == Mode.Byte)
        {
            dataBits = numChars * 8;
        }
        else if (mode == Mode.Kanji)
        {
            dataBits = numChars * 13;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(mode), "Unsupported mode for this calculation");
        }

        return 4 + ccBits + dataBits;
    }

}
