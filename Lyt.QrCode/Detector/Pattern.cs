namespace Lyt.QrCode.Detector;

internal sealed class Pattern(float x, float y, float estimatedModuleSize, int count = 1) : ResultPoint(x, y)
{
    internal float EstimatedModuleSize { get; private set; } = estimatedModuleSize;

    internal int Count { get; private set; } = count;

    /// <summary> 
    /// Determines if this finder pattern "about equals" a finder pattern at the stated
    /// position and size -- meaning, it is at nearly the same center with nearly the same size.
    /// </summary>
    internal bool AboutEquals(float moduleSize, float i, float j)
    {
        if (Math.Abs(i - this.Y) <= moduleSize && Math.Abs(j - this.X) <= moduleSize)
        {
            float moduleSizeDiff = Math.Abs(moduleSize - this.EstimatedModuleSize);
            return moduleSizeDiff <= 1.0f || moduleSizeDiff <= this.EstimatedModuleSize;

        }

        return false;
    }

    /// <summary>
    /// Combines this object's current estimate of a finder pattern position and module size
    /// with a new estimate. It returns a new Pattern containing a weighted average
    /// based on count.
    /// </summary>
    internal Pattern CombineEstimate(float i, float j, float newModuleSize)
    {
        int combinedCount = this.Count + 1;
        float combinedX = (this.Count * this.X + j) / combinedCount;
        float combinedY = (this.Count * this.Y + i) / combinedCount;
        float combinedModuleSize = (this.Count * this.EstimatedModuleSize + newModuleSize) / combinedCount;
        return new Pattern(combinedX, combinedY, combinedModuleSize, combinedCount);
    }

    /// <summary>
    /// Get square of distance between a and b.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    internal static double SquaredDistance(Pattern a, Pattern b)
    {
        double x = a.X - b.X;
        double y = a.Y - b.Y;
        return x * x + y * y;
    }
}
