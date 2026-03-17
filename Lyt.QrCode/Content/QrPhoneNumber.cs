namespace Lyt.QrCode.Content;

public partial class QrPhoneNumber : QrContent<QrPhoneNumber>
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

    public override string RawString => $"tel:{this.Number}";
}
