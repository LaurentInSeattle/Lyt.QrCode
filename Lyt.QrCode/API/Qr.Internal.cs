namespace Lyt.QrCode.API;

/// Factory class for creating QR code images and vector paths from various content types.
public static partial class Qr
{
#pragma warning disable CA2255 
    // The 'ModuleInitializer' attribute should not be used in libraries
    [ModuleInitializer]
    internal static void AssemblyInitializer()
    {
        try
        {
            // Force some static initializations up front 
            if ( !QrCode.IsInitialized)
            {
                QrCode.Initialize();
            }

            var _1 = QrVersion.FromDimension(45);
            bool _2 = EncodingUtilities.EucJpIsSupported;
            var _3 = ErrorCorrectionLevel.AllEclFromLowToHigh;
            int[][] _4 = FormatInformation.FormatInformationDecodeLookup;
        }
        catch (Exception ex)
        {
            // VERY unexpected: Swallow everything 
            Debug.WriteLine(ex.Message);
            Debug.WriteLine(ex.ToString());
        }
    }
#pragma warning restore CA2255 

    internal enum EncoderOutput
    {
        Unsupported = 0,

        Image, // byte[]

        Vectors, // string 

        Modules, // bool[,]
    }
}
