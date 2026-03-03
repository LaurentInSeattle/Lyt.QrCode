namespace Lyt.QrCode.DebugApp;

internal sealed class Test
{
    private const string rootPath = "C:\\Users\\Laurent\\Desktop\\QrTests";

    internal void Run()
    {
        string text = "https://github.com/LaurentInSeattle/Lyt.Jigsaw";

        var qrCode = QrCode.EncodeText(text, Ecc.Quartile);
        byte[] image = PngBuilder.ToPngImage(qrCode, 16, 2);

        string fullPath = Path.Combine(rootPath, "test.png");
        File.WriteAllBytes(fullPath, image);

        fullPath = Path.Combine(rootPath, "test.svg");
        string svg = PathBuilder.ToSvgImageString(qrCode, 16, 2);
        File.WriteAllText(fullPath, svg);

        //image = QrFactory.CreateQrCodePngImage(new WebLink(text, "Jigsaw"));
        //File.WriteAllBytes("C:\\Users\\Laurent\\Desktop\\test.png", image);

        Thresholding("screen");
        Thresholding("Sample");
    }

    private static void Thresholding(string filename)
    {
        string imagePathLoad = filename + ".png";
        var sourceImage = LoadSourceImage(imagePathLoad);
        string imagePathSave = filename + "Save.png";
        SaveSourceImage(imagePathSave, sourceImage);

        var grayscaleImage = sourceImage.ToGrayscale();
        string imagePathSaveGray = filename + "Gray.png";
        SaveGrayscaleImage(imagePathSaveGray, grayscaleImage);

        var grayscaleImageEQ = sourceImage.ToGrayscale();
        grayscaleImageEQ.HistogramEqualization();
        string imagePathSaveGrayEQ = filename + "GrayEQ.png";
        SaveGrayscaleImage(imagePathSaveGrayEQ, grayscaleImageEQ);

        var bitMatrixImage1 = grayscaleImage.ToBitMatrixBasicThresholding();
        byte[] bwImage1 = PngBuilder.ToPngImage(bitMatrixImage1);
        string bwPath1 = Path.Combine(rootPath, filename + "Bw1.png");
        File.WriteAllBytes(bwPath1, bwImage1);

        var bitMatrixImage2 = grayscaleImage.ToBitMatrixAdaptiveThresholding();
        byte[] bwImage2 = PngBuilder.ToPngImage(bitMatrixImage2);
        string bwPath2 = Path.Combine(rootPath, filename + "Bw2.png");
        File.WriteAllBytes(bwPath2, bwImage2);
    }

    private static SourceImage LoadSourceImage(string imagePath)
    {
        string fullPath = Path.Combine(rootPath, imagePath);
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(fullPath);
        byte[] pixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixels);
        Console.WriteLine($"Loaded image: {fullPath}, Size: {image.Width}x{image.Height}");
        var sourceImage =
            new SourceImage(
                image.Width, image.Height, image.Width * 4, PixelFormat.RGBA32, pixels);
        return sourceImage;
    }

    private static void SaveSourceImage(string imagePath, SourceImage sourceImage)
    {
        string fullPath = Path.Combine(rootPath, imagePath);
        using var image =
               SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(
                   sourceImage.Pixels, sourceImage.Width, sourceImage.Height);
        image.Save(fullPath);
        Console.WriteLine($"Saved image: {fullPath}, Size: {image.Width}x{image.Height}");
    }

    private static void SaveGrayscaleImage(string imagePath, GrayscaleImage grayscaleImage)
    {
        string fullPath = Path.Combine(rootPath, imagePath);
        using var image =
               SixLabors.ImageSharp.Image.LoadPixelData<L8>(
                   grayscaleImage.Pixels, grayscaleImage.Width, grayscaleImage.Height);
        image.Save(fullPath);
        Console.WriteLine($"Saved image: {fullPath}, Size: {image.Width}x{image.Height}");
    }
}
