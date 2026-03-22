namespace Lyt.QrCode.API;

public class EncodeResult<T> : MessageLog where T : class
{
    [MemberNotNullWhen(true, nameof(Result))]
    public bool Success => this.Result is not null && !this.Error; 

    public T? Result { get; set; }

    /// <summary> The version (size) of this QR code (between 1 for the smallest and 40 for the biggest). </summary>
    /// <remarks> Valid if and only if Success is true </remarks>
    public int QrCodeVersion { get; internal set; } = -1;

    /// <summary> The width and height of this QR code, in modules (pixels). The size is a value between 21 and 177.  </summary>
    /// <remarks> Valid if and only if Success is true </remarks>
    public int QrCodeDimension { get; internal set; } = -1;

}

