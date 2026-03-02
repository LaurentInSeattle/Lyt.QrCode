namespace Lyt.QrCode.Image;

internal interface IPixelProvider
{
    int Width { get; }
    int Height { get; }
    bool GetPixel(int x, int y);
}
