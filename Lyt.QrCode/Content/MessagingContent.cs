namespace Lyt.QrCode.Content;

using static Lyt.QrCode.Content.Messaging;

#region Documentation 

/*

MMS 

        private readonly string _number, _subject;
        private readonly MMSEncoding _encoding;

        public override string ToString() => _encoding switch
        {
            MMSEncoding.MMSTO => $"mmsto:{_number}{(string.IsNullOrEmpty(_subject) ? string.Empty : $"?subject={Uri.EscapeDataString(_subject)}")}",
            MMSEncoding.MMS => $"mms:{_number}{(string.IsNullOrEmpty(_subject) ? string.Empty : $"?body={Uri.EscapeDataString(_subject)}")}",
            _ => string.Empty,
        };

        /// <summary>
        /// Defines the encoding types for the MMS payload.
        /// </summary>
        public enum MMSEncoding
        {
            /// <summary>
            /// Uses the "mms:" URI scheme.
            /// </summary>
            MMS,

            /// <summary>
            /// Uses the "mmsto:" URI scheme.
            /// </summary>
            MMSTO
        }

SMS

        private readonly string _number, _subject;
        private readonly SMSEncoding _encoding;

        public override string ToString() => _encoding switch
        {
            SMSEncoding.SMS => $"sms:{_number}{(string.IsNullOrEmpty(_subject) ? string.Empty : $"?body={Uri.EscapeDataString(_subject)}")}",
            SMSEncoding.SMS_iOS => $"sms:{_number}{(string.IsNullOrEmpty(_subject) ? string.Empty : $";body={Uri.EscapeDataString(_subject)}")}",
            SMSEncoding.SMSTO => $"SMSTO:{_number}:{_subject}",
            _ => string.Empty,
        };

        public enum SMSEncoding
        {
            /// <summary>
            /// Standard SMS encoding.
            /// </summary>
            SMS,
            /// <summary>
            /// SMSTO encoding.
            /// </summary>
            SMSTO,
            /// <summary>
            /// SMS encoding for iOS.
            /// </summary>
            SMS_iOS
        }

SKype : username

        public override string ToString() => $"skype:{_skypeUsername}?call";


WhatsApp : Number + text 
            var cleanedPhone = Regex.Replace(_number, @"^[0+]+|[ ()-]", string.Empty);
            return ($"https://wa.me/{cleanedPhone}?text={Uri.EscapeDataString(_message)}");

*/

#endregion Documentation 

public class Messaging
{
    public enum MessagingProtocol
    {
        Sms , 
        SmsTo, 
        SmsIos, 
        Mms, 
        MmsTo,
        Skype,
        WhatsApp,
    }

    public Messaging(string number, string text, MessagingProtocol messagingProtocol)
    {
        
    }
}


public class MessagingContent (Messaging messaging) : QrContent<Messaging>(messaging)
{
    public override string RawString
    {
        get
        {
            var msg = this.Content;
            return ";"; 
            //return this.Content.Protocol switch
            //{
            //    GeoProtocol.Geo => $"geo:{latString},{longString}",
            //    GeoProtocol.GoogleMapsLink => $"https://www.google.com/maps/@{latString},{longString}",
            //    GeoProtocol.BingMapsLink => $"https://www.bing.com/maps/search?style=r&cp={latString}%7E{longString}",
            //    _ => throw new NotImplementedException("Unsupported geo protocol"),
            //};
        }
    }
}
