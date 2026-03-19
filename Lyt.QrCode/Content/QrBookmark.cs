namespace Lyt.QrCode.Content;

public class QrBookmark : QrContent<QrBookmark>, IQrParsable<QrBookmark>
{
    public QrBookmark(string url, string title = "") : base(isBinaryData: false)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null, empty or white space", nameof(url));
        }

        this.Url = Uri.EscapeDataString(url);

        // Title can be empty, but not null. 
        this.Title = title;
    }

    public string Title { get; }

    public string Url { get; }

    public override string QrString => $"MEBKM:TITLE:{this.Title};URL:{this.Url};;";

    public static bool TryParse(string source, [NotNullWhen(true)] out QrBookmark? qrBookmark)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source string cannot be null, empty or white space", nameof(source));
        }

        const string key = "MEBKM:";
        const string titleKey = "TITLE:";
        const string urlKey = "URL:";

        qrBookmark = null;
        if (!source.StartsWith(key))
        {
            return false;
        }

        string title = string.Empty;
        string url = string.Empty;
        source = source[key.Length..];
        string[] tokens = source.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string token in tokens)
        {
            if (token.StartsWith(titleKey))
            {
                // Can be empty 
                title = token[titleKey.Length..];
                continue;
            }

            if (token.StartsWith(urlKey))
            {
                url = token[urlKey.Length..];
                if (string.IsNullOrWhiteSpace(url))
                {
                    // URL cannot be empty 
                    return false;
                }

                continue;
            }
        }

        try
        {
            qrBookmark = new QrBookmark(title, url);
            return true;
        }
        catch
        {
            // Swallow everything 
        }

        return false; 
    }
}
