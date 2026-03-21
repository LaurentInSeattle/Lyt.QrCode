namespace Lyt.QrCode.API;

public class EncodeResults<T> where T : class
{
    [MemberNotNullWhen(true, nameof(Result))]
    public bool Success { get; set; }

    public T? Result { get; set; } 

    public string Message { get; set;  } = string.Empty;
}
