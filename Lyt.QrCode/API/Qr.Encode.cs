namespace Lyt.QrCode.API;

// See:  https://en.wikipedia.org/wiki/QR_code 

/// Factory class for creating QR code images and vector paths from various content types.
public static partial class Qr
{
    // Wikipedia: 
    // The maximum data a QR code can hold is 2,953 bytes of binary data when using the largest
    // possible version (Version 40) with the lowest error correction level.
    // The capacity varies significantly depending on the data type, version, and error correction level used.
    public const int MaxDataBytes = 2_953;

    // Wikipedia: 
    // The maximum string length in a QR code depends on the type of characters used(encoding mode),
    // the QR code version(size), and the error correction level.
    // The theoretical maximum capacity for a single QR code(Version 40, the largest) is: 
    //      Numeric only: 7,089 characters.
    //      Alphanumeric: 4,296 characters(uppercase letters, numbers, and some symbols).
    //      Binary/Byte: 2,953 characters(8-bit bytes).
    //      Kanji/Kana: 1,817 characters.
    public const int MaxStringNumeric = 7_089;
    public const int MaxStringAlphanumeric = 4_296;
    public const int MaxStringKanji = 1_817;

    public static async Task<EncodeResult<TResult>> EncodeAsync<TContent, TResult>(
        TContent content,
        EncodeParameters? encodeParameters = null)
        where TContent : class
        where TResult : class
        => await Task.Run(() => { return Qr.Encode<TContent, TResult>(content, encodeParameters); });

    public static EncodeResult<TResult> Encode<TContent, TResult>(
        TContent content,
        EncodeParameters? encodeParameters = null)
        where TContent : class
        where TResult : class
    {
        var apiResult = new EncodeResult<TResult>();
        encodeParameters ??= new EncodeParameters();
        if (!encodeParameters.Validate())
        {
            // Invalid parameters - use default values
            apiResult.AddInfoMessage("Invalid parameters: using default values");
            encodeParameters = new EncodeParameters();
        }

        if (!TryCreateQrContent<TContent>(content, apiResult, out QrContent? qrContent))
        {
            apiResult.AddErrorMessage("Invalid Content.");
            return apiResult;
        }

        if (!TryDetermineEncoderOutput<TResult>(apiResult, out EncoderOutput encoderOutput)
            || encoderOutput == EncoderOutput.Unsupported)
        {
            return apiResult;
        }

        try
        {
            var errorCorrectionLevel = ErrorCorrectionLevel.FromEnumeration(encodeParameters.ErrorCorrectionLevel);

            // Note: Encoding will throw if any issue
            var qrCode =
                qrContent.IsBinaryData ?
                    QrCode.EncodeBytes(qrContent.QrBytes, errorCorrectionLevel) :
                    QrCode.EncodeText(qrContent.QrString, errorCorrectionLevel);
            apiResult.QrCodeVersion = qrCode.Version;
            apiResult.QrCodeDimension = qrCode.Size;
            object? rawResult;
            switch (encoderOutput)
            {
                default:
                case EncoderOutput.Unsupported:
                    apiResult.AddErrorMessage("Encoder output is not supported.");
                    return apiResult;

                case EncoderOutput.Image:
                    switch (encodeParameters.ImageFormat)
                    {
                        default:
                            apiResult.AddErrorMessage("Image format is not supported.");
                            return apiResult;

                        case EncodeParameters.QrImageFormat.Png:
                            byte[] pngImage =
                                PngBuilder.ToImage(
                                    qrCode,
                                    encodeParameters.Scale,
                                    encodeParameters.Border,
                                    encodeParameters.Foreground,
                                    encodeParameters.Background);
                            rawResult = pngImage;
                            break;
                        case EncodeParameters.QrImageFormat.Bmp:
                            byte[] bmpImage =
                                PngBuilder.ToImage(
                                    qrCode,
                                    encodeParameters.Scale,
                                    encodeParameters.Border,
                                    encodeParameters.Foreground,
                                    encodeParameters.Background);
                            rawResult = bmpImage;
                            break;
                    }

                    break;

                case EncoderOutput.Vectors:
                    string vectorPath =
                        PathBuilder.ToSvgImageString(
                            qrCode,
                            encodeParameters.Border,
                            encodeParameters.Foreground,
                            encodeParameters.Background,
                            encodeParameters.VectorFormat);
                    rawResult = vectorPath;
                    break;

                case EncoderOutput.Modules:
                    bool[,] modules = qrCode.GetModules();
                    rawResult = modules;
                    break;
            }

            // Must use exact type match (and not IsAssignableFrom) 
            if (rawResult.GetType() == typeof(TResult))
            {
                // Safe to cast
                apiResult.Result = (TResult)rawResult;
                return apiResult;
            }

            apiResult.AddErrorMessage("Encoder output cannot be safely cast to specified by TResult generic parameter.");
        }
        catch (Exception ex)
        {
            if (Debugger.IsAttached) { Debugger.Break(); }
            Debug.WriteLine(ex.ToString());

            apiResult.AddErrorMessage("Exception thrown when encoding: " + ex.Message);
            apiResult.AddErrorMessage(ex.ToString());
        }

        return apiResult;
    }

    private static bool TryCreateQrContent<T>(
        T content,
        MessageLog messageLog,
        [NotNullWhen(true)] out QrContent? qrContent)
        where T : class
    {
        qrContent = null;
        if (content is null)
        {
            return false;
        }

        bool ValidateString(string stringContent)
        {
            if (string.IsNullOrWhiteSpace(stringContent))
            {
                messageLog.AddErrorMessage("String content is empty.");
                return false;
            }

            if (stringContent.IsNumeric() && (stringContent.Length > MaxStringNumeric))
            {
                messageLog.AddErrorMessage("Numeric string content is too long to fit in a QR code.");
                return false;
            }
            else if (stringContent.IsAlphanumeric() && (stringContent.Length > MaxStringAlphanumeric))
            {
                messageLog.AddErrorMessage("Alphanumeric string content is too long to fit in a QR code.");
                return false;
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(stringContent);
                if ((bytes.Length == 0) || (bytes.Length > Qr.MaxDataBytes))
                {
                    messageLog.AddErrorMessage("String content when encoded in UTF-8 is too long to fit in a QR code.");
                    return false;
                }
            }

            return true;
        }

        if (content is string stringContent)
        {
            if (!ValidateString(stringContent))
            {
                return false;
            }

            qrContent = new QrContent() { QrString = stringContent };
            return true;
        }

        bool ValidateBytes(byte[] bytesContent)
        {
            if (bytesContent.Length == 0)
            {
                messageLog.AddErrorMessage("Binary content is empty.");
                return false;
            }

            if ((bytesContent.Length > Qr.MaxDataBytes))
            {
                messageLog.AddErrorMessage("Binary content is too long to fit in a QR code.");
                return false;
            }

            return true;
        }

        if (content is byte[] bytesContent)
        {
            if (!ValidateBytes(bytesContent))
            {
                return false;
            }

            qrContent = new QrContent(isBinaryData: true) { QrBytes = bytesContent };
            return true;
        }

        if (content is QrContent qrC)
        {
            if (qrC.IsBinaryData)
            {
                if (ValidateBytes(qrC.QrBytes))
                {
                    return false;
                }
            }
            else
            {
                if (!ValidateString(qrC.QrString))
                {
                    return false;
                }
            }

            qrContent = qrC;
            return true;
        }

        // Unsupported content type
        messageLog.AddErrorMessage("Unsupported content type. Consider using ToString() or some serialization.");
        return false;
    }

    private static bool TryDetermineEncoderOutput<T>(
        MessageLog messageLog,
        out EncoderOutput encoderOutput) where T : class
    {
        encoderOutput = EncoderOutput.Unsupported;
        Type type = typeof(T);
        if (type == typeof(byte[]))
        {
            encoderOutput = EncoderOutput.Image;
            return true;
        }

        if (type == typeof(string))
        {
            encoderOutput = EncoderOutput.Vectors;
            return true;
        }

        if (type == typeof(bool[,]))
        {
            encoderOutput = EncoderOutput.Modules;
            return true;
        }

        messageLog.AddErrorMessage("Encoder output is not supported.");
        return false;
    }
}
