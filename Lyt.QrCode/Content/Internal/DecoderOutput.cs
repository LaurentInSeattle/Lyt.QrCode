namespace Lyt.QrCode.Content.Internal;

internal class DecoderOutput : Dictionary<Type, MethodInfo>
{
    internal static DecoderOutput Create()
    {
        var decoderOutput = new DecoderOutput();

        var supportedTypes = new List<Type>()
        {
            typeof(QrBookmark),
            typeof(QrCalendarEvent),
            typeof(QrGeoLocation),
            typeof(QrMail),
            typeof(QrMeCard),
            typeof(QrPhoneNumber),
            typeof(QrPhoneNumber),
            typeof(QrTextMessage),
            typeof(QrUri),
            typeof(QrWifi),
        };

        foreach (Type supportedType in supportedTypes)
        {
            decoderOutput.TryAddQrParser(supportedType);
        }

        return decoderOutput;
    }

    internal bool TryAddQrParser(Type type)
    {
        // TODO: Make sure the type is a QrContent<T>  

        // TODO: Reflect into this type to check for a TryParse method that matches the required signature
        // this.Add(type);

        return true;
    }
}
