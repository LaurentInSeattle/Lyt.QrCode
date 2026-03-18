namespace Lyt.QrCode.Content;

internal class QrVCard(string firstName, string lastName) 
    : QrContactCard(firstName, lastName) , IQrParsable<QrVCard>
{
    /// <summary> The kind of address (home or work). </summary>
    /// <remarks>  VCard Only  </remarks>
    public enum AddressKind
    {
        Home, // Default 
        Work,
        HomePref,
        WorkPref,
    }

    public AddressKind Kind { get; set; } = AddressKind.Home;

    public string AddressKindToString()
        => this.Kind switch
        {
            AddressKind.Home => "home",
            AddressKind.Work => "work",
            AddressKind.HomePref => "home,pref",
            AddressKind.WorkPref => "work,pref",
            _ => "home,pref"
        };

    public override string RawString
    {
        get
        {
            var sb = new StringBuilder();
            var card = this;

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

            return sb.ToString();
        }
    }

    public static bool TryParse(string source, [NotNullWhen(true)] out QrVCard? qrVCard)
        => throw new NotImplementedException();
}
