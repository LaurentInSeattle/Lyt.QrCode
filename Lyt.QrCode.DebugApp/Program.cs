namespace Lyt.QrCode.DebugApp;

internal class Program
{
    /// <summary> This app' uses the QR code library from source. </summary>
    static void Main(string[] _)
    {
        Console.WriteLine("Hello, QrCode.DebugApp!");

        try
        {
            var test = new Test();
            test.Initialize();
            test.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception thrown: " + ex.Message);
            Console.WriteLine(ex.ToString());
        }

        Console.WriteLine("Press 'Enter' to exit");
        Console.ReadLine();
    }
}
