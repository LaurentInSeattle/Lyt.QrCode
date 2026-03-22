namespace Lyt.QrCode.Detector;

/// <summary> Immutable holding class for a 2D point with float coordinates X and Y. </summary>
internal class QrPoint(float x, float y)
{
    internal float X { get; } = x;

    internal float Y { get; } = y;

    public override string ToString() => $"({this.X}, {this.Y})";

    /// <summary> Convert to PixelPoint with rounded to integer image coordinates  </summary>
    /// <returns> A new QrPixelPoint object</returns>
    internal QrPixelPoint ToPixelPoint()
        => new((int)Math.Round(this.X), (int)Math.Round(this.Y));

    /// <summary> Returns the distance between two points </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double Distance(QrPoint a, QrPoint b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary> Returns the squared distance between two QrPoint a and b. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double SquaredDistance(QrPoint a, QrPoint b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Orders an array of three QrPoints in an order [A,B,C] such that AB is less than AC and
    /// BC is less than AC and the angle between BC and BA is less than 180 degrees.
    /// </summary>
    /// <param name="patterns">array of three <see cref="QrPoint" /> to order</param>
    internal static void OrderBestPatterns(QrPoint[] patterns)
    {
        // Find distances between pattern centers
        double zeroOneDistance = Distance(patterns[0], patterns[1]);
        double oneTwoDistance = Distance(patterns[1], patterns[2]);
        double zeroTwoDistance = Distance(patterns[0], patterns[2]);

        // Assume one closest to other two is B; A and C will just be guesses at first
        QrPoint pointA, pointB, pointC;
        if (oneTwoDistance >= zeroOneDistance && oneTwoDistance >= zeroTwoDistance)
        {
            pointB = patterns[0];
            pointA = patterns[1];
            pointC = patterns[2];
        }
        else if (zeroTwoDistance >= oneTwoDistance && zeroTwoDistance >= zeroOneDistance)
        {
            pointB = patterns[1];
            pointA = patterns[0];
            pointC = patterns[2];
        }
        else
        {
            pointB = patterns[2];
            pointA = patterns[0];
            pointC = patterns[1];
        }

        // Use cross product to figure out whether A and C are correct or flipped.
        // This asks whether BC x BA has a positive z component, which is the arrangement
        // we want for A, B, C. If it's negative, then we've got it flipped around and
        // should swap A and C.
        if (CrossProductZ(pointA, pointB, pointC) < 0.0f)
        {
            // Swap A and C
            (pointC, pointA) = (pointA, pointC);
        }

        patterns[0] = pointA;
        patterns[1] = pointB;
        patterns[2] = pointC;
    }

    /// <summary> Returns the z component of the cross product between vectors BC and BA. </summary>
    internal static double CrossProductZ(QrPoint pointA, QrPoint pointB, QrPoint pointC)
    {
        float bX = pointB.X;
        float bY = pointB.Y;
        return ((pointC.X - bX) * (pointA.Y - bY)) - ((pointC.Y - bY) * (pointA.X - bX));
    }
}
