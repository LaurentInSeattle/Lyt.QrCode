namespace Lyt.QrCode.Image;

public sealed partial class BitMatrixImage
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
        resampled = null;

        if (dimension <= 0 || dimension <= 0)
        {
            return false;
        }

        resampled = new BitMatrixImage(dimension, dimension);
        float[] points = new float[dimension << 1];

        for (int y = 0; y < dimension; y++)
        {
            int max = points.Length;
            float iValue = (float)y + 0.5f;
            for (int x = 0; x < max; x += 2)
            {
                points[x] = (float)(x >> 1) + 0.5f;
                points[x + 1] = iValue;
            }

            transform.TransformPointsInPlace(points);

            // Quick check to see if points transformed to something inside the image;
            // sufficient to check the endpoints
            if (!CheckAndNudgePoints(this, points))
            {
                return false;
            } 

            try
            {
                int imageWidth = this.Width;
                int imageHeight = this.Height;
                for (int x = 0; x < max; x += 2)
                {
                    int imagex = (int)points[x];
                    int imagey = (int)points[x + 1];

                    if (imagex < 0 || imagex >= imageWidth || imagey < 0 || imagey >= imageHeight)
                    {
                        return false;
                    }

                    resampled[x >> 1, y] = this[imagex, imagey];
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                // java version:
                // 
                // This feels wrong, but, sometimes if the finder patterns are misidentified, the resulting
                // transform gets "twisted" such that it maps a straight line of points to a set of points
                // whose endpoints are in bounds, but others are not. There is probably some mathematical
                // way to detect this about the transformation that I don't know yet.
                // This results in an ugly runtime exception despite our clever checks above -- can't have
                // that. We could check each point's coordinates but that feels duplicative. We settle for
                // catching and wrapping ArrayIndexOutOfBoundsException.
                if (Debugger.IsAttached) {  Debug.WriteLine($"Exception in TryResample: {ex}"); }
                return false;
            }
        }

        return true;
    }

    /// <summary> <p>Checks a set of points that have been transformed to sample points on an image against
    /// the image's dimensions to see if the point are even within the image.</p>
    /// 
    /// <p>This method will actually "nudge" the endpoints back onto the image if they are found to be
    /// barely (less than 1 pixel) off the image. This accounts for imperfect detection of finder
    /// patterns in an image where the QR Code runs all the way to the image border.</p>
    /// 
    /// <p>For efficiency, the method will check points from either end of the line until one is found
    /// to be within the image. Because the set of points are assumed to be linear, this is valid.</p>
    /// </summary>
    /// <param name="image">image into which the points should map
    /// </param>
    /// <param name="points">actual points in x1,y1,...,xn,yn form
    /// </param>
    private static bool CheckAndNudgePoints(BitMatrixImage image, float[] points)
    {
        int width = image.Width;
        int height = image.Height;

        // Check and nudge points from start until we see some that are OK:
        bool nudged = true;
        int maxOffset = points.Length - 1; // points.length must be even
        for (int offset = 0; offset < maxOffset && nudged; offset += 2)
        {
            int x = (int)points[offset];
            int y = (int)points[offset + 1];
            if (x < -1 || x > width || y < -1 || y > height)
            {
                return false;
            }

            nudged = false;
            if (x == -1)
            {
                points[offset] = 0.0f;
                nudged = true;
            }
            else if (x == width)
            {
                points[offset] = width - 1;
                nudged = true;
            }
            if (y == -1)
            {
                points[offset + 1] = 0.0f;
                nudged = true;
            }
            else if (y == height)
            {
                points[offset + 1] = height - 1;
                nudged = true;
            }
        }

        // Check and nudge points from end:
        nudged = true;
        for (int offset = points.Length - 2; offset >= 0 && nudged; offset -= 2)
        {
            int x = (int)points[offset];
            int y = (int)points[offset + 1];
            if (x < -1 || x > width || y < -1 || y > height)
            {
                return false;
            }

            nudged = false;
            if (x == -1)
            {
                points[offset] = 0.0f;
                nudged = true;
            }
            else if (x == width)
            {
                points[offset] = width - 1;
                nudged = true;
            }
            if (y == -1)
            {
                points[offset + 1] = 0.0f;
                nudged = true;
            }
            else if (y == height)
            {
                points[offset + 1] = height - 1;
                nudged = true;
            }
        }

        return true;
    }
}
