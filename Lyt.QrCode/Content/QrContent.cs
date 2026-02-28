namespace Lyt.QrCode.Content;

public abstract class QrContent<T>(T content, bool isBinaryData = false) where T : class  
{
    public T Content { get; set; } = content;

    public bool IsBinaryData { get; set; } = isBinaryData;

    public abstract string RawString { get; set; }

    public abstract byte[] RawBytes { get; set; }
}
