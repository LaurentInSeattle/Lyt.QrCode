namespace Lyt.QrCode.Utilities;

internal static class ByteArrayExtensions
{
    internal static byte[] CopyOfRange(this byte[] original, int from, int to)
    {
        Debug.Assert(from >= 0 && from <= to && to <= original.Length);

        byte[] result = new byte[to - from];
        Array.Copy(original, from, result, 0, to - from);
        return result;
    }

    internal static byte[] CopyOf(this byte[] original, int newLength)
    {
        byte[] result = new byte[newLength];
        Array.Copy(original, result, Math.Min(original.Length, newLength));
        return result;
    }
}
