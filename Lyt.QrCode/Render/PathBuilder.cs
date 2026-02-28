namespace Lyt.QrCode.Render;

internal sealed class PathBuilder
{
    private readonly int size;

    // The modules of this QR code (false = light, true = dark).
    private readonly bool[,] modules;

    internal PathBuilder (int size, bool[,] qrCodeModules)
    {
        this.size = size;

        // Copy modules to so that rectangles can be optimized without modifying the original QR code.
        this.modules = new bool[size, size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                this.modules[y, x] = qrCodeModules[y, x];
            }
        }
    }

    /// <summary>  Creates a SVG image string for the given QR code.</summary>
    /// <param name="qrCode">The QR code.</param>
    /// <param name="border">The border width, as a factor of the module (QR code pixel) size.</param>
    /// <param name="foreground">The foreground color (dark modules), in RGB value (little endian).</param>
    /// <param name="background">The background color (light modules), in RGB value (little endian).</param>
    internal static string ToSvgImageString(
        QrCode qrCode, int border, 
        int foreground = 0, int background = 0xFFFFFF, 
        RenderParameters.Format pathFormat = RenderParameters.Format.Svg)
    {
        var builder = new PathBuilder(qrCode.Size, qrCode.GetModules());
        string foregroundHex = $"#{foreground:X6}";
        string backgroundHex = $"#{background:X6}";
        return builder.ToSvgString(border, foregroundHex, backgroundHex, pathFormat);
    }

    private string ToSvgString(int border, string foreground, string background, RenderParameters.Format pathFormat)
    {
        if ((border < 0) || (border > 1024))
        {
            throw new ArgumentOutOfRangeException(nameof(border), "Border must be non-negative and less than 1024.");
        }

        if (!Enum.IsDefined(pathFormat))
        {
            throw new InvalidEnumArgumentException(nameof(pathFormat), (int)pathFormat, typeof(RenderParameters.Format));
        }

        var sb = new StringBuilder(); 
        
        // TODO: Implement MicrosoftXaml and AvaloniaAxaml path formats.
        switch (pathFormat)
        {
            default:
            case RenderParameters.Format.Svg:
                int dim = size + border * 2;
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n")
                  .Append("<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">\n")
                  .Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" viewBox=\"0 0 {dim} {dim}\" stroke=\"none\">\n")
                  .Append($"\t<rect width=\"100%\" height=\"100%\" fill=\"{background}\"/>\n")
                  .Append("\t<path d=\"");
                break;

            case RenderParameters.Format.MicrosoftXaml:
                break;

            case RenderParameters.Format.AvaloniaAxaml:
                break;
        }


        CreatePath(sb, this.modules, border, pathFormat);

        // TODO: Implement MicrosoftXaml and AvaloniaAxaml path formats.
        switch (pathFormat)
        {
            default:
            case RenderParameters.Format.Svg:
                sb.Append($"\" fill=\"{foreground}\"/>\n")
                  .Append("</svg>\n"); 
                break;

            case RenderParameters.Format.MicrosoftXaml:
                break;

            case RenderParameters.Format.AvaloniaAxaml:
                break;
        }

        return sb.ToString();
    }

    // Append a SVG/XAML path for the QR code to the provided string builder
    private static void CreatePath(StringBuilder path, bool[,] modules, int border, RenderParameters.Format pathFormat)
    {
        // Simple algorithm to reduce the number of rectangles for drawing the QR code and reduce SVG/XAML size.
        int size = modules.GetLength(0);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (modules[y, x])
                {
                    DrawLargestRectangle(path, modules, x, y, border);
                }
            }
        }
    }

    // Find, draw and clear largest rectangle with (x, y) as the top left corner
    private static void DrawLargestRectangle(StringBuilder path, bool[,] modules, int x, int y, int border)
    {
        int size = modules.GetLength(0);
        int bestW = 1;
        int bestH = 1;
        int maxArea = 1;
        int xLimit = size;
        int iy = y;
        while (iy < size && modules[iy, x])
        {
            int w = 0;
            while (x + w < xLimit && modules[iy, x + w])
            {
                w++;
            }

            int area = w * (iy - y + 1);
            if (area > maxArea)
            {
                maxArea = area;
                bestW = w;
                bestH = iy - y + 1;
            }

            xLimit = x + w;
            iy++;
        }

        // append path command
        if (x != 0 || y != 0)
        {
            path.Append(' ');
        }

        // Different locales use different minus signs.
        FormattableString pathElement = $"M{x + border},{y + border}h{bestW}v{bestH}h{-bestW}z";
        path.Append(pathElement.ToString(CultureInfo.InvariantCulture));

        // clear processed modules
        ClearRectangle(modules, x, y, bestW, bestH);
    }

    // Clear a rectangle of modules
    private static void ClearRectangle(bool[,] modules, int x, int y, int width, int height)
    {
        for (int iy = y; iy < y + height; iy++)
        {
            for (int ix = x; ix < x + width; ix++)
            {
                modules[iy, ix] = false;
            }
        }
    }
}
