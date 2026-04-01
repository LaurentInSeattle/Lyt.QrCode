namespace Lyt.Png.Internals; 

internal static class StreamExtensions
{
    internal static int ReadBigEndianInt32(this Stream stream) => 
        (stream.ReadOrTerminate() << 24) + 
        (stream.ReadOrTerminate() << 16) + 
        (stream.ReadOrTerminate() << 8) +
        stream.ReadOrTerminate();

    internal static int ReadBigEndianInt32(this byte[] bytes, int offset) => 
        (bytes[0 + offset] << 24) + 
        (bytes[1 + offset] << 16) + 
        (bytes[2 + offset] << 8) + 
        bytes[3 + offset];

    internal static void WriteBigEndianInt32(this Stream stream, int value)
    {
        stream.WriteByte((byte)(value >> 24));
        stream.WriteByte((byte)(value >> 16));
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
    }

    internal static byte ReadOrTerminate(this Stream stream)
    {
        int b = stream.ReadByte();
        if (b == -1)
        {
            throw new InvalidOperationException($"Unexpected end of stream at {stream.Position}.");
        }

        return (byte) b;
    }

    internal static bool TryReadHeaderBytes(this Stream stream, out byte[] bytes)
    {
        bytes = new byte[8];
        return stream.Read(bytes, 0, 8) == 8;
    }
}