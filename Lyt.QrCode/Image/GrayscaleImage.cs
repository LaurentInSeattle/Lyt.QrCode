namespace Lyt.QrCode.Image;

internal class GrayscaleImage
{
    public int Width { get; }

    public int Height { get; }

    public byte[] Pixels { get; }

    internal GrayscaleImage(int width, int height, byte[] pixels)
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
        this.Pixels = pixels;
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
