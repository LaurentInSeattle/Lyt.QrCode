namespace Lyt.QrCode.API;

/// <summary> Holds error and information messages for encoding and decoding QR codes. </summary>
public class MessageLog
{
    public bool Error { get; set; }

    public List<string> Messages { get; set; } = [];

    internal void AddInfoMessage(string message) => this.Messages.Add(message);

    internal void AddErrorMessage(string message)
    {
        this.Error = true;
        this.Messages.Add(message);
    }
}