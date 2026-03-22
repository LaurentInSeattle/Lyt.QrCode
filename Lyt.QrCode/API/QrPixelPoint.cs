namespace Lyt.QrCode.API;

public class QrPixelPoint(int x =-1, int y =-1)
{
    private const int MaxCoordinate = 16 * 1_024;
    
    public int X { get; } = x;

    public int Y { get; } = y;

    public bool IsValid => (this.X >= 0 && this.Y >= 0) && (this.X <= MaxCoordinate && this.Y <= MaxCoordinate); 

    public override string ToString() => this.IsValid ? $"({this.X}, {this.Y})" : "Invalid";             
}
