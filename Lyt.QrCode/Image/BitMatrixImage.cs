namespace Lyt.QrCode.Image;

/// <summary>
/// Represents a 2D matrix of bits. In function arguments below, and throughout the common
/// module, x is the column position, and y is the row position. The ordering is always x, y.
/// The origin is at the top-left.
/// </summary>
internal sealed partial class BitMatrixImage : IPixelProvider
{
    internal int Width { get; }

    internal int Height { get; }

    internal BitArray Bits{ get; }

    int IPixelProvider.Width => this.Width;

    int IPixelProvider.Height => this.Height;

    bool IPixelProvider.GetPixel(int x, int y) => this[x,y];

    internal BitMatrixImage(int width, int height)
    {
        this.Width = width;
        this.Height = height;
        this.Bits = new BitArray(width * height);
    }

    /// <summary>Gets or sets the requested bit, where true means black. </summary>
    /// <param name="x">The horizontal component (i.e. which column) </param>
    /// <param name="y">The vertical component (i.e. which row)</param>
    internal bool this[int x, int y]
    {
        get => this.Bits[y * this.Width + x];
        set => this.Bits[y * this.Width + x] = value;
    }
}
