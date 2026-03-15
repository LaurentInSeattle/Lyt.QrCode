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
        VCard ,  // vCard 4.0 from 2011 (RFC 6350) 
        MeCard,  // NTT Docomo Japanese preferred format 
    }

    public ContactCard(ContactCardFormat format , string firstName, string lastName)
    {
        this.Format = format;
        this.FirstName = firstName;
        this.LastName = lastName;
    }

    public ContactCardFormat Format { get; }

    public string FirstName { get; }

    public string LastName { get; }

    // TODO: All other relevant properties 
}

internal sealed class ContactCardContent(ContactCard contactCard) : QrContent<ContactCard>(contactCard)
{
    public override string RawString
    {
        get
        {
            var card = this.Content;
            // TODO:
            // Handle vCard or MeCard Format 
            return
                card.Format == ContactCardFormat.VCard ?
                    $"vCard" :
                    $"MeCard";
        }
    }
}
