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

internal static class PixelFormatExtensions
{
    internal static int BytesPerPixel(this PixelFormat format)
        => format switch
        {
            PixelFormat.Gray8 => 1,
            PixelFormat.Gray16 => 2,
            PixelFormat.RGB565 => 2,            
            PixelFormat.UYVY or PixelFormat.YUYV => 2, // 4 bytes for two pixels            
            PixelFormat.RGB24  or PixelFormat.BGR24 => 3,            
            PixelFormat.ARGB32 or PixelFormat.ABGR32 => 4,
            PixelFormat.BGRA32  or PixelFormat.RGBA32 => 4,

            _ => throw new ArgumentOutOfRangeException(nameof(format), "Unsupported pixel format.")
        };

    internal static bool IsColor(this PixelFormat format)
        => format switch
        {
            PixelFormat.Gray8 or PixelFormat.Gray16 => false,
            _ => true
        };
}