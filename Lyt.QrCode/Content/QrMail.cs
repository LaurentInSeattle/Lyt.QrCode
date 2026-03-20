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

    private const string protocolMailTo = "mailto:";
    private const string protocolSmtp = "SMTP:";
    private const string protocolMatMsg = "MATMSG:";

    private const string toMatMsgKey = "TO:";
    private const string subjectMatMsgKey = "SUB:";
    private const string bodyMatMsgKey = "BODY:";

    const string subjectMailToKey = "subject=";
    const string bodyMailToKey = "body=";

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
    
    public string ProtocolToString() => 
        this.Protocol switch
        {
            MailTo => protocolMailTo,
            Smtp => protocolSmtp,
            MatMsg => protocolMatMsg,
            _ => throw new NotImplementedException(),
        };

    public override string QrString
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
            if (! // NOT any of these three
                (source.StartsWith(protocolMailTo) ||
                source.StartsWith(protocolSmtp) ||
                source.StartsWith(protocolMatMsg)))
            {
                return false;
            }

            EmailProtocol protocol = EmailProtocol.MailTo;
            if (source.StartsWith(protocolMailTo))
            {
                source = source[protocolMailTo.Length..];
                protocol = MailTo;
            }
            else if (source.StartsWith(protocolSmtp))
            {
                source = source[protocolSmtp.Length..];
                protocol = Smtp;
            }
            else if (source.StartsWith(protocolMatMsg))
            {
                source = source[protocolMatMsg.Length..];
                protocol = MatMsg;
            }
            else
            {
                throw new NotImplementedException();
            }

            char splitChar = protocol switch
            {
                MailTo => '?', // Split using '?' 
                Smtp => ':',   // Split using ':' 
                MatMsg => ';', // Split using ';' 
                _ => throw new NotImplementedException(),
            };

            string recipient = string.Empty;
            string subject = string.Empty;
            string body = string.Empty;

            // Do NOT remove empty entries ! 
            string[] tokens = source.Split([splitChar], StringSplitOptions.TrimEntries);
            switch (protocol)
            {
                case MailTo:
                    recipient = tokens[0];
                    foreach (string token in tokens)
                    {
                        if (token.StartsWith(subjectMailToKey))
                        {
                            subject = token[subjectMailToKey.Length..];
                        }

                        if (token.StartsWith(bodyMailToKey))
                        {
                            body = token[bodyMailToKey.Length..];
                        }
                    }

                    break;

                case Smtp:
                    recipient = tokens[0];
                    if( tokens.Length == 2)
                    {
                        subject = tokens[1];
                    }
                    if (tokens.Length == 3)
                    {
                        subject = tokens[1];
                        body = tokens[2];
                    }
                    else
                    {
                        return false;
                    }

                    break;

                case MatMsg:
                    foreach (string token in tokens)
                    {
                        if (token.StartsWith(toMatMsgKey))
                        {
                            recipient = token[toMatMsgKey.Length..];
                        }

                        if (token.StartsWith(subjectMatMsgKey))
                        {
                            subject = token[subjectMatMsgKey.Length..];
                        }

                        if (token.StartsWith(bodyMatMsgKey))
                        {
                            body = token[bodyMatMsgKey.Length..];
                        }
                    }

                    break;

                default:
                    throw new NotImplementedException();
            }

            // Must have a recipient 
            if (string.IsNullOrWhiteSpace(recipient))
            {
                return false;
            } 

            qrMail = new QrMail(recipient, subject, body, protocol);
            return true;
        }
        catch
        {
            // Swallow everything else 
        }

        return false;
    }
}