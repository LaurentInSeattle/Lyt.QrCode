namespace Lyt.Png.Internals; 

internal readonly struct HeaderValidationResult(
    int byte1, int byte2, int byte3, int byte4, int byte5, int byte6, int byte7, int byte8)
{
    private static readonly byte[] ExpectedHeader = [ 137, 80, 78, 71, 13, 10, 26, 10 ];

    public int Byte1 { get; } = byte1;

    public int Byte2 { get; } = byte2;

    public int Byte3 { get; } = byte3;

    public int Byte4 { get; } = byte4;

    public int Byte5 { get; } = byte5;

    public int Byte6 { get; } = byte6;

    public int Byte7 { get; } = byte7;

    public int Byte8 { get; } = byte8;

    private bool IsValid { get; } =
            byte1 == ExpectedHeader[0] && byte2 == ExpectedHeader[1] &&
            byte3 == ExpectedHeader[2] && byte4 == ExpectedHeader[3] &&
            byte5 == ExpectedHeader[4] && byte6 == ExpectedHeader[5] &&
            byte7 == ExpectedHeader[6] && byte8 == ExpectedHeader[7];

    public override string ToString() 
        => $"{this.Byte1} {this.Byte2} {this.Byte3} {this.Byte4} {this.Byte5} {this.Byte6} {this.Byte7} {this.Byte8}";
}