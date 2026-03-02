namespace Lyt.QrCode.Image;

/// <summary>
///   <p>Represents a 2D matrix of bits. In function arguments below, and throughout the common
/// module, x is the column position, and y is the row position. The ordering is always x, y.
/// The origin is at the top-left.</p>
///   <p>Internally the bits are represented in a 1-D array of 32-bit ints. However, each row begins
/// with a new int. This is done intentionally so that we can copy out a row into a <see cref="BitArray"/> very
/// efficiently.</p>
///   <p>The ordering of bits is row-major. Within each int, the least significant bits are used first,
/// meaning they represent lower x values. This is compatible with <see cref="BitArray"/>'s implementation.</p>
/// </summary>
internal sealed class BitMatrixImage
{
    internal int Width { get; }

    internal int Stride { get; }

    internal int Height { get; }

    internal int[] Bits { get; }

    internal BitMatrixImage(int width, int height)
    {
        this.Width = width;
        this.Height = height;
        this.Stride = (width + 31) >> 5;
        this.Bits = new int[this.Stride * height];
    }

    internal BitMatrixImage(int width, int height, int stride, int[] bits)
    {
        this.Width = width;
        this.Height = height;
        this.Stride = stride;
        this.Bits = bits;
    }

    internal BitMatrixImage(int width, int height, int[] bits)
    {
        this.Width = width;
        this.Height = height;
        this.Stride = (width + 31) >> 5;
        this.Bits = bits;
    }
}
