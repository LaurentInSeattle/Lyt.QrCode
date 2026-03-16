namespace Lyt.QrCode.Content;

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

    /// <summary> The kind of address (home or work). </summary>
    /// <remarks>  VCard Only  </remarks>
    public enum AddressKind
    {
        Home, // Default 
        Work,
        HomePref,
        WorkPref,
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

    // VCard Only 
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

    public string AddressKindToString()
        // VCard only 
        => this.Kind switch
        {
            AddressKind.Home => "home",
            AddressKind.Work => "work",
            AddressKind.HomePref => "home,pref",
            AddressKind.WorkPref => "work,pref",
            _ => "home,pref"
        };
}

internal sealed class ContactCardContent(ContactCard contactCard) : QrContent<ContactCard>(contactCard)
{
    public override string RawString
    {
        get
        {
            var sb = new StringBuilder();
            var card = this.Content;

            // These may have been set to whitespace via properties after construction so we need to check
            // again and set to empty if needed. 
            string streetString = !string.IsNullOrWhiteSpace(card.Street) ? card.Street : "";
            string houseNumberString = !string.IsNullOrWhiteSpace(card.HouseNumber) ? card.HouseNumber : "";
            string cityString = !string.IsNullOrWhiteSpace(card.City) ? card.City : "";
            string stateRegionString = !string.IsNullOrWhiteSpace(card.StateRegion) ? card.StateRegion : "";
            string zipCodeString = !string.IsNullOrWhiteSpace(card.ZipCode) ? card.ZipCode : "";
            string countryString = !string.IsNullOrWhiteSpace(card.Country) ? card.Country : "";

            // No need to push an address if everything is empty 
            bool allEmpty =
                string.IsNullOrEmpty(streetString) &&
                string.IsNullOrEmpty(houseNumberString) &&
                string.IsNullOrEmpty(cityString) &&
                string.IsNullOrEmpty(zipCodeString) &&
                string.IsNullOrEmpty(countryString);

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

                if (!allEmpty)
                {
                    string streetHouse =
                        card.Format == AddressFormat.European ?
                            $"{streetString} {houseNumberString}" :
                            $"{houseNumberString} {streetString}";
                    string addressString =
                            $"ADR:,,{streetHouse},{cityString},{stateRegionString},{zipCodeString},{countryString};";
                    sb.Append(addressString);
                }

                // Terminator 
                sb.Append(';');
            }
            else
            {
                // Format 4.0 Only (from 2011- should be fine in 2026 and later on) 
                sb.AppendLine("BEGIN:VCARD");
                sb.AppendLine($"VERSION:4.0");

                sb.AppendLine($"N:{card.LastName};{card.FirstName};;;");
                sb.AppendLine($"FN:{card.FirstName} {card.LastName}");

                if (!string.IsNullOrEmpty(card.Nickname))
                {
                    sb.AppendLine($"NICKNAME:{card.Nickname}");
                }

                if (!string.IsNullOrEmpty(card.Organization))
                {
                    sb.AppendLine($"ORG:{card.Organization}");
                }

                if (!string.IsNullOrEmpty(card.OrganizationTitle))
                {
                    sb.AppendLine($"TITLE:{card.OrganizationTitle}");
                }

                if (!string.IsNullOrEmpty(card.Phone))
                {
                    sb.AppendLine($"TEL;TYPE=home,voice;VALUE=uri:tel:{card.Phone}");
                }

                if (!string.IsNullOrEmpty(card.MobilePhone))
                {
                    sb.AppendLine($"TEL;TYPE=home,cell;VALUE=uri:tel:{card.MobilePhone}");
                }

                if (!string.IsNullOrEmpty(card.WorkPhone))
                {
                    sb.AppendLine($"TEL;TYPE=work,voice;VALUE=uri:tel:{card.WorkPhone}");
                }

                if (!allEmpty)
                {
                    string addressStringHeader = "ADR;TYPE=" + card.AddressKindToString() + ":";
                    string streetHouse =
                        card.Format == AddressFormat.European ?
                            $"{streetString} {houseNumberString}" :
                            $"{houseNumberString} {streetString}";
                    string addressString =
                            $"{streetHouse};{cityString};{stateRegionString};{zipCodeString};{countryString};";
                    sb.AppendLine(string.Concat(addressStringHeader, addressString));
                }

                if (card.Birthday != null)
                {
                    sb.AppendLine($"BDAY:{((DateTime)card.Birthday).ToString("yyyyMMdd", CultureInfo.InvariantCulture)};");
                }

                if (!string.IsNullOrWhiteSpace(card.Email))
                {
                    sb.AppendLine($"EMAIL:{card.Email};");
                }

                if (!string.IsNullOrWhiteSpace(card.Website))
                {
                    sb.AppendLine($"URL:{card.Website};");
                }

                if (!string.IsNullOrWhiteSpace(card.Note))
                {
                    sb.AppendLine($"NOTE:{card.Note};");
                }

                sb.AppendLine("END:VCARD");
            }

            return sb.ToString();
        }
    }
}
