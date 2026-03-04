namespace Lyt.QrCode.Image;

public sealed partial class SourceImage
{
    public DecoderResult Decode(bool skipDetector, DetectorCallback? detectorCallback)
    {
        // TODO : Still need to cleanup the API !
        // 
        var grayscaleImage = this.ToGrayscale();
        var bitMatrixImage = grayscaleImage.ToBitMatrixAdaptiveThresholding();
        bool result = bitMatrixImage.TryDetect(detectorCallback, out var detectorResult); 
        if (result) 
        {
            return bitMatrixImage.Decode(skipDetector);
        }

        return new DecoderResult();
    }
}
