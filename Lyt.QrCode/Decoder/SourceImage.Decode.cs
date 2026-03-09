namespace Lyt.QrCode.Image;

public sealed partial class SourceImage
{
    public bool TryDecode(
        DecodeParameters decodeParameters, 
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
            if ( bitMatrixImage.TryDecode(decodeParameters, out decoderResult))
            {
                decoderResult = new DecoderResult(detectorResult);
                return true; 
            }
        }

        return false;
    }
}
