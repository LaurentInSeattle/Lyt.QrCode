namespace Lyt.QrCode.Utilities;

internal static class MathExtensions
{
    /// Euclidean distance between integer points A and B
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(int aX, int aY, int bX, int bY)
    {
        double xDiff = aX - bX;
        double yDiff = aY - bY;
        return (float)Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
    }

    /// <summary>
    /// Rounds to the nearest int, where x.5 rounds up to x+1. Semantics of this shortcut
    /// differ slightly from {@link Math#round(float)} in that half rounds down for negative
    /// values. -2.5 rounds to -3, not -2. For purposes here it makes no difference.
    /// </summary>
    /// <param name="d"> float value to round</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Round(float d)
    {
        if (float.IsNaN(d))
        {
            return 0;
        }

        if (float.IsPositiveInfinity(d))
        {
            return int.MaxValue;
        }

        return (int)(d + (d < 0.0f ? -0.5f : 0.5f));
    }

    /// <summary> Same as above for doubles  </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Round(double d)
    {
        if (double.IsNaN(d))
        {
            return 0;
        }

        if (double.IsPositiveInfinity(d))
        {
            return int.MaxValue;
        }

        return (int)(d + (d < 0.0f ? -0.5f : 0.5f));
    }
}
