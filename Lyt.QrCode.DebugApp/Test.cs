namespace Lyt.QrCode.DebugApp;

internal sealed class Test
{
    private string rootPath = "C:\\Users\\Laurent\\Desktop\\QrTests";

    internal void Run()
    {
        rootPath = "C:\\Users\\Laurent\\Desktop\\QrTests\\Encode";

        //this.Encode("This a test text string.", "Text");

        //string text = "012345RSTUVWXYZ $%*+-./:";
        //byte[] bytes = Encoding.UTF8.GetBytes(text);
        //this.Encode(bytes, "Bytes");

        // this.Encode(new QrBookmark("https://github.com/LaurentInSeattle/Lyt.QrCode", "QrCode Library"), "Bookmark");

        //this.Encode(
        //    new QrCalendarEvent(
        //        "Party",
        //        DateTime.Parse("05/12/1926 20:00"),
        //        DateTime.Parse("05/12/1926 23:00"),
        //        isAllDay: false,
        //        "Mario's Home", 
        //        "Celebrate Luigi's birthday",
        //        includeVcalendarTags: true), 
        //    "Event");

        //this.Encode(new QrGeoLocation(37.810729, -122.476552), "Presidio");

        //this.Encode(
        //    new QrMail(                
        //        "ly.testud@outlook.com",
        //        "Hello Laurent!",
        //        "I hope all is well in California"),
        //    "Mail");

        //var mecard = new QrMeCard("Laurent", "Testud")
        //{
        //    City = "San Francisco",
        //    StateRegion = "CA",
        //    ZipCode = "94578",
        //    Email = "ly.testud@outlook.com",
        //    Website = "https://github.com/LaurentInSeattle/Lyt.QrCode",
        //};
        //this.Encode(mecard, "MeCard");

        //var vcard = new QrVCard("Laurent", "Testud")
        //{
        //    City = "San Francisco",
        //    StateRegion = "CA",
        //    ZipCode = "94578",
        //    Email = "ly.testud@outlook.com",
        //    Website = "https://github.com/LaurentInSeattle/Lyt.QrCode",
        //};
        //this.Encode(vcard, "VCard");

        //this.Encode(new QrPhoneNumber("12066197812"), "Phone");

        //this.Encode(
        //    new QrTextMessage("12066197812", "Hello Laurent!", QrTextMessage.MessagingProtocol.SmsIos),
        //    "Sms");

        //var uri = new Uri("https://github.com/LaurentInSeattle/Lyt.QrCode");
        //this.Encode(new QrUri(uri), "Uri");

        //this.Encode(new QrWifi( "MySecretNetwork", "-Hello*0|0*World-"), "Wifi");

        //Thresholding("screen");
        //Thresholding("Sample");

        // Detect("screen");
        // Detect("Sample");

        rootPath = "C:\\Users\\Laurent\\Desktop\\QrTests\\Decode";

        this.Decode("Wifi");
        //this.Decode("Presidio");
        //this.Decode("Phone");
        //this.Decode("Bookmark");
        // Decode("screen");
        //Decode("screenRotated");
        //Decode("screenPortrait");
    }

    private static void OnDetect(QrPoint resultPoint)
        => Console.WriteLine("Detected: " + resultPoint.ToString());

    private void Detect(string filename)
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
        var sourceImage = this.LoadSourceImage(imagePathLoad);
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

    private void Decode(string filename)
    {
        string imagePathLoad = filename + ".png";
        var sourceImage = this.LoadSourceImage(imagePathLoad);
        var before = DateTime.Now;
        DateTime after;
        Console.WriteLine("Decode: " + filename);
        if (Qr.TryDecodeQrCodeFromImage(sourceImage, out var result, OnDetect))
        {
            after = DateTime.Now;
            Console.WriteLine("Decoded, Content:  " + result.Text);
            if (result.IsParsed)
            {
                Console.WriteLine("Parsed, Type:  " + result.ParsedType!.FullName);
            } 
        }
        else
        {
            after = DateTime.Now;
            Console.WriteLine("Failed to Decode");
        }

        // About 60 ms for a 800x600 image 
        Console.WriteLine("Decode: " + (after - before).TotalMilliseconds.ToString());
    }

    private void Thresholding(string filename)
    {
        string imagePathLoad = filename + ".png";
        var sourceImage = this.LoadSourceImage(imagePathLoad);
        string imagePathSave = filename + "Save.png";
        this.SaveSourceImage(imagePathSave, sourceImage);

        var grayscaleImage = sourceImage.ToGrayscale();
        string imagePathSaveGray = filename + "Gray.png";
        this.SaveGrayscaleImage(imagePathSaveGray, grayscaleImage);

        var grayscaleImageEQ = sourceImage.ToGrayscale();
        grayscaleImageEQ.HistogramEqualization();
        string imagePathSaveGrayEQ = filename + "GrayEQ.png";
        this.SaveGrayscaleImage(imagePathSaveGrayEQ, grayscaleImageEQ);

        var bitMatrixImage1 = grayscaleImage.ToBitMatrixBasicThresholding();
        byte[] bwImage1 = PngBuilder.ToImage(bitMatrixImage1);
        string bwPath1 = Path.Combine(rootPath, filename + "Bw1.png");
        File.WriteAllBytes(bwPath1, bwImage1);

        var bitMatrixImage2 = grayscaleImage.ToBitMatrixAdaptiveThresholding();
        byte[] bwImage2 = PngBuilder.ToImage(bitMatrixImage2);
        string bwPath2 = Path.Combine(rootPath, filename + "Bw2.png");
        File.WriteAllBytes(bwPath2, bwImage2);
    }

    private void Encode<T>(T content, string filename) where T : class
    {
        if (Qr.TryEncode(content, out byte[]? imagePng))
        {
            Console.WriteLine("Encoded: " + filename);
            string fullPath = Path.Combine(rootPath, filename + ".png");
            File.WriteAllBytes(fullPath, imagePng);
        }
    }

    private void Encode(string filename)
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

    private SourceImage LoadSourceImage(string imagePath)
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

    private void SaveSourceImage(string imagePath, SourceImage sourceImage)
    {
        string fullPath = Path.Combine(rootPath, imagePath);
        using var image =
               SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(
                   sourceImage.Pixels, sourceImage.Width, sourceImage.Height);
        image.Save(fullPath);
        Console.WriteLine($"Saved image: {fullPath}, Size: {image.Width}x{image.Height}");
    }

    private void SaveGrayscaleImage(string imagePath, GrayscaleImage grayscaleImage)
    {
        string fullPath = Path.Combine(rootPath, imagePath);
        using var image =
               SixLabors.ImageSharp.Image.LoadPixelData<L8>(
                   grayscaleImage.Pixels, grayscaleImage.Width, grayscaleImage.Height);
        image.Save(fullPath);
        Console.WriteLine($"Saved image: {fullPath}, Size: {image.Width}x{image.Height}");
    }
}
