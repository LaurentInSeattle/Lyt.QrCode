namespace Lyt.QrCode.API;

public class EncodeResult<TResult> : MessageLog where TResult : class
{
    /// <summary> True if encoding was succesful, otherwise false. </summary>
    [MemberNotNullWhen(true, nameof(Result))]
    public bool Success => this.Result is not null && !this.Error; 

    /// <summary> The result object of type TResult of the encoding process. Not null if Success if true. </summary>
    public TResult? Result { get; set; }

    /// <summary> The version (size) of this QR code (between 1 for the smallest and 40 for the biggest). </summary>
    /// <remarks> Valid if and only if Success is true </remarks>
    public int QrCodeVersion { get; internal set; } = -1;

    /// <summary> The width and height of this QR code, in modules (pixels). The size is a value between 21 and 177.  </summary>
    /// <remarks> Valid if and only if Success is true </remarks>
    public int QrCodeDimension { get; internal set; } = -1;

}

