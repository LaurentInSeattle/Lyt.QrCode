namespace Lyt.QrCode.Data;

/// <summary>
/// Encapsulates the parameters for one error-correction block in one symbol version.
/// This includes the number of data codewords, and the number of times a block with these
/// parameters is used consecutively in the QR code version's format.
/// </summary>
internal sealed record class ECB(int Count, int DataCodewords); 
