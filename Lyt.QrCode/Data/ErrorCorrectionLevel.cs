namespace Lyt.QrCode.Data;

// TODO : Eliminate ECC duplicate class used in encoding 

/// <summary>
/// <p>See ISO 18004:2006, 6.5.1. This enum encapsulates the four error correction levels
/// defined by the QR code standard.</p>
/// </summary>
internal sealed class ErrorCorrectionLevel
{
    /// <summary> L = ~7% correction</summary>
    public static readonly ErrorCorrectionLevel L = new (0, 0x01, "L");

    /// <summary> M = ~15% correction</summary>
    public static readonly ErrorCorrectionLevel M = new (1, 0x00, "M");

    /// <summary> Q = ~25% correction</summary>
    public static readonly ErrorCorrectionLevel Q = new (2, 0x03, "Q");

    /// <summary> H = ~30% correction</summary>
    public static readonly ErrorCorrectionLevel H = new (3, 0x02, "H");

    private static readonly ErrorCorrectionLevel[] AllEclInBitsOrder = [M, L, H, Q];

    private ErrorCorrectionLevel(int ordinal, int bits, string name)
    {
        this.Ordinal = ordinal;
        this.Bits = bits;
        this.Name = name;
    }

    /// <summary> Gets the bits value. </summary>
    public int Bits {  get; private set; }

    /// <summary> Gets the name. </summary>
    public string Name { get; private set; }

    /// <summary> Gets the Ordinal. </summary>
    public int Ordinal { get; private set; }

    public override string ToString() => this.Name;

    public static ErrorCorrectionLevel FromBits(int bits)
    {
        if (bits < 0 || bits >= AllEclInBitsOrder.Length)
        {
            throw new IndexOutOfRangeException(nameof(bits));
        }

        return AllEclInBitsOrder[bits];
    }
}
