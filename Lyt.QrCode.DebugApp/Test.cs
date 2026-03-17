namespace Lyt.QrCode.DebugApp;

internal sealed class Test
{
    private const string rootPath = "C:\\Users\\Laurent\\Desktop\\QrTests";

#pragma warning disable CA1822 // Mark members as static
    internal void Run()
    {
        // Encode("test");

        Encode(new QrBookmark("https://github.com/LaurentInSeattle/Lyt.QrCode", "QrCode Library"), "Bookmark"); 

        //Thresholding("screen");
        //Thresholding("Sample");

        // Detect("screen");
        // Detect("Sample");

        //Decode("screen");
        //Decode("screenRotated");
        Decode("screenPortrait");
    }

    private static void OnDetect(QrPoint resultPoint)
        => Console.WriteLine("Detected: " + resultPoint.ToString());

    private static void Detect(string filename)
    {
        // Screen 
        // Should return: (331.5, 430.5), (333, 285.5), (486.5, 277)
        // Returns: 
        //10:44:14:249    Detected: (486.5, 277)
        //10:44:14:492    Detected: (333, 285.5)
        //10:44:14:492    Detected: (331.5, 430.5)
        //10:44:14:492    patternInfo: (331.5, 430.5), (333, 285.5), (486.5, 277)

        // Sample
        // Should return: (190, 366.5), (205, 162), (421.5, 165)
        // Returns:
        //10:47:44:389    Detected: (205, 162)
        //10:47:44:389    Detected: (421.5, 165)
        //10:47:44:389    Detected: (190, 366.5)
        //10:47:44:389    patternInfo: (190, 366.5), (205, 162), (421.5, 165)

        string imagePathLoad = filename + ".png";
        var sourceImage = LoadSourceImage(imagePathLoad);
        if (Qr.TryDecodeQrCodeFromImage(sourceImage, out var result, OnDetect))
        {
            // TODO: Print out results 
            if (result.DetectorResult is DetectorResult detectorResult)
            {
                Console.WriteLine("Detected ");
                var resampledImage = detectorResult.Resampled;
                byte[] resImage = PngBuilder.ToImage(resampledImage);
                string resPath = Path.Combine(rootPath, filename + "Resampled.png");
                File.WriteAllBytes(resPath, resImage);
            }
        }
        else
        {
            Console.WriteLine("Failed to Detect ");
        }
    }

    private static void Decode(string filename)
    {
        string imagePathLoad = filename + ".png";
        var sourceImage = LoadSourceImage(imagePathLoad);
        var before = DateTime.Now;
        DateTime after;
        Console.WriteLine("Decode: " + filename);
        if (Qr.TryDecodeQrCodeFromImage(sourceImage, out var result, OnDetect))
        {
            after = DateTime.Now;
            Console.WriteLine("Decoded, Content:  " + result.Text);
        }
        else
        {
            after = DateTime.Now;
            Console.WriteLine("Failed to Decode");
        }

        // About 60 ms for a 800x600 image 
        Console.WriteLine("Decode: " + (after - before).TotalMilliseconds.ToString());
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
        byte[] bwImage1 = PngBuilder.ToImage(bitMatrixImage1);
        string bwPath1 = Path.Combine(rootPath, filename + "Bw1.png");
        File.WriteAllBytes(bwPath1, bwImage1);

        var bitMatrixImage2 = grayscaleImage.ToBitMatrixAdaptiveThresholding();
        byte[] bwImage2 = PngBuilder.ToImage(bitMatrixImage2);
        string bwPath2 = Path.Combine(rootPath, filename + "Bw2.png");
        File.WriteAllBytes(bwPath2, bwImage2);
    }

    private static void Encode<T>(T content, string filename) where T : class
    {
        if (Qr.TryEncode(content, out byte[]? imagePng))
        {
            Console.WriteLine("Encoded: " + filename);
            string fullPath = Path.Combine(rootPath, filename + ".png");
            File.WriteAllBytes(fullPath, imagePng);
        }
    }

    private static void Encode(string filename)
    {
        string text = "https://github.com/LaurentInSeattle/Lyt.Jigsaw";
        if (Qr.TryEncode(text, out byte[]? imagePng))
        {
            string fullPath = Path.Combine(rootPath, filename + ".png");
            File.WriteAllBytes(fullPath, imagePng);
        }

        var renderParameters = new RenderParameters()
        {
            ImageFormat = RenderParameters.QrImageFormat.Bmp,
            Border = 2,
            Scale = 16,
        };

        if (Qr.TryEncode(text, out byte[]? imageBmp))
        {
            string fullPath = Path.Combine(rootPath, filename + ".bmp");
            File.WriteAllBytes(fullPath, imageBmp);
        }

        if (Qr.TryEncode(text, out string? vectors, renderParameters))
        {
            string fullPath = Path.Combine(rootPath, filename + ".svg");
            File.WriteAllText(fullPath, vectors);
        }

        if (Qr.TryEncode(text, out bool[,]? modules))
        {
            Console.WriteLine("Encoded: " + modules.ToString());
        }

        if (Qr.TryEncode(text, out QrCode? qrCode))
        {
            Console.WriteLine("Encoded: " + qrCode.ErrorCorrectionLevel.ToString());
        }
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

#pragma warning restore CA1822 // Mark members as static
}
