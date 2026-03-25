namespace Lyt.QrCode.DebugApp;

internal sealed class Test
{
    private const string link = "https://github.com/LaurentInSeattle/Lyt.QrCode";

    private string rootPath = "C:\\Users\\Laurent\\Desktop\\QrTests";

    internal void Run()
    {
        rootPath = "C:\\Users\\Laurent\\Desktop\\QrTests\\Encode";

        this.Encode("This a test plain text string.", "Text");
        this.Decode("Text");

        string text = "012345RSTUVWXYZ $%*+-./:";
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        this.Encode(bytes, "Bytes");
        this.Decode("Bytes");

        this.Encode(link, "Link");
        this.Encode(new QrUri(new(link)), "Url");
        this.Encode(new QrBookmark(link, "QrCode Library"), "Bookmark");

        this.Encode(
            new QrCalendarEvent(
                "Birthday Party",
                DateTime.Parse("05/12/2026 20:00"),
                DateTime.Parse("05/12/2026 23:00"),
                isAllDay: false,
                "Mario's Home",
                "Celebrate Luigi's birthday",
                includeVcalendarTags: true),
            "Event");

        this.Encode(new QrGeoLocation(37.810729, -122.476552), "Presidio");

        this.Encode(
            new QrMail(
                "ly.testud@outlook.com",
                "Hello Laurent!",
                "I hope all is well in California"),
            "Mail");

        var mecard = new QrMeCard("Laurent", "Testud")
        {
            Nickname = "Enzo",
            Format = ContactAddressFormat.NorthAmerica,
            PoBox = "PO: 152",
            HouseNumber = "7152",
            Street = "Market St.",
            City = "San Francisco",
            StateRegion = "CA",
            ZipCode = "94578",
            Email = "ly.testud@outlook.com",
            Website = link,
            Country = "USA",
            MobilePhone = "+1 (206) 619 3868",
            Note = "Hello World!",
            BirthdayString = "05/12/1968",
            Organization = "Home",
            Title = "Mr.",
        };

        this.Encode(mecard, "MeCard");

        //var vcard = new QrVCard("Laurent", "Testud")
        //{
        //    Fullname = "Laurent Yves Testud",
        //    Nickname = "Enzo",
        //    Format = ContactAddressFormat.NorthAmerica,
        //    Kind = QrVCard.AddressKind.Work,
        //    HouseNumber = "7152",
        //    Street = "Market St.",
        //    City = "San Francisco",
        //    StateRegion = "CA",
        //    ZipCode = "94578",
        //    Email = "ly.testud@outlook.com",
        //    Website = link,
        //    Country = "USA",
        //    MobilePhone = "+1 (206) 619 3868",
        //    Note = "Hello World!",
        //    BirthdayString = "05/12/1968",
        //    Organization = "Home",
        //    Title = "Mr.",
        //};

        //this.Encode(vcard, "VCard");

        // this.Encode(new QrPhoneNumber("12064258779733"), "Phone");

        //this.Encode(
        //    new QrTextMessage("12066197812", "Hello Laurent!", QrTextMessage.MessagingProtocol.SmsIos),
        //    "Sms");

        //var uri = new Uri(link);
        //this.Encode(new QrUri(uri), "Uri");

        //this.Encode(new QrWifi( "MySecretNetwork", "-Hello*0|0*World-"), "Wifi");

        //Thresholding("screen");
        //Thresholding("Sample");

        // Detect("screen");
        // Detect("Sample");

        //rootPath = "C:\\Users\\Laurent\\Desktop\\QrTests\\Decode";

        // this.Decode("Url");
        // this.Decode("Wifi");
        // this.Decode("Presidio");
        // this.Decode("Phone");
        // this.Decode("Bookmark");
        // this.Decode("VCard");

        this.Decode("MeCard");

        // Decode("screen");
        // Decode("screenRotated");
        // Decode("screenPortrait");
    }

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

        //string imagePathLoad = filename + ".png";
        //var sourceImage = this.LoadSourceImage(imagePathLoad);
        //var result = Qr.Decode(sourceImage, OnDetect); 
        //if ( result.Success)
        //{
        //    Console.WriteLine("Detected ");
        //    var resampledImage = detectorResult.Resampled;
        //    byte[] resImage = PngBuilder.ToImage(resampledImage);
        //    string resPath = Path.Combine(rootPath, filename + "Resampled.png");
        //    File.WriteAllBytes(resPath, resImage);
        //}
        //else
        //{
        //    Console.WriteLine("Failed to Detect ");
        //}
    }

    private static void OnDetect(QrPixelPoint point)
        => Console.WriteLine("Detected: " + point.ToString());

    private void Decode(string filename)
    {
        var sourceImage = this.LoadSourceImage(filename + ".png");
        Console.WriteLine("Decode: " + filename);
        var before = DateTime.Now;
        var result = Qr.Decode(sourceImage, OnDetect);
        DateTime after = DateTime.Now;
        if (result.IsDetected)
        {
            Console.WriteLine("Detected ");
            Console.WriteLine(result.TopLeft.ToString());
            Console.WriteLine(result.TopRight.ToString());
            Console.WriteLine(result.BottomLeft.ToString());
            if (result.IsAligned)
            {
                Console.WriteLine("Aligned ");
                Console.WriteLine(result.Alignment.ToString());
            }

            if (result.Success)
            {
                Console.WriteLine("Decoded, Content:  " + result.Text);
                if (result.IsParsed)
                {
                    Console.WriteLine("Parsed, Type:  " + result.ParsedType.FullName);
                    if (result.TryGet(out QrMeCard? qrMeCard))
                    {
                        Console.WriteLine("Parsed as a 'MeCard'");
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("Failed to Detect anything");
        }

        // About 60 ms for a 800x600 image 
        Console.WriteLine("Milleseconds to decode: " + (after - before).TotalMilliseconds.ToString("F1"));
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
        var before = DateTime.Now;
        var encode = Qr.Encode<T, byte[]>(content);
        DateTime after = DateTime.Now;
        if (encode.Success)
        {
            Console.WriteLine("Encoded: " + filename);
            Console.WriteLine("Qr Code Version: " + encode.QrCodeVersion + "  -  Qr Code Dimension: " + encode.QrCodeDimension);
            string fullPath = Path.Combine(rootPath, filename + ".png");
            File.WriteAllBytes(fullPath, encode.Result);
        }

        Console.WriteLine("Milleseconds to encode: " + (after - before).TotalMilliseconds.ToString("F1"));
    }

    private void Encode(string filename)
    {
        var encodeImage = Qr.Encode<string, byte[]>(link);
        if (encodeImage.Success)
        {
            string fullPath = Path.Combine(rootPath, filename + ".png");
            File.WriteAllBytes(fullPath, encodeImage.Result);
        }

        var encodeParameters = new EncodeParameters()
        {
            ImageFormat = EncodeParameters.QrImageFormat.Bmp,
            Border = 2,
            Scale = 16,
        };

        encodeImage = Qr.Encode<string, byte[]>(link);
        if (encodeImage.Success)
        {
            string fullPath = Path.Combine(rootPath, filename + ".bmp");
            File.WriteAllBytes(fullPath, encodeImage.Result);
        }

        var encodeVectors = Qr.Encode<string, string>(link, encodeParameters);
        if (encodeVectors.Success)
        {
            string fullPath = Path.Combine(rootPath, filename + ".svg");
            File.WriteAllText(fullPath, encodeVectors.Result);
        }

        var encodeModules = Qr.Encode<string, bool[,]>(link);
        if (encodeModules.Success)
        {
            Console.WriteLine("Encoded: " + encodeModules.Result.ToString());
        }

        var encodeQrCode = Qr.Encode<string, QrCode>(link);
        if (encodeQrCode.Success)
        {
            Console.WriteLine("Encoded: " + encodeQrCode.Result.ErrorCorrectionLevel.ToString());
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
