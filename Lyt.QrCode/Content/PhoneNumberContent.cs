namespace Lyt.QrCode.Content;

public partial class PhoneNumber
{
    [GeneratedRegex(@"^[0+]+|[ ()-]")]
    private static partial Regex PhoneNumberRegex();

    public PhoneNumber(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new ArgumentException("Phone number is required, cannot be null, empty or white space", nameof(number));
        }

        string cleanedPhoneNumber = PhoneNumberRegex().Replace(number, string.Empty);
        this.Number = cleanedPhoneNumber;
    }

    public string Number { get; private set; }
}

public class PhoneNumberContent (PhoneNumber phoneNumber) : QrContent<PhoneNumber>(phoneNumber)
{
    public override string RawString => $"tel:{this.Content.Number}";
}
