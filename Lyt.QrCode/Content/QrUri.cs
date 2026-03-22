namespace Lyt.QrCode.Content;

using static QrUri;

#region Documentation 

// https://en.wikipedia.org/wiki/List_of_URI_schemes

#endregion Documentation 


/// <summary> Incomplete ~ Maybe not needed </summary>
/// <remarks> NOT supported in IOS :( </remarks>
public sealed class QrUri(Uri uri, Kind kind = Kind.Absolute) : QrContent<QrUri>, IQrParsable<QrUri>
{
    public enum Kind
    {
        Original, 
        Absolute,
    }

    public Uri Content { get; set; } = uri;

    public Kind UriKind { get; set; } = kind;

    public override string QrString => this.UriKind switch
    {
        Kind.Original => Uri.EscapeDataString(this.Content.OriginalString),
        Kind.Absolute => Uri.EscapeDataString(this.Content.AbsoluteUri),

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
            // Detect a valid URL scheme  ! 

            string maybeUri = Uri.UnescapeDataString(source);
            var uri = new Uri(maybeUri); 
            qrUri = new QrUri(uri, uri.IsAbsoluteUri ? Kind.Absolute : Kind.Original);
            return true;
        }
        catch
        {
            // Swallow everything else 
        }

        return false;
    }
}
