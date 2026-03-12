namespace Lyt.QrCode.Detector;

public sealed class Patterns(
    Pattern bottomLeft, Pattern topLeft, Pattern topRight, AlignmentPattern? alignmentPattern)
{
    public Pattern TopLeft { get; } = topLeft;

    public Pattern TopRight { get; } = topRight;

    public Pattern BottomLeft { get; } = bottomLeft;

    public AlignmentPattern? AlignmentPattern { get; } = alignmentPattern;

    internal bool TryProcess(
        BitMatrixImage image, 
        DetectorCallback? detectorCallback,
        [NotNullWhen(true)] out DetectorResult? detectorResult)
    {
        detectorResult = null;
        float moduleSize = this.CalculateModuleSize(image);
        if (float.IsNaN(moduleSize) || (moduleSize < 1.0))
        {
            return false;
        }

        bool CalculateDimension(out int dimension)
        {
            int tltrCentersDimension =
                MathExtensions.Round(QrPoint.Distance(this.TopLeft, this.TopRight) / moduleSize);
            int tlblCentersDimension =
                MathExtensions.Round(QrPoint.Distance(this.TopLeft, this.BottomLeft) / moduleSize);
            dimension = ((tltrCentersDimension + tlblCentersDimension) >> 1) + 7;
            switch (dimension & 0x03)
            {
                // mod 4
                case 0:
                    dimension++;
                    break;
                // 1? do nothing
                case 2:
                    dimension--;
                    break;
                case 3:
                    dimension -= 2;
                    break;
            }

            return true;
        }

        if ( !CalculateDimension( out int dimension)) 
        { 
            return false; 
        }

        if ( !QrVersion.IsValidDimension(dimension))
        {
            return false;
        }

        // Anything above version 1 has an alignment pattern
        var provisionalVersion = QrVersion.FromDimension(dimension);
        int modulesBetweenFPCenters = provisionalVersion.DimensionForVersion - 7;
        AlignmentPattern? alignmentPattern = null;
        if (provisionalVersion.HasAlignmentPatternCenters)
        {
            // Guess where a "bottom right" finder pattern would have been
            float bottomRightX = this.TopRight.X - this.TopLeft.X + this.BottomLeft.X;
            float bottomRightY = this.TopRight.Y - this.TopLeft.Y + this.BottomLeft.Y;

            // Estimate that alignment pattern is closer by 3 modules
            // from "bottom right" to known top left location
            float correctionToTopLeft = 1.0f - 3.0f / (float)modulesBetweenFPCenters;
            int estAlignmentX = (int)(this.TopLeft.X + correctionToTopLeft * (bottomRightX - this.TopLeft.X));
            int estAlignmentY = (int)(this.TopLeft.Y + correctionToTopLeft * (bottomRightY - this.TopLeft.Y));

            /// Attempts to locate an alignment pattern in a limited region of the image, which is
            /// guessed to contain it. This method uses {@link AlignmentPattern}.</p>
            /// <param name="overallEstModuleSize">estimated module size so far</param>
            /// <param name="estAlignmentX">x coordinate of center of area probably containing alignment pattern</param>
            /// <param name="estAlignmentY">y coordinate of above</param>
            /// <param name="allowanceFactor">number of pixels in all directions to search from the center</param>
            bool TryFindAlignmentInRegion(
                float overallEstModuleSize, 
                int estAlignmentX, int estAlignmentY, 
                float allowanceFactor,
                [NotNullWhen(true)] out AlignmentPattern? alignmentPattern)
            {
                alignmentPattern = null;

                // Look for an alignment pattern (3 modules in size) around where it should be
                int allowance = (int)(allowanceFactor * overallEstModuleSize);
                int alignmentAreaLeftX = Math.Max(0, estAlignmentX - allowance);
                int alignmentAreaRightX = Math.Min(image.Width - 1, estAlignmentX + allowance);
                if (alignmentAreaRightX - alignmentAreaLeftX < overallEstModuleSize * 3)
                {
                    return false;
                }

                int alignmentAreaTopY = Math.Max(0, estAlignmentY - allowance);
                int alignmentAreaBottomY = Math.Min(image.Height - 1, estAlignmentY + allowance);

                if (image.TryFindAlignmentPattern(
                    alignmentAreaLeftX,
                    alignmentAreaTopY,
                    alignmentAreaRightX - alignmentAreaLeftX,
                    alignmentAreaBottomY - alignmentAreaTopY,
                    overallEstModuleSize,
                    detectorCallback,
                    out alignmentPattern))
                {

                    return true;
                }

                return false;
            }

            // Kind of arbitrary -- expand search radius before giving up
            for (int i = 4; i <= 16; i <<= 1)
            {
                if (TryFindAlignmentInRegion(
                    moduleSize, estAlignmentX, estAlignmentY, (float)i, out alignmentPattern))
                {
                    Debug.WriteLine($"Found alignment pattern at ({alignmentPattern.X},{alignmentPattern.Y}) with estimated module size {moduleSize}");
                    break;
                }
            }
        }

        // If we didn't find alignment pattern... we'll try anyway without it
        var transform = PerspectiveTransform.CreateFromPatterns(
            this.TopLeft, this.TopRight, this.BottomLeft, alignmentPattern, dimension);
        if (!image.TryResample(dimension, transform, out BitMatrixImage? resampled ))
        {
            return false;
        }

        detectorResult = new DetectorResult(resampled, this);
        return true;
    }

    /// <summary>
    /// Computes an average estimated module size based on estimated derived from the positions
    /// of the three finder patterns.
    /// </summary>
    private float CalculateModuleSize(BitMatrixImage image)
    {
        /// <summary> <p>This method traces a line from a point in the image, in the direction towards another point.
        /// It begins in a black region, and keeps going until it finds white, then black, then white again.
        /// It reports the distance from the start to this point.</p>
        /// 
        /// <p>This is used when figuring out how wide a finder pattern is, when the finder pattern
        /// may be skewed or rotated.</p>
        /// </summary>
        float SizeOfBlackWhiteBlackRun(int fromX, int fromY, int toX, int toY)
        {
            // Mild variant of Bresenham's algorithm;
            // see http://en.wikipedia.org/wiki/Bresenham's_line_algorithm
            bool steep = Math.Abs(toY - fromY) > Math.Abs(toX - fromX);
            if (steep)
            {
                (fromY, fromX) = (fromX, fromY);
                (toY, toX) = (toX, toY);
            }

            int dx = Math.Abs(toX - fromX);
            int dy = Math.Abs(toY - fromY);
            int error = -dx >> 1;
            int xstep = fromX < toX ? 1 : -1;
            int ystep = fromY < toY ? 1 : -1;

            // In black pixels, looking for white, first or second time.
            // Loop up until x == toX, but not beyond
            int state = 0;
            int xLimit = toX + xstep;
            for (int x = fromX, y = fromY; x != xLimit; x += xstep)
            {
                int realX = steep ? y : x;
                int realY = steep ? x : y;

                // Does current pixel mean we have moved white to black or vice versa?
                // Scanning black in state 0,2 and white in state 1, so if we find the wrong
                // color, advance to next state or end if we are in state 2 already
                if ((state == 1) == image[realX, realY])
                {
                    if (state == 2)
                    {
                        return MathExtensions.Distance(x, y, fromX, fromY);
                    }
                    state++;
                }

                error += dy;
                if (error > 0)
                {
                    if (y == toY)
                    {


                        break;
                    }
                    y += ystep;
                    error -= dx;
                }
            }

            // Found black-white-black; give the benefit of the doubt that the next pixel outside the image
            // is "white" so this last point at (toX+xStep,toY) is the right ending. This is really a
            // small approximation; (toX+xStep,toY+yStep) might be really correct. Ignore this.
            if (state == 2)
            {
                return MathExtensions.Distance(toX + xstep, toY, fromX, fromY);
            }

            // else we didn't find even black-white-black; no estimate is really possible
            return float.NaN;

        }

        /// <summary> See {@link #sizeOfBlackWhiteBlackRun(int, int, int, int)}; computes the total width of
        /// a finder pattern by looking for a black-white-black run from the center in the direction
        /// of another point (another finder pattern center), and in the opposite direction too.
        /// </summary>
        float SizeOfBlackWhiteBlackRunBothWays(int fromX, int fromY, int toX, int toY)
        {
            float result = SizeOfBlackWhiteBlackRun(fromX, fromY, toX, toY);

            // Now count other way -- don't run off image though of course
            float scale = 1.0f;
            int otherToX = fromX - (toX - fromX);
            if (otherToX < 0)
            {
                //UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1042'"
                scale = (float)fromX / (float)(fromX - otherToX);
                otherToX = 0;
            }
            else if (otherToX >= image.Width)
            {
                scale = (float)(image.Width - 1 - fromX) / (float)(otherToX - fromX);
                otherToX = image.Width - 1;
            }

            int otherToY = (int)(fromY - (toY - fromY) * scale);

            scale = 1.0f;
            if (otherToY < 0)
            {
                scale = (float)fromY / (float)(fromY - otherToY);
                otherToY = 0;
            }
            else if (otherToY >= image.Height)
            {
                scale = (float)(image.Height - 1 - fromY) / (float)(otherToY - fromY);
                otherToY = image.Height - 1;
            }

            otherToX = (int)(fromX + (otherToX - fromX) * scale);

            result += SizeOfBlackWhiteBlackRun(fromX, fromY, otherToX, otherToY);
            return result - 1.0f; // -1 because we counted the middle pixel twice
        }

        /// <summary> <p>Estimates module size based on two finder patterns -- it uses
        /// {@link #sizeOfBlackWhiteBlackRunBothWays(int, int, int, int)} to figure the
        /// width of each, measuring along the axis between their centers.</p>
        /// </summary>
        float CalculateModuleSizeOneWay(QrPoint pattern, QrPoint otherPattern)
        {
            float moduleSizeEst1 = SizeOfBlackWhiteBlackRunBothWays((int)pattern.X, (int)pattern.Y, (int)otherPattern.X, (int)otherPattern.Y);
            float moduleSizeEst2 = SizeOfBlackWhiteBlackRunBothWays((int)otherPattern.X, (int)otherPattern.Y, (int)pattern.X, (int)pattern.Y);
            if (float.IsNaN(moduleSizeEst1))
            {
                return moduleSizeEst2 / 7.0f;
            }

            if (float.IsNaN(moduleSizeEst2))
            {
                return moduleSizeEst1 / 7.0f;
            }

            // Average them, and divide by 7 since we've counted the width of 3 black modules,
            // and 1 white and 1 black module on either side. Ergo, divide sum by 14.
            return (moduleSizeEst1 + moduleSizeEst2) / 14.0f;
        }

        // Take the average
        return (CalculateModuleSizeOneWay(this.TopLeft, this.TopRight) +
                CalculateModuleSizeOneWay(this.TopLeft, this.BottomLeft)) / 2.0f;
    }
}

