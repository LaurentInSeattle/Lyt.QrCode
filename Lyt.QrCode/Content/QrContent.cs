namespace Lyt.QrCode.Content;

public class QrContent<T>(T content, bool isBinaryData = false) 
    : QrContent (isBinaryData) 
    where T : class  
{
    public T Content { get; set; } = content;
}

public class QrContent(bool isBinaryData = false) 
{
    public virtual bool IsBinaryData { get; set; } = isBinaryData;

    public virtual string RawString { get; set; } = string.Empty;

    public virtual byte[] RawBytes { get; set; } = [];
}
