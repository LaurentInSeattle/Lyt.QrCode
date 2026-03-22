namespace Lyt.QrCode.API;

public sealed class EncodeParameters
{
    //  There are four levels of error correction, with higher levels providing better durability at the expense
    //  of higher code density: 
    //  Level Low: ~7% of data restored (Lowest)
    //  Level Medium: ~15% of data restored (Most common)
    //  Level Quartile: ~25% of data restored
    //  Level High: ~30% of data restored (Highest/best for logos)
    public enum QrErrorCorrectionLevel
    {
        Low, 
        Medium, 
        Quartile, 
        High,
    }

    // Image formats 
    public enum QrImageFormat
    {
        // Only PNG and Bmp for now 
        Png = 0,
        Bmp = 1,
    }

    // Vector formats 
    public enum QrVectorFormat
    {
        Svg,
        MicrosoftXaml,
        AvaloniaAxaml,
    }

    public EncodeParameters() { }

    public bool Validate()
        =>
            this.Scale > 0 &&       // at least one 
            this.Border >= 0 &&     // can be zero 
            this.Scale <= 1024 &&
            this.Border <= 64;      // smaller because it is scaled 

    /// <summary> The minimum desired Error Correction Level, defaults to Medium.</summary>
    /// <remarks> Will be automatically increased as long as the data fits.</remarks>
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; init; } = QrErrorCorrectionLevel.Medium;

    /// <summary> The width and height, in pixels, of each module (QR code pixel), defaults to 16. </summary>
    public int Scale { get; init; } = 16;

    /// <summary> The border width, as a factor of the module size, defaults to 2. </summary>
    /// <remarks> Expressed in count of modules, actual border size in pixels will be: Border * Scale</remarks>
    public int Border { get; init; } = 2;

    /// <summary> The foreground color (dark modules), in RGB value, defaults to Black. </summary>
    public int Foreground { get; init; } = 0;

    /// <summary> The background color (light modules), in RGB value, defaults to White. </summary>
    public int Background { get; init; } = 0xFFFFFF;

    /// <summary> The Image Format, when the encoding output is byte[], defaults to PNG. </summary>
    public QrImageFormat ImageFormat { get; init; } = QrImageFormat.Png;

    /// <summary> The Vector Format, when the encoding output is string, defaults to SVG. </summary>
    public QrVectorFormat VectorFormat { get; init; } = QrVectorFormat.Svg;
}
