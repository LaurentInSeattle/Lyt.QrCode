namespace Lyt.QrCode.Content.Internal;

internal class QrString(string content) : QrContent<string>
{
    public string Content { get; set; } = content;

    public override string RawString
    {
        get => this.Content;
        set { /* do nothing */ }
    } 

    public override byte[] RawBytes
    {
        get => Encoding.UTF8.GetBytes(this.Content);
        set { /* do nothing */ }
    }
}
