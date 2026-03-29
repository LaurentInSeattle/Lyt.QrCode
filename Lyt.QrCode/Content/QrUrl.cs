namespace Lyt.QrCode.Content;

#region Documentation 

// See  https://en.wikipedia.org/wiki/URL 
// RFC: https://www.rfc-editor.org/rfc/rfc6270 

#endregion Documentation 

/// <summary> URL to a web page </summary>
/// <remarks> 
/// Does mostly nothing when encoding, just detects web hhtp and https protocols when decoding. 
/// </remarks>  
public sealed class QrUrl(string url) : QrContent<QrUrl>, IQrParsable<QrUrl>
{
    public string Url { get; set; } = url;

    public override string QrString => this.Url;

    public static bool TryParse(string source, [NotNullWhen(true)] out QrUrl? qrUrl)
    {
        qrUrl = null;
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source string cannot be null, empty or white space", nameof(source));
        }

        try
        {
            if (!source.StartsWith("http:", StringComparison.InvariantCultureIgnoreCase) &&
                !source.StartsWith("https:", StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            string maybeUrl = Uri.UnescapeDataString(source);
            qrUrl = new QrUrl(maybeUrl);
            return true;
        }
        catch
        {
            // Swallow everything else 
        }

        return false;
    }
}
