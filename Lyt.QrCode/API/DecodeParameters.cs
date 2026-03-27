namespace Lyt.QrCode.API;

public sealed class DecodeParameters
{
    public DecodeParameters() { }

    public bool Validate() => true;
    //{
    //    // this.CharacterSet ??= string.Empty;
    //    return true; 
    //}

    /// <summary> True when it is not necessary to parse the Text result of the QR code.</summary>
    public bool SkipParsing { get; set; } = false;
}
