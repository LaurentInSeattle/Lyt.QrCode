namespace Lyt.QrCode.Render;

internal sealed class BitmapBuilder
{
    const int imageHeaderSize = 54;

    /// <summary>  Creates a BITMAP image for the given QR code. </summary>
    /// <param name="qrCode">The QR code.</param>
    /// <param name="border">The border width, as a factor of the module (QR code pixel) size.</param>
    /// <param name="scale">The width and height, in pixels, of each module.</param>
    /// <param name="foreground">The foreground color (dark modules), in RGB value (little endian).</param>
    /// <param name="background">The background color (light modules), in RGB value (little endian).</param>
    /// <returns> A Bitmap image, as a byte array.</returns>
    internal static byte[] ToImage(QrCode qrCode, int scale, int border, int foreground = 0, int background = 0xFFFFFF)
    {
        int imageSize = (qrCode.Size + border * 2) * scale;
        var builder = new BitmapBuilder(imageSize, imageSize, foreground, background);
        return builder.Bytes;
    }

    private readonly int width;
    private readonly int height;
    private readonly byte[] pixelBytes;
    private readonly byte[] bitmapBytes;
    private readonly byte foregroundRed;
    private readonly byte foregroundGreen;
    private readonly byte foregroundBlue;
    private readonly byte backgroundRed;
    private readonly byte backgroundGreen;
    private readonly byte backgroundBlue;


    internal BitmapBuilder(int width, int height, int foreground, int background)
    {
        this.width = width;
        this.height = height;
        this.pixelBytes = new byte[width * height * 4];
        this.bitmapBytes = new byte[imageHeaderSize + this.pixelBytes.Length];
    }

    internal byte[] Bytes => this.bitmapBytes;
}

/*

public class RawBitmap
{
    public readonly int Width;
    public readonly int Height;
    private readonly byte[] ImageBytes;

    public RawBitmap(int width, int height)
    {
        Width = width;
        Height = height;
        ImageBytes = new byte[width * height * 4];
    }

    public void SetPixel(int x, int y, RawColor color)
    {
        int offset = ((Height - y - 1) * Width + x) * 4;
        ImageBytes[offset + 0] = color.B;
        ImageBytes[offset + 1] = color.G;
        ImageBytes[offset + 2] = color.R;
    }

    public byte[] GetBitmapBytes()
    {
        const int imageHeaderSize = 54;
        byte[] bmpBytes = new byte[ImageBytes.Length + imageHeaderSize];
        bmpBytes[0] = (byte)'B';
        bmpBytes[1] = (byte)'M';
        bmpBytes[14] = 40;
        Array.Copy(BitConverter.GetBytes(bmpBytes.Length), 0, bmpBytes, 2, 4);
        Array.Copy(BitConverter.GetBytes(imageHeaderSize), 0, bmpBytes, 10, 4);
        Array.Copy(BitConverter.GetBytes(Width), 0, bmpBytes, 18, 4);
        Array.Copy(BitConverter.GetBytes(Height), 0, bmpBytes, 22, 4);
        Array.Copy(BitConverter.GetBytes(32), 0, bmpBytes, 28, 2);
        Array.Copy(BitConverter.GetBytes(ImageBytes.Length), 0, bmpBytes, 34, 4);
        Array.Copy(ImageBytes, 0, bmpBytes, imageHeaderSize, ImageBytes.Length);
        return bmpBytes;
    }

    public void Save(string filename)
    {
        byte[] bytes = GetBitmapBytes();
        File.WriteAllBytes(filename, bytes);
    }
}


using SKDocument document = SKDocument.CreatePdf("output.pdf");
        using SKBitmap bitmap = SKBitmap.Decode("file.png");
        using SKCanvas pageCanvas = document.BeginPage(bitmap.Width, bitmap.Height);
        pageCanvas.DrawBitmap(bitmap, new SKRect(0, 0, bitmap.Width, bitmap.Height));
        document.EndPage();
        document.Close();

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// set your license here:
// QuestPDF.Settings.License = LicenseType.Community;

Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(2, Unit.Centimetre);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(x => x.FontSize(20));
        
        page.Header()
            .Text("Hello PDF!")
            .SemiBold().FontSize(36).FontColor(Colors.Blue.Medium);
        
        page.Content()
            .PaddingVertical(1, Unit.Centimetre)
            .Column(x =>
            {
                x.Spacing(20);
                
                x.Item().Text(Placeholders.LoremIpsum());
                x.Item().Image(Placeholders.Image(200, 100));
            });
        
        page.Footer()
            .AlignCenter()
            .Text(x =>
            {
                x.Span("Page ");
                x.CurrentPageNumber();
            });
    });
})
.GeneratePdf("hello.pdf");
*/