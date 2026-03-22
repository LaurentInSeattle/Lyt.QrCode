namespace Lyt.QrCode.API;

public class EncodeResult<T> : MessageLog where T : class
{
    [MemberNotNullWhen(true, nameof(Result))]
    public bool Success => this.Result is not null && !this.Error; 

    public T? Result { get; set; }
}

