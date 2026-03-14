namespace Lyt.QrCode.Content;

public sealed class UriContent(Uri uri, UriContent.Kind kind = UriContent.Kind.Canonical) : QrContent<Uri>(uri)
{
    public enum Kind
    {
        Canonical, 
        Original, 
        Absolute,
    }

    public UriContent.Kind UriKind { get; set; } = kind;

    public override string RawString => this.UriKind switch
    {
        Kind.Canonical => this.Content.ToString(),
        Kind.Original => this.Content.OriginalString,
        Kind.Absolute => this.Content.AbsoluteUri,
        _ => throw new NotImplementedException(),
    };

    public override byte[] RawBytes => Encoding.UTF8.GetBytes(this.RawString);
}
