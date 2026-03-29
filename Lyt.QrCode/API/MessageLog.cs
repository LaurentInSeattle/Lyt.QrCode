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

    [Conditional("DEBUG")]
    public void DebugShowErrors()
    {
        if (this.Error)
        {
            Debug.WriteLine("Error encoding and decoding QR codes");
            Debug.Indent();
            foreach (string message in this.Messages)
            {
                Debug.WriteLine(message);
            }

            Debug.Unindent();

            if ( Debugger.IsAttached )
            {
                Debugger.Break();   
            }
        }
    }
}