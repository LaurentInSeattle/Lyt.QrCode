namespace Lyt.QrCode;

/// <summary> Error correction level in QR code symbol. </summary>
public sealed class Ecc
{
    /// <summary> Low error correction level. The QR code can tolerate about 7% erroneous codewords. </summary>
    /// <value>Low error correction level.</value>
    public static readonly Ecc Low = new(0, 1);

    /// <summary> Medium error correction level. The QR code can tolerate about 15% erroneous codewords. </summary>
    /// <value>Medium error correction level.</value>
    public static readonly Ecc Medium = new(1, 0);

    /// <summary> Quartile error correction level. The QR code can tolerate about 25% erroneous codewords. </summary>
    /// <value>Quartile error correction level.</value>
    public static readonly Ecc Quartile = new(2, 3);

    /// <summary> High error correction level. The QR code can tolerate about 30% erroneous codewords. </summary>
    /// <value>High error correction level.</value>
    public static readonly Ecc High = new(3, 2);

    internal static readonly Ecc[] AllValues = [Low, Medium, Quartile, High];

    /// <summary> Ordinal number of error correction level (in the range 0 to 3). </summary>
    /// <remarks> Higher number represent a higher amount of error tolerance. </remarks>
    /// <value>Ordinal number.</value>
    public int Ordinal { get; }

    // In the range 0 to 3 (unsigned 2-bit integer).
    internal uint FormatBits { get; }

    private Ecc(int ordinal, uint fb)
    {
        this.Ordinal = ordinal;
        this.FormatBits = fb;
    }
}
