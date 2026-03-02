namespace Lyt.QrCode.DebugApp;

using static System.Net.Mime.MediaTypeNames;

internal sealed class Test
{
    string rootPath = "C:\\Users\\Laurent\\Desktop\\QrTests"; 

    internal void Run ()     
    {
        string imagePathLoad = "screen.png";
        var sourceImage = this.LoadSourceImage(imagePathLoad);
        string imagePathSave = "screenSave.png";
        this.SaveSourceImage(imagePathSave, sourceImage);

        var grayscaleImage = sourceImage.ToGrayscale();
        string imagePathSaveGray = "screenGray.png";
        this.SaveGrayscaleImage(imagePathSaveGray, grayscaleImage);
        grayscaleImage.HistogramEqualization();
        string imagePathSaveGrayEQ = "screenGrayEQ.png";
        this.SaveGrayscaleImage(imagePathSaveGrayEQ, grayscaleImage);

        var bitMatrixImage1 = grayscaleImage.ToBitMatrixBasicThresholding();
        byte[] bwImage = PngBuilder.ToPngImage(bitMatrixImage1);
        string bwPath = Path.Combine(rootPath, "screenBw.png");
        File.WriteAllBytes(bwPath, bwImage);

        //var bitMatrixImage2 = grayscaleImage.ToBitMatrixBasicThresholding();
    }

    private SourceImage LoadSourceImage(string imagePath)
    {
        string fullPath = Path.Combine(rootPath ,  imagePath) ; 
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(fullPath);
        byte[] pixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixels);
        Console.WriteLine($"Loaded image: {fullPath}, Size: {image.Width}x{image.Height}");
        var sourceImage =
            new SourceImage(
                image.Width, image.Height, image.Width * 4, PixelFormat.RGBA32, pixels);
        return sourceImage;
    }

    private void SaveSourceImage(string imagePath, SourceImage sourceImage)
    {
        string fullPath = Path.Combine(rootPath, imagePath);
        using (var image =
               SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(
                   sourceImage.Pixels,sourceImage.Width, sourceImage.Height))
        {
            image.Save(fullPath);
            Console.WriteLine($"Saved image: {fullPath}, Size: {image.Width}x{image.Height}");
        }
    }

    private void SaveGrayscaleImage(string imagePath, GrayscaleImage grayscaleImage)
    {
        string fullPath = Path.Combine(rootPath, imagePath);
        using (var image =
               SixLabors.ImageSharp.Image.LoadPixelData<L8>(
                   grayscaleImage.Pixels, grayscaleImage.Width, grayscaleImage.Height))
        {
            image.Save(fullPath);
            Console.WriteLine($"Saved image: {fullPath}, Size: {image.Width}x{image.Height}");
        }
    }
}
