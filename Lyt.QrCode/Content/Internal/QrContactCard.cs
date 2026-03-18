namespace Lyt.QrCode.Content.Internal;

#region Documentation 


// VCARD : https://en.wikipedia.org/wiki/VCard 

// MeCard : https://en.wikipedia.org/wiki/MeCard_(QR_code) 

#endregion Documentation 

/// <summary> 
/// Represents a contact card containing personal and organizational information, 
/// base class for vCard and MeCard formats.
/// </summary>
public class QrContactCard : QrContent, IQrParsable<QrContactCard> 
{
    /// <summary> 
    /// The address format.
    /// European format: [Street] [House Number] and [Postal Code] [City] - Default
    /// North American (and others): [House Number] [Street] and [City] [Postal Code]) 
    /// </summary>
    public enum AddressFormat
    {
        /// <summary> European format (Default) </summary>
        European,

        /// <summary> North American and others format. </summary>
        NorthAmerica,
    }

    public QrContactCard(string firstName, string lastName) : base (isBinaryData: false)
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

    public string FirstName { get; }

    public string LastName { get; }

    public AddressFormat Format { get; set; } = AddressFormat.European;

    // All other relevant optional Card fields as properties defaulting to empty 
    public string Nickname { get; set; } = string.Empty;

    public string Organization { get; set; } = string.Empty;

    public string OrganizationTitle { get; set; } = string.Empty;

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

    public static bool TryParse(string source, [NotNullWhen(true)] out QrContactCard? _)
        => throw new NotImplementedException();
}
