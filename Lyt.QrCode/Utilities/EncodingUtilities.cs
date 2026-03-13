namespace Lyt.QrCode.Utilities;

internal static class EncodingUtilities
{
    /// <summary> name of the default encoding of the current platform (name) </summary>
    internal static readonly string PlatformDefaultEncodingName;

    /// <summary> type of default encoding of the current platform </summary>
    internal static readonly Encoding PlatformDefaultEncoding;

    #region Retained for binary compatibility with earlier versions

    // All four Encodings below default to PlatformDefaultEncoding if not supported on current platform 

    /// <summary> Shift JIS encoding if available </summary>
    internal static readonly Encoding ShiftJisEncoding;

    /// <summary> GB 2312 encoding if available </summary>
    internal static readonly Encoding Gb2312Encoding;

    /// <summary> ECU JP encoding if available </summary>
    internal static readonly Encoding EucJpEncoding;

    /// <summary> ISO8859-1 encoding if available </summary>
    internal static readonly Encoding ISO88591Encoding;

    private static readonly bool AssumeShiftJIS;

    /// <summary> Whether JIS_IS is supported or not </summary>
    internal static readonly bool JISIsSupported;

    /// <summary> EUC_JP is supported or not </summary>
    internal static readonly bool EucJpIsSupported;

    internal const string ShiftJis = "SJIS";

    internal const string GB2312 = "GB2312";

    internal const string EucJp = "EUC-JP";

    internal const string UTF8 = "UTF-8";

    internal const string ISO88591 = "ISO-8859-1";

    #endregion Retained for binary compatibility with earlier versions

    internal static Dictionary<string, EncodingInfo> supportedEncodings;

    static EncodingUtilities()
    {
#pragma warning disable IDE0028  // Simplify collection initialization
        // with is still in preview
        supportedEncodings = new(16);
#pragma warning restore IDE0028 

        EncodingInfo[] codePages = Encoding.GetEncodings();
        // Debug.WriteLine("Available Encodings:");
        foreach (EncodingInfo codePage in codePages)
        {
            // Debug.WriteLine($"- Code page ID: {codePage.CodePage}, IANA name: {codePage.Name}, Display name: {codePage.DisplayName}");
            supportedEncodings[codePage.Name] = codePage;
        }

        PlatformDefaultEncodingName = UTF8;
        PlatformDefaultEncoding = Encoding.UTF8;

        Gb2312Encoding = GetEncoding(GB2312) ?? PlatformDefaultEncoding;
        EucJpEncoding = GetEncoding(EucJp) ?? PlatformDefaultEncoding;
        ISO88591Encoding = GetEncoding(ISO88591) ?? PlatformDefaultEncoding;
        ShiftJisEncoding = GetEncoding(ShiftJis) ?? PlatformDefaultEncoding;

        string platformDefaultEncodingWebName = PlatformDefaultEncoding.WebName;
        JISIsSupported = IsEncodingSupported(ShiftJis);
        EucJpIsSupported = IsEncodingSupported(EucJp);
        AssumeShiftJIS = 
            (JISIsSupported && platformDefaultEncodingWebName.Equals(ShiftJisEncoding.WebName)) || 
            (EucJpIsSupported && platformDefaultEncodingWebName.Equals(EucJpEncoding.WebName));
    }

    /// <summary> returns the encoding object fo the specified name, or null if not supported  </summary>
    /// <param name="encodingName"></param>
    private static bool IsEncodingSupported(string encodingName)
    {
        if (string.IsNullOrEmpty(encodingName))
        {
            return false;
        }

        return supportedEncodings.ContainsKey(encodingName); 
    }

    /// <summary> returns the encoding object fo the specified name, or null if not supported  </summary>
    /// <param name="encodingName"></param>
    internal static Encoding? GetEncoding(string encodingName)
    {
        if (string.IsNullOrEmpty(encodingName))
        {
            return null;
        }

        Encoding? encoding = null;
        if (supportedEncodings.ContainsKey(encodingName))
        {
            encoding = Encoding.GetEncoding(encodingName);
        } 

        return encoding;
    }

    /// <summary> Guesses the encoding. </summary>
    /// <param name="bytes">bytes encoding a string, whose encoding should be guessed</param>
    /// <param name="hints">decode hints if applicable</param>
    /// <return> name of guessed encoding; at the moment will only guess one of:
    /// "SJIS", "UTF8", "ISO8859_1", or the platform default encoding if none
    /// of these can possibly be correct</return>
    internal static string GuessEncoding(byte[] bytes, string characterSet)
    {
        var c = GuessCharset(bytes, characterSet);
        if (c == ShiftJisEncoding && ShiftJisEncoding != null)
        {
            return "SJIS";
        }

        return c.WebName.ToUpper();
    }

    /// <summary> Guesses the character set encoding. </summary>
    /// <param name="bytes">bytes encoding a string, whose encoding should be guessed</param>
    /// <param name="hints">decode hints if applicable</param>
    /// <returns>Charset of guessed encoding; at the moment will only guess one of:
    ///  {@link #SHIFT_JIS_CHARSET}, {@link StandardCharsets#UTF_8},
    ///  {@link StandardCharsets#ISO_8859_1}, {@link StandardCharsets#UTF_16},
    ///  or the platform default encoding if
    ///  none of these can possibly be correct</returns>
    internal static Encoding GuessCharset(byte[] bytes, string characterSet)
    {
        if (!string.IsNullOrWhiteSpace(characterSet) )
        {
            var encoding = GetEncoding(characterSet);
            if (encoding != null)
            {
                return encoding;
            }
        }

        // First try UTF-16, assuming anything with its BOM is UTF-16
        if (bytes.Length > 2 &&
            ((bytes[0] == (byte)0xFE && bytes[1] == (byte)0xFF) ||
             (bytes[0] == (byte)0xFF && bytes[1] == (byte)0xFE)))
        {
            return Encoding.Unicode;
        }

        // For now, merely tries to distinguish ISO-8859-1, UTF-8 and Shift_JIS,
        // which should be by far the most common encodings.
        int length = bytes.Length;
        bool canBeISO88591 = true;
        bool canBeShiftJIS = JISIsSupported;
        bool canBeUTF8 = true;
        int utf8BytesLeft = 0;
        int utf2BytesChars = 0;
        int utf3BytesChars = 0;
        int utf4BytesChars = 0;
        int sjisBytesLeft = 0;
        int sjisKatakanaChars = 0;
        int sjisCurKatakanaWordLength = 0;
        int sjisCurDoubleBytesWordLength = 0;
        int sjisMaxKatakanaWordLength = 0;
        int sjisMaxDoubleBytesWordLength = 0;
        int isoHighOther = 0;

        bool utf8bom = bytes.Length > 3 &&
            bytes[0] == 0xEF &&
            bytes[1] == 0xBB &&
            bytes[2] == 0xBF;

        for (int i = 0;
             i < length && (canBeISO88591 || canBeShiftJIS || canBeUTF8);
             i++)
        {

            int value = bytes[i] & 0xFF;

            // UTF-8 stuff
            if (canBeUTF8)
            {
                if (utf8BytesLeft > 0)
                {
                    if ((value & 0x80) == 0)
                    {
                        canBeUTF8 = false;
                    }
                    else
                    {
                        utf8BytesLeft--;
                    }
                }
                else if ((value & 0x80) != 0)
                {
                    if ((value & 0x40) == 0)
                    {
                        canBeUTF8 = false;
                    }
                    else
                    {
                        utf8BytesLeft++;
                        if ((value & 0x20) == 0)
                        {
                            utf2BytesChars++;
                        }
                        else
                        {
                            utf8BytesLeft++;
                            if ((value & 0x10) == 0)
                            {
                                utf3BytesChars++;
                            }
                            else
                            {
                                utf8BytesLeft++;
                                if ((value & 0x08) == 0)
                                {
                                    utf4BytesChars++;
                                }
                                else
                                {
                                    canBeUTF8 = false;
                                }
                            }
                        }
                    }
                }
            }

            // ISO-8859-1 stuff
            if (canBeISO88591)
            {
                if (value > 0x7F && value < 0xA0)
                {
                    canBeISO88591 = false;
                }
                else if (value > 0x9F)
                {
                    if (value < 0xC0 || value == 0xD7 || value == 0xF7)
                    {
                        isoHighOther++;
                    }
                }
            }

            // Shift_JIS stuff
            if (canBeShiftJIS)
            {
                if (sjisBytesLeft > 0)
                {
                    if (value < 0x40 || value == 0x7F || value > 0xFC)
                    {
                        canBeShiftJIS = false;
                    }
                    else
                    {
                        sjisBytesLeft--;
                    }
                }
                else if (value == 0x80 || value == 0xA0 || value > 0xEF)
                {
                    canBeShiftJIS = false;
                }
                else if (value > 0xA0 && value < 0xE0)
                {
                    sjisKatakanaChars++;
                    sjisCurDoubleBytesWordLength = 0;
                    sjisCurKatakanaWordLength++;
                    if (sjisCurKatakanaWordLength > sjisMaxKatakanaWordLength)
                    {
                        sjisMaxKatakanaWordLength = sjisCurKatakanaWordLength;
                    }
                }
                else if (value > 0x7F)
                {
                    sjisBytesLeft++;
                    //sjisDoubleBytesChars++;
                    sjisCurKatakanaWordLength = 0;
                    sjisCurDoubleBytesWordLength++;
                    if (sjisCurDoubleBytesWordLength > sjisMaxDoubleBytesWordLength)
                    {
                        sjisMaxDoubleBytesWordLength = sjisCurDoubleBytesWordLength;
                    }
                }
                else
                {
                    //sjisLowChars++;
                    sjisCurKatakanaWordLength = 0;
                    sjisCurDoubleBytesWordLength = 0;
                }
            }
        }

        if (canBeUTF8 && utf8BytesLeft > 0)
        {
            canBeUTF8 = false;
        }
        if (canBeShiftJIS && sjisBytesLeft > 0)
        {
            canBeShiftJIS = false;
        }

        // Easy -- if there is BOM or at least 1 valid not-single byte character (and no evidence it can't be UTF-8), done
        if (canBeUTF8 && (utf8bom || utf2BytesChars + utf3BytesChars + utf4BytesChars > 0))
        {
            return Encoding.UTF8;
        }

        // Easy -- if assuming Shift_JIS or >= 3 valid consecutive not-ascii characters (and no evidence it can't be), done
        if (canBeShiftJIS && (AssumeShiftJIS || sjisMaxKatakanaWordLength >= 3 || sjisMaxDoubleBytesWordLength >= 3) && ShiftJisEncoding != null)
        {
            return ShiftJisEncoding;
        }
        
        // Distinguishing Shift_JIS and ISO-8859-1 can be a little tough for short words. The crude heuristic is:
        // - If we saw
        //   - only two consecutive katakana chars in the whole text, or
        //   - at least 10% of bytes that could be "upper" not-alphanumeric Latin1,
        // - then we conclude Shift_JIS, else ISO-8859-1
        if (canBeISO88591 && canBeShiftJIS && ISO88591Encoding != null && ShiftJisEncoding != null)
        {
            return 
                (sjisMaxKatakanaWordLength == 2 && sjisKatakanaChars == 2) || isoHighOther * 10 >= length ? 
                    ShiftJisEncoding : 
                    ISO88591Encoding;
        }

        // Otherwise, try in order ISO-8859-1, Shift JIS, UTF-8 and fall back to default platform encoding
        if (canBeISO88591 && ISO88591Encoding != null)
        {
            return ISO88591Encoding;
        }

        if (canBeShiftJIS && ShiftJisEncoding != null)
        {
            return ShiftJisEncoding;
        }
        
        if (canBeUTF8)
        {
            return Encoding.UTF8;
        }
        
        // Otherwise, we take a wild guess with platform encoding
        return PlatformDefaultEncoding;
    }
}