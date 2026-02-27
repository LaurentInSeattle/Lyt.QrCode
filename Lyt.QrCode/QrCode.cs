namespace Lyt.QrCode;

public sealed partial class QrCode
{
    /// <summary> The minimum version (size) supported in the QR Code Model 2 standard – namely 1. </summary>
    public const int MinVersion = 1;

    /// <summary> The maximum version (size) supported in the QR Code Model 2 standard – namely 40. </summary>
    public const int MaxVersion = 40;

    /// <summary> The version (size) of this QR code (between 1 for the smallest and 40 for the biggest). </summary>
    public int Version { get; }

    /// <summary> 
    /// The width and height of this QR code, in modules (pixels).
    /// The size is a value between 21 and 177.
    /// This is equal to version &#xD7; 4 + 17.
    /// </summary>
    public int Size { get; }

    /// <summary> The error correction capacity used for this QR code. </summary>
    public Ecc ErrorCorrectionLevel { get; } = Ecc.Quartile;

    /// <summary>
    /// The index of the mask pattern used fort this QR code (between 0 and 7).
    /// Even if a QR code is created with automatic mask selection (<c>mask</c> = 1),
    /// this property returns the effective mask used.
    /// </summary>
    public int Mask { get; }

    /// <summary>
    /// Gets the color of the module (pixel) at the specified coordinates.
    /// The top left corner has the coordinates (x=0, y=0). <i>x</i>-coordinates extend from left to right,
    /// <i>y</i>-coordinates extend from top to bottom.
    /// If coordinates outside the bounds of this QR code are specified, light (<c>false</c>) is returned.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns>The color of the specified module: <c>true</c> for dark modules and <c>false</c>
    /// for light modules (or if the coordinates are outside the bounds).</returns>
    public bool GetModule(int x, int y)
        => 0 <= x && x < this.Size && 0 <= y && y < this.Size && this.modules[y, x];

    public bool[,] GetModules() => this.modules;
}
