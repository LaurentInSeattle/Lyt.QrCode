namespace Lyt.QrCode.API;

// See:  https://en.wikipedia.org/wiki/QR_code 

// Present in GlobalUsings.cs: BUT KEEP for avoiding ambiguous reference to System.Net
using Lyt.QrCode.Content;
using Lyt.QrCode.Content.Internal;

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

    public static bool TryEncode<TContent, TResult>(
        TContent content,
        [NotNullWhen(true)] out TResult? result,
        RenderParameters? renderParameters = null)
        where TContent : class
        where TResult : class
    {
        result = null;
        renderParameters ??= new RenderParameters();
        if (!renderParameters.Validate())
        {
            // Invalid parameters - use default values
            Debug.WriteLine("Invalid parameters - use default values");
            if (Debugger.IsAttached) { Debugger.Break(); }
            renderParameters = new RenderParameters();
        }

        if (!TryCreateQrContent<TContent>(content, out QrContent? qrContent))
        {
            return false;
        }

        if (!TryDetermineEncoderOutput<TResult>(out EncoderOutput encoderOutput)
            || encoderOutput == EncoderOutput.Unsupported)
        {
            return false;
        }

        try
        {
            var qrCode =
                qrContent.IsBinaryData ?
                    QrCode.EncodeBytes(qrContent.RawBytes, ErrorCorrectionLevel.Quartile) :
                    QrCode.EncodeText(qrContent.RawString, ErrorCorrectionLevel.Quartile);

            object? rawResult; 
            switch (encoderOutput)
            {
                default:
                case EncoderOutput.Unsupported:
                    return false;

                case EncoderOutput.Image:
                    switch (renderParameters.ImageFormat)
                    {
                        default:
                            return false;

                        case RenderParameters.QrImageFormat.Png:
                            byte[] pngImage =
                                PngBuilder.ToImage(
                                    qrCode,
                                    renderParameters.Scale,
                                    renderParameters.Border,
                                    renderParameters.Foreground,
                                    renderParameters.Background);
                            rawResult = pngImage;
                            break;
                        case RenderParameters.QrImageFormat.Bmp:
                            byte[] bmpImage =
                                PngBuilder.ToImage(
                                    qrCode,
                                    renderParameters.Scale,
                                    renderParameters.Border,
                                    renderParameters.Foreground,
                                    renderParameters.Background);
                            rawResult = bmpImage;
                            break;
                    }

                    break;

                case EncoderOutput.Vectors:
                    string vectorPath =
                        PathBuilder.ToSvgImageString(
                            qrCode,
                            renderParameters.Border,
                            renderParameters.Foreground,
                            renderParameters.Background,
                            renderParameters.VectorFormat);
                    rawResult = vectorPath;
                    break;

                case EncoderOutput.Modules:
                    bool[,] modules = qrCode.GetModules();
                    rawResult = modules;
                    break;

                case EncoderOutput.QrCode:
                    rawResult = qrCode;
                    break;
            }

            // Must use exact type match (and not IsAssignableFrom) 
            if (rawResult.GetType() == typeof(TResult))
            {
                // Safe to cast
                result = (TResult) rawResult;
                return true;
            } 
        }
        catch (Exception ex)
        {
            if (Debugger.IsAttached) { Debugger.Break(); }
            Debug.WriteLine(ex.ToString());
        }

        return false;
    }

    private static bool TryCreateQrContent<T>(
        T content, [NotNullWhen(true)] out QrContent? qrContent)
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
                return false;
            }

            if (stringContent.IsNumeric() && (stringContent.Length > MaxStringNumeric))
            {
                return false;
            }
            else if (stringContent.IsAlphanumeric() && (stringContent.Length > MaxStringAlphanumeric))
            {
                return false;
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(stringContent);
                if ((bytes.Length == 0) || (bytes.Length > Qr.MaxDataBytes))
                {
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

            qrContent = new QrContent() { RawString = stringContent }; 
            return true;
        }

        bool ValidateBytes(byte[] bytesContent)
        {
            if ((bytesContent.Length == 0) || (bytesContent.Length > Qr.MaxDataBytes))
            {
                return false;
            }

            return true;
        }

        if (content is byte[] bytesContent)
        {
            if (!ValidateBytes(bytesContent))
            {
                return false ;
            }

            qrContent = new QrContent(isBinaryData:true) { RawBytes = bytesContent};
            return true;
        }

        if (content is QrContent qrC)
        {
            if (qrC.IsBinaryData)
            {
                if (ValidateBytes(qrC.RawBytes))
                {
                    return false; 
                } 
            } 
            else
            {
                if (!ValidateString(qrC.RawString))
                {
                    return false;
                }
            }

            qrContent = qrC;
            return true;
        }

        // Unsupported content type
        return false;
    }

    private static bool TryDetermineEncoderOutput<T>(out EncoderOutput encoderOutput) where T : class
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

        if (type == typeof(QrCode))
        {
            encoderOutput = EncoderOutput.QrCode;
            return true;
        }

        if (type == typeof(bool[,]))
        {
            encoderOutput = EncoderOutput.Modules;
            return true;
        }

        return false ; 
    }
}
