namespace Lyt.QrCode.Content;

public class BytesContent(byte[] content) : QrContent<byte[]>(content)
{
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
