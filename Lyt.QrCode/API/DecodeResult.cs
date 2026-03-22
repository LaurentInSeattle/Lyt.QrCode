namespace Lyt.QrCode.API;

public class DecodeResult : MessageLog
{
    [MemberNotNullWhen(true, nameof(Text))]
    [MemberNotNullWhen(true, nameof(Bytes))]
    public bool Success =>
        this.IsDetected &&
        !string.IsNullOrWhiteSpace(this.Text) &&
        this.Bytes is not null &&
        this.Bytes.Length > 0 &&
        !this.Error;

    /// <summary> Text encoded by the QR Code, if applicable, otherwise empty<summary> 
    public string? Text { get; internal set; } = string.Empty;

    /// <summary> Bytes encoded by the QR Code, if applicable, otherwise null ><summary> 
    public byte[]? Bytes { get; internal set; } = null;

    /// <summary> True when a canonical object has been successfully parsed, otherwise false. ><summary> 
    [MemberNotNullWhen(true, nameof(ParsedObject))]
    [MemberNotNullWhen(true, nameof(ParsedType))]
    public bool IsParsed => this.ParsedType is not null && this.ParsedObject is not null;

    /// <summary></summary> The type of the canonical object,if successfully parsed, otherwise null.<summary> 
    public Type? ParsedType { get; internal set; } = null;

    /// <summary> The actual canonical object, if successfully parsed, otherwise null.<summary> 
    public object? ParsedObject { get; internal set; } = null;

    /// <summary> True when a QR code has been successfully detected but possibly not aligned, otherwise false. ><summary> 
    public bool IsDetected => this.TopLeft.IsValid && this.TopRight.IsValid && this.BottomLeft.IsValid;

    /// <summary> True when a QR code has been successfully detected AND aligned, otherwise false. ><summary> 
    public bool IsAligned =>  this.IsDetected && this.Alignment.IsValid ;

    public QrPixelPoint TopLeft { get; internal set; } = new();

    public QrPixelPoint TopRight { get; internal set; } = new();

    public QrPixelPoint BottomLeft { get; internal set; } = new();

    public QrPixelPoint Alignment { get; internal set; } = new();

    /// <summary> Tries to get the parsed object, if successfully parsed and of matching type, otherwise null.<summary> 
    public bool TryGet<T>([NotNullWhen(true)] out T? result) where T : QrContent
    {
        result = null;

        if (this.ParsedType is null || this.ParsedObject is null)
        {
            return false;
        }

        if (typeof(T) == this.ParsedType)
        {
            result = (T)this.ParsedObject!;
            return true;
        }

        return false;
    }
}
