namespace Lyt.QrCode.Content;

#region Documentation 


// VCARD : https://en.wikipedia.org/wiki/VCard 

// MeCard : https://en.wikipedia.org/wiki/MeCard_(QR_code) 

#endregion Documentation 

/// <summary> 
/// The address format for both VCard and MeCard.
/// European format: [Street] [House Number] and [Postal Code] [City] - Default
/// North American (and others): [House Number] [Street] and [City] [Postal Code]) 
/// </summary>
public enum ContactAddressFormat
{
    /// <summary> European format (Default) </summary>
    European,

    /// <summary> North American and others format. </summary>
    NorthAmerica,
}

/// <summary> 
/// Represents a contact card containing personal and organizational information, 
/// base class for vCard and MeCard formats.
/// </summary>
public class QrContactCard<T> : QrContent<T> where T : class, IQrParsable<T>
{
    protected const string nameKey = "N:";
    protected const string nicknameKey = "NICKNAME:";
    protected const string orgKey = "ORG:";
    protected const string titleKey = "TITLE:";
    protected const string birthdayKey = "BDAY:";
    protected const string emailKey = "EMAIL:";
    protected const string websiteKey = "URL:";
    protected const string noteKey = "NOTE:";

    /// <summary> Constructor for use by applications  </summary>
    public QrContactCard(string firstName, string lastName) : base(isBinaryData: false)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First Name is required, cannot be null, empty or white space", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last Name is required, cannot be null, empty or white space", nameof(lastName));
        }

        this.FirstName = firstName;
        this.LastName = lastName;
        this.BirthdayString = string.Empty;
        this.Birthday = null;
    }

    /// <summary> Constructor for internal use when parsing contact cards. </summary>
    protected QrContactCard() : base(isBinaryData: false)
    {
        this.FirstName = string.Empty;
        this.LastName = string.Empty;
        this.BirthdayString = string.Empty;
        this.Birthday = null;
    }

    public string FirstName { get; protected set; }

    public string LastName { get; protected set; }

    public ContactAddressFormat Format { get; set; } = ContactAddressFormat.European;

    // All other relevant optional Card fields as properties defaulting to empty 
    public string Title { get; set; } = string.Empty;

    public string Nickname { get; set; } = string.Empty;

    public string Organization { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string MobilePhone { get; set; } = string.Empty;

    public string WorkPhone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Website { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string HouseNumber { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string ZipCode { get; set; } = string.Empty;

    public string StateRegion { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public string BirthdayString
    {
        get => field;
        set
        {
            field = value;
            if (DateTime.TryParse(value, out var date))
            {
                this.Birthday = date;
            }
        }
    }

    public DateTime? Birthday { get; set; } = null;

    static bool TryParse(string source, [NotNullWhen(true)] out T? _)
        => throw new NotImplementedException();
}
