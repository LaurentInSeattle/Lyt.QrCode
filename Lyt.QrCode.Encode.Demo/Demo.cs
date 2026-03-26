namespace Lyt.QrCode.Encode.Demo;

// See: GlobalUsings.cs for required 'usings' 

internal sealed class Demo
{
    private const string link = "https://github.com/LaurentInSeattle/Lyt.QrCode";
    private const string email = "Someone@SomeDomain.it";
    private const string phone = "+1 (206) 659 3868";

    private string rootPath = string.Empty;

    internal void Initialize()
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string path = Path.Combine(desktop, "Lyt_Qr_Code_Demo_Encode");
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        Directory.CreateDirectory(path);
        this.rootPath = path;
    }

    internal void Run()
    {
        // Encode plain text 
        this.Encode("This a test plain text string.", "Text");

        // Encode an array of bytes 
        string text = "012345RSTUVWXYZ $%*+-./:";
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        this.Encode(bytes, "Bytes");

        // Encode a web link
        this.Encode(link, "Link");

        // Encode a bookmark (web link + title) 
        this.Encode(new QrBookmark(link, "QrCode Library"), "Bookmark");

        // Encode a iCal Calendar event 
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

        // Encode a geo location from latitude and longitude
        this.Encode(new QrGeoLocation(37.810729, -122.476552), "Presidio");

        // Encode an email with email address, subject line and message text 
        this.Encode(
            new QrMail(email, "Hello Laurent!", "I hope all is well in California!"),
            "Mail");

        // Encode a Me Card 
        var mecard = new QrMeCard("Laurent", "InSeattle")
        {
            Nickname = "Enzo",
            Format = ContactAddressFormat.NorthAmerica,
            PoBox = "PO: 152",
            HouseNumber = "7152",
            Street = "Market St.",
            City = "San Francisco",
            StateRegion = "CA",
            ZipCode = "94578",
            Email = email,
            Website = link,
            Country = "USA",
            MobilePhone = phone,
            Note = "Hello QR Codes!",
            BirthdayString = "05/12/1968",
            Organization = "Home",
            Title = "Mr.",
        };

        this.Encode(mecard, "MeCard");

        // Encode a VCard 
        var vcard = new QrVCard("Laurent", "InSeattle")
        {
            Fullname = "Laurent Yves InSeattle",
            Nickname = "Enzo",
            Format = ContactAddressFormat.NorthAmerica,
            Kind = QrVCard.AddressKind.Work,
            HouseNumber = "7152",
            Street = "Market St.",
            City = "San Francisco",
            StateRegion = "CA",
            ZipCode = "94578",
            Email = email,
            Website = link,
            Country = "USA",
            MobilePhone = phone,
            Note = "Hello QR Codes!",
            BirthdayString = "05/12/1968",
            Organization = "Home",
            Title = "Mr.",
        };
        this.Encode(vcard, "VCard");

        // Encode a Phone number
        this.Encode(new QrPhoneNumber(phone), "Phone");

        // Encode a SMS message number
        this.Encode(
            new QrTextMessage(phone, "Hello Laurent!", QrTextMessage.MessagingProtocol.SmsIos),
            "Sms");

    }

    private void Encode<T>(T content, string filename) where T : class
    {
        Console.WriteLine(" ");
        var before = DateTime.Now;

        // Encode with all defaults:
        // Black and White PNG image, with Border == 2 and Scale == 16
        var encodePng = Qr.Encode<T, byte[]>(content);

        DateTime after = DateTime.Now;
        if (encodePng.Success)
        {
            Console.WriteLine("Encoded: " + filename);
            Console.WriteLine("Qr Code Version: " + encodePng.QrCodeVersion + "  -  Qr Code Dimension: " + encodePng.QrCodeDimension);
            string fullPath = Path.Combine(rootPath, filename + ".png");
            File.WriteAllBytes(fullPath, encodePng.Result);
        }

        Console.WriteLine("Milleseconds to encode: " + (after - before).TotalMilliseconds.ToString("F1"));

        // Encode same content as a MUCH bigger BMP image  
        var encodeParameters = new EncodeParameters()
        {
            ImageFormat = EncodeParameters.QrImageFormat.Bmp,
            Border = 4,
            Scale = 22,
            Background = 0x00_FF_FA_F9, // Slightly pink
            Foreground = 0x00_00_00_40, // Very dark blue
        };
        var encodeBitmap = Qr.Encode<T, byte[]>(content, encodeParameters);
        if (encodeBitmap.Success)
        {
            string fullPath = Path.Combine(rootPath, filename + ".bmp");
            File.WriteAllBytes(fullPath, encodeBitmap.Result);
        }

        // Encode same content as a SVG vector image:
        // SVG is text: => Encode with 'string' for the second generic parameter
        var encodeSvgParameters = new EncodeParameters()
        {
            VectorFormat = EncodeParameters.QrVectorFormat.Svg,
        };
        var encodeSvg = Qr.Encode<T, string>(content, encodeSvgParameters);
        if (encodeSvg.Success)
        {
            string fullPath = Path.Combine(rootPath, filename + ".svg");
            File.WriteAllText(fullPath, encodeSvg.Result);
        }

        // Encode same content to only get the modules 
        var encodeModules = Qr.Encode<T, bool[,]>(content);
        if (encodeModules.Success)
        {
            // Access the modules as 2D array of booleans 
            bool[,] modules = encodeModules.Result;
            if (encodeModules.QrCodeDimension != modules.GetLength(0))
            {
                throw new Exception("Encoding problem with dimensions"); 
            }

            // Module at top left corner should be black since it is part of a 'finder' pattern
            bool corner = modules[0,0];
            if (!corner)
            {
                throw new Exception("Encoding problem with module value");
            }
        }
    }
}
