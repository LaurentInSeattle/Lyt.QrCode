namespace Lyt.QrCode.Detector;

public sealed class ResultPoint(float x, float y)
{
    public float X { get; } = x;

    public float Y { get; } = y;

    public override string ToString() => $"({this.X}, {this.Y})";
}
