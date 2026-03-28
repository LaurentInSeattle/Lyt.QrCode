namespace Lyt.QrCode.API;

public sealed class DecodeParameters
{
    public DecodeParameters() { }

#pragma warning disable CA1822 
    // Mark members as static
    // Could evolve later in something non static 
    public bool Validate() => true;
    //{
    //    // this.CharacterSet ??= string.Empty;
    //    return true; 
    //}

#pragma warning restore CA1822 

    /// <summary> True when it is not necessary to parse the Text result of the QR code.</summary>
    public bool SkipParsing { get; set; } = false;
}
