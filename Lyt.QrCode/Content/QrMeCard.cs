namespace Lyt.QrCode.Content;

/// <summary> A support class to encode MeCards within a QR code  </summary>
/// <remark> MeCards are somewhat more compact than VCards </remark>
public class QrMeCard : QrContactCard<QrMeCard>, IQrParsable<QrMeCard>
{
    public QrMeCard(string firstName, string lastName) : base(firstName, lastName) { }

    private QrMeCard() : base() { }

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

            sb.Append("MECARD:");
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
                    card.Format == AddressFormat.European ?
                        $"{streetString} {houseNumberString}" :
                        $"{houseNumberString} {streetString}";
                string addressString =
                        $"ADR:,,{streetHouse},{cityString},{stateRegionString},{zipCodeString},{countryString};";
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
            if (!source.StartsWith("MECARD:"))
            {
                return false;
            }

            // TODO
            string firstName = string.Empty;
            string lastName = string.Empty;
            qrMeCard = new QrMeCard();
            return false;
        }
        catch
        {
            // Swallow everything else 
        }

        return false;
    }
}
