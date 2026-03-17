namespace Lyt.QrCode.Content;

using static QrUri; 

public sealed class QrUri(Uri uri, Kind kind = Kind.Canonical) : QrContent<Uri>
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
}
