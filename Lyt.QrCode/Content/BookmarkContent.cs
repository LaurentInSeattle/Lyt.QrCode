namespace Lyt.QrCode.Content;

public class Bookmark
{
    public Bookmark(string url, string title = "")
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
}

public sealed class BookmarkContent(Bookmark webLink) : QrContent<Bookmark>(webLink)
{
    public override string RawString { get; set; } = $"MEBKM:TITLE:{webLink.Title};URL:{webLink.Url};;";
}
