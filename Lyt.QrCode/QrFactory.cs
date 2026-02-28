namespace Lyt.QrCode;

public static class QrFactory
{
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
}
