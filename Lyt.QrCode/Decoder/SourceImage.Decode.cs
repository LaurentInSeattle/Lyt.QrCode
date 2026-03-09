namespace Lyt.QrCode.Image;

public sealed partial class SourceImage
{
    public bool TryDecode(
        DecodeParameters decodeParameters, 
        DetectorCallback? detectorCallback, 
        [NotNullWhen(true)] out DecoderResult? decoderResult)
    {
        decoderResult= null;
        var grayscaleImage = this.ToGrayscale();
        var bitMatrixImage = grayscaleImage.ToBitMatrixAdaptiveThresholding();
        bool detected = bitMatrixImage.TryDetect(detectorCallback, out var detectorResult); 
        if (detected && detectorResult is not null)
        {
            var resampled = detectorResult.Resampled;
            if (resampled.TryDecode(decodeParameters, out decoderResult))
            {
                decoderResult.DetectorResult = detectorResult;
                return true; 
            }
        }

        return false;
    }
}
