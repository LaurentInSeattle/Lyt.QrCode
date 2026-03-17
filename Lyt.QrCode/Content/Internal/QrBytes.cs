namespace Lyt.QrCode.Content.Internal;

internal class QrBytes(byte[] content) : QrContent<byte[]>
{
    public byte[] Content { get; set; } = content; 

    public override string RawString
    {
        get => Encoding.UTF8.GetString(this.Content);
        set { /* do nothing */ }
    }

    public override byte[] RawBytes
    {
        get => this.Content;
        set { /* do nothing */ }
    }
}
