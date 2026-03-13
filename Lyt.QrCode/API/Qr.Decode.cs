namespace Lyt.QrCode.API;

// See:  https://en.wikipedia.org/wiki/QR_code 

// Present in GlobalUsings.cs: BUT KEEP for avoiding ambiguous reference to System.Net
using Lyt.QrCode.Content;

/// <summary> 
/// Callback which is invoked when a possible significant point in the QR code image, such as a corner, is found.
/// </summary>
public delegate void DetectorCallback(QrPoint point);

/// Factory class for creating QR code images and vector paths from various content types.
/// Partial: Decode API 
public static partial class Qr
{
    public static bool TryDecodeQrCodeFromImage(
        SourceImage sourceImage,
        [NotNullWhen(true)] out DecoderResult? decoderResult,
        DetectorCallback? detectorCallback = null,
        DecodeParameters? decodeParameters = null)
    {
        decoderResult = null;
        decodeParameters ??= new DecodeParameters();
        if (!decodeParameters.Validate())
        {
            // Invalid parameters - use default values
            if (Debugger.IsAttached) { Debugger.Break(); }
            decodeParameters = new DecodeParameters();
        }

        if (sourceImage.TryDecode(decodeParameters, detectorCallback, out decoderResult))
        {
            return true;
        }

        return false;
    }
}
