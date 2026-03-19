namespace Lyt.QrCode.Content;

public partial class QrPhoneNumber : QrContent<QrPhoneNumber>, IQrParsable<QrPhoneNumber>
{
    [GeneratedRegex(@"^[0+]+|[ ()-]")]
    private static partial Regex PhoneNumberRegex();

    public QrPhoneNumber(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new ArgumentException("Phone number is required, cannot be null, empty or white space", nameof(number));
        }

        string cleanedPhoneNumber = PhoneNumberRegex().Replace(number, string.Empty);
        this.Number = cleanedPhoneNumber;
    }

    public string Number { get; private set; }

    public override string QrString => $"tel:{this.Number}";

    public static bool TryParse(string source, [NotNullWhen(true)] out QrPhoneNumber? qrPhoneNumber)
    {
        const string key = "tel:";
        qrPhoneNumber = null;
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Sourcestring cannot be null, empty or white space", nameof(source));
        }

        try
        {
            if (!source.StartsWith(key))
            {
                return false;
            }

            source = source[key.Length..];
            qrPhoneNumber = new QrPhoneNumber(source);
            return true;
        }
        catch
        {
            // Swallow everything 
        }

        return false;
    }
}
