namespace Lyt.QrCode.Decode.Demo;

// See: GlobalUsings.cs for required 'usings' 

internal sealed class Demo
{
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
        // TODO
    }

    private SourceImage LoadSourceImageWithSixLabors(string imagePath)
    {
        string fullPath = Path.Combine(this.rootPath, imagePath);
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(fullPath);
        byte[] pixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixels);
        Console.WriteLine($"Loaded image: {fullPath}, Size: {image.Width}x{image.Height}");
        var sourceImage =
            new SourceImage(
                image.Width, image.Height, image.Width * 4, PixelFormat.RGBA32, pixels);
        return sourceImage;
    }

    /* 

    WinForms / GDI+ 

        using System.Drawing;
    using System.IO;

    public static Image LoadImageInMemory(string filePath)
    {
        // Use File.ReadAllBytes to load the file's data into a byte array
        byte[] imageBytes = File.ReadAllBytes(filePath);

        // Create a MemoryStream from the byte array
        MemoryStream ms = new MemoryStream(imageBytes);

        // Create the Image from the MemoryStream
        Image image = Image.FromStream(ms);

        // Note: The MemoryStream 'ms' must remain open for the lifetime of the 'image' object
        // if using GDI+. To dispose of the stream immediately, you must make a copy of the 
        // image, for example, into a new Bitmap.
        // Bitmap bitmapCopy = new Bitmap(image); 
        // ms.Dispose(); // You can then dispose of the stream

        return image;
    }


    WPF / WIC 

    using System.Windows.Media.Imaging;

    public static BitmapImage LoadImageToMemory(string path)
    {
        BitmapImage image = new BitmapImage();
        using (FileStream stream = File.Open(path, FileMode.Open))
        {
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = new MemoryStream();
            stream.CopyTo(image.StreamSource);
            image.EndInit();
        }
        // The FileStream is closed here by the 'using' block, and because of 
        // CacheOption.OnLoad, the image data is already in memory.
        return image;
    }

    */
}
