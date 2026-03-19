namespace Lyt.QrCode.Content.Internal;

internal class DecoderOutput : Dictionary<Type, MethodInfo>
{
    internal static DecoderOutput Create()
    {
        var decoderOutput = new DecoderOutput();

        var supportedTypes = new List<Type>()
        {
            typeof(QrUri),
            typeof(QrBookmark),
            typeof(QrGeoLocation),
            typeof(QrPhoneNumber),
            typeof(QrWifi),
            typeof(QrCalendarEvent),
            typeof(QrMail),
            typeof(QrMeCard),
            typeof(QrVCard),
            typeof(QrTextMessage),
        };

        foreach (Type supportedType in supportedTypes)
        {
            if (!decoderOutput.TryAddQrParser(supportedType))
            {
                Debug.WriteLine("==> Parser:  Unsupported type: " + supportedType.Name);
            }
        }

        return decoderOutput;
    }

    internal bool TryAddQrParser(Type type)
    {
        // Make sure the provide type is a QrContent<T> , T being the provided type 
        if (!type.IsSubclassOf(typeof(QrContent)))
        {
            return false;
        }

        var baseType = type.BaseType;
        if ((baseType is null) || (!baseType.IsGenericType))
        {
            return false;
        }

        Type[] typeParameters = baseType.GetGenericArguments();
        if ((typeParameters.Length != 1) || (type != typeParameters[0]))
        {
            return false;
        }

        // Reflect into the provided type to check for a static TryParse public method
        // that matches the required signature
        // Ex:    public static bool TryParse(string source, [NotNullWhen(true)] out QrBookmark? qrBookmark)
        var methodInfo = type.GetMethod("TryParse", BindingFlags.Public | BindingFlags.Static);
        if (methodInfo is null)
        {
            return false;
        }

        // Should return bool 
        if (methodInfo.ReturnType != typeof(bool))
        {
            return false;
        }

        // Should take a string as first parameter 
        var parameters = methodInfo.GetParameters();
        if ((parameters.Length != 2) || (parameters[0].ParameterType != typeof(string)))
        {
            return false;
        }

        // Second parameter should be 'out' 
        var nextParameter = parameters[1];
        if (!nextParameter.IsOut )
        {
            return false;
        }

        // Second parameter is verified  to be 'out' and should be same as type
        var realType = nextParameter.ParameterType.GetElementType();
        if (realType != type )
        {
            return false; 
        } 
 
        this.Add(type, methodInfo);
        return true;
    }
}
