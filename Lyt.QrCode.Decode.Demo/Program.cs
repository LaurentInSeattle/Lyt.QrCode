namespace Lyt.QrCode.Decode.Demo;

internal class Program
{
    /// <summary> 
    /// This app' uses the QR code library as a downloaded Nuget package.
    /// This app' uses SixLabors.ImageSharp to load image files from disk as a downloaded Nuget package.
    /// No dependencies required for generating SVG, PNG and BMP images.
    /// </summary>
    static void Main(string[] _)
    {
        Console.WriteLine("Hello, QrCode Decode Demo!");
        Console.WriteLine("");
        //Console.WriteLine("Press 'Enter' to continue");
        //Console.ReadLine();
        //Console.WriteLine("");

        try
        {
            var demo = new Demo();
            demo.Initialize();
            demo.Run();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Exception thrown: " + ex.Message);
            Console.WriteLine(ex.ToString());
        }

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("");
        Console.WriteLine("Press 'Enter' to exit");
        Console.ReadLine();
    }
}
