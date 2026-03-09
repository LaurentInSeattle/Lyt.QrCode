namespace Lyt.QrCode.API;

public readonly struct DecodeParameters
{
    public DecodeParameters() { }

    public bool Validate() => this.CharacterSet is not null ; 

    /// <summary> True when the source image is known to be a QrCode, this will skip the detection step.</summary>
    public bool SkipDetector { get; init; } = false ;

    public string CharacterSet { get; init; } = string.Empty;
}
