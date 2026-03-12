namespace Lyt.QrCode.Image;

internal sealed partial class GrayscaleImage
{
    // The Adaptive Thresholding method uses 5x5 blocks to compute local luminance, where each block is 8x8 pixels.
    // So this is the smallest dimension in each axis we can accept.
    private const int BlockSizePower = 3;
    private const int BlockSize = 1 << BlockSizePower; // ...0100...00
    private const int BlockSizeMask = BlockSize - 1;   // ...0011...11
    private const int MinimumDimension = 40;
    private const int MinimumDynamicRange = 24;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Cap(int value, int max) => value < 2 ? 2 : value > max ? max : value;

    internal BitMatrixImage ToBitMatrix()
    {
        if (this.Width >= MinimumDimension && this.Height >= MinimumDimension)
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

    internal BitMatrixImage ToBitMatrixAdaptiveThresholding()
    {
        int width = this.Width;
        int height = this.Height;
        int subWidth = width >> BlockSizePower;
        if ((width & BlockSizeMask) != 0)
        {
            subWidth++;
        }

        int subHeight = height >> BlockSizePower;
        if ((height & BlockSizeMask) != 0)
        {
            subHeight++;
        }

        byte[] luminances = this.Pixels;

        /// Calculates a single black point for each 8x8 block of pixels and saves it away.
        int[][] CalculateBlackPoints()
        {
            int maxYOffset = height - BlockSize;
            int maxXOffset = width - BlockSize;
            int[][] blackPoints = new int[subHeight][];
            for (int i = 0; i < subHeight; i++)
            {
                blackPoints[i] = new int[subWidth];
            }

            for (int y = 0; y < subHeight; y++)
            {
                int yoffset = y << BlockSizePower;
                if (yoffset > maxYOffset)
                {
                    yoffset = maxYOffset;
                }

                int[] blackPointsY = blackPoints[y];
                int[]? blackPointsY1 = y > 0 ? blackPoints[y - 1] : null;
                for (int x = 0; x < subWidth; x++)
                {
                    int xoffset = x << BlockSizePower;
                    if (xoffset > maxXOffset)
                    {
                        xoffset = maxXOffset;
                    }

                    int sum = 0;
                    int min = 0xFF;
                    int max = 0;
                    for (int yy = 0, offset = yoffset * width + xoffset; yy < BlockSize; yy++, offset += width)
                    {
                        for (int xx = 0; xx < BlockSize; xx++)
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
                        if (max - min > MinimumDynamicRange)
                        {
                            // finish the rest of the rows quickly
                            for (yy++, offset += width; yy < BlockSize; yy++, offset += width)
                            {
                                for (int xx = 0; xx < BlockSize; xx++)
                                {
                                    sum += luminances[offset + xx] & 0xFF;
                                }
                            }
                        }
                    }

                    // The default estimate is the average of the values in the block.
                    int average = sum >> (BlockSizePower * 2);
                    if (max - min <= MinimumDynamicRange)
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
                for (int y = 0; y < BlockSize; y++, offset += stride)
                {
                    for (int x = 0; x < BlockSize; x++)
                    {
                        int pixel = luminances[offset + x] & 0xff;
                        // Comparison needs to be <= so that black == 0 pixels are black even if the threshold is 0.
                        bitMatrix[xoffset + x, yoffset + y] = (pixel <= threshold);
                    }
                }
            }

            int maxYOffset = height - BlockSize;
            int maxXOffset = width - BlockSize;

            for (int y = 0; y < subHeight; y++)
            {
                int yoffset = y << BlockSizePower;
                if (yoffset > maxYOffset)
                {
                    yoffset = maxYOffset;
                }

                int top = Cap(y, subHeight - 3);
                for (int x = 0; x < subWidth; x++)
                {
                    int xoffset = x << BlockSizePower;
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
