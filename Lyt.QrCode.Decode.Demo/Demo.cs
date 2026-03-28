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

}
