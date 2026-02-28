namespace Lyt.QrCode.Content;

public sealed class WebLink
{
        public WebLink(string url, string title ="")
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("URL cannot be null or empty", nameof(url));
            }
    
            this.Url = url;

            // Title can be empty, but not null. 
            this.Title = title;
        }

        public string Title { get; }

        public string Url { get; }
}

public sealed class WebLinkContent(WebLink webLink) : QrContent<WebLink>(webLink)
{
    public override string RawString { get; set; } = $"MEBKM:TITLE:{webLink.Title};URL:{webLink.Url};;";

    public override byte[] RawBytes
    {
        get => Encoding.UTF8.GetBytes(this.RawString);
        set { /* do nothing */ }
    }
}
