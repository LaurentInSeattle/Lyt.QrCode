namespace Lyt.QrCode.Content;

public class Bookmark : QrContent<Bookmark>
{
    static Bookmark fake = new Bookmark("fake"); 

    public Bookmark(string url, string title = "") : base(fake, isBinaryData: false)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null, empty or white space", nameof(url));
        }

        this.Url = url;

        // Title can be empty, but not null. 
        this.Title = title;
    }

    public string Title { get; }

    public string Url { get; }

    public override string RawString => $"MEBKM:TITLE:{this.Title};URL:{this.Url};;";
}
