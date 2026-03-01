namespace Lyt.QrCode.Image;

public enum PixelFormat
{
    Gray8,
    Gray16,

    RGB24,
    BGR24,

    ARGB32,
    ABGR32,
    BGRA32,
    RGBA32,

    /// <summary> 2 bytes per pixel, 5 bit red, 6 bits green and 5 bits blue </summary>
    RGB565,

    /// <summary> 4 bytes for two pixels, UYVY formatted </summary>
    UYVY,
    /// <summary> 4 bytes for two pixels, YUYV formatted </summary>
    YUYV
}

public static class PixelFormatExtensions
{
    public static int BytesPerPixel(this PixelFormat format)
    {
        return format switch
        {
            PixelFormat.Gray8 => 1,
            PixelFormat.Gray16 => 2,
            PixelFormat.RGB24 => 3,
            PixelFormat.BGR24 => 3,
            PixelFormat.ARGB32 => 4,
            PixelFormat.ABGR32 => 4,
            PixelFormat.BGRA32 => 4,
            PixelFormat.RGBA32 => 4,
            PixelFormat.RGB565 => 2,
            PixelFormat.UYVY => 2, // 4 bytes for two pixels
            PixelFormat.YUYV => 2, // 4 bytes for two pixels
            _ => throw new ArgumentOutOfRangeException(nameof(format), "Unsupported pixel format.")
        };
    }
}