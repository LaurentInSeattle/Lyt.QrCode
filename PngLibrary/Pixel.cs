namespace Lyt.Png;

/// <summary> A pixel in a <see cref="Png"/> image. </summary>
public readonly struct Pixel
{
    /// <summary> The red value for the pixel. </summary>
    public byte R { get; }

    /// <summary> The green value for the pixel. </summary>
    public byte G { get; }

    /// <summary> The blue value for the pixel. </summary>
    public byte B { get; }

    /// <summary> The alpha transparency value for the pixel. </summary>
    public byte A { get; }
    
    /// <summary>
    /// Whether the pixel is grayscale (if <see langword="true"/> <see cref="R"/>, <see cref="G"/> and <see cref="B"/> 
    /// will all have the same value).
    /// </summary>
    public bool IsGrayscale { get; }

    /// <summary> Create a new <see cref="Pixel"/>. </summary>
    /// <param name="r">The red value for the pixel.</param>
    /// <param name="g">The green value for the pixel.</param>
    /// <param name="b">The blue value for the pixel.</param>
    /// <param name="a">The alpha transparency value for the pixel.</param>
    /// <param name="isGrayscale">Whether the pixel is grayscale.</param>
    public Pixel(byte r, byte g, byte b, byte a, bool isGrayscale)
    {
        this.R = r;
        this.G = g;
        this.B = b;
        this.A = a;
        this.IsGrayscale = isGrayscale;
    }

    /// <summary> Create a new <see cref="Pixel"/> which has <see cref="IsGrayscale"/> false and is fully opaque. </summary>
    /// <param name="r">The red value for the pixel.</param>
    /// <param name="g">The green value for the pixel.</param>
    /// <param name="b">The blue value for the pixel.</param>
    public Pixel(byte r, byte g, byte b)
    {
        this.R = r;
        this.G = g;
        this.B = b;
        this.A = 255;
        this.IsGrayscale = false;
    }

    /// <summary> Create a new grayscale <see cref="Pixel"/>. </summary>
    /// <param name="grayscale">The grayscale value.</param>
    public Pixel(byte grayscale)
    {
        this.R = grayscale;
        this.G = grayscale;
        this.B = grayscale;
        this.A = 255;
        this.IsGrayscale = true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is Pixel pixel)
        {
            return 
                this.IsGrayscale == pixel.IsGrayscale && 
                this.A == pixel.A && 
                this.R == pixel.R && 
                this.G == pixel.G && 
                this.B == pixel.B;
        }

        return false;
    }

    /// <summary> Whether the pixel values are equal. </summary>
    /// <param name="other">The other pixel.</param>
    /// <returns><see langword="true"/> if all pixel values are equal otherwise <see langword="false"/>.</returns>
    public bool Equals(Pixel other)
        =>  this.R == other.R &&
            this.G == other.G && 
            this.B == other.B && 
            this.A == other.A && 
            this.IsGrayscale == other.IsGrayscale;

    public static bool operator ==(Pixel left, Pixel right) => left.Equals(right);

    public static bool operator !=(Pixel left, Pixel right) => !(left == right);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = this.R.GetHashCode();
            hashCode = (hashCode * 397) ^ this.G.GetHashCode();
            hashCode = (hashCode * 397) ^ this.B.GetHashCode();
            hashCode = (hashCode * 397) ^ this.A.GetHashCode();
            hashCode = (hashCode * 397) ^ this.IsGrayscale.GetHashCode();
            return hashCode;
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"(RGBA: {this.R}, {this.G}, {this.B}, {this.A})";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ToColorInt(Pixel p)
        => ToColorInt(p.R, p.G, p.B, p.A);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ToColorInt(byte r, byte g, byte b, byte a = 255)
        => (a << 24) + (r << 16) + (g << 8) + b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (byte r, byte g, byte b, byte a) FromColorInt(int i)
        => ((byte)(i >> 16), (byte)(i >> 8), (byte)i, (byte)(i >> 24));
}