namespace Lyt.QrCode.Content;

using System.Xml.Linq;

#region Documentation 


// VCARD : https://en.wikipedia.org/wiki/VCard 

// MeCard : https://en.wikipedia.org/wiki/MeCard_(QR_code) 

#endregion Documentation 

using static Lyt.QrCode.Content.ContactCard;

public class ContactCard
{
    // CONSIDER:
    // creating one class for vCard and a separate for MeCard with a base class 
    public enum ContactCardFormat
    {
        VCard,  // vCard 4.0 from 2011 (RFC 6350) 
        MeCard,  // NTT Docomo Japanese preferred format 
    }


    /// <summary> 
    /// The address format.
    /// Default: European format, ([Street] [House Number] and [Postal Code] [City])
    /// Reversed: North American and others format ([House Number] [Street] and [City] [Postal Code])
    /// </summary>
    public enum AddressFormat
    {
        /// <summary> European format (Default) </summary>
        European,

        /// <summary> North American and others format. </summary>
        NorthAmerica,
    }

    /// <summary> The kind of address (home or work). </summary>
    public enum AddressKind
    {
        Home, // Default 
        Work,
    }

    public ContactCard(ContactCardFormat format, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First Name is required, cannot be null, empty or white space", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last Name is required, cannot be null, empty or white space", nameof(lastName));
        }

        this.CardFormat = format;
        this.FirstName = firstName;
        this.LastName = lastName;
        this.BirthdayString = string.Empty;
        this.Birthday = null;
    }

    public ContactCardFormat CardFormat { get; }

    public string FirstName { get; }

    public string LastName { get; }

    public AddressFormat Format { get; set; } = AddressFormat.European;

    public AddressKind Kind { get; set; } = AddressKind.Home;

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
}

internal sealed class ContactCardContent(ContactCard contactCard) : QrContent<ContactCard>(contactCard)
{
    public override string RawString
    {
        get
        {
            var sb = new StringBuilder();
            var card = this.Content;

            // Handle vCard or MeCard Format 
            if (card.CardFormat == ContactCardFormat.MeCard)
            {
                sb.Append("MECARD:");
                sb.Append($"N:{card.LastName},{card.FirstName}");
                if (!string.IsNullOrWhiteSpace(card.Nickname))
                {
                    sb.Append($"NICKNAME:{card.Nickname};");
                }

                if (card.Birthday != null)
                {
                    sb.Append($"BDAY:{((DateTime)card.Birthday).ToString("yyyyMMdd", CultureInfo.InvariantCulture)};");
                }

                if (!string.IsNullOrWhiteSpace(card.Organization))
                {
                    sb.Append($"ORG:{card.Organization};");
                }

                if (!string.IsNullOrWhiteSpace(card.OrganizationTitle))
                {
                    sb.Append($"TITLE:{card.OrganizationTitle};");
                }

                if (!string.IsNullOrWhiteSpace(card.Phone))
                {
                    sb.Append($"TEL:{card.Phone};");
                }

                if (!string.IsNullOrWhiteSpace(card.MobilePhone))
                {
                    sb.Append($"TEL:{card.MobilePhone};");
                }

                if (!string.IsNullOrWhiteSpace(card.WorkPhone))
                {
                    sb.Append($"TEL:{card.WorkPhone};");
                }

                if (!string.IsNullOrWhiteSpace(card.Email))
                {
                    sb.Append($"EMAIL:{card.Email};");
                }

                if (!string.IsNullOrWhiteSpace(card.Website))
                {
                    sb.Append($"URL:{card.Website};");
                }

                if (!string.IsNullOrWhiteSpace(card.Note))
                {
                    sb.Append($"NOTE:{card.Note};");
                }

                // These may have been set to whitespace via properties after construction so we need to check
                // again and set to empty if needed. 
                string streetString = !string.IsNullOrWhiteSpace(card.Street) ? card.Street : "";
                string houseNumberString = !string.IsNullOrWhiteSpace(card.HouseNumber) ? card.HouseNumber : "";
                string cityString = !string.IsNullOrWhiteSpace(card.City) ? card.City : "";
                string stateRegionString = !string.IsNullOrWhiteSpace(card.StateRegion) ? card.StateRegion : "";
                string zipCodeString = !string.IsNullOrWhiteSpace(card.ZipCode) ? card.ZipCode : "";
                string countryString = !string.IsNullOrWhiteSpace(card.Country) ? card.Country : "" ;

                // No need to push an address if everything is empty 
                bool allEmpty =
                    string.IsNullOrEmpty(streetString) &&
                    string.IsNullOrEmpty(houseNumberString) &&
                    string.IsNullOrEmpty(cityString) &&
                    string.IsNullOrEmpty(zipCodeString) &&
                    string.IsNullOrEmpty(countryString);
                if (!allEmpty)
                {
                    string streetHouse =
                        card.Format == AddressFormat.European ?
                            $"{streetString} {houseNumberString}" :
                            $"{houseNumberString} {streetString}" ;
                    string addressString =
                            $"ADR:,,{streetHouse},{cityString},{stateRegionString},{zipCodeString},{countryString};";
                    sb.Append(addressString);
                }

                // Terminator 
                sb.Append(';');
            }
            else
            {

            }

            return sb.ToString();
        }
    }
}
