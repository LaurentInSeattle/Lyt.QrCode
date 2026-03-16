namespace Lyt.QrCode.Content;

using static Lyt.QrCode.Content.Mail;

#region Documentation 

// See: https://www.ietf.org/rfc/rfc2368.txt

// See: https://en.wikipedia.org/wiki/Mailto

#endregion Documentation 

public class Mail 
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

    // SIMPLIFIED: Does NOT implement the RFC 2368 in full.
    // CONSIDER: Implement CC, BCC, and more stuff...
    public Mail(EmailProtocol protocol, string recipient, string subject = "", string body = "")
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
            EmailProtocol.MailTo => "mailto:",
            EmailProtocol.Smtp => "SMTP:",
            EmailProtocol.MatMsg => "MATMSG:",
            _ => throw new NotImplementedException(),
        };
}

internal sealed class MailContent(Mail mail) : QrContent<Mail>(mail)
{
    public override string RawString
    {
        get
        {
            Mail mail = this.Content;
            string emailString; 
            string recipient = mail.Recipient;
            switch (mail.Protocol)
            {
                case EmailProtocol.MailTo:
                    string subjectMailTo =
                        string.IsNullOrWhiteSpace(mail.Subject) ?
                            string.Empty :
                            string.Concat("subject=", Uri.EscapeDataString(mail.Subject));
                    string bodyMailTo =
                        string.IsNullOrWhiteSpace(mail.Body) ?
                            string.Empty :
                            string.Concat("body=", Uri.EscapeDataString(mail.Body));
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

                case EmailProtocol.Smtp:
                case EmailProtocol.MatMsg:
                    string subject =
                        string.IsNullOrWhiteSpace(mail.Subject) ?
                            string.Empty :
                            EscapeBasic(mail.Subject);
                    string body =
                        string.IsNullOrWhiteSpace(mail.Body) ?
                            string.Empty :
                            EscapeBasic(mail.Body);
                    emailString = 
                        mail.Protocol == EmailProtocol.Smtp ?
                            $"{recipient}:{subject}:{body}":
                            $"TO:{recipient};SUB:{subject};BODY:{body};;";
                    break;

                default:
                    throw new NotImplementedException();
            }

            return string.Concat(mail.ProtocolToString(), emailString); 
        }
    }
}