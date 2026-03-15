namespace Lyt.QrCode.Content;

using static UriContent; 

public sealed class UriContent(Uri uri, Kind kind = Kind.Canonical) : QrContent<Uri>(uri)
{
    public enum Kind
    {
        Canonical, 
        Original, 
        Absolute,
    }

    public Kind UriKind { get; set; } = kind;

    public override string RawString => this.UriKind switch
    {
        Kind.Canonical => this.Content.ToString(),
        Kind.Original => this.Content.OriginalString,
        Kind.Absolute => this.Content.AbsoluteUri,
        _ => throw new NotImplementedException(),
    };
}
