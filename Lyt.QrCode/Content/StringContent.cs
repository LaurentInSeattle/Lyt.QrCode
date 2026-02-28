namespace Lyt.QrCode.Content;

public class StringContent : QrContent<string>
{
    public StringContent(string content) : base(content) => this.RawString = content;

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
