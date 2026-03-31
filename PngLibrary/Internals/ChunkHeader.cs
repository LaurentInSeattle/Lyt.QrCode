namespace Lyt.Png.Internals;

/// <summary> The header for a data chunk in a PNG file. </summary>
/// <param name="Position"> The position/start of the chunk header within the stream. </param>
/// <param name="Length"> The length of the chunk in bytes. </param>
/// <param name="Name"> The name of the chunk, uppercase first letter means the chunk is critical (vs. ancillary). </param>
internal record class ChunkHeader(long Position, int Length, string Name)
{
    /// <summary> Whether the chunk is critical (must be read by all readers) or ancillary (may be ignored). </summary>
    internal bool IsCritical => char.IsUpper(this.Name[0]);

    /// <summary>
    /// A public chunk is one that is defined in the International Standard or is registered in the list of public chunk types maintained by the Registration Authority. 
    /// Applications can also define private (unregistered) chunk types for their own purposes.
    /// </summary>
    internal bool IsPublic => char.IsUpper(this.Name[1]);

    /// <summary> Whether the (if unrecognized) chunk is safe to copy. </summary>
    internal bool IsSafeToCopy => char.IsUpper(this.Name[3]);

    /// <inheritdoc />
    public override string ToString() => $"{this.Name} at {this.Position} (length: {this.Length}).";
}