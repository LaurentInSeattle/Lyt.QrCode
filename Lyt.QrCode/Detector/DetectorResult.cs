namespace Lyt.QrCode.Detector;

/// <summary>
/// Encapsulates the result of detecting a QrCode in an image. This includes the raw
/// matrix of black/white pixels corresponding to the barcode, and possibly points of interest
/// in the image, like the location of finder patterns or corners of the barcode in the image.
/// </summary>
internal sealed record class DetectorResult(BitMatrixImage Resampled, Patterns Patterns); 