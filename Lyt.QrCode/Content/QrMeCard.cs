namespace Lyt.QrCode.Content;

/// <summary> A support class to encode MeCards within a QR code  </summary>
/// <remark> MeCards are somewhat more compact than VCards </remark>
/// <remark> Will encode and decode phone numbers in this order: Regular, Mobile, Work</remark>
public class QrMeCard : QrContactCard<QrMeCard>, IQrParsable<QrMeCard>
{
    private const string protocol = "MECARD:";
    private const string phoneKey = "TEL:";
    private const string addressKey = "ADR:";

    public QrMeCard(string firstName, string lastName) : base(firstName, lastName) { }

    private QrMeCard() : base() { }

    public string PoBox { get; set; } = string.Empty;

    public string RoomNumber { get; set; } = string.Empty;

    public override string QrString
    {
        get
        {
            var sb = new StringBuilder();
            var card = this;

            // These may have been set to whitespace via properties after construction so we need to check
            // again and set to empty if needed. 
            string poBoxString = !string.IsNullOrWhiteSpace(card.PoBox) ? card.PoBox : "";
            string roomNumberString = !string.IsNullOrWhiteSpace(card.RoomNumber) ? card.RoomNumber : "";
            string streetString = !string.IsNullOrWhiteSpace(card.Street) ? card.Street : "";
            string houseNumberString = !string.IsNullOrWhiteSpace(card.HouseNumber) ? card.HouseNumber : "";
            string cityString = !string.IsNullOrWhiteSpace(card.City) ? card.City : "";
            string stateRegionString = !string.IsNullOrWhiteSpace(card.StateRegion) ? card.StateRegion : "";
            string zipCodeString = !string.IsNullOrWhiteSpace(card.ZipCode) ? card.ZipCode : "";
            string countryString = !string.IsNullOrWhiteSpace(card.Country) ? card.Country : "";

            // No need to push an address if everything is empty 
            bool allEmpty =
                string.IsNullOrEmpty(poBoxString) &&
                string.IsNullOrEmpty(roomNumberString) &&
                string.IsNullOrEmpty(streetString) &&
                string.IsNullOrEmpty(houseNumberString) &&
                string.IsNullOrEmpty(cityString) &&
                string.IsNullOrEmpty(zipCodeString) &&
                string.IsNullOrEmpty(countryString);

            sb.Append(protocol);
            sb.Append($"N:{card.LastName},{card.FirstName};");
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

            if (!string.IsNullOrWhiteSpace(card.Title))
            {
                sb.Append($"TITLE:{card.Title};");
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
                    card.Format == ContactAddressFormat.European ?
                        $"{streetString} {houseNumberString}" :
                        $"{houseNumberString} {streetString}";
                string addressString =
                        $"ADR:{poBoxString},{roomNumberString},{streetHouse},{cityString},{stateRegionString},{zipCodeString},{countryString};";
                sb.Append(addressString);
            }

            // Terminator 
            sb.Append(';');

            return sb.ToString();
        }
    }

    public static bool TryParse(string source, [NotNullWhen(true)] out QrMeCard? qrMeCard)
    {
        qrMeCard = null;
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source string cannot be null, empty or white space", nameof(source));
        }

        try
        {
            if (!source.StartsWith(protocol))
            {
                return false;
            }

            source = source[protocol.Length..];
            string[] tokens = source.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string firstName = string.Empty;
            string lastName = string.Empty;
            qrMeCard = new QrMeCard();

            foreach (string line in tokens)
            {
                if (line.StartsWith(nameKey))
                {
                    string names = line[nameKey.Length..];
                    string[] namesTokens = names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (namesTokens.Length == 0)
                    {
                        return false;
                    }

                    qrMeCard.LastName = namesTokens[0];
                    if (tokens.Length >= 2)
                    {
                        qrMeCard.FirstName = namesTokens[1];
                    }

                    continue;
                }

                if (line.StartsWith(nicknameKey))
                {
                    qrMeCard.Nickname = line[nicknameKey.Length..];
                    continue;
                }

                if (line.StartsWith(orgKey))
                {
                    qrMeCard.Organization = line[orgKey.Length..];
                    continue;
                }

                if (line.StartsWith(titleKey))
                {
                    qrMeCard.Title = line[titleKey.Length..];
                    continue;
                }

                if (line.StartsWith(phoneKey) && string.IsNullOrWhiteSpace(qrMeCard.Phone))
                {
                    qrMeCard.Phone = line[phoneKey.Length..];
                    continue;
                }

                if (line.StartsWith(phoneKey) && 
                    ! string.IsNullOrWhiteSpace( qrMeCard.Phone) &&
                    string.IsNullOrWhiteSpace(qrMeCard.MobilePhone))
                {
                    qrMeCard.MobilePhone = line[phoneKey.Length..];
                    continue;
                }

                if (line.StartsWith(phoneKey) &&
                    !string.IsNullOrWhiteSpace(qrMeCard.Phone) && 
                    !string.IsNullOrWhiteSpace(qrMeCard.MobilePhone) &&
                    string.IsNullOrWhiteSpace(qrMeCard.WorkPhone))
                {
                    qrMeCard.WorkPhone = line[phoneKey.Length..];
                    continue;
                }

                if (line.StartsWith(birthdayKey))
                {
                    string birthdayString = line[birthdayKey.Length..];
                    if (DateTime.TryParseExact(
                        birthdayString,
                        "yyyyMMdd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime parsedDate))
                    {
                        qrMeCard.Birthday = parsedDate;
                    }

                    qrMeCard.BirthdayString = birthdayString;
                    continue;
                }

                if (line.StartsWith(emailKey))
                {
                    qrMeCard.Email = line[emailKey.Length..];
                    continue;
                }

                if (line.StartsWith(websiteKey))
                {
                    qrMeCard.Website = line[websiteKey.Length..];
                    continue;
                }

                if (line.StartsWith(noteKey))
                {
                    qrMeCard.Note = line[noteKey.Length..];
                    continue;
                }

                if (line.StartsWith(addressKey))
                {
                    string addressRaw = line[addressKey.Length..];

                    // Split on ',' to get the address elements 
                    // DO NOT: StringSplitOptions.RemoveEmptyEntries
                    // DO NOT: StringSplitOptions.TrimEntries
                    string[] addressTokens = addressRaw.Split(',', StringSplitOptions.None);
                    if (addressTokens.Length < 7)
                    {
                        // Just skip this address item instead of failing 
                        continue;
                    }

                    qrMeCard.PoBox = addressTokens[0];
                    qrMeCard.RoomNumber = addressTokens[1];
                    qrMeCard.City = addressTokens[3];
                    qrMeCard.StateRegion = addressTokens[4];
                    qrMeCard.ZipCode = addressTokens[5];
                    qrMeCard.Country = addressTokens[6];

                    qrMeCard.Format = ContactAddressFormat.European;
                    string streetHouse = addressTokens[2];
                    int firstSpace = streetHouse.IndexOf(' ');
                    if (firstSpace == -1)
                    {
                        // No separator: assume Street field and no number 
                        qrMeCard.Street = streetHouse;
                    }
                    else
                    {
                        string firstPart = streetHouse[..firstSpace];
                        string lastPart = streetHouse[firstSpace..];
                        if (int.TryParse(firstPart, out int _))
                        {
                            // first part is a number !
                            qrMeCard.Format = ContactAddressFormat.NorthAmerica;
                            qrMeCard.HouseNumber = firstPart;
                            qrMeCard.Street = lastPart;
                        }
                        else
                        {
                            // Assume euro and reverse order (compared to North America) 
                            qrMeCard.HouseNumber = lastPart;
                            qrMeCard.Street = firstPart;
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
