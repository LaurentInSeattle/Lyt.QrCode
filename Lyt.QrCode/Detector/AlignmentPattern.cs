namespace Lyt.QrCode.Detector;

/// <summary> 
/// Encapsulates an alignment pattern, which are the smaller square patterns found in
/// all but the simplest QR Codes.</p>
/// </summary>
internal sealed class AlignmentPattern : ResultPoint
{
    private readonly float estimatedModuleSize;

    internal AlignmentPattern(float posX, float posY, float estimatedModuleSize) : base(posX, posY) 
        => this.estimatedModuleSize = estimatedModuleSize;

    /// <summary> 
    /// Determines if this alignment pattern "about equals" an alignment pattern at the stated
    /// position and size -- meaning, it is at nearly the same center with nearly the same size.
    /// </summary>
    internal bool AboutEquals(float moduleSize, float i, float j)
    {
        if (Math.Abs(i - this.Y) <= moduleSize && Math.Abs(j - this.X) <= moduleSize)
        {
            float moduleSizeDiff = Math.Abs(moduleSize - estimatedModuleSize);
            return moduleSizeDiff <= 1.0f || moduleSizeDiff <= estimatedModuleSize;
        }

        return false;
    }

    /// <summary>
    /// Combines this object's current estimate of a finder pattern position and module size
    /// with a new estimate. Returns a new AlignmentPattern containing an average of the two.
    /// </summary>
    /// <param name="newModuleSize">New size of the module.</param>
    internal AlignmentPattern CombineEstimate(float i, float j, float newModuleSize)
    {
        float combinedX = (this.X + j) / 2.0f;
        float combinedY = (this.Y + i) / 2.0f;
        float combinedModuleSize = (estimatedModuleSize + newModuleSize) / 2.0f;
        return new AlignmentPattern(combinedX, combinedY, combinedModuleSize);
    }
}
