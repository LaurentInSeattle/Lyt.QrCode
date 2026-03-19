namespace Lyt.QrCode.Content;

using static Lyt.QrCode.Content.QrTextMessage.MessagingProtocol;

#region Documentation 

/*
    Text message protocols include
    SMS (Short Message Service) for 160-character plain text,  MMS (Multimedia Messaging Service) for media, 
    and RCS (Rich Communication Services) for modern, internet-based features. 
    SMS is universal and cellular-based, while RCS offers high-res media, typing indicators, and encryption, 
    designed to replace SMS. 

Main Text Message Protocols

    SMS (Short Message Service): The traditional standard for sending plain text messages up to 160 
        characters, operating over cellular networks. 
        It is highly reliable, works without internet, but lacks modern features.
    MMS (Multimedia Messaging Service): An extension of SMS that allows for the transmission of multimedia 
        files, including images, audio, and video.
    RCS (Rich Communication Services):
        A modern, next-generation protocol that functions like messenger apps (WhatsApp/iMessage), supporting 
        high-resolution media, group chats, read receipts, and typing indicators. It operates over Wi-Fi or data.
    OTT (Over-the-top) Messaging: Apps like WhatsApp or Meta Messenger that use the internet rather than 
        cellular carriers to exchange messages.

 */

#endregion Documentation 

public partial class QrTextMessage : QrContent<QrTextMessage>, IQrParsable<QrTextMessage>
{
    [GeneratedRegex(@"^[0+]+|[ ()-]")]
    private static partial Regex PhoneNumberRegex();

    private const string protocolSms = "sms:";
    private const string protocolMms = "mms:";
    private const string protocolMmsTo = "mmsto:";
    private const string protocolWhatsApp = "https://wa.me/";

    public enum MessagingProtocol
    {
        Sms,
        SmsIos,
        Mms,
        MmsTo,
        WhatsApp,
    }

    public QrTextMessage(string number, string text, MessagingProtocol messagingProtocol = Sms)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new ArgumentException("Phone number is required, cannot be null, empty or white space", nameof(number));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Message text is required, cannot be null, empty or white space", nameof(text));
        }

        string cleanedPhoneNumber = PhoneNumberRegex().Replace(number, string.Empty);
        this.Number = cleanedPhoneNumber;
        this.Text = text;
        this.Protocol = messagingProtocol;
    }

    public string Number { get; private set; }

    public string Text { get; private set; }

    public MessagingProtocol Protocol { get; private set; }

    public string ProtocolToString() =>
        this.Protocol switch
        {
            Sms => protocolSms,
            SmsIos => protocolSms,
            Mms => protocolMms,
            MmsTo => protocolMmsTo,
            WhatsApp => protocolWhatsApp,

            _ => throw new NotImplementedException(),
        };

    public override string QrString
    {
        get
        {
            var m = this;
            return m.Protocol switch
            {
                Sms => $"{protocolSms}{m.Number}?body={Uri.EscapeDataString(m.Text)}",
                SmsIos => $"{protocolSms}{m.Number};body={Uri.EscapeDataString(m.Text)}",
                Mms => $"{protocolMms}{m.Number}?body={Uri.EscapeDataString(m.Text)}",
                MmsTo => $"{protocolMmsTo}{m.Number}?subject={Uri.EscapeDataString(m.Text)}",
                WhatsApp => ($"{protocolWhatsApp}{m.Number}?text={Uri.EscapeDataString(m.Text)}"),

                _ => throw new NotImplementedException("Unsupported messaging protocol"),
            };
        }
    }

    public static bool TryParse(string source, [NotNullWhen(true)] out QrTextMessage? qrTextMessage)
    {
        qrTextMessage = null;
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source string cannot be null, empty or white space", nameof(source));
        }

        try
        {
            MessagingProtocol protocol = MessagingProtocol.Sms;
            string number = string.Empty;
            string text = string.Empty;

            if (! // NOT any of these four 
                (source.StartsWith(protocolSms) ||
                source.StartsWith(protocolMms) ||
                source.StartsWith(protocolMmsTo) ||
                source.StartsWith(protocolWhatsApp)))
            {
                return false;
            }

            string[] tokens = source.Split(['?', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length != 2)
            {
                return false;
            }

            string maybeNumber = tokens[0];
            if (maybeNumber.StartsWith(protocolSms))
            {
                maybeNumber = maybeNumber[protocolSms.Length..];
                protocol = MessagingProtocol.Sms;
            }
            else if (maybeNumber.StartsWith(protocolMms))
            {
                maybeNumber = maybeNumber[protocolMms.Length..];
                protocol = MessagingProtocol.Mms;
            }
            else if (maybeNumber.StartsWith(protocolMmsTo))
            {
                maybeNumber = maybeNumber[protocolMmsTo.Length..];
                protocol = MessagingProtocol.MmsTo;
            }
            else if (maybeNumber.StartsWith(protocolWhatsApp))
            {
                maybeNumber = maybeNumber[protocolWhatsApp.Length..];
                protocol = MessagingProtocol.WhatsApp;
            }
            else
            {
                return false;
            }

            if (maybeNumber.Length >= 3)
            {
                number = maybeNumber;
            }

            if (string.IsNullOrWhiteSpace(number) || string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string maybeText = tokens[1];
            if (maybeText.Length < 5)
            {
                return false;
            }

            string messageKey = protocol switch
            {
                MessagingProtocol.Sms or MessagingProtocol.Mms => "body=",
                MessagingProtocol.MmsTo => "subject=",
                MessagingProtocol.WhatsApp => "text=",
                _ => throw new NotImplementedException("Unsupported messaging protocol"),
            };

            text = maybeText[messageKey.Length..];
            text = Uri.UnescapeDataString(text);
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            qrTextMessage = new QrTextMessage(number, text, protocol);
            return true;
        }
        catch
        {
            // Swallow everything else 
        }

        return false;
    }
}
