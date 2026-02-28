namespace Lyt.QrCode;

public readonly struct RenderParameters
{
    public enum Format     
    {
        Svg, 
        MicrosoftXaml,
        AvaloniaAxaml,
    }

    public RenderParameters() { }

    public bool Validate()
        =>     
            this.Scale > 0 &&
            this.Border >= 0 && 
            this.Scale <= 1024 &&
            this.Border <= 1024;

    /// <summary> The width and height, in pixels, of each module (QR code pixel). </summary>
    public int Scale { get; init; } = 16;
    
    /// <summary> The border width, as a factor of the module size. </summary>
    public int Border { get; init; } = 2;
    
    /// <summary> The foreground color (dark modules), in RGB value. </summary>
    public int Foreground { get; init; } = 0; // Default to black

    /// <summary> The background color (light modules), in RGB value. </summary>
    public int Background { get; init; } = 0xFFFFFF; // Default to white

    public Format PathFormat { get; init; } = Format.Svg;
}
