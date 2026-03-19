namespace Lyt.QrCode.Content;

using static QrUri; 

public sealed class QrUri(Uri uri, Kind kind = Kind.Canonical) : QrContent, IQrParsable<QrUri>
{
    public enum Kind
    {
        Canonical, 
        Original, 
        Absolute,
    }

    public Uri Content { get; set; } = uri;

    public Kind UriKind { get; set; } = kind;

    public override string RawString => this.UriKind switch
    {
        Kind.Canonical => this.Content.ToString(),
        Kind.Original => this.Content.OriginalString,
        Kind.Absolute => this.Content.AbsoluteUri,
        _ => throw new NotImplementedException(),
    };

    public static bool TryParse(string source, [NotNullWhen(true)] out QrUri? qrUri)
    {
        qrUri = null;
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source string cannot be null, empty or white space", nameof(source));
        }

        try
        {
            // TODO
            //qrMail = new QrMail();
            return true;
        }
        catch
        {
            // Swallow everything else 
        }

        return false;
    }
}
