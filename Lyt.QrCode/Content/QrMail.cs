namespace Lyt.QrCode.Content;

using static Lyt.QrCode.Content.QrMail.EmailProtocol;

#region Documentation 

// See: https://www.ietf.org/rfc/rfc2368.txt

// See: https://en.wikipedia.org/wiki/Mailto

#endregion Documentation 

public class QrMail : QrContent<QrMail>, IQrParsable<QrMail>
{
    /// <summary> The email protocol. </summary>
    public enum EmailProtocol
    {
        /// <summary> The 'classic' "mailto:" URI scheme. </summary>
        MailTo,

        /// <summary> the simple mail protocol "SMTP:" format. </summary>
        Smtp,

        /// <summary> For the "MATMSG:" format. </summary>
        MatMsg,
    }

    // VERY SIMPLIFIED: Does NOT implement the RFC 2368 in full.
    // CONSIDER: Implement CC, BCC, and more stuff...
    public QrMail(
        string recipient, string subject = "", string body = "", EmailProtocol protocol = EmailProtocol.MailTo)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new ArgumentException("Email recipient is required, cannot be null, empty or white space", nameof(recipient));
        }

        this.Protocol = protocol;
        this.Recipient = recipient;
        this.Subject = subject; 
        this.Body = body;
    }

    public EmailProtocol Protocol { get; private set; }

    public string Recipient { get; private set; }

    public string Subject { get; private set; }  

    public string Body { get; private set; }
    
    public string ProtocolToString()
        => this.Protocol switch
        {
            MailTo => "mailto:",
            Smtp => "SMTP:",
            MatMsg => "MATMSG:",
            _ => throw new NotImplementedException(),
        };

    public override string RawString
    {
        get
        {
            string emailString; 
            string recipient = this.Recipient;
            switch (this.Protocol)
            {
                case MailTo:
                    string subjectMailTo =
                        string.IsNullOrWhiteSpace(this.Subject) ?
                            string.Empty :
                            string.Concat("subject=", Uri.EscapeDataString(this.Subject));
                    string bodyMailTo =
                        string.IsNullOrWhiteSpace(this.Body) ?
                            string.Empty :
                            string.Concat("body=", Uri.EscapeDataString(this.Body));
                    emailString = recipient; 
                    if (!string.IsNullOrEmpty(subjectMailTo) || !string.IsNullOrEmpty(bodyMailTo))
                    {
                        emailString += "?";
                        bool hasSubject = false;
                        if (!string.IsNullOrEmpty(subjectMailTo)) 
                        {
                            emailString += subjectMailTo;
                            hasSubject = true;
                        }

                        if (!string.IsNullOrEmpty(bodyMailTo))
                        {
                            if (hasSubject)
                            {
                                emailString += "?";
                            }

                            emailString += bodyMailTo;
                        }
                    }

                    break;

                case Smtp:
                case MatMsg:
                    string subject =
                        string.IsNullOrWhiteSpace(this.Subject) ?
                            string.Empty :
                            EscapeBasic(this.Subject);
                    string body =
                        string.IsNullOrWhiteSpace(this.Body) ?
                            string.Empty :
                            EscapeBasic(this.Body);
                    emailString = 
                        this.Protocol == Smtp ?
                            $"{recipient}:{subject}:{body}":
                            $"TO:{recipient};SUB:{subject};BODY:{body};;";
                    break;

                default:
                    throw new NotImplementedException();
            }

            return string.Concat(this.ProtocolToString(), emailString); 
        }
    }

    public static bool TryParse(string source, [NotNullWhen(true)] out QrMail? qrMail)
    {
        qrMail = null;
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source string cannot be null, empty or white space", nameof(source));
        }

        try
        {
            // TODO
            //qrMail = new QrMail();
            return true;
        }
        catch
        {
            // Swallow everything else 
        }

        return false;
    }
}