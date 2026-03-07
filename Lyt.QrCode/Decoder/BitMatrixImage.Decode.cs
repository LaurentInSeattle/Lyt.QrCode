namespace Lyt.QrCode.Image;

internal sealed partial class BitMatrixImage
{
    //internal DecoderResult Decode(bool skipDetector)
    //{
    //    // TODO 
    //    // 
    //    return new DecoderResult();
    //}

    internal bool TryResample (
        int dimension, PerspectiveTransform transform, [NotNullWhen(true)] out BitMatrixImage? resampled)
    {
        // TODO 
        resampled = null;
        return false;
    }   
}
