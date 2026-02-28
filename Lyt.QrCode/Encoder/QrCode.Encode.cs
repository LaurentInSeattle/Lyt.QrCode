namespace Lyt.QrCode;

public sealed partial class QrCode
{
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
    private readonly bool[,] modules;

    // Indicates function modules that are not subjected to masking.
    // NOT Discarded when constructor finishes to prevent nullable warnings all over.
    private readonly bool[,] isFunction;

    /// <summary> 
    /// Constructs a QR code with the specified version number, error correction level, data codeword bytes, and mask number.
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

        this.modules = new bool[this.Size, this.Size];  // Initially all light
        this.isFunction = new bool[this.Size, this.Size];

        // Compute ECC, draw modules, do masking
        this.DrawFunctionPatterns();
        byte[] allCodewords = this.AddEccAndInterleave(dataCodewords);

        // Draws the given sequence of 8-bit codewords (data and error correction) onto the entire
        // data area of this QR code. Function modules need to be marked off before this is called.
        void DrawCodewords(byte[] data)
        {
            if (data.Length != GetNumRawDataModules(this.Version) / 8)
            {
                throw new ArgumentOutOfRangeException(nameof(data), "data length does not match version");
            }

            int i = 0;  // Bit index into the data

            // Do the funny zigzag scan
            for (int right = this.Size - 1; right >= 1; right -= 2)
            {
                // Index of right column in each column pair
                if (right == 6)
                {
                    right = 5;
                }

                for (int vert = 0; vert < this.Size; vert++)
                {
                    // Vertical counter
                    for (int j = 0; j < 2; j++)
                    {
                        int x = right - j;  // Actual x coordinate
                        bool upward = ((right + 1) & 2) == 0;
                        int y = upward ? this.Size - 1 - vert : vert;  // Actual y coordinate
                        if (!this.isFunction[y, +x] && i < data.Length * 8)
                        {
                            this.modules[y, x] = GetBit(data[(uint)i >> 3], 7 - (i & 7));
                            i++;
                        }

                        // If this QR code has any remainder bits (0 to 7), they were assigned as
                        // 0/false/light by the constructor and are left unchanged by this function
                    }
                }
            }

            Debug.Assert(i == data.Length * 8);
        }

        DrawCodewords(allCodewords);

        // Do masking
        if (mask == -1)
        {
            // Automatically choose best mask
            int minPenalty = int.MaxValue;
            for (uint i = 0; i < 8; i++)
            {
                this.ApplyMask(i);
                this.DrawFormatBits(i);
                int penalty = this.GetPenaltyScore();
                if (penalty < minPenalty)
                {
                    mask = (int)i;
                    minPenalty = penalty;
                }

                this.ApplyMask(i);  // Undoes the mask due to XOR
            }
        }

        Debug.Assert(0 <= mask && mask <= 7);
        this.Mask = mask;

        this.ApplyMask((uint)mask);  // Apply the final choice of mask
        this.DrawFormatBits((uint)mask);  // Overwrite old format bits

        // Discard the temporary array to save memory
        // Dont realy care 
        // _isFunction = null;
    }

    // TODO Fix this stupid list thing !!!

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

    public static QrCode EncodeBytes(byte[] bytes, Ecc ecc)
    {
        var segments = new List<QrSegment>() { QrSegment.MakeBytes(bytes) } ;
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
    /// <param name="ecc">The minimal or fixed error correction level to use .</param>
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
        Ecc ecc, 
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

        // Find the minimal version number to use
        int foundVersion = 0;
        int dataUsedBits = 0;
        for (int version = minVersion; version <= maxVersion; version++)
        {
            int numDataBits = GetNumDataCodewords(version, ecc) * 8;  // Number of data bits available
            dataUsedBits = QrSegment.GetTotalBits(segments, version);
            if (dataUsedBits != -1 && dataUsedBits <= numDataBits)
            {
                foundVersion = version;
                break;  // This version number is found to be suitable
            }

            if (version < maxVersion)
            {
                continue;
            }

            // All versions in the range could not fit the given data
            string msg = "Segment too long";
            if (dataUsedBits != -1)
            {
                msg = $"Data length = {dataUsedBits} bits, Max capacity = {numDataBits} bits";
            }

            throw new ArgumentException(msg);
        }

        Debug.Assert(dataUsedBits != -1);

        // Increase the error correction level while the data still fits in the current version number
        foreach (Ecc newEcc in Ecc.AllValues)
        {  
            // From low to high
            if (boostEcl && dataUsedBits <= GetNumDataCodewords(foundVersion, newEcc) * 8)
            {
                ecc = newEcc;
            }
        }

        // Concatenate all segments to create the data bit string
        var bitArray = new BitArray(0);
        foreach (var seg in segments)
        {
            bitArray.AppendBits(seg.EncodingMode.ModeBits, 4);
            bitArray.AppendBits((uint)seg.NumChars, seg.EncodingMode.NumCharCountBits(foundVersion));
            bitArray.AppendData(seg.GetData());
        }

        Debug.Assert(bitArray.Length == dataUsedBits);

        // Add terminator and pad up to a byte if applicable
        int dataCapacityBits = GetNumDataCodewords(foundVersion, ecc) * 8;
        Debug.Assert(bitArray.Length <= dataCapacityBits);
        bitArray.AppendBits(0, Math.Min(4, dataCapacityBits - bitArray.Length));
        bitArray.AppendBits(0, (8 - bitArray.Length % 8) % 8);
        Debug.Assert(bitArray.Length % 8 == 0);

        // Pad with alternating bytes until data capacity is reached
        for (uint padByte = 0xEC; bitArray.Length < dataCapacityBits; padByte ^= 0xEC ^ 0x11)
        {
            bitArray.AppendBits(padByte, 8);
        }

        // Pack bits into bytes in big endian
        byte[] dataCodewords = new byte[bitArray.Length / 8];
        for (int i = 0; i < bitArray.Length; i++)
        {
            if (bitArray.Get(i))
            {
                dataCodewords[i >> 3] |= (byte)(1 << (7 - (i & 7)));
            }
        }

        // Create the QR code object
        return new QrCode(foundVersion, ecc, dataCodewords, mask);
    }

    // Reads this object's version field, and draws and marks all function modules.
    private void DrawFunctionPatterns()
    {
        // Draw horizontal and vertical timing patterns
        for (int i = 0; i < this.Size; i++)
        {
            this.SetFunctionModule(6, i, i % 2 == 0);
            this.SetFunctionModule(i, 6, i % 2 == 0);
        }

        // Draws a 9*9 finder pattern including the border separator,
        // with the center module at (x, y). Modules can be out of bounds.
        void DrawFinderPattern(int x, int y)
        {
            for (int dy = -4; dy <= 4; dy++)
            {
                for (int dx = -4; dx <= 4; dx++)
                {
                    // Chebyshev/infinity norm
                    // See : https://en.wikipedia.org/wiki/Chebyshev_distance
                    int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    int xx = x + dx, yy = y + dy;
                    if (0 <= xx && xx < this.Size && 0 <= yy && yy < this.Size)
                    {
                        this.SetFunctionModule(xx, yy, dist != 2 && dist != 4);
                    }
                }
            }
        }

        // Draw 3 finder patterns (all corners except bottom right; overwrites some timing modules)
        DrawFinderPattern(3, 3);
        DrawFinderPattern(this.Size - 4, 3);
        DrawFinderPattern(3, this.Size - 4);

        // Draws a 5*5 alignment pattern, with the center module
        // at (x, y). All modules must be in bounds.
        void DrawAlignmentPattern(int x, int y)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    int distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    this.SetFunctionModule(x + dx, y + dy, distance != 1);
                }
            }
        }

        // Returns an ascending list of positions of alignment patterns for this version number.
        // Each position is in the range [0,177), and are used on both the x and y axes.
        // This could be implemented as lookup table of 40 variable-length lists of unsigned bytes.
        int[] GetAlignmentPatternPositions()
        {
            if (this.Version == 1)
            {
                return [];
            }
            else
            {
                int numAlign = this.Version / 7 + 2;
                int step = (this.Version * 8 + numAlign * 3 + 5) / (numAlign * 4 - 4) * 2;
                int[] result = new int[numAlign];
                result[0] = 6;
                for (int i = result.Length - 1, pos = this.Size - 7; i >= 1; i--, pos -= step)
                {
                    result[i] = pos;
                }

                return result;
            }
        }

        // Draw numerous alignment patterns
        int[] alignPatPos = GetAlignmentPatternPositions();
        int numAlign = alignPatPos.Length;
        for (int i = 0; i < numAlign; i++)
        {
            for (int j = 0; j < numAlign; j++)
            {
                // Don't draw on the three finder corners
                if (!(i == 0 && j == 0 || i == 0 && j == numAlign - 1 || i == numAlign - 1 && j == 0))
                {
                    DrawAlignmentPattern(alignPatPos[i], alignPatPos[j]);
                }
            }
        }

        // Draw configuration data
        this.DrawFormatBits(0);  // Dummy mask value; overwritten later in the constructor

        // Draws two copies of the version bits (with its own error correction code),
        // based on this object's version field, iff 7 <= version <= 40.
        void DrawVersion()
        {
            if (this.Version < 7)
            {
                return;
            }

            // Calculate error correction code and pack bits
            uint rem = (uint)this.Version;  // version is uint6, in the range [7, 40]
            for (int i = 0; i < 12; i++)
            {
                rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
            }

            uint bits = ((uint)this.Version << 12) | rem;  // uint18
            Debug.Assert(bits >> 18 == 0);

            // Draw two copies
            for (int i = 0; i < 18; i++)
            {
                bool bit = GetBit(bits, i);
                int a = this.Size - 11 + i % 3;
                int b = i / 3;
                this.SetFunctionModule(a, b, bit);
                this.SetFunctionModule(b, a, bit);
            }
        }

        DrawVersion();
    }

    // Returns true iff the i'th bit of x is set to 1.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool GetBit(uint x, int i) => ((x >> i) & 1) != 0;

    // Draws two copies of the format bits (with its own error correction code)
    // based on the given mask and this object's error correction level field.
    private void DrawFormatBits(uint mask)
    {
        // Calculate error correction code and pack bits
        uint data = (this.ErrorCorrectionLevel.FormatBits << 3) | mask;  // errCorrLvl is uint2, mask is uint3
        uint rem = data;
        for (int i = 0; i < 10; i++)
        {
            rem = (rem << 1) ^ ((rem >> 9) * 0x537);
        }

        uint bits = ((data << 10) | rem) ^ 0x5412;  // uint15
        Debug.Assert(bits >> 15 == 0);

        // Draw first copy
        for (int i = 0; i <= 5; i++)
        {
            this.SetFunctionModule(8, i, GetBit(bits, i));
        }

        this.SetFunctionModule(8, 7, GetBit(bits, 6));
        this.SetFunctionModule(8, 8, GetBit(bits, 7));
        this.SetFunctionModule(7, 8, GetBit(bits, 8));
        for (int i = 9; i < 15; i++)
        {
            this.SetFunctionModule(14 - i, 8, GetBit(bits, i));
        }

        // Draw second copy
        for (int i = 0; i < 8; i++)
        {
            this.SetFunctionModule(this.Size - 1 - i, 8, GetBit(bits, i));
        }

        for (int i = 8; i < 15; i++)
        {
            this.SetFunctionModule(8, this.Size - 15 + i, GetBit(bits, i));
        }

        this.SetFunctionModule(8, this.Size - 8, true);  // Always dark
    }

    // Sets the color of a module and marks it as a function module.
    // Only used by the constructor. Coordinates must be in bounds.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetFunctionModule(int x, int y, bool isDark)
    {
        this.modules[y, x] = isDark;
        this.isFunction[y, x] = true;
    }

    /// <summary>
    /// Returns the number of 8-bit data codewords contained in a QR code of the given version number and error correction level.
    /// The result is the net data capacity, without error correction data, and after discarding remainder bits.
    /// </summary>
    /// <param name="version">The version number.</param>
    /// <param name="ecc">The error correction level.</param>
    private static int GetNumDataCodewords(int version, Ecc ecc)
        => GetNumRawDataModules(version) / 8 - EccCodewordsPerBlock[ecc.Ordinal, version] * NumErrorCorrectionBlocks[ecc.Ordinal, version];

    // Returns a new byte string representing the given data with the appropriate error correction
    // codewords appended to it, based on this object's version and error correction level.
    private byte[] AddEccAndInterleave(byte[] data)
    {
        if (data.Length != GetNumDataCodewords(this.Version, this.ErrorCorrectionLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(data), "Length of data does not match version and ecl");
        }

        // Calculate parameter numbers
        int numBlocks = NumErrorCorrectionBlocks[this.ErrorCorrectionLevel.Ordinal, this.Version];
        int blockEccLen = EccCodewordsPerBlock[this.ErrorCorrectionLevel.Ordinal, this.Version];
        int rawCodewords = GetNumRawDataModules(this.Version) / 8;
        int numShortBlocks = numBlocks - rawCodewords % numBlocks;
        int shortBlockLen = rawCodewords / numBlocks;

        // Split data into blocks and append ECC to each block
        byte[][] blocks = new byte[numBlocks][];
        var rsDiv = new ReedSolomonGenerator(blockEccLen);
        for (int i = 0, k = 0; i < numBlocks; i++)
        {
            byte[] dat = data.CopyOfRange(k, k + shortBlockLen - blockEccLen + (i < numShortBlocks ? 0 : 1));
            k += dat.Length;
            byte[] block = dat.CopyOf(shortBlockLen + 1);
            byte[] ecc = rsDiv.GetRemainder(dat);
            Array.Copy(ecc, 0, block, block.Length - blockEccLen, ecc.Length);
            blocks[i] = block;
        }

        // Interleave (not concatenate) the bytes from every block into a single sequence
        byte[] result = new byte[rawCodewords];
        for (int i = 0, k = 0; i < blocks[0].Length; i++)
        {
            for (int j = 0; j < blocks.Length; j++)
            {
                // Skip the padding byte in short blocks
                if (i != shortBlockLen - blockEccLen || j >= numShortBlocks)
                {
                    result[k] = blocks[j][i];
                    k++;
                }
            }
        }

        return result;
    }

    // XORs the codeword modules in this QR code with the given mask pattern.
    // The function modules must be marked and the codeword bits must be drawn
    // before masking. Due to the arithmetic of XOR, calling applyMask() with
    // the same mask value a second time will undo the mask. A final well-formed
    // QR code needs exactly one (not zero, two, etc.) mask applied.
    private void ApplyMask(uint mask)
    {
        if (mask < 0 || mask > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(mask), "Mask value out of range");
        }

        for (int y = 0; y < this.Size; y++)
        {
            for (int x = 0; x < this.Size; x++)
            {
                bool invert = false;
                switch (mask)
                {
                    case 0: invert = (x + y) % 2 == 0; break;
                    case 1: invert = y % 2 == 0; break;
                    case 2: invert = x % 3 == 0; break;
                    case 3: invert = (x + y) % 3 == 0; break;
                    case 4: invert = (x / 3 + y / 2) % 2 == 0; break;
                    case 5: invert = x * y % 2 + x * y % 3 == 0; break;
                    case 6: invert = (x * y % 2 + x * y % 3) % 2 == 0; break;
                    case 7: invert = ((x + y) % 2 + x * y % 3) % 2 == 0; break;
                }

                this.modules[y, x] ^= invert && !this.isFunction[y, x];
            }
        }
    }


    // Calculates and returns the penalty score based on state of this QR code's current modules.
    // This is used by the automatic mask choice algorithm to find the mask pattern that yields the lowest score.
    private int GetPenaltyScore()
    {
        // Evaluating which mask is best.
        const int PenaltyN1 = 3;
        const int PenaltyN2 = 3;
        const int PenaltyN3 = 40;
        const int PenaltyN4 = 10;

        int result = 0;

        // Adjacent modules in row having same color, and finder-like patterns
        for (int y = 0; y < this.Size; y++)
        {
            bool runColor = false;
            int runX = 0;
            var finderPenalty = new FinderPenalty(this.Size);
            for (int x = 0; x < this.Size; x++)
            {
                if (this.modules[y, x] == runColor)
                {
                    runX++;
                    if (runX == 5)
                    {
                        result += PenaltyN1;
                    }
                    else if (runX > 5)
                    {
                        result++;
                    }
                }
                else
                {
                    finderPenalty.AddHistory(runX);
                    if (!runColor)
                    {
                        result += finderPenalty.CountPatterns() * PenaltyN3;
                    }

                    runColor = this.modules[y, x];
                    runX = 1;
                }

            }

            result += finderPenalty.TerminateAndCount(runColor, runX) * PenaltyN3;
        }

        // Adjacent modules in column having same color, and finder-like patterns
        for (int x = 0; x < this.Size; x++)
        {
            bool runColor = false;
            int runY = 0;
            var finderPenalty = new FinderPenalty(this.Size);
            for (int y = 0; y < this.Size; y++)
            {
                if (this.modules[y, x] == runColor)
                {
                    runY++;
                    if (runY == 5)
                    {
                        result += PenaltyN1;
                    }
                    else if (runY > 5)
                    {
                        result++;
                    }
                }
                else
                {
                    finderPenalty.AddHistory(runY);
                    if (!runColor)
                    {
                        result += finderPenalty.CountPatterns() * PenaltyN3;
                    }

                    runColor = this.modules[y, x];
                    runY = 1;
                }
            }

            result += finderPenalty.TerminateAndCount(runColor, runY) * PenaltyN3;
        }

        // 2*2 blocks of modules having same color
        for (int y = 0; y < this.Size - 1; y++)
        {
            for (int x = 0; x < this.Size - 1; x++)
            {
                bool color = this.modules[y, x];
                if (color == this.modules[y, x + 1] &&
                      color == this.modules[y + 1, x] &&
                      color == this.modules[y + 1, x + 1])
                {
                    result += PenaltyN2;
                }
            }
        }

        // Balance of dark and light modules
        int dark = 0;
        for (int y = 0; y < this.Size; y++)
        {
            for (int x = 0; x < this.Size; x++)
            {
                if (this.modules[y, x])
                {
                    dark++;
                }
            }
        }

        int total = this.Size * this.Size;  // Note that size is odd, so dark/total != 1/2
        
        // Compute the smallest integer k >= 0 such that (45-5k)% <= dark/total <= (55+5k)%
        int k = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1;
        result += k * PenaltyN4;

        Debug.Assert(0 <= result && result <= 2568888);  // Non-tight upper bound based on default values of PENALTY_N1, ..., N4
        return result;
    }

    /// <summary>
    /// Returns the number of data bits that can be stored in a QR code of the given version.
    /// The returned number is after all function modules are excluded. This includes remainder bits,
    /// so it might not be a multiple of 8. The result is in the range [208, 29648].
    /// </summary>
    private static int GetNumRawDataModules(int version)
    {
        if (version < MinVersion || version > MaxVersion)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version number out of range");
        }

        int size = version * 4 + 17;
        int result = size * size;   // Number of modules in the whole QR code square
        result -= 8 * 8 * 3;        // Subtract the three finders with separators
        result -= 15 * 2 + 1;       // Subtract the format information and dark module
        result -= (size - 16) * 2;  // Subtract the timing patterns (excluding finders)
        
        // The five lines above are equivalent to: int result = (16 * ver + 128) * ver + 64;

        if (version >= 2)
        {
            int numAlign = version / 7 + 2;
            result -= (numAlign - 1) * (numAlign - 1) * 25;  // Subtract alignment patterns not overlapping with timing patterns
            result -= (numAlign - 2) * 2 * 20;  // Subtract alignment patterns that overlap with timing patterns
                                                // The two lines above are equivalent to: result -= (25 * numAlign - 10) * numAlign - 55;
            if (version >= 7)
            {
                result -= 6 * 3 * 2;  // Subtract version information
            }
        }

        Debug.Assert(208 <= result && result <= 29648);
        return result;
    }
}
