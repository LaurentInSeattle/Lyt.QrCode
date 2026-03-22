namespace Lyt.QrCode.Parser;

public abstract class QrContent<T>(bool isBinaryData = false)
    : QrContent(isBinaryData)
    where T : class, IQrParsable<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string EscapeBasic(string content) => content.Replace(":", "\\:");
}

public class QrContent(bool isBinaryData = false)
{
    public virtual bool IsBinaryData { get; set; } = isBinaryData;

    public virtual string QrString { get; set; } = string.Empty;

    public virtual byte[] QrBytes { get; set; } = [];
}

public interface IQrParsable<TSelf> where TSelf : IQrParsable<TSelf>
{
    static abstract bool TryParse(string source, [NotNullWhen(true)] out TSelf? tself); 
}