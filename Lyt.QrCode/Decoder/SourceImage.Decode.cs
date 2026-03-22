namespace Lyt.QrCode.Image;

public sealed partial class SourceImage
{
    internal bool TryDecode(
        MessageLog messageLog,
        DecodeParameters decodeParameters, 
        DetectorCallback? detectorCallback, 
        [NotNullWhen(true)] out DecoderResult? decoderResult)
    {
        decoderResult= null;
        try
        {
            var grayscaleImage = this.ToGrayscale();
            var bitMatrixImage = grayscaleImage.ToBitMatrixAdaptiveThresholding();
            bool detected = bitMatrixImage.TryDetect(messageLog, detectorCallback, out var detectorResult);
            if (detected && detectorResult is not null)
            {
                var resampled = detectorResult.Resampled;
                if (resampled.TryDecode(messageLog, decodeParameters, out decoderResult))
                {
                    decoderResult.DetectorResult = detectorResult;
                    return true;
                }
            }
        } 
        catch (Exception ex)
        {
            messageLog.AddErrorMessage("Exception thrown: " + ex.Message);
            messageLog.AddErrorMessage(ex.ToString());
        }

        return false;
    }
}
