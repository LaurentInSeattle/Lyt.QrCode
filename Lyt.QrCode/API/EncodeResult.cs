namespace Lyt.QrCode.API;

public class EncodeResult<T> : MessageLog where T : class
{
    [MemberNotNullWhen(true, nameof(Result))]
    public bool Success => this.Result is not null && !this.Error; 

    public T? Result { get; set; }
}

public class MessageLog
{
    public bool Error { get; set; }

    public List<string> Messages { get; set; } = [];

    public void AddInfoMessage(string message) => this.Messages.Add(message);

    public void AddErrorMessage(string message)
    {
        this.Error = true;
        this.Messages.Add(message);
    }
}
