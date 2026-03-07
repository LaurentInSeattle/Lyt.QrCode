namespace Lyt.QrCode.Decoder;

/// <summary> Encapsulates the result of decoding a QrCode within an image. </summary>
public sealed class DecoderResult
{
    internal DecoderResult(DetectorResult detectorResult)
    {
        this.DetectorResult = detectorResult;
    }

    /// <summary>  raw text encoded by the barcode, if applicable, otherwise <code>null</code><summary> 
    public string Text { get; internal set; } = string.Empty;

    /// <summary>  raw bytes encoded by the barcode, if applicable, otherwise <code>null</code><summary> 
    public byte[]? RawBytes { get; internal set; } = null;

    /// <summary>  The points identifying finder patterns of the corners of the QrCode.  <summary> 
    public DetectorResult DetectorResult { get; private set; }
}