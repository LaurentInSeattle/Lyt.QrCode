namespace Lyt.QrCode.Content;

public class PhoneNumber
{
    public PhoneNumber(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            throw new ArgumentException("Phone number is required, cannot be null, empty or white space", nameof(number));
        }

        this.Number = number;
    }

    public string Number { get; private set; }
}

public class PhoneNumberContent (PhoneNumber phoneNumber) : QrContent<PhoneNumber>(phoneNumber)
{
    public override string RawString => $"tel:{this.Content.Number}";
}
