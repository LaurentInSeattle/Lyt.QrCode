namespace Lyt.QrCode.Decoder;

/// <summary> Encapsulates the result of decoding a QrCode within an image. </summary>
public sealed class DecoderResult
{
    /// <summary>  raw text encoded by the barcode, if applicable, otherwise <code>null</code><summary> 
    public string Text { get; internal set; } = string.Empty;

    /// <summary>  raw bytes encoded by the barcode, if applicable, otherwise <code>null</code><summary> 
    public byte[]? RawBytes { get; internal set; } = null;

    /// <summary> The list of byte segments in the result, or empty list if not applicable </summary>
    public List<byte[]> ByteSegments { get; internal set; } = new();

    /// <summary> name of error correction level used, or string.Empty if not applicable </summary>
    public string ECLevel { get; internal set; } = string.Empty;

    /// <summary> number of errors corrected, or zero (?) if not applicable </summary>
    public int ErrorsCorrected { get; internal set ; }

    /// <summary> the symbology identifier </summary>
    public int SymbologyModifier { get; internal set; }

    /// <summary>  The points identifying finder patterns of the corners of the QrCode.  <summary> 
    public DetectorResult? DetectorResult { get; internal set; } // Can be null 
}