namespace Lyt.QrCode.Encode.Demo;

internal class Program
{
    /// <summary> This app' uses the QR code library as a downloaded Nuget package  </summary>
    static void Main(string[] _)
    {
        Console.WriteLine("Hello, QrCode Encode Demo!");
        Console.WriteLine("");
        Console.WriteLine("This demo will create a new folder on the 'Desktop' containing new files for generated QR codes.");
        Console.WriteLine("Press 'Enter' to continue");
        Console.ReadLine();
        Console.WriteLine("");

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
