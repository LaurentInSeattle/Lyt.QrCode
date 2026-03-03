namespace Lyt.QrCode;

public readonly struct DecodeParameters
{
    public DecodeParameters() { }

    public bool Validate() => true; 

    /// <summary> True when the source image is known to be a QrCode, this will skip the detection step.</summary>
    public bool SkipDetector { get; init; } = false ;
}
