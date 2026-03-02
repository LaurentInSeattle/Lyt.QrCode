namespace Lyt.QrCode.Image;

internal class GrayscaleImage
{
    public int Width { get; }

    public int Height { get; }

    public byte[] Pixels { get; }

    internal GrayscaleImage(int width, int height, byte[] pixels, bool isLocked = false)
    {
        if ((width <= 0) || (height <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width and Height must be positive.");
        }

        if (pixels.Length != width * height)
        {
            throw new ArgumentException("Pixel data length does not match width and height.", nameof(pixels));
        }

        this.Width = width;
        this.Height = height;

        if (isLocked)
        {
            // Copy the pixel data bacause the input array is locked in memory by the graphics framework of the app.
            byte[] clonedPixels = new byte[pixels.Length];
            Array.Copy(pixels, clonedPixels, pixels.Length);
            this.Pixels = clonedPixels;
        }
        else
        {
            this.Pixels = pixels;
        }
    }

    internal GrayscaleImage(int width, int height, int stride, byte[] pixels)
    {
        if ((width <= 0) || (height <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width and Height must be positive.");
        }
        if (pixels.Length != height * stride)
        {
            throw new ArgumentException("Pixel data length does not match height and stride.", nameof(pixels));
        }

        this.Width = width;
        this.Height = height;
        this.Pixels = new byte[width * height];

        for (int j = 0; j < height; j++)
        {
            Array.Copy(pixels, j * stride, this.Pixels, j * width, width);
        }
    }

    internal GrayscaleImage Clone()
    {
        byte[] clonedPixels = new byte[this.Pixels.Length];
        Array.Copy(this.Pixels, clonedPixels, this.Pixels.Length);
        return new GrayscaleImage(this.Width, this.Height, clonedPixels);
    }

    internal BitMatrixImage ToBitMatrix()
    {
        var bitMatrix = new BitMatrixImage(this.Width, this.Height);
        for (int y = 0; y < this.Height; y++)
        {
            for (int x = 0; x < this.Width; x++)
            {
                // TODO:
                // Implement adaptive thresholding for better performance in different lighting conditions.
                if (this.Pixels[y * this.Width + x] < 128) // Thresholding at 128
                {
                    int index = y * bitMatrix.Stride + (x >> 5);
                    bitMatrix.Bits[index] |= (1 << (x & 0x1F));
                }
            }
        }

        return bitMatrix;
    }

    internal GrayscaleImage Crop(int x, int y, int width, int height)
    {
        if ((x < 0) || (y < 0) || (width <= 0) || (height <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Crop parameters must be non-negative and dimensions must be positive.");
        }

        if ((x + width > this.Width) || (y + height > this.Height))
        {
            throw new ArgumentException("Crop area exceeds image bounds.");
        }

        byte[] croppedPixels = new byte[width * height];
        for (int j = 0; j < height; j++)
        {
            Array.Copy(this.Pixels, (y + j) * this.Width + x, croppedPixels, j * width, width);
        }

        return new GrayscaleImage(width, height, croppedPixels);
    }

    internal GrayscaleImage FlipVertical()
    {
        byte[] flippedPixels = new byte[this.Pixels.Length];
        for (int y = 0; y < this.Height; y++)
        {
            Array.Copy(this.Pixels, y * this.Width, flippedPixels, (this.Height - 1 - y) * this.Width, this.Width);
        }

        return new GrayscaleImage(this.Width, this.Height, flippedPixels);
    }

    internal GrayscaleImage FlipHorizontal()
    {
        byte[] flippedPixels = new byte[this.Pixels.Length];
        for (int y = 0; y < this.Height; y++)
        {
            for (int x = 0; x < this.Width; x++)
            {
                flippedPixels[y * this.Width + (this.Width - 1 - x)] = this.Pixels[y * this.Width + x];
            }
        }

        return new GrayscaleImage(this.Width, this.Height, flippedPixels);
    }

    internal GrayscaleImage Rotate90()
    {
        byte[] rotatedPixels = new byte[this.Pixels.Length];
        for (int y = 0; y < this.Height; y++)
        {
            for (int x = 0; x < this.Width; x++)
            {
                rotatedPixels[x * this.Height + (this.Height - 1 - y)] = this.Pixels[y * this.Width + x];
            }
        }

        return new GrayscaleImage(this.Height, this.Width, rotatedPixels);
    }

    internal GrayscaleImage Rotate180()
    {
        byte[] rotatedPixels = new byte[this.Pixels.Length];
        for (int y = 0; y < this.Height; y++)
        {
            for (int x = 0; x < this.Width; x++)
            {
                rotatedPixels[(this.Height - 1 - y) * this.Width + (this.Width - 1 - x)] = this.Pixels[y * this.Width + x];
            }
        }

        return new GrayscaleImage(this.Width, this.Height, rotatedPixels);
    }

    internal GrayscaleImage Invert()
    {
        byte[] invertedPixels = new byte[this.Pixels.Length];
        for (int i = 0; i < this.Pixels.Length; i++)
        {
            invertedPixels[i] = (byte)(255 - this.Pixels[i]);
        }

        return new GrayscaleImage(this.Width, this.Height, invertedPixels);
    }

#if DEBUG
    internal byte GetPixel(int x, int y)
    {
        if ((x < 0) || (x >= this.Width) || (y < 0) || (y >= this.Height))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Pixel coordinates are out of bounds.");
        }

        return this.Pixels[y * this.Width + x];
    }
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte GetPixel(int x, int y) => this.Pixels[y * this.Width + x];
#endif 

    internal byte[] GetRow(int y)
    {
        if ((y < 0) || (y >= this.Height))
        {
            throw new ArgumentOutOfRangeException(nameof(y), "Row index is out of bounds.");
        }

        byte[] row = new byte[this.Width];
        Array.Copy(this.Pixels, y * this.Width, row, 0, this.Width);
        return row;
    }

    /// <summary> Enhances the contrast of the grayscale image by redistributing pixel intensity values using histogram equalization. </summary>
    /// <remarks> 
    /// This method modifies the pixel values in place to achieve a more uniform distribution of
    /// intensities, which can improve the visibility of features in images with poor contrast. The image must be in
    /// grayscale format prior to calling this method. Histogram equalization is commonly used in image processing to
    /// normalize lighting and improve detail in photographs or scanned documents.
    /// </remarks>
    internal void HistogramEqualization()
    {
        // Calculate Histogram 
        double[] histogram = new double[256];
        for (int y = 0; y < this.Height; ++y)
        {
            for (int x = 0; x < this.Width; ++x)
            {
                byte pixelValue = this.Pixels[y * this.Width + x];
                ++histogram[pixelValue];
            }
        }

        // Probability
        double totalElements = this.Width * this.Height;
        double[] probability = new double[256];
        for (int i = 0; i < 256; i++)
        {
            probability[i] = histogram[i] / totalElements;
        }

        // Calculate comulative frequency of probability array
        double[] cumulativeProbability = new double[256];
        cumulativeProbability[0] = probability[0];
        for (int i = 1; i < 256; ++i)
        {
            cumulativeProbability[i] = cumulativeProbability[i - 1] + probability[i];
        }
    
        // Multiply all cumulative probabilities by 255
        int[] floorProbability = new int[256];
        for (int i = 0; i < 256; i++)
        {
            floorProbability[i] = (int)Math.Floor(cumulativeProbability[i] * 255);
        }

        // Adjust all pixel values in the original image based on the floor probability array
        for (int y = 0; y < this.Height; y++)
        {
            for (int x = 0; x < this.Width; x++)
            {
                this.Pixels[y * this.Width + x] = (byte)floorProbability[this.Pixels[y * this.Width + x]];
            }
        }
    }
}
