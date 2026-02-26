namespace Lyt.QrCode;

public sealed partial class QrCode
{
    // For use in getPenaltyScore(), when evaluating which mask is best.
    private const int PenaltyN1 = 3;
    private const int PenaltyN2 = 3;
    private const int PenaltyN3 = 40;
    private const int PenaltyN4 = 10;


    private static readonly byte[,] EccCodewordsPerBlock = 
    {
        // Version: (note that index 0 is for padding, and is set to an illegal value)
        //  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40     Error correction level		    { 255,  7, 10, 15, 20, 26, 18, 20, 24, 30, 18, 20, 24, 26, 30, 22, 24, 28, 30, 28, 28, 28, 28, 30, 30, 26, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },  // Low
        { 255,  7, 10, 15, 20, 26, 18, 20, 24, 30, 18, 20, 24, 26, 30, 22, 24, 28, 30, 28, 28, 28, 28, 30, 30, 26, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },  // Low
        { 255, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26, 26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28 },  // Medium
        { 255, 13, 22, 18, 26, 18, 24, 18, 22, 20, 24, 28, 26, 24, 20, 30, 24, 28, 28, 26, 30, 28, 30, 30, 30, 30, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },  // Quartile
        { 255, 17, 28, 22, 16, 22, 28, 26, 26, 24, 28, 24, 28, 22, 24, 24, 30, 28, 28, 26, 28, 30, 24, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 }   // High
    };

    private static readonly byte[,] NumErrorCorrectionBlocks = 
    {
        // Version: (note that index 0 is for padding, and is set to an illegal value)
        //  0, 1, 2, 3, 4, 5, 6, 7, 8, 9,10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40     Error correction level
        { 255, 1, 1, 1, 1, 1, 2, 2, 2, 2, 4,  4,  4,  4,  4,  6,  6,  6,  6,  7,  8,  8,  9,  9, 10, 12, 12, 12, 13, 14, 15, 16, 17, 18, 19, 19, 20, 21, 22, 24, 25 },  // Low
        { 255, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5,  5,  8,  9,  9, 10, 10, 11, 13, 14, 16, 17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49 },  // Medium
        { 255, 1, 1, 2, 2, 4, 4, 6, 6, 8, 8,  8, 10, 12, 16, 12, 17, 16, 18, 21, 20, 23, 23, 25, 27, 29, 34, 34, 35, 38, 40, 43, 45, 48, 51, 53, 56, 59, 62, 65, 68 },  // Quartile
        { 255, 1, 1, 2, 4, 4, 4, 5, 6, 8, 8, 11, 11, 16, 16, 18, 16, 19, 21, 25, 25, 25, 34, 30, 32, 35, 37, 40, 42, 45, 48, 51, 54, 57, 60, 63, 66, 70, 74, 77, 81 }   // High
    };

    // The modules of this QR code (false = light, true = dark).
    // Immutable after constructor finishes. Accessed through GetModule().
    private readonly bool[,] _modules;

    // Indicates function modules that are not subjected to masking. Discarded when constructor finishes.
    private readonly bool[,] _isFunction;

    /// <summary> Constructs a QR code with the specified version number, error correction level, data codeword bytes, and mask number.
    /// </summary>
    /// <param name="version">The version (size) to use (between 1 to 40).</param>
    /// <param name="ecl">The error correction level to use.</param>
    /// <param name="dataCodewords">The bytes representing segments to encode (without ECC).</param>
    /// <param name="mask">The mask pattern to use (either -1 for automatic selection, or a value from 0 to 7 for fixed choice).</param>
    /// <exception cref="ArgumentOutOfRangeException">The version or mask value is out of range,
    /// or the data has an invalid length for the specified version and error correction level.</exception>
    internal QrCode(int version, Ecc ecl, byte[] dataCodewords, int mask = -1)
    {
        // Check arguments and initialize fields
        if (version < MinVersion || version > MaxVersion)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version value out of range");
        }

        if (mask < -1 || mask > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(mask), "Mask value out of range");
        }

        this.Version = version;
        this.Size = version * 4 + 17;
        this.ErrorCorrectionLevel = ecl;

        this._modules = new bool[this.Size, this.Size];  // Initially all light
        this._isFunction = new bool[this.Size, this.Size];

        //// Compute ECC, draw modules, do masking
        //DrawFunctionPatterns();
        //var allCodewords = AddEccAndInterleave(dataCodewords);
        //DrawCodewords(allCodewords);

        //// Do masking
        //if (mask == -1)
        //{
        //    // Automatically choose best mask
        //    var minPenalty = int.MaxValue;
        //    for (uint i = 0; i < 8; i++)
        //    {
        //        ApplyMask(i);
        //        DrawFormatBits(i);
        //        var penalty = GetPenaltyScore();
        //        if (penalty < minPenalty)
        //        {
        //            mask = (int)i;
        //            minPenalty = penalty;
        //        }
        //        ApplyMask(i);  // Undoes the mask due to XOR
        //    }
        //}

        Debug.Assert(0 <= mask && mask <= 7);
        this.Mask = mask;

        //ApplyMask((uint)mask);  // Apply the final choice of mask
        //DrawFormatBits((uint)mask);  // Overwrite old format bits
        //_isFunction = null;
    }

    /// <summary>
    /// Creates a QR code representing the specified text using the specified error correction level.
    /// As a conservative upper bound, this function is guaranteed to succeed for strings with up to 738
    /// Unicode code points (not UTF-16 code units) if the low error correction level is used. The smallest possible
    /// QR code version (size) is automatically chosen. The resulting ECC level will be higher than the one
    /// specified if it can be achieved without increasing the size (version).
    /// </summary>
    /// <param name="text">The text to be encoded. The full range of Unicode characters may be used.</param>
    /// <param name="ecc">The minimum error correction level to use.</param>
    /// <returns>The created QR code instance representing the specified text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> .</exception>
    /// <exception cref="DataTooLongException">The text is too long to fit in the largest QR code size (version)
    /// at the specified error correction level.</exception>
    public static QrCode EncodeText(string text, Ecc ecc)
    {
        text.ThrowIfNullOrWhiteSpace();
        var segments = QrSegment.MakeSegments(text);
        return EncodeSegments(segments, ecc);
    }

    /// <summary>
    /// Creates a QR code representing the specified segments with the specified encoding parameters.
    /// The smallest possible QR code version (size) is used. The range of versions can be
    /// restricted by the <paramref name="minVersion"/> and <paramref name="maxVersion"/> parameters.
    /// If <paramref name="boostEcl"/> is <c>true</c>, the resulting ECC level will be higher than the
    /// one specified if it can be achieved without increasing the size (version).
    /// The QR code mask is usually automatically chosen. It can be explicitly set with the <paramref name="mask"/>
    /// parameter by using a value between 0 to 7 (inclusive). -1 is for automatic mode (which may be slow).
    /// This function allows the user to create a custom sequence of segments that switches
    /// between modes (such as alphanumeric and byte) to encode text in less space and gives full control over all
    /// encoding parameters.
    /// </summary>
    /// <param name="segments">The segments to encode.</param>
    /// <param name="ecl">The minimal or fixed error correction level to use .</param>
    /// <param name="minVersion">The minimum version (size) of the QR code (between 1 and 40).</param>
    /// <param name="maxVersion">The maximum version (size) of the QR code (between 1 and 40).</param>
    /// <param name="mask">The mask number to use (between 0 and 7), or -1 for automatic mask selection.</param>
    /// <param name="boostEcl">If <c>true</c> the ECC level wil be increased if it can be achieved without increasing the size (version).</param>
    /// <returns>The created QR code representing the segments.</returns>
    /// <exception cref="ArgumentOutOfRangeException">1 &#x2264; minVersion &#x2264; maxVersion &#x2264; 40
    /// or -1 &#x2264; mask &#x2264; 7 is violated.</exception>
    /// <exception cref="DataTooLongException">The segments are too long to fit in the largest QR code size (version)
    /// at the specified error correction level.</exception>
    internal static QrCode EncodeSegments(
        List<QrSegment> segments, 
        Ecc ecl, 
        int minVersion = QrCode.MinVersion, 
        int maxVersion = QrCode.MaxVersion, 
        int mask = -1, 
        bool boostEcl = true)
    {
        if (minVersion < QrCode.MinVersion || minVersion > maxVersion)
        {
            throw new ArgumentOutOfRangeException(nameof(minVersion), "Invalid value");
        }
        if (maxVersion > QrCode.MaxVersion)
        {
            throw new ArgumentOutOfRangeException(nameof(maxVersion), "Invalid value");
        }
        if (mask < -1 || mask > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(mask), "Invalid value");
        }

        // TODO: CONTINUE HERE 
        int version = 1;

        //// Find the minimal version number to use
        //int version, dataUsedBits;
        //for (version = minVersion; ; version++)
        //{
        //    var numDataBits = GetNumDataCodewords(version, ecl) * 8;  // Number of data bits available
        //    dataUsedBits = QrSegment.GetTotalBits(segments, version);
        //    if (dataUsedBits != -1 && dataUsedBits <= numDataBits)
        //    {
        //        break;  // This version number is found to be suitable
        //    }

        //    if (version < maxVersion)
        //    {
        //        continue;
        //    }

        //    // All versions in the range could not fit the given data
        //    var msg = "Segment too long";
        //    if (dataUsedBits != -1)
        //    {
        //        msg = $"Data length = {dataUsedBits} bits, Max capacity = {numDataBits} bits";
        //    }

        //    throw new DataTooLongException(msg);
        //}

        //Debug.Assert(dataUsedBits != -1);

        //// Increase the error correction level while the data still fits in the current version number
        //foreach (var newEcl in Ecc.AllValues)
        //{  // From low to high
        //    if (boostEcl && dataUsedBits <= GetNumDataCodewords(version, newEcl) * 8)
        //    {
        //        ecl = newEcl;
        //    }
        //}

        //// Concatenate all segments to create the data bit string
        //var ba = new BitArray(0);
        //foreach (var seg in segments)
        //{
        //    ba.AppendBits(seg.EncodingMode.ModeBits, 4);
        //    ba.AppendBits((uint)seg.NumChars, seg.EncodingMode.NumCharCountBits(version));
        //    ba.AppendData(seg.GetData());
        //}

        //Debug.Assert(ba.Length == dataUsedBits);

        //// Add terminator and pad up to a byte if applicable
        //var dataCapacityBits = GetNumDataCodewords(version, ecl) * 8;
        //Debug.Assert(ba.Length <= dataCapacityBits);
        //ba.AppendBits(0, Math.Min(4, dataCapacityBits - ba.Length));
        //ba.AppendBits(0, (8 - ba.Length % 8) % 8);
        //Debug.Assert(ba.Length % 8 == 0);

        //// Pad with alternating bytes until data capacity is reached
        //for (uint padByte = 0xEC; ba.Length < dataCapacityBits; padByte ^= 0xEC ^ 0x11)
        //{
        //    ba.AppendBits(padByte, 8);
        //}

        //// Pack bits into bytes in big endian
        //var dataCodewords = new byte[ba.Length / 8];
        //for (var i = 0; i < ba.Length; i++)
        //{
        //    if (ba.Get(i))
        //    {
        //        dataCodewords[i >> 3] |= (byte)(1 << (7 - (i & 7)));
        //    }
        //}

        // Create the QR code object
        return new QrCode(version, ecl, new byte [111] /*dataCodewords */, mask);
    }

}
