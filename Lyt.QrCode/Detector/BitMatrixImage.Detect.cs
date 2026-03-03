namespace Lyt.QrCode.Image;

internal sealed partial class BitMatrixImage
{
    internal DetectorResult Detect()
    {
        var points = new ResultPoint[4];
        return new DetectorResult(this, points);
    }
}
