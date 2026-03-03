namespace Lyt.QrCode.Image;

public sealed partial class SourceImage
{
    public DecoderResult Decode(bool skipDetector)
    {
        // TODO 
        // 
        var grayscaleImage = this.ToGrayscale();
        var bitMatrixImage = grayscaleImage.ToBitMatrixAdaptiveThresholding();
        return new DecoderResult();
    }
}
