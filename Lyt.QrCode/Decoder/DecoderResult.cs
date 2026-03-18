namespace Lyt.QrCode.Decoder;

/// <summary> Encapsulates the result of decoding a QrCode within an image. </summary>
public sealed class DecoderResult
{
    /// <summary> Text encoded by the QR Code, if applicable, otherwise empty<summary> 
    public string Text { get; internal set; } = string.Empty;

    /// <summary> Bytes encoded by the QR Code, if applicable, otherwise null ><summary> 
    public byte[]? RawBytes { get; internal set; } = null;

    public bool TryGet<T>([NotNullWhen(true)] out T? result) where T : QrContent
    {
        result = null;

        if (this.ParsedType is null || this.ParsedObject is null)
        {
            return false;
        }

        if (typeof(T) == this.ParsedType)
        {
            result = (T)this.ParsedObject!;
            return true;
        }

        return false;
    }

    public bool IsParsed => this.ParsedType is not null && this.ParsedObject is not null;

    public Type? ParsedType { get; internal set; } = null;

    public object? ParsedObject { get; internal set; } = null;

    /// <summary>  The points identifying finder patterns of the corners of the QrCode.  <summary> 
    public DetectorResult? DetectorResult { get; internal set; } // Can be null 

    #region TODO : Relocate these elements into some debug data structure 

    ///// <summary> name of error correction level used, or string.Empty if not applicable </summary>
    //// TODO: Possibly NOT needed 
    //public string ECLevel { get; internal set; } = string.Empty;

    ///// <summary> number of errors corrected, or zero (?) if not applicable </summary>
    //// TODO: Possibly NOT needed 
    //public int ErrorsCorrected { get; internal set; }

    ///// <summary> the symbology identifier </summary>
    //// TODO: Possibly NOT needed 
    //public int SymbologyModifier { get; internal set; }

    //// TODO: Possibly NOT needed 
    ///// <summary> The list of byte segments in the result, or empty list if not applicable </summary>
    //public List<byte[]> ByteSegments { get; internal set; } = [];

    #endregion TODO : Relocate these elements into some debug data structure 
}