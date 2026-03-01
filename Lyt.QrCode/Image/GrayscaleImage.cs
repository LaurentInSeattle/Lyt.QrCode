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

    internal byte[] GetRow (int y)
    {
        if ((y < 0) || (y >= this.Height))
        {
            throw new ArgumentOutOfRangeException(nameof(y), "Row index is out of bounds.");
        }

        byte[] row = new byte[this.Width];
        Array.Copy(this.Pixels, y * this.Width, row, 0, this.Width);
        return row;
    }
}

/*

    public class MyBitplane
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public byte[,] PixelData { get; set; }

        public MyBitplane(MyBitplane bitplane)
        {
            this.Width = bitplane.Width;
            this.Height = bitplane.Height;

            for (int y = 0; y < this.Height; ++y)
                for (int x = 0; x < this.Width; ++x)
                    SetPixel(x, y, bitplane.GetPixel(x, y));
        }

        public MyBitplane(int w, int h)
        {
            Width = w;
            Height = h;

            PixelData = new byte[Height, Width];
        }

        public byte GetPixel(int x, int y)
        {
            return PixelData[y, x];
        }

        public void SetPixel(int x, int y, byte value)
        {
            PixelData[y, x] = value;
        }
    }
}

    /// Histogram Equalization
        /// </summary>
        /// <param name="bitplane">bitplane of current image</param>
        private static void HE(ref MyBitplane bitplane)
        {
            // Histogram
            double[] histogram = calculateHistogram(bitplane);

            // Probability
            double totalElements = bitplane.Width * bitplane.Height;
            double[] probability = new double[256];
            int i;
            for (i = 0; i < 256; i++)
                probability[i] = histogram[i] / totalElements;

            // Comulative probability
            double[] comulativeProbability = calculateComulativeFrequency(probability);

            // Multiply comulative probability by 256
            int[] floorProbability = new int[256];
            for (i = 0; i < 256; i++)
                floorProbability[i] = (int)Math.Floor(comulativeProbability[i] * 255);

            // Transform old value to new value
            int x;
            for (int y = 0; y < bitplane.Height; y++)
                for (x = 0; x < bitplane.Width; x++)
                    bitplane.SetPixel(x, y, (byte)floorProbability[bitplane.GetPixel(x, y)]);
        }
 
        /// <summary>
        /// Calculates histogram based on input bitplane
        /// </summary>
        /// <param name="bitplane">bitplane of current image</param>
        /// <returns>double array histogram of input bitplane</returns>
        public static double[] calculateHistogram(MyBitplane bitplane)
        }

        /// <summary>
        /// Calculate comulative frequency of input array
        /// </summary>
        /// <param name="array">double array of frequencies</param>
        /// <returns>double array for comulative frequencies</returns>
        public static double[] calculateComulativeFrequency(double[] array)


        } */