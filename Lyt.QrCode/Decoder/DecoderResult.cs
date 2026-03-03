namespace Lyt.QrCode.Decoder;

/// <summary> Encapsulates the result of decoding a QrCode within an image. </summary>
public sealed class DecoderResult
{
    /// <summary>  raw text encoded by the barcode, if applicable, otherwise <code>null</code><summary> 
    public string Text { get; set; } = string.Empty;

    /// <summary>  raw bytes encoded by the barcode, if applicable, otherwise <code>null</code><summary> 
    public byte[]? RawBytes { get; set; } = null;

    /// <summary>  The points identifying finder patterns of the corners of the QrCode.  <summary> 
    public ResultPoint[] ResultPoints { get; set; } = new ResultPoint [4] ; 
}