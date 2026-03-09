namespace Lyt.QrCode.API;

public struct DecodeParameters
{
    public DecodeParameters() { }

    public bool Validate()
    {
        if (this.CharacterSet is null)
        {
            this.CharacterSet = string.Empty;
        }

        return true; 
    }
    /// <summary> True when the source image is known to be a QrCode, this will skip the detection step.</summary>
    public bool SkipDetector { get; set; } = false;

    public string CharacterSet { get; set; } = string.Empty;
}
