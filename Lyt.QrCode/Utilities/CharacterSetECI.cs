namespace Lyt.QrCode.Utilities;

// TODO: Prevent throwing exceptions for unsupported encodings !!!

/// <summary> Encapsulates a Character Set ECI, according to "Extended Channel Interpretations" 5.3.1.1 of ISO 18004. </summary>
internal sealed class CharacterSetECI : ECI
{
    internal static readonly IDictionary<int, CharacterSetECI> ValueToECI;

    internal static readonly IDictionary<string, CharacterSetECI> NameToECI;

    private Encoding? encoding;

    /// <summary> The encoding name </summary>
    internal string EncodingName { get; private set; }

    /// <summary> gets or sets the encoding class:  can be set externally if override is necessary. </summary>
    internal Encoding? Encoding
    {
        get => this.encoding ??= GetEncoding(this);
        set => this.encoding = value;
    }

    static CharacterSetECI()
    {
        ValueToECI = new Dictionary<int, CharacterSetECI>();
        NameToECI = new Dictionary<string, CharacterSetECI>();

        static void AddCharacterSet(int value, string[] encodingNames)
        {
            var eci = new CharacterSetECI(value, encodingNames[0]);
            ValueToECI[value] = eci; // can't use valueOf
            foreach (string t in encodingNames)
            {
                if (!NameToECI.ContainsKey(t))
                {
                    NameToECI[t] = eci;
                }
            }
        }

        // TODO:  figure out if these values are even right!
        // Note: TODO above mentioned in original code 
        AddCharacterSet(0, ["CP437", "IBM437"]);
        AddCharacterSet(1, ["ISO-8859-1", "ISO8859_1"]);
        AddCharacterSet(2, ["CP437", "IBM437"]);
        AddCharacterSet(3, ["ISO-8859-1", "ISO8859_1"]);
        AddCharacterSet(4, ["ISO-8859-2", "ISO8859_2"]);
        AddCharacterSet(5, ["ISO-8859-3", "ISO8859_3"]);
        AddCharacterSet(6, ["ISO-8859-4", "ISO8859_4"]);
        AddCharacterSet(7, ["ISO-8859-5", "ISO8859_5"]);
        AddCharacterSet(8, ["ISO-8859-6", "ISO8859_6"]);
        AddCharacterSet(9, ["ISO-8859-7", "ISO8859_7"]);
        AddCharacterSet(10, ["ISO-8859-8", "ISO8859_8"]);
        AddCharacterSet(11, ["ISO-8859-9", "ISO8859_9"]);
        AddCharacterSet(12, ["ISO-8859-4", "ISO-8859-10", "ISO8859_10"]); // use ISO-8859-4 because ISO-8859-10 isn't supported
        AddCharacterSet(13, ["ISO-8859-11", "ISO8859_11", "WINDOWS-874"]);
        AddCharacterSet(15, ["ISO-8859-13", "ISO8859_13"]);
        AddCharacterSet(16, ["ISO-8859-1", "ISO-8859-14", "ISO8859_14"]); // use ISO-8859-1 because ISO-8859-14 isn't supported
        AddCharacterSet(17, ["ISO-8859-15", "ISO8859_15"]);
        AddCharacterSet(18, ["ISO-8859-3", "ISO-8859-16", "ISO8859_16"]); // use ISO-8859-3 because ISO-8859-16 isn't supported
        AddCharacterSet(20, ["SJIS", "SHIFT_JIS", "ISO-2022-JP"]);
        AddCharacterSet(21, ["WINDOWS-1250", "CP1250"]);
        AddCharacterSet(22, ["WINDOWS-1251", "CP1251"]);
        AddCharacterSet(23, ["WINDOWS-1252", "CP1252"]);
        AddCharacterSet(24, ["WINDOWS-1256", "CP1256"]);
        AddCharacterSet(25, ["UTF-16BE", "UNICODEBIG", "UNICODEFFFE"]);
        AddCharacterSet(26, ["UTF-8", "UTF8"]);
        AddCharacterSet(27, ["US-ASCII"]);
        AddCharacterSet(170, ["US-ASCII"]);
        AddCharacterSet(28, ["BIG5"]);
        AddCharacterSet(29, ["GB18030", "GB2312", "EUC_CN", "GBK"]);
        AddCharacterSet(30, ["EUC-KR", "EUC_KR"]);
    }

    private CharacterSetECI(int value, string encodingName) : base(value) => this.EncodingName = encodingName;


    /// <param name="value">character set ECI value</param>
    /// <returns><see cref="CharacterSetECI"/> representing ECI of given value, or null if it is legal but unsupported</returns>
    public static CharacterSetECI? GetCharacterSetECIByValue(int value)
    {
        if (!ValueToECI.TryGetValue(value, out CharacterSetECI? charSet))
        {
            return null;
        }

        return charSet;
    }

    /// <param name="name">character set ECI encoding name</param>
    /// <returns><see cref="CharacterSetECI"/> representing ECI for character encoding, or null if it is legal but unsupported</returns>
    public static CharacterSetECI? GetCharacterSetECIByName(string name)
    {
        if (!NameToECI.TryGetValue(name.ToUpper(), out CharacterSetECI? value))
        {
            return null;
        }

        return value;
    }

    /// <param name="encoding">encoding</param>
    /// <returns>CharacterSetECI representing ECI for character encoding, or null if it is legal but unsupported</returns>
    public static CharacterSetECI? GetCharacterSetECI(Encoding encoding)
    {
        if (!NameToECI.TryGetValue(encoding.WebName.ToUpper(), out CharacterSetECI? value))
        {
            return null;
        }

        return value;
    }

    /// <summary> returns the encoding object for the specified charset </summary>
    /// <param name="charsetECI"></param>
    public static Encoding? GetEncoding(CharacterSetECI charsetECI)
    {
        if (charsetECI == null)
        {
            return null;
        }

        // don't use property here because of StackOverflow
        return charsetECI.encoding ??= GetEncoding(charsetECI.EncodingName);
    }

    /// <summary> returns the encoding object fo the specified name </summary>
    /// <param name="encodingName"></param>
    public static Encoding? GetEncoding(string encodingName)
    {
        if (string.IsNullOrEmpty(encodingName))
        {
            return null;
        }

        Encoding? encoding = null;

        try
        {
            encoding = Encoding.GetEncoding(encodingName);
        }
        catch (ArgumentException)
        {
            try
            {
                // Silverlight only supports a limited number of character sets, trying fallback to UTF-8
                encoding = Encoding.GetEncoding(EncodingUtilities.UTF8);
            }
            catch (Exception)
            {
                // Swallow ? 
            }
        }
        catch (Exception)
        {
            return null;
        }

        return encoding;
    }
}