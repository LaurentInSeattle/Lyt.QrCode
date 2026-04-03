namespace Lyt.QrCode.Render;

using Lyt.Png;

internal static class ImageRenderer
{
    const int imageHeaderSize = 54;

    /// <summary>  Creates a BITMAP image of the provided Pixel Provider. (usually a QR code) </summary>
    /// <param name="pixelProvider">The pixelProvider</param>
    /// <param name="border">The border width, as a factor of the module (QR code pixel) size.</param>
    /// <param name="scale">The width and height, in pixels, of each module.</param>
    /// <param name="foreground">The foreground color (dark modules), in RGB value (little endian).</param>
    /// <param name="background">The background color (light modules), in RGB value (little endian).</param>
    /// <returns> A Bitmap image, as a byte array.</returns>
    internal static byte[] ToBitmapImage(
        this IPixelProvider pixelProvider,
        int scale, int border, int foreground = 0, int background = 0xFFFFFF)
    {
        int width = pixelProvider.Width;
        int height = pixelProvider.Height;
        int imageWidth = (width + border * 2) * scale;
        int imageHeight = (height + border * 2) * scale;
        var sourceImage = SourceImage.FromPixelProvider(pixelProvider, scale, border, foreground, background);
        byte[] bitmapBytes = new byte[imageHeaderSize + sourceImage.Pixels.Length];

        // Generate Bitmap Header
        bitmapBytes[0] = (byte)'B';
        bitmapBytes[1] = (byte)'M';
        bitmapBytes[14] = 40; // Basic bitmap header
        Array.Copy(BitConverter.GetBytes(bitmapBytes.Length), 0, bitmapBytes, 2, 4);
        Array.Copy(BitConverter.GetBytes(imageHeaderSize), 0, bitmapBytes, 10, 4);
        Array.Copy(BitConverter.GetBytes(imageWidth), 0, bitmapBytes, 18, 4);
        Array.Copy(BitConverter.GetBytes(imageHeight), 0, bitmapBytes, 22, 4);
        Array.Copy(BitConverter.GetBytes(32), 0, bitmapBytes, 28, 2);

        // Copy the length of the pixel data into the header
        Array.Copy(BitConverter.GetBytes(sourceImage.Pixels.Length), 0, bitmapBytes, 34, 4);

        // Copy source image pixels into the bitmap 
        Array.Copy(sourceImage.Pixels, 0, bitmapBytes, imageHeaderSize, sourceImage.Pixels.Length);

        return bitmapBytes;
    }

    internal static byte[] ToPngImage(
        this IPixelProvider pixelProvider,
        int scale, int border, int foreground = 0, int background = 0xFFFFFF)
    {
        var sourceImage = SourceImage.FromPixelProvider(pixelProvider, scale, border, foreground, background);
        var pngImage = PngImage.FromBitmap(sourceImage.Pixels, sourceImage.Width, sourceImage.Height, true);
        return pngImage.Save();
    }
}
