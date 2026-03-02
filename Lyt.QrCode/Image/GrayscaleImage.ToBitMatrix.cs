namespace Lyt.QrCode.Image;

internal sealed partial class GrayscaleImage
{
    // The Adaptive Thresholding method uses 5x5 blocks to compute local luminance, where each block is 8x8 pixels.
    // So this is the smallest dimension in each axis we can accept.
    private const int BLOCK_SIZE_POWER = 3;
    private const int BLOCK_SIZE = 1 << BLOCK_SIZE_POWER; // ...0100...00
    private const int BLOCK_SIZE_MASK = BLOCK_SIZE - 1;   // ...0011...11
    private const int MINIMUM_DIMENSION = 40;
    private const int MIN_DYNAMIC_RANGE = 24;

    internal BitMatrixImage ToBitMatrix()
    {
        if (this.Width >= MINIMUM_DIMENSION && this.Height >= MINIMUM_DIMENSION)
        {
            return this.ToBitMatrixAdaptiveThresholding();
        }
        else
        {
            // If the image is too small, fall back to the global histogram approach.
            this.HistogramEqualization();
            return this.ToBitMatrixBasicThresholding();
        }
    }

    internal BitMatrixImage ToBitMatrixBasicThresholding()
    {
        var bitMatrix = new BitMatrixImage(this.Width, this.Height);
        for (int y = 0; y < this.Height; y++)
        {
            for (int x = 0; x < this.Width; x++)
            {
                // Basic thresholding at mid point (128) for simplicity.
                // True means black, so set the corresponding bit in the BitMatrix.
                bitMatrix[x,y] = this.Pixels[y * this.Width + x] < 128;
            }
        }

        return bitMatrix;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Cap(int value, int max) => value < 2 ? 2 : value > max ? max : value;

    internal BitMatrixImage ToBitMatrixAdaptiveThresholding()
    {
        int width = this.Width;
        int height = this.Height;
        int subWidth = width >> BLOCK_SIZE_POWER;
        if ((width & BLOCK_SIZE_MASK) != 0)
        {
            subWidth++;
        }

        int subHeight = height >> BLOCK_SIZE_POWER;
        if ((height & BLOCK_SIZE_MASK) != 0)
        {
            subHeight++;
        }

        byte[] luminances = this.Pixels;

        /// Calculates a single black point for each 8x8 block of pixels and saves it away.
        int[][] CalculateBlackPoints()
        {
            int maxYOffset = height - BLOCK_SIZE;
            int maxXOffset = width - BLOCK_SIZE;
            int[][] blackPoints = new int[subHeight][];
            for (int i = 0; i < subHeight; i++)
            {
                blackPoints[i] = new int[subWidth];
            }

            for (int y = 0; y < subHeight; y++)
            {
                int yoffset = y << BLOCK_SIZE_POWER;
                if (yoffset > maxYOffset)
                {
                    yoffset = maxYOffset;
                }

                int[] blackPointsY = blackPoints[y];
                int[]? blackPointsY1 = y > 0 ? blackPoints[y - 1] : null;
                for (int x = 0; x < subWidth; x++)
                {
                    int xoffset = x << BLOCK_SIZE_POWER;
                    if (xoffset > maxXOffset)
                    {
                        xoffset = maxXOffset;
                    }

                    int sum = 0;
                    int min = 0xFF;
                    int max = 0;
                    for (int yy = 0, offset = yoffset * width + xoffset; yy < BLOCK_SIZE; yy++, offset += width)
                    {
                        for (int xx = 0; xx < BLOCK_SIZE; xx++)
                        {
                            int pixel = luminances[offset + xx] & 0xFF;
                            // still looking for good contrast
                            sum += pixel;
                            if (pixel < min)
                            {
                                min = pixel;
                            }
                            if (pixel > max)
                            {
                                max = pixel;
                            }
                        }

                        // short-circuit min/max tests once dynamic range is met
                        if (max - min > MIN_DYNAMIC_RANGE)
                        {
                            // finish the rest of the rows quickly
                            for (yy++, offset += width; yy < BLOCK_SIZE; yy++, offset += width)
                            {
                                for (int xx = 0; xx < BLOCK_SIZE; xx++)
                                {
                                    sum += luminances[offset + xx] & 0xFF;
                                }
                            }
                        }
                    }

                    // The default estimate is the average of the values in the block.
                    int average = sum >> (BLOCK_SIZE_POWER * 2);
                    if (max - min <= MIN_DYNAMIC_RANGE)
                    {
                        // If variation within the block is low, assume this is a block with only light or only
                        // dark pixels. In that case we do not want to use the average, as it would divide this
                        // low contrast area into black and white pixels, essentially creating data out of noise.
                        //
                        // The default assumption is that the block is light/background. Since no estimate for
                        // the level of dark pixels exists locally, use half the min for the block.
                        average = min >> 1;

                        if (blackPointsY1 != null && x > 0)
                        {
                            // Correct the "white background" assumption for blocks that have neighbors by comparing
                            // the pixels in this block to the previously calculated black points. This is based on
                            // the fact that dark barcode symbology is always surrounded by some amount of light
                            // background for which reasonable black point estimates were made. The bp estimated at
                            // the boundaries is used for the interior.

                            // The (min < bp) is arbitrary but works better than other heuristics that were tried.
                            int averageNeighborBlackPoint = (blackPointsY1[x] + (2 * blackPointsY[x - 1]) + blackPointsY1[x - 1]) >> 2;
                            if (min < averageNeighborBlackPoint)
                            {
                                average = averageNeighborBlackPoint;
                            }
                        }
                    }

                    blackPointsY[x] = average;
                }
            }

            return blackPoints;
        }

        int[][] blackPoints = CalculateBlackPoints();

        var bitMatrix = new BitMatrixImage(width, height);

        /// For each 8x8 block in the image, calculate the average black point using a 5x5 grid
        /// of the blocks around it. Also handles the corner cases (fractional blocks are computed based
        /// on the last 8 pixels in the row/column which are also used in the previous block).
        void CalculateThresholdForBlock()
        {
            /// <summary> Applies a single threshold to an 8x8 block of pixels. </summary>
            /// <param name="luminances">The luminances.</param>
            /// <param name="xoffset">The xoffset.</param>
            /// <param name="yoffset">The yoffset.</param>
            /// <param name="threshold">The threshold.</param>
            /// <param name="stride">The stride.</param>
            /// <param name="matrix">The matrix.</param>
            void ThresholdBlock(
                int xoffset, int yoffset, int threshold, int stride)
            {
                int offset = (yoffset * stride) + xoffset;
                for (int y = 0; y < BLOCK_SIZE; y++, offset += stride)
                {
                    for (int x = 0; x < BLOCK_SIZE; x++)
                    {
                        int pixel = luminances[offset + x] & 0xff;
                        // Comparison needs to be <= so that black == 0 pixels are black even if the threshold is 0.
                        bitMatrix[xoffset + x, yoffset + y] = (pixel <= threshold);
                    }
                }
            }

            int maxYOffset = height - BLOCK_SIZE;
            int maxXOffset = width - BLOCK_SIZE;

            for (int y = 0; y < subHeight; y++)
            {
                int yoffset = y << BLOCK_SIZE_POWER;
                if (yoffset > maxYOffset)
                {
                    yoffset = maxYOffset;
                }

                int top = Cap(y, subHeight - 3);
                for (int x = 0; x < subWidth; x++)
                {
                    int xoffset = x << BLOCK_SIZE_POWER;
                    if (xoffset > maxXOffset)
                    {
                        xoffset = maxXOffset;
                    }

                    int left = Cap(x, subWidth - 3);
                    int sum = 0;
                    for (int z = -2; z <= 2; z++)
                    {
                        int[] blackRow = blackPoints[top + z];
                        sum += blackRow[left - 2];
                        sum += blackRow[left - 1];
                        sum += blackRow[left];
                        sum += blackRow[left + 1];
                        sum += blackRow[left + 2];
                    }

                    int average = sum / 25;
                    ThresholdBlock(xoffset, yoffset, average, width);
                }
            }
        }

        CalculateThresholdForBlock();
        return bitMatrix;
    }
}
