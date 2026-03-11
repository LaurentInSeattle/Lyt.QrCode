namespace Lyt.QrCode.Data;

// TODO : Eliminate ECC duplicate class used in encoding 

/// <summary>
/// This class defines the four error correction levels defined by the QR code standard.
/// See ISO 18004:2006, 6.5.1. 
/// </summary>
public sealed class ErrorCorrectionLevel
{
    /// <summary> Low error correction capacity. The QR code can tolerate about 7% erroneous codewords. </summary>
    public static readonly ErrorCorrectionLevel Low = new(0, 0x01, "Low");

    /// <summary> Medium error correction capacity . The QR code can tolerate about 15% erroneous codewords. </summary>
    public static readonly ErrorCorrectionLevel Medium = new(1, 0x00, "Medium");

    /// <summary> Quartile error correction capacity. Default. The QR code can tolerate about 25% erroneous codewords. </summary>
    public static readonly ErrorCorrectionLevel Quartile = new(2, 0x03, "Quartile");

    /// <summary> High error correction capacity. The QR code can tolerate about 30% erroneous codewords. </summary>
    public static readonly ErrorCorrectionLevel High = new(3, 0x02, "High");

    private static readonly ErrorCorrectionLevel[] AllEclInBitsOrder = [Medium, Low, High, Quartile];

    internal static readonly ErrorCorrectionLevel[] AllEclFromLowToHigh = [Low, Medium, Quartile, High];

    private ErrorCorrectionLevel(int ordinal, int bits, string name)
    {
        this.Ordinal = ordinal;
        this.FormatBits = bits;
        this.Name = name;
    }

    /// <summary> Gets the format bits value. </summary>
    public int FormatBits { get; private set; }

    /// <summary> Gets the name. </summary>
    public string Name { get; private set; }

    /// <summary> Gets the Ordinal. </summary>
    public int Ordinal { get; private set; }

    public override string ToString() => this.Name;

    public static ErrorCorrectionLevel FromFormatBits(int formatBits)
    {
        if (formatBits < 0 || formatBits >= AllEclInBitsOrder.Length)
        {
            throw new IndexOutOfRangeException(nameof(formatBits));
        }

        return ErrorCorrectionLevel.AllEclInBitsOrder[formatBits];
    }
}
