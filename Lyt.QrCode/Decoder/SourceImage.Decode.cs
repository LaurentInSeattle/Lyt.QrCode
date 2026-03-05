namespace Lyt.QrCode.Image;

public sealed partial class SourceImage
{
    public bool TryDecode(
        bool skipDetector, 
        DetectorCallback? detectorCallback, 
        [NotNullWhen(true)] out DecoderResult? decoderResult)
    {
        decoderResult= null;
        // TODO : Still need to cleanup the API !
        // 
        var grayscaleImage = this.ToGrayscale();
        var bitMatrixImage = grayscaleImage.ToBitMatrixAdaptiveThresholding();
        bool result = bitMatrixImage.TryDetect(detectorCallback, out var detectorResult); 
        if (result) 
        {
            decoderResult = new DecoderResult(detectorResult);
            return true; //  bitMatrixImage.Decode(skipDetector);
        }

        return false;
    }
}
