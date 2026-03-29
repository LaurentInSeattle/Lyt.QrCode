namespace Lyt.QrCode.Decode.Demo;

// See: GlobalUsings.cs for required 'usings' 

internal sealed class Demo
{
    private string rootPath = string.Empty;

    internal void Initialize ()
    {
        this.rootPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        ResourcesUtilities.SetResourcesPath("Lyt.QrCode.Decode.Demo.Resources");
        ResourcesUtilities.SetExecutingAssembly(Assembly.GetExecutingAssembly());
    }

    internal void Run()
    {
        // Load embedded resource image 
        string[] resourceNames = 
            [
                "screen-qr.jpg", // Valid VCard
                "bus-qr.jpg",    // Valid, Binary: 4uJ3qQiKu6TbLwKVrTTvdk25xxCyEfxK/DRfKM/cUwQG9oYKam8zXrac+sjZzuDl 
                "review-qr.jpg", // Valid web page 
                "retail-qr.jpg", // Should detect 3 finder points, but is NOT a QR Code
            ];
        foreach (string resourceName in resourceNames)
        {
            Console.WriteLine("");
            Console.WriteLine("Decoding: " + resourceName);

            byte[] imageBytes = Demo.LoadEmbeddedResourceImage(resourceName);
            SourceImage sourceImage = Demo.LoadSourceImageWithSixLaborsFromBytes(imageBytes);
            Demo.Decode(sourceImage);
        }
    }

    private static void OnDetect(QrPixelPoint point)
        => Console.WriteLine("Detected: " + point.ToString());

    private static void Decode(SourceImage sourceImage)
    {
        var before = DateTime.Now;
        var result = Qr.Decode(sourceImage, OnDetect);
        DateTime after = DateTime.Now;
        // result.DebugShowErrors();

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
                    if (result.ParsedObject is QrVCard qrVCard)
                    {
                        Console.WriteLine("Decoded, Content is a VCard from:  " + qrVCard.FirstName);
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("Failed to Detect anything... Not a Qr Code image ?");
        }

        // About 60 ms for a 800x600 image 
        Console.WriteLine("Milleseconds to decode: " + (after - before).TotalMilliseconds.ToString("F1"));
    }
    private static SourceImage LoadSourceImageWithSixLaborsFromBytes(byte[] imageBytes)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes);
        byte[] pixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixels);
        return
            new SourceImage( image.Width, image.Height, image.Width * 4, PixelFormat.RGBA32, pixels);
    }

    private SourceImage LoadSourceImageWithSixLaborsFromFile(string imagePath)
    {
        string fullPath = Path.Combine(this.rootPath, imagePath);
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(fullPath);
        byte[] pixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixels);
        Console.WriteLine($"Loaded image: {fullPath}, Size: {image.Width}x{image.Height}");
        return 
            new SourceImage(
                image.Width, image.Height, image.Width * 4, PixelFormat.RGBA32, pixels);
    }

    private static byte[] LoadEmbeddedResourceImage(string resourceName)
        => ResourcesUtilities.LoadEmbeddedBinaryResource(resourceName, out string? _);
}
