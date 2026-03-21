namespace Lyt.QrCode.Content;

/// <summary> A support class to encode VCards within a QR code  </summary>
public class QrVCard : QrContactCard<QrVCard>, IQrParsable<QrVCard>
{
    private const string fullnameKey = "FN:";
    private const string phoneKey = "TEL;TYPE=home,voice;VALUE=uri:tel:";
    private const string mobilePhoneKey = "TEL;TYPE=home,cell;VALUE=uri:tel:";
    private const string workPhoneKey = "TEL;TYPE=work,voice;VALUE=uri:tel:";
    private const string addressPartialKey = "ADR;TYPE=";

    public QrVCard(string firstName, string lastName) : base(firstName, lastName) { }

    private QrVCard() : base() { }

    /// <summary> The kind of address (home or work). </summary>
    /// <remarks>  MeCard does not have that, VCard Only  </remarks>
    public enum AddressKind
    {
        Home, // Default 
        Work,
        HomePref,
        WorkPref,
    }

    public AddressKind Kind { get; set; } = AddressKind.Home;

    public string Fullname { get; set; } = string.Empty;

    public string AddressKindToString()
        => this.Kind switch
        {
            AddressKind.Home => "home",
            AddressKind.Work => "work",
            AddressKind.HomePref => "home,pref",
            AddressKind.WorkPref => "work,pref",
            _ => "home,pref"
        };

    public static AddressKind AddressKindFromString(string key)
        => key switch
        {
            "home" => AddressKind.Home,
            "work" => AddressKind.Work ,
            "home,pref" => AddressKind.HomePref,
            "work,pref" => AddressKind.WorkPref,
            _ => AddressKind.Home
        };

    public override string QrString
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

            if (!string.IsNullOrWhiteSpace(card.Fullname))
            {
                sb.AppendLine($"FN:{card.Fullname}");
            }
            else
            {
                sb.AppendLine($"FN:{card.FirstName} {card.LastName}");
            }

            if (!string.IsNullOrWhiteSpace(card.Nickname))
            {
                sb.AppendLine($"NICKNAME:{card.Nickname}");
            }

            if (!string.IsNullOrWhiteSpace(card.Organization))
            {
                sb.AppendLine($"ORG:{card.Organization}");
            }

            if (!string.IsNullOrWhiteSpace(card.Title))
            {
                sb.AppendLine($"TITLE:{card.Title}");
            }

            if (!string.IsNullOrWhiteSpace(card.Phone))
            {
                sb.AppendLine($"TEL;TYPE=home,voice;VALUE=uri:tel:{card.Phone}");
            }

            if (!string.IsNullOrWhiteSpace(card.MobilePhone))
            {
                sb.AppendLine($"TEL;TYPE=home,cell;VALUE=uri:tel:{card.MobilePhone}");
            }

            if (!string.IsNullOrWhiteSpace(card.WorkPhone))
            {
                sb.AppendLine($"TEL;TYPE=work,voice;VALUE=uri:tel:{card.WorkPhone}");
            }

            if (!allEmpty)
            {
                string addressStringHeader = "ADR;TYPE=" + card.AddressKindToString() + ":";
                string streetHouse =
                    card.Format == ContactAddressFormat.European ?
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
    {
        qrVCard = null;
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source string cannot be null, empty or white space", nameof(source));
        }

        try
        {
            if (!source.StartsWith("BEGIN:VCARD"))
            {
                return false;
            }

            qrVCard = new QrVCard();

            string[] lines = source.SplitLines();

            foreach (string rawLine in lines)
            {
                if (rawLine.StartsWith("BEGIN") || rawLine.StartsWith("END") || rawLine.StartsWith("VERSION"))
                {
                    continue;
                }

                string line = rawLine.Trim(';');
                if (string.IsNullOrWhiteSpace(line))
                {
                    return false;
                }

                if (line.StartsWith(nameKey))
                {
                    string names = line[nameKey.Length..];
                    string[] tokens = names.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (tokens.Length ==  0)
                    {
                        return false; 
                    }

                    qrVCard.LastName = tokens[0];
                    if (tokens.Length >= 2)
                    {
                        qrVCard.FirstName = tokens[1];
                    }

                    continue;
                }

                if (line.StartsWith(fullnameKey))
                {
                    qrVCard.Fullname = line[fullnameKey.Length..];
                    continue;
                }

                if (line.StartsWith(nicknameKey))
                {
                    qrVCard.Nickname = line[nicknameKey.Length..];
                    continue;
                }

                if (line.StartsWith(orgKey))
                {
                    qrVCard.Organization = line[orgKey.Length..];
                    continue;
                }

                if (line.StartsWith(titleKey))
                {
                    qrVCard.Title = line[titleKey.Length..];
                    continue;
                }

                if (line.StartsWith(phoneKey))
                {
                    qrVCard.Phone = line[phoneKey.Length..];
                    continue;
                }

                if (line.StartsWith(mobilePhoneKey))
                {
                    qrVCard.MobilePhone = line[mobilePhoneKey.Length..];
                    continue;
                }

                if (line.StartsWith(workPhoneKey))
                {
                    qrVCard.WorkPhone = line[workPhoneKey.Length..];
                    continue;
                }

                if (line.StartsWith(birthdayKey))
                {
                    string birthdayString = line[birthdayKey.Length..];
                    if ( DateTime.TryParseExact(
                        birthdayString, 
                        "yyyyMMdd", 
                        CultureInfo.InvariantCulture, 
                        DateTimeStyles.None, 
                        out DateTime parsedDate))
                    {
                        qrVCard.Birthday = parsedDate;
                    }

                    qrVCard.BirthdayString = birthdayString; 
                    continue;
                }

                if (line.StartsWith(emailKey))
                {
                    qrVCard.Email = line[emailKey.Length..];
                    continue;
                }

                if (line.StartsWith(websiteKey))
                {
                    qrVCard.Website = line[websiteKey.Length..];
                    continue;
                }

                if (line.StartsWith(noteKey))
                {
                    qrVCard.Note = line[noteKey.Length..];
                    continue;
                }

                if (line.StartsWith(addressPartialKey))
                {
                    // Not use of rawLine here so that we get the proper count of address items
                    string addressRaw = rawLine[addressPartialKey.Length..];

                    // Split on ':' to get the address kind 
                    string[] tokens = 
                        addressRaw.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (tokens.Length != 2 )
                    {
                        // Just skip this address item instead of failing 
                        continue; 
                    } 

                    qrVCard.Kind = AddressKindFromString(tokens[0]);

                    // Split on ';' to get the address elements 
                    // DO NOT: StringSplitOptions.RemoveEmptyEntries
                    // DO NOT: StringSplitOptions.TrimEntries
                    string[] addressTokens = tokens[1].Split(';', StringSplitOptions.None);
                    if (addressTokens.Length < 5)
                    {
                        // Just skip this address item instead of failing 
                        continue;
                    }

                    qrVCard.City = addressTokens[1];    
                    qrVCard.StateRegion = addressTokens[2];
                    qrVCard.ZipCode = addressTokens[3];
                    qrVCard.Country = addressTokens[4];

                    qrVCard.Format = ContactAddressFormat.European; 
                    string streetHouse = addressTokens[0];
                    int firstSpace = streetHouse.IndexOf(' ');
                    if (firstSpace == -1)
                    {
                        // No separator: assume Street field and no number 
                        qrVCard.Street = streetHouse;
                    } 
                    else
                    {
                        string firstPart = streetHouse[..firstSpace];
                        string lastPart = streetHouse[firstSpace..];
                        if (int.TryParse(firstPart, out int _))
                        {
                            // first part is a number !
                            qrVCard.Format = ContactAddressFormat.NorthAmerica;
                            qrVCard.HouseNumber = firstPart; 
                            qrVCard.Street = lastPart;
                        } 
                        else
                        {
                            // Assume euro and reverse order (compared to North America) 
                            qrVCard.HouseNumber = lastPart;
                            qrVCard.Street = firstPart;
                        }
                    }

                    continue;
                }
            }

            return true;
        }
        catch
        {
            // Swallow everything else 
        }

        return false;
    }
}
