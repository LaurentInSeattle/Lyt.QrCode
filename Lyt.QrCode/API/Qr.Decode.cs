namespace Lyt.QrCode.API;

// See:  https://en.wikipedia.org/wiki/QR_code 

/// <summary> 
/// Callback which is invoked when a possible significant point in the QR code image, such as a corner, is found.
/// </summary>
public delegate void DetectorCallback(QrPixelPoint point);

/// Factory class for creating QR code images and vector paths from various content types.
/// Partial: Decode API 
public static partial class Qr
{
    internal static Parsers decoderOutput;

    static Qr() => decoderOutput = Parsers.Create();

    /// <summary> Tries to add QR Code custom type parser to the object recognizer.</summary>
    /// <returns> True if valid encoder and parser.</returns>
    public static bool TryAddCustomQrContentType(Type type) => decoderOutput.TryAddQrContentType(type);

    public static async Task<DecodeResult> DecodeAsync(
        SourceImage sourceImage,
        DetectorCallback? detectorCallback = null,
        DecodeParameters? decodeParameters = null)
        => await Task.Run(() => { return Qr.Decode(sourceImage, detectorCallback, decodeParameters); });

    public static DecodeResult Decode(
        SourceImage sourceImage,
        DetectorCallback? detectorCallback = null,
        DecodeParameters? decodeParameters = null)
    {
        var apiResult = new DecodeResult();
        decodeParameters ??= new DecodeParameters();
        if (!decodeParameters.Validate())
        {
            // Invalid parameters - use default values
            apiResult.AddInfoMessage("Invalid parameters: using default values");
            decodeParameters = new DecodeParameters();
        }

        if (sourceImage.TryDecode(apiResult, decodeParameters, detectorCallback, out DecoderResult? decoderResult))
        {
            if ((decoderResult.DetectorResult is DetectorResult detectorResult) &&
                    (detectorResult.Patterns is Patterns patterns))
            {
                apiResult.TopLeft = patterns.TopLeft is null ? new() : patterns.TopLeft.ToPixelPoint();
                apiResult.TopRight = patterns.TopRight is null ? new() : patterns.TopRight.ToPixelPoint();
                apiResult.BottomLeft = patterns.BottomLeft is null ? new() : patterns.BottomLeft.ToPixelPoint();
                apiResult.Alignment = patterns.AlignmentPattern is null ? new() : patterns.AlignmentPattern.ToPixelPoint();
            }

            if (decodeParameters.SkipParsing)
            {
                apiResult.AddInfoMessage("Skipping Parsing: No supported content.");
            }
            else
            {
                bool parsed = TryParse(decoderResult);
                if (parsed)
                {
                    apiResult.ParsedType = decoderResult.ParsedType;
                    apiResult.ParsedObject = decoderResult.ParsedObject;
                }
                else
                {
                    apiResult.AddInfoMessage("Could not identify any supported content");
                    Debug.WriteLine("Could not identify any supported content");
                }

                // Decoding if successful, even we have failed to parse any content
                apiResult.Text = decoderResult.Text;
                apiResult.Bytes = decoderResult.Bytes;
            }
        }

        return apiResult;
    }

    private static bool TryParse(DecoderResult decoderResult)
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
