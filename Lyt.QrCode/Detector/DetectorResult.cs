namespace Lyt.QrCode.Detector; 

/// <summary>
/// Encapsulates the result of detecting a QrCode in an image. This includes the raw
/// matrix of black/white pixels corresponding to the barcode, and possibly points of interest
/// in the image, like the location of finder patterns or corners of the barcode in the image.
/// </summary>
internal sealed class DetectorResult
{
    /// <summary> the detected bits </summary>
    internal BitMatrixImage Bits { get; private set; }

    /// <summary> the pixel points where the result is found </summary>
    internal ResultPoint[] Points { get; private set; }

    internal DetectorResult(BitMatrixImage bits, ResultPoint[] points)
    {
        this.Bits = bits;
        this.Points = points;
    }
}