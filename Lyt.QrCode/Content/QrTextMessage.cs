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

    public enum MessagingProtocol
    {
        Sms , 
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
                Sms => "sms:",
                SmsIos => "sms:",

                Mms => "mms:",
                MmsTo => "mmsto:",

                WhatsApp => "https:",

                _ => throw new NotImplementedException(),
            };
    public override string RawString
    {
        get
        {
            var ms = this;
            return ms.Protocol switch
            {
                Sms => $"sms:{ms.Number}?body={Uri.EscapeDataString(ms.Text)}",
                SmsIos => $"sms:{ms.Number};body={Uri.EscapeDataString(ms.Text)}",
                Mms => $"mms:{ms.Number}?body={Uri.EscapeDataString(ms.Text)}",
                MmsTo => $"mmsto:{ms.Number}?subject={Uri.EscapeDataString(ms.Text)}",
                WhatsApp => ($"https://wa.me/{ms.Number}?text={Uri.EscapeDataString(ms.Text)}"),
                _ => throw new NotImplementedException("Unsupported messaging protocol"),
            };
        }
    }

    public static bool TryParse(string source, [NotNullWhen(true)] out QrTextMessage? qrTextMessage)
        => throw new NotImplementedException();
}
