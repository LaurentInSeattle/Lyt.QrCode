namespace Lyt.QrCode.Image;

public sealed partial class BitMatrixImage
{
#pragma warning disable CS8618 
    // Non-nullable field must contain a non-null value when exiting constructor. 
    // Created static when decoding for the first time 
    private static ReedSolomonDecoder RsDecoder;
#pragma warning restore CS8618 

    /// <summary> See ISO 18004:2006, 6.4.4 Table 5 </summary>
    private static readonly char[] AlphanumericCharacters =
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:".ToCharArray();

    private const int GB2312_SUBSET = 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char ToAlphaNumericChar(int value) => AlphanumericCharacters[value];

    /// <summary> Tries to decode the resampled image, provided by the detector </summary>
    internal bool TryDecode(
        DecodeParameters decodeParameters,
        [NotNullWhen(true)] out DecoderResult? decoderResult)
    {
        BitMatrixImage.RsDecoder ??= new ReedSolomonDecoder(GenericGF.QR_CODE_FIELD_256);

        decoderResult = null;
        int dimension = this.Height;
        if (dimension < 21 || (dimension & 0x03) != 1)
        {
            return false;
        }

        if (!this.TryReadVersion(out var qrVersion))
        {
            return false;
        }

        if (!this.TryReadFormatInformation(out var formatInformation))
        {
            return false;
        }

        if (!this.TryReadCodewords(qrVersion, formatInformation, out byte[]? codewords))
        {
            return false;
        }

        // Separate into data blocks
        ErrorCorrectionLevel ecLevel = formatInformation.ErrorCorrectionLevel;
        var dataBlocks = DataBlock.GetDataBlocks(codewords, qrVersion, ecLevel);

        // Count total number of data bytes
        int totalBytes = 0;
        foreach (var dataBlock in dataBlocks)
        {
            totalBytes += dataBlock.NumDataCodewords;
        }

        byte[] resultBytes = new byte[totalBytes];
        int resultOffset = 0;

        // Error-correct and copy data blocks together into a stream of bytes
        int errorsCorrected = 0;
        foreach (var dataBlock in dataBlocks)
        {
            byte[] codewordBytes = dataBlock.Codewords;
            int numDataCodewords = dataBlock.NumDataCodewords;
            if (!this.TryCorrectErrors(codewordBytes, numDataCodewords, out int errorsCorrectedLastRun))
            {
                Debug.WriteLine("Failed to correct errors");
                return false;
            }

            errorsCorrected += errorsCorrectedLastRun;
            for (int i = 0; i < numDataCodewords; i++)
            {
                resultBytes[resultOffset++] = codewordBytes[i];
            }
        }

        // Decode the contents of that array of bytes
        if (TryDecodeBytes(resultBytes, qrVersion, decodeParameters.CharacterSet, out decoderResult))
        {
            // success !
            //decoderResult.ErrorsCorrected = errorsCorrected;
            //decoderResult.ECLevel = ecLevel.ToString();
            return true;
        }

        return false;
    }

    /// <summary>
    /// QR Codes can encode text as bits in one of several modes, and can use multiple modes
    /// in one QR Code. This method decodes the bits back into text.</p>
    /// See ISO 18004:2006, 6.4.3 - 6.4.7
    /// </summary>
    // CONSIDER: Make local 
    internal static bool TryDecodeBytes(
        byte[] bytes, QrVersion version, string characterSet,
        [NotNullWhen(true)] out DecoderResult? decoderResult)
    {
        decoderResult = null;
        var bits = new BitSource(bytes);
        var result = new StringBuilder(50);
        var byteSegments = new List<byte[]>();
        int symbolSequence;
        int parityData;
        // int symbologyModifier;

        try
        {
            CharacterSetECI? currentCharacterSetECI = null;
            bool fc1InEffect = false;

#pragma warning disable CS0219 
            // Variable is assigned but its value is never used
            // Related to sumbology modifier. Consider delete
            bool hasFNC1first = false;
            bool hasFNC1second = false;
#pragma warning restore CS0219 

            EncodingMode mode;
            do
            {
                // While still another segment to read...
                if (bits.Available < 4)
                {
                    // OK, assume we're done. Really, a TERMINATOR mode should have been recorded here
                    mode = EncodingMode.Terminator;
                }
                else
                {
                    try
                    {
                        mode = EncodingMode.FromBits(bits.ReadBits(4)); // mode is encoded by 4 bits
                    }
                    catch (ArgumentException)
                    {
                        return false;
                    }
                }
                switch (mode.Name)
                {
                    case EncodingMode.Names.Terminator:
                        break;

                    case EncodingMode.Names.Fnc1FirstPosition:
                        // We do little with FNC1 except alter the parsed result a bit according to the spec
                        hasFNC1first = true; // symbology detection
                                             
                        fc1InEffect = true;
                        break;

                    case EncodingMode.Names.Fnc1SecondPosition:
                        // We do little with FNC1 except alter the parsed result a bit according to the spec
                        hasFNC1second = true; // symbology detection
                        fc1InEffect = true;
                        break;

                    case EncodingMode.Names.StructuredAppend:
                        if (bits.Available < 16)
                        {
                            return false;
                        }

                        // not really supported; but sequence number and parity is added later to the result metadata
                        // Read next 8 bits (symbol sequence #) and 8 bits (parity data), then continue
                        symbolSequence = bits.ReadBits(8);
                        parityData = bits.ReadBits(8);
                        break;

                    case EncodingMode.Names.Eci:
                        // Count doesn't apply to ECI
                        int ParseEciValue()
                        {
                            int firstByte = bits.ReadBits(8);
                            if ((firstByte & 0x80) == 0)
                            {
                                // just one byte
                                return firstByte & 0x7F;
                            }

                            if ((firstByte & 0xC0) == 0x80)
                            {
                                // two bytes
                                int secondByte = bits.ReadBits(8);
                                return ((firstByte & 0x3F) << 8) | secondByte;
                            }
                            if ((
                                firstByte & 0xE0) == 0xC0)
                            {
                                // three bytes
                                int secondThirdBytes = bits.ReadBits(16);
                                return ((firstByte & 0x1F) << 16) | secondThirdBytes;
                            }

                            throw new ArgumentException("Bad ECI bits starting with byte " + firstByte);
                        }

                        int value = ParseEciValue();
                        currentCharacterSetECI = CharacterSetECI.GetCharacterSetECIByValue(value);
                        if (currentCharacterSetECI == null)
                        {
                            return false;
                        }

                        break;

                    case EncodingMode.Names.Hanzi:
                        // First handle Hanzi mode which does not start with character count
                        //chinese mode contains a sub set indicator right after mode indicator
                        int subset = bits.ReadBits(4);
                        int countHanzi = bits.ReadBits(mode.GetCharacterCountBits(version));
                        if (subset == GB2312_SUBSET)
                        {
                            /// <summary> DecodeHanziSegment:  See specification GBT 18284-2000 </summary>
                            bool DecodeHanziSegment()
                            {
                                // Don't crash trying to read more bits than we have available.
                                if (countHanzi * 13 > bits.Available)
                                {
                                    return false;
                                }

                                // Each character will require 2 bytes.
                                // Read the characters as 2-byte pairs and decode as GB2312 afterwards
                                byte[] buffer = new byte[2 * countHanzi];
                                int offset = 0;
                                while (countHanzi > 0)
                                {
                                    // Each 13 bits encodes a 2-byte character
                                    int twoBytes = bits.ReadBits(13);
                                    int assembledTwoBytes = ((twoBytes / 0x060) << 8) | (twoBytes % 0x060);
                                    if (assembledTwoBytes < 0x00A00)
                                    {
                                        // In the 0xA1A1 to 0xAAFE range
                                        assembledTwoBytes += 0x0A1A1;
                                    }
                                    else
                                    {
                                        // In the 0xB0A1 to 0xFAFE range
                                        assembledTwoBytes += 0x0A6A1;
                                    }

                                    buffer[offset] = (byte)((assembledTwoBytes >> 8) & 0xFF);
                                    buffer[offset + 1] = (byte)(assembledTwoBytes & 0xFF);
                                    offset += 2;
                                    countHanzi--;
                                }

                                var encoding = EncodingUtilities.Gb2312Encoding;
                                encoding ??= EncodingUtilities.PlatformDefaultEncoding;
                                result.Append(encoding.GetString(buffer, 0, buffer.Length));

                                return true;
                            }

                            if (!DecodeHanziSegment())
                            {
                                return false;
                            }
                        }

                        break;

                    default:
                        // "Normal" QR code modes: How many characters will follow, encoded in this mode?
                        int count = bits.ReadBits(mode.GetCharacterCountBits(version));
                        switch (mode.Name)
                        {
                            case EncodingMode.Names.Numeric:

                                bool DecodeNumericSegment()
                                {
                                    // Read three digits at a time
                                    while (count >= 3)
                                    {
                                        // Each 10 bits encodes three digits
                                        if (bits.Available < 10)
                                        {
                                            return false;
                                        }
                                        int threeDigitsBits = bits.ReadBits(10);
                                        if (threeDigitsBits >= 1000)
                                        {
                                            return false;
                                        }
                                        result.Append(ToAlphaNumericChar(threeDigitsBits / 100));
                                        result.Append(ToAlphaNumericChar((threeDigitsBits / 10) % 10));
                                        result.Append(ToAlphaNumericChar(threeDigitsBits % 10));

                                        count -= 3;
                                    }
                                    if (count == 2)
                                    {
                                        // Two digits left over to read, encoded in 7 bits
                                        if (bits.Available < 7)
                                        {
                                            return false;
                                        }
                                        int twoDigitsBits = bits.ReadBits(7);
                                        if (twoDigitsBits >= 100)
                                        {
                                            return false;
                                        }
                                        result.Append(ToAlphaNumericChar(twoDigitsBits / 10));
                                        result.Append(ToAlphaNumericChar(twoDigitsBits % 10));
                                    }
                                    else if (count == 1)
                                    {
                                        // One digit left over to read
                                        if (bits.Available < 4)
                                        {
                                            return false;
                                        }
                                        int digitBits = bits.ReadBits(4);
                                        if (digitBits >= 10)
                                        {
                                            return false;
                                        }
                                        result.Append(ToAlphaNumericChar(digitBits));
                                    }

                                    return true;
                                }

                                if (!DecodeNumericSegment())
                                {
                                    return false;
                                }

                                break;

                            case EncodingMode.Names.Alphanumeric:

                                bool DecodeAlphanumericSegment()
                                {
                                    // Read two characters at a time
                                    int start = result.Length;
                                    while (count > 1)
                                    {
                                        if (bits.Available < 11)
                                        {
                                            return false;
                                        }
                                        int nextTwoCharsBits = bits.ReadBits(11);
                                        result.Append(ToAlphaNumericChar(nextTwoCharsBits / 45));
                                        result.Append(ToAlphaNumericChar(nextTwoCharsBits % 45));
                                        count -= 2;
                                    }

                                    if (count == 1)
                                    {
                                        // special case: one character left
                                        if (bits.Available < 6)
                                        {
                                            return false;
                                        }
                                        result.Append(ToAlphaNumericChar(bits.ReadBits(6)));
                                    }

                                    // See section 6.4.8.1, 6.4.8.2
                                    if (fc1InEffect)
                                    {
                                        // We need to massage the result a bit if in an FNC1 mode:
                                        for (int i = start; i < result.Length; i++)
                                        {
                                            if (result[i] == '%')
                                            {
                                                if (i < result.Length - 1 && result[i + 1] == '%')
                                                {
                                                    // %% is rendered as %
                                                    result.Remove(i + 1, 1);
                                                }
                                                else
                                                {
                                                    // In alpha mode, % should be converted to FNC1 separator 0x1D
                                                    result.Remove(i, 1);
                                                    result.Insert(i, [(char)0x1D]);
                                                }
                                            }
                                        }
                                    }

                                    return true;
                                }

                                if (!DecodeAlphanumericSegment())
                                {
                                    return false;
                                }

                                break;

                            case EncodingMode.Names.Byte:
                                bool DecodeByteSegment()
                                {
                                    // Don't crash trying to read more bits than we have available.
                                    if (count << 3 > bits.Available)
                                    {
                                        return false;
                                    }

                                    byte[] readBytes = new byte[count];
                                    for (int i = 0; i < count; i++)
                                    {
                                        readBytes[i] = (byte)bits.ReadBits(8);
                                    }

                                    Encoding? encoding;
                                    if (currentCharacterSetECI is null)
                                    {
                                        // The spec isn't clear on this mode; see
                                        // section 6.4.5: t does not say which encoding to assuming
                                        // upon decoding. I have seen ISO-8859-1 used as well as
                                        // Shift_JIS -- without anything like an ECI designator to
                                        // give a hint.
                                        encoding = EncodingUtilities.GuessCharset(readBytes, characterSet);
                                    }
                                    else
                                    {
                                        encoding = EncodingUtilities.GetEncoding(currentCharacterSetECI.EncodingName);
                                    }

                                    encoding ??= EncodingUtilities.PlatformDefaultEncoding;

                                    result.Append(encoding.GetString(readBytes, 0, readBytes.Length));
                                    byteSegments.Add(readBytes);
                                    return true;
                                }

                                if (!DecodeByteSegment())
                                {
                                    return false;
                                }

                                break;

                            case EncodingMode.Names.Kanji:
                                bool DecodeKanjiSegment()
                                {
                                    // Don't crash trying to read more bits than we have available.
                                    if (count * 13 > bits.Available)
                                    {
                                        return false;
                                    }

                                    // Each character will require 2 bytes. Read the characters as 2-byte pairs
                                    // and decode as Shift_JIS afterwards
                                    byte[] buffer = new byte[2 * count];
                                    int offset = 0;
                                    while (count > 0)
                                    {
                                        // Each 13 bits encodes a 2-byte character
                                        int twoBytes = bits.ReadBits(13);
                                        int assembledTwoBytes = ((twoBytes / 0x0C0) << 8) | (twoBytes % 0x0C0);
                                        if (assembledTwoBytes < 0x01F00)
                                        {
                                            // In the 0x8140 to 0x9FFC range
                                            assembledTwoBytes += 0x08140;
                                        }
                                        else
                                        {
                                            // In the 0xE040 to 0xEBBF range
                                            assembledTwoBytes += 0x0C140;
                                        }

                                        buffer[offset] = (byte)(assembledTwoBytes >> 8);
                                        buffer[offset + 1] = (byte)assembledTwoBytes;
                                        offset += 2;
                                        count--;
                                    }

                                    // Shift_JIS may not be supported in some environments:
                                    var encoding = EncodingUtilities.ShiftJisEncoding;
                                    encoding ??= EncodingUtilities.PlatformDefaultEncoding;
                                    result.Append(encoding.GetString(buffer, 0, buffer.Length));

                                    return true;
                                }

                                if (!DecodeKanjiSegment())
                                {
                                    return false;
                                }

                                break;

                            default:
                                return false;
                        }
                        break;
                }
            } while (mode != EncodingMode.Terminator);

            //if (currentCharacterSetECI != null)
            //{
            //    if (hasFNC1first)
            //    {
            //        symbologyModifier = 4;
            //    }
            //    else if (hasFNC1second)
            //    {
            //        symbologyModifier = 6;
            //    }
            //    else
            //    {
            //        symbologyModifier = 2;
            //    }
            //}
            //else
            //{
            //    if (hasFNC1first)
            //    {
            //        symbologyModifier = 3;
            //    }
            //    else if (hasFNC1second)
            //    {
            //        symbologyModifier = 5;
            //    }
            //    else
            //    {
            //        symbologyModifier = 1;
            //    }
            //}
        }
        catch (ArgumentException ex)
        {
            // Most likely from ReadBits() calls
            Debug.WriteLine(ex.ToString());
            return false;
        }

        decoderResult = new DecoderResult()
        {
            RawBytes = bytes,
            Text = result.ToString(),
            // TODO : Relocate these elements into some debug data structure 
            //ByteSegments = byteSegments,
            //SymbologyModifier = symbologyModifier,
        };

        return true;
    }

    /// <summary>
    ///  Given data and error-correction codewords received, possibly corrupted by errors, attempts to
    /// correct the errors in-place using Reed-Solomon error correction.
    /// </summary>
    /// <param name="codewordBytes">data and error correction codewords</param>
    /// <param name="numDataCodewords">number of codewords that are data bytes</param>
    /// <param name="errorsCorrected">the number of errors corrected</param>
    // CONSIDER: Make local 
#pragma warning disable CA1822 
    // Mark members as static
    // Compiler or IDE fails: cannot be made static 
    private bool TryCorrectErrors(byte[] codewordBytes, int numDataCodewords, out int errorsCorrected)
#pragma warning restore CA1822 // Mark members as static
    {
        int numCodewords = codewordBytes.Length;

        // First read into an array of ints
        int[] codewordsInts = new int[numCodewords];
        for (int i = 0; i < numCodewords; i++)
        {
            codewordsInts[i] = codewordBytes[i] & 0xFF;
        }

        int numECCodewords = codewordBytes.Length - numDataCodewords;
        try
        {
            // Reed Solomon decoding can throw exceptions
            if (!RsDecoder.Decode(codewordsInts, numECCodewords, out errorsCorrected))
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            errorsCorrected = 0;
            Debug.WriteLine(ex.ToString());
            return false;
        }

        // Copy back into array of bytes -- only need to worry about the bytes that were data
        // We don't care about errors in the error-correction codewords
        for (int i = 0; i < numDataCodewords; i++)
        {
            codewordBytes[i] = (byte)codewordsInts[i];
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CopyBit(int i, int j, int versionBits) => this[i, j] ? (versionBits << 1) | 0x1 : versionBits << 1;

    #region Original CopyBit ~ TODO ? ~ Maybe 
    // TODO: Make local vars of Decode 
    // TODO: Figure out if any need for this 'mirrored' bool 
    // NOT ever Written , only read in CopyBit below
    // private bool mirrored;
    //{
    //    bool bit = this.mirrored ? this[j, i] : this[i, j];
    //    return bit ? (versionBits << 1) | 0x1 : versionBits << 1;
    //}
    #endregion TODO ~ Maybe 

    /// <summary>
    /// Reads version information from one of its two locations within the QR Code.
    /// </summary>
    /// <returns> QR Version encapsulating the QR Code's version </returns>
    /// <throws>  ReaderException if both version information locations cannot be parsed as </throws>
    // CONSIDER: Make local 
    internal bool TryReadVersion([NotNullWhen(true)] out QrVersion? qrVersion)
    {
        int dimension = this.Height;
        int provisionalVersion = (dimension - 17) >> 2;
        if (provisionalVersion <= 6)
        {
            qrVersion = QrVersion.FromVersionNumber(provisionalVersion);
            return true;
        }

        // Read top-right version info: 3 wide by 6 tall
        int versionBits = 0;
        int ijMin = dimension - 11;
        for (int j = 5; j >= 0; j--)
        {
            for (int i = dimension - 9; i >= ijMin; i--)
            {
                versionBits = this.CopyBit(i, j, versionBits);
            }
        }

        if (QrVersion.TryDecodeVersionInformation(versionBits, out qrVersion) && 
            qrVersion.DimensionForVersion == dimension)
        {
            return true;
        }

        // Failed. Try bottom left: 6 wide by 3 tall
        versionBits = 0;
        for (int i = 5; i >= 0; i--)
        {
            for (int j = dimension - 9; j >= ijMin; j--)
            {
                versionBits = this.CopyBit(i, j, versionBits);
            }
        }

        if (QrVersion.TryDecodeVersionInformation(versionBits, out qrVersion) &&
            qrVersion.DimensionForVersion == dimension)
        {
            return true;
        }

        return false;
    }

    /// <summary> Reads format information from one of its two locations within the QR Code. </summary>
    /// <returns> FormatInformation encapsulating the QR Code's format info </returns>
    /// <throws>  ReaderException if both format information locations cannot be parsed as </throws>
    // CONSIDER: Make local 
    internal bool TryReadFormatInformation([NotNullWhen(true)] out FormatInformation? formatInformation)
    {
        // Read top-left format info bits
        int formatInfoBits1 = 0;
        for (int i = 0; i < 6; i++)
        {
            formatInfoBits1 = this.CopyBit(i, 8, formatInfoBits1);
        }

        // .. and skip a bit in the timing pattern ...
        formatInfoBits1 = this.CopyBit(7, 8, formatInfoBits1);
        formatInfoBits1 = this.CopyBit(8, 8, formatInfoBits1);
        formatInfoBits1 = this.CopyBit(8, 7, formatInfoBits1);

        // .. and skip a bit in the timing pattern ...
        for (int j = 5; j >= 0; j--)
        {
            formatInfoBits1 = this.CopyBit(8, j, formatInfoBits1);
        }

        // Read the top-right/bottom-left pattern too
        int dimension = this.Height;
        int formatInfoBits2 = 0;
        int jMin = dimension - 7;
        for (int j = dimension - 1; j >= jMin; j--)
        {
            formatInfoBits2 = this.CopyBit(8, j, formatInfoBits2);
        }

        for (int i = dimension - 8; i < dimension; i++)
        {
            formatInfoBits2 = this.CopyBit(i, 8, formatInfoBits2);
        }

        formatInformation = FormatInformation.DecodeFormatInformation(formatInfoBits1, formatInfoBits2);
        if (formatInformation is not null)
        {
            return true;
        }

        return false;
    }

    // CONSIDER: Make local 
    internal bool TryReadCodewords(
        QrVersion qrVersion, FormatInformation formatInformation, [NotNullWhen(true)] out byte[]? codewords)
    {
        codewords = new byte[qrVersion.TotalCodewords];

        // Get the data mask for the format used in this QR Code. This will exclude
        // some bits from reading as we wind through the bit matrix.
        int dimension = this.Height;
        var functionFlipWhen = DataMask.FromMask(formatInformation.DataMask);
        this.FlipWhen(functionFlipWhen);

        var functionPattern = BitMatrixImage.CreateFunctionPattern(qrVersion, dimension);
        bool readingUp = true;
        int resultOffset = 0;
        int currentByte = 0;
        int bitsRead = 0;

        // Read columns in pairs, from right to left
        for (int j = dimension - 1; j > 0; j -= 2)
        {
            if (j == 6)
            {
                // Skip whole column with vertical alignment pattern;
                // saves time and makes the other code proceed more cleanly
                j--;
            }

            // Read alternatingly from bottom to top then top to bottom
            for (int count = 0; count < dimension; count++)
            {
                int i = readingUp ? dimension - 1 - count : count;
                for (int col = 0; col < 2; col++)
                {
                    // Ignore bits covered by the function pattern
                    if (!functionPattern[j - col, i])
                    {
                        // Read a bit
                        bitsRead++;
                        currentByte <<= 1;
                        if (this[j - col, i])
                        {
                            currentByte |= 1;
                        }

                        // If we've made a whole byte, save it off
                        if (bitsRead == 8)
                        {
                            codewords[resultOffset++] = (byte)currentByte;
                            bitsRead = 0;
                            currentByte = 0;
                        }
                    }
                }
            }

            readingUp ^= true; // readingUp = !readingUp; // switch directions
        }

        if (resultOffset != qrVersion.TotalCodewords)
        {
            return false;
        }

        return true;
    }

    internal bool TryResample(
        int dimension, PerspectiveTransform transform, [NotNullWhen(true)] out BitMatrixImage? resampled)
    {
        resampled = null;

        if (dimension <= 0 || dimension <= 0)
        {
            return false;
        }

        resampled = new BitMatrixImage(dimension, dimension);
        float[] points = new float[dimension << 1];

        for (int y = 0; y < dimension; y++)
        {
            int max = points.Length;
            float iValue = (float)y + 0.5f;
            for (int x = 0; x < max; x += 2)
            {
                points[x] = (float)(x >> 1) + 0.5f;
                points[x + 1] = iValue;
            }

            transform.TransformPointsInPlace(points);

            // Quick check to see if points transformed to something inside the image;
            // sufficient to check the endpoints
            if (!CheckAndNudgePoints(this, points))
            {
                return false;
            }

            try
            {
                int imageWidth = this.Width;
                int imageHeight = this.Height;
                for (int x = 0; x < max; x += 2)
                {
                    int imagex = (int)points[x];
                    int imagey = (int)points[x + 1];

                    if (imagex < 0 || imagex >= imageWidth || imagey < 0 || imagey >= imageHeight)
                    {
                        return false;
                    }

                    resampled[x >> 1, y] = this[imagex, imagey];
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                // java version:
                // 
                // This feels wrong, but, sometimes if the finder patterns are misidentified, the resulting
                // transform gets "twisted" such that it maps a straight line of points to a set of points
                // whose endpoints are in bounds, but others are not. There is probably some mathematical
                // way to detect this about the transformation that I don't know yet.
                // This results in an ugly runtime exception despite our clever checks above -- can't have
                // that. We could check each point's coordinates but that feels duplicative. We settle for
                // catching and wrapping ArrayIndexOutOfBoundsException.
                if (Debugger.IsAttached) { Debug.WriteLine($"Exception in TryResample: {ex}"); }
                return false;
            }
        }

        return true;
    }

    /// <summary> Checks a set of points that have been transformed to sample points on an image against
    /// the image's dimensions to see if the point are even within the image.
    /// 
    /// This method will actually "nudge" the endpoints back onto the image if they are found to be
    /// barely (less than 1 pixel) off the image. This accounts for imperfect detection of finder
    /// patterns in an image where the QR Code runs all the way to the image border.
    /// 
    /// For efficiency, the method will check points from either end of the line until one is found
    /// to be within the image. Because the set of points are assumed to be linear, this is valid.
    /// </summary>
    /// <param name="image">image into which the points should map </param>
    /// <param name="points">actual points in x1,y1,...,xn,yn form </param>
    /// 
    // TODO: Make local 
    private static bool CheckAndNudgePoints(BitMatrixImage image, float[] points)
    {
        int width = image.Width;
        int height = image.Height;

        // Check and nudge points from start until we see some that are OK:
        bool nudged = true;
        int maxOffset = points.Length - 1; // points.length must be even
        for (int offset = 0; offset < maxOffset && nudged; offset += 2)
        {
            int x = (int)points[offset];
            int y = (int)points[offset + 1];
            if (x < -1 || x > width || y < -1 || y > height)
            {
                return false;
            }

            nudged = false;
            if (x == -1)
            {
                points[offset] = 0.0f;
                nudged = true;
            }
            else if (x == width)
            {
                points[offset] = width - 1;
                nudged = true;
            }
            if (y == -1)
            {
                points[offset + 1] = 0.0f;
                nudged = true;
            }
            else if (y == height)
            {
                points[offset + 1] = height - 1;
                nudged = true;
            }
        }

        // Check and nudge points from end:
        nudged = true;
        for (int offset = points.Length - 2; offset >= 0 && nudged; offset -= 2)
        {
            int x = (int)points[offset];
            int y = (int)points[offset + 1];
            if (x < -1 || x > width || y < -1 || y > height)
            {
                return false;
            }

            nudged = false;
            if (x == -1)
            {
                points[offset] = 0.0f;
                nudged = true;
            }
            else if (x == width)
            {
                points[offset] = width - 1;
                nudged = true;
            }
            if (y == -1)
            {
                points[offset + 1] = 0.0f;
                nudged = true;
            }
            else if (y == height)
            {
                points[offset + 1] = height - 1;
                nudged = true;
            }
        }

        return true;
    }

    /// <summary> flip all of the bits, if shouldBeFlipped is true for the coordinates </summary>
    /// <param name="shouldBeFlipped">should return true, if the bit at a given coordinate should be flipped</param>
    internal void FlipWhen(Func<int, int, bool> shouldBeFlipped)
    {
        for (int y = 0; y < this.Height; y++)
        {
            for (int x = 0; x < this.Width; x++)
            {
                if (shouldBeFlipped(y, x))
                {
                    this[x, y] = !this[x, y];
                }
            }
        }
    }

    internal static BitMatrixImage CreateFunctionPattern(QrVersion qrVersion, int dimension)
    {
        var bitMatrix = new BitMatrixImage(dimension, dimension);

        /// <summary> Sets a square region of the bit matrix to true. </summary>
        /// <param name="left">The horizontal position to begin at (inclusive) </param>
        /// <param name="top">The vertical position to begin at (inclusive) </param>
        /// <param name="width">The width of the region </param>
        /// <param name="height">The height of the region </param>
        void SetRegion(int left, int top, int width, int height)
        {
            if (top < 0 || left < 0)
            {
                throw new ArgumentException("Left and top must be nonnegative");
            }

            if (height < 1 || width < 1)
            {
                throw new ArgumentException("Height and width must be at least 1");
            }
            int right = left + width;
            int bottom = top + height;
            if (bottom > dimension || right > dimension)
            {
                throw new ArgumentException("The region must fit inside the matrix");
            }

            for (int y = top; y < bottom; y++)
            {
                for (int x = left; x < right; x++)
                {
                    bitMatrix[x, y] = true;
                }
            }
        }

        // Top left finder pattern + separator + format
        SetRegion(0, 0, 9, 9);
        
        // Top right finder pattern + separator + format
        SetRegion(dimension - 8, 0, 8, 9);
        
        // Bottom left finder pattern + separator + format
        SetRegion(0, dimension - 8, 9, 8);

        // Alignment patterns
        int[] alignmentPatternCenters = qrVersion.AlignmentPatternCenters; 
        int max = alignmentPatternCenters.Length;
        for (int x = 0; x < max; x++)
        {
            int i = alignmentPatternCenters[x] - 2;
            for (int y = 0; y < max; y++)
            {
                if ((x != 0 || (y != 0 && y != max - 1)) && (x != max - 1 || y != 0))
                {
                    SetRegion(alignmentPatternCenters[y] - 2, i, 5, 5);
                }
                // else no alignment patterns near the three finder patterns
            }
        }

        // Vertical timing pattern
        SetRegion(6, 9, 1, dimension - 17);
        // Horizontal timing pattern
        SetRegion(9, 6, dimension - 17, 1);

        if (qrVersion.VersionNumber > 6)
        {
            // Version info, top right
            SetRegion(dimension - 11, 0, 3, 6);
            // Version info, bottom left
            SetRegion(0, dimension - 11, 6, 3);
        }

        return bitMatrix;
    }
}
