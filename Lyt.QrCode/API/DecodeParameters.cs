namespace Lyt.QrCode.API;

public sealed class DecodeParameters
{
    public DecodeParameters() { }

    public bool Validate()
    {
        this.CharacterSet ??= string.Empty;

        return true; 
    }

    /// <summary> True when the source image is known to be a QrCode, this will skip the detection step.</summary>
    public bool SkipDetector { get; set; } = false;

    public string CharacterSet { get; set; } = string.Empty;
}
