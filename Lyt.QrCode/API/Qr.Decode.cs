namespace Lyt.QrCode.API;

// See:  https://en.wikipedia.org/wiki/QR_code 

// Present in GlobalUsings.cs: BUT KEEP for avoiding ambiguous reference to System.Net
using Lyt.QrCode.Content;
using Lyt.QrCode.Parser;

/// <summary> 
/// Callback which is invoked when a possible significant point in the QR code image, such as a corner, is found.
/// </summary>
public delegate void DetectorCallback(QrPoint point);

/// Factory class for creating QR code images and vector paths from various content types.
/// Partial: Decode API 
public static partial class Qr
{
    internal static Parsers decoderOutput;

    static Qr() => decoderOutput = Parsers.Create();

    /// <summary> Tries to add QR Code custom type parser to the object recognizer.</summary>
    /// <returns> True if valid encoder and parser.</returns>
    public static bool TryAddCustomQrContentType(Type type) => decoderOutput.TryAddQrContentType(type);

    public static bool TryDecode<T>(
        SourceImage sourceImage,
        [NotNullWhen(true)] out T? decodedContent,
        DetectorCallback? detectorCallback = null,
        DecodeParameters? decodeParameters = null)
        where T : QrContent
    {
        decodedContent = null;
        if (TryDecode(
            sourceImage, out DecoderResult? decoderResult, detectorCallback, decodeParameters))
        {
            if (decoderResult.ParsedObject is object decodedObject)
            {
                if (decoderResult.ParsedObject.GetType() == typeof(T))
                {
                    // safe to cast
                    decodedContent = (T)decodedObject;
                    return true;
                }
            }
        }

        return false;
    }

    public static bool TryDecode(
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
            Debug.WriteLine("Invalid parameters - use default values");
            if (Debugger.IsAttached) { Debugger.Break(); }
            decodeParameters = new DecodeParameters();
        }

        if (sourceImage.TryDecode(decodeParameters, detectorCallback, out decoderResult))
        {
            if (!decodeParameters.SkipParsing)
            {
                bool parsed = TryParse(decoderResult);
                if (!parsed)
                {
                    Debug.WriteLine("Could not identify any supported content");
                }

                // Decoding if successful, even we have failed to parse any content 
                return true;
            }
        }

        return false;
    }

    public static bool TryParse(DecoderResult decoderResult)
    {
        string source = decoderResult.Text;
        if (!string.IsNullOrWhiteSpace(source))
        {
            foreach (var kvp in decoderOutput)
            {
                // Invoke with null first parameter because the method is static 
                // out parameter will be returned in the arguments array,
                // copied over the original useless one
                MethodInfo method = kvp.Value;
                object?[] arguments = [source, null];
                object? value = method.Invoke(null, arguments);
                if ((value is bool parsed) && (parsed) && (arguments[1] is object decodedObject))
                {
                    Type expectedType = kvp.Key;
                    if (expectedType == decodedObject.GetType())
                    {
                        // Success 
                        decoderResult.ParsedType = expectedType;
                        decoderResult.ParsedObject = decodedObject;
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
