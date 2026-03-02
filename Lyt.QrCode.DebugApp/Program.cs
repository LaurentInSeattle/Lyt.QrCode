namespace Lyt.QrCode.DebugApp;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, QrCode.DebugApp!");

        try
        {
            new Test().Run();
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
