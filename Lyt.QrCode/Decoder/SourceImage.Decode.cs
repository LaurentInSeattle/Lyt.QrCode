namespace Lyt.QrCode.Image;

public sealed partial class SourceImage
{
    public bool TryDecode(
        bool skipDetector, // Needed ??? 
        DetectorCallback? detectorCallback, 
        [NotNullWhen(true)] out DecoderResult? decoderResult)
    {
        decoderResult= null;
        // TODO : Still need to cleanup the API !
        // 
        var grayscaleImage = this.ToGrayscale();
        var bitMatrixImage = grayscaleImage.ToBitMatrixAdaptiveThresholding();
        bool detected = bitMatrixImage.TryDetect(detectorCallback, out var detectorResult); 
        if (detected && detectorResult is not null)
        {
            decoderResult = new DecoderResult(detectorResult);
            return true; //  bitMatrixImage.Decode(skipDetector);
        }

        return false;
    }
}
