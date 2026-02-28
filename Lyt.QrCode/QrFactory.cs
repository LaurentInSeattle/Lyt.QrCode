namespace Lyt.QrCode;

// https://en.wikipedia.org/wiki/QR_code 

// Present in GlobalUsings.cs: BUT KEEP for avoiding ambiguous reference to System.Net
using Lyt.QrCode.Content;

/// Factory class for creating QR code images and vector paths from various content types.
public static class QrFactory
{
    public static byte[] CreateQrCodePngImage(
        string content, RenderParameters renderParameters = default)
        => QrFactory.CreateQrCodePngImage(new StringContent(content), renderParameters);

    public static string CreateQrCodeVectorPath(
        string content, RenderParameters renderParameters = default)
        => QrFactory.CreateQrCodeVectorPath(new StringContent(content), renderParameters);

    public static byte[] CreateQrCodePngImage(
        WebLink weblink, RenderParameters renderParameters = default)
        => QrFactory.CreateQrCodePngImage(new WebLinkContent(weblink), renderParameters);

    public static string CreateQrCodeVectorPath(
        WebLink weblink, RenderParameters renderParameters = default)
        => QrFactory.CreateQrCodeVectorPath(new WebLinkContent(weblink), renderParameters);

    public static byte[] CreateQrCodePngImage<T>(
        QrContent<T> content,
        RenderParameters renderParameters = default)
        where T : class
    {
        var qrCode =
            content.IsBinaryData ?
                QrCode.EncodeBytes(content.RawBytes, Ecc.Quartile) :
                QrCode.EncodeText(content.RawString, Ecc.Quartile);
        if (!renderParameters.Validate())
        {
            renderParameters = new RenderParameters();
        }

        byte[] pngImage =
            PngBuilder.ToPngImage(
                qrCode,
                renderParameters.Scale,
                renderParameters.Border,
                renderParameters.Foreground,
                renderParameters.Background);
        return pngImage;
    }

    public static string CreateQrCodeVectorPath<T>(
        QrContent<T> content,
        RenderParameters renderParameters = default)
        where T : class
    {
        var qrCode =
            content.IsBinaryData ?
                QrCode.EncodeBytes(content.RawBytes, Ecc.Quartile) :
                QrCode.EncodeText(content.RawString, Ecc.Quartile);
        if (!renderParameters.Validate())
        {
            renderParameters = new RenderParameters();
        }

        string vectorPath =
            PathBuilder.ToSvgImageString(
                qrCode,
                renderParameters.Border,
                renderParameters.Foreground,
                renderParameters.Background, 
                renderParameters.PathFormat);
        return vectorPath;
    }
}
