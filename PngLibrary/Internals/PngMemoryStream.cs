namespace Lyt.Png.Internals;

/// <summary> 
/// A MemoryStream that keeps track of the bytes written to it since the last call to <see cref="WriteChunkHeader"/>. 
/// This is used to conveniently calculate the CRC for PNG chunks.
/// </summary>
internal class PngMemoryStream : MemoryStream
{
    // Holds the bytes written to the stream since the last call to WriteChunkHeader
    private readonly List<byte> written = [];

    public void WriteChunkHeader(byte[] header)
    {
        written.Clear();
        this.Write(header, 0, header.Length);
    }

    public void WriteChunkLength(int length) => StreamHelper.WriteBigEndianInt32(this, length);

    public override void Write(byte[] buffer, int offset, int count)
    {
        written.AddRange(buffer.Skip(offset).Take(count));
        base.Write(buffer, offset, count);
    }

    public void WriteCrc() => StreamHelper.WriteBigEndianInt32(this, (int)Crc32.Calculate(written));
}
