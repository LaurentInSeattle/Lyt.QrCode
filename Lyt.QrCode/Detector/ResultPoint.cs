namespace Lyt.QrCode.Detector;

internal sealed class ResultPoint(float x, float y)
{
    internal float X { get; } = x;

    internal float Y { get; } = y;

    public override string ToString() => $"({this.X}, {this.Y})";
}
