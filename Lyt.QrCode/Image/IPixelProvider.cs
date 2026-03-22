namespace Lyt.QrCode.Image;

internal interface IPixelProvider
{
    internal int Width { get; }

    internal int Height { get; }
    
    internal bool GetPixel(int x, int y);
}
