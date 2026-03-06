namespace Lyt.QrCode.Data;

/// <summary> <p>Encapsulates a set of error-correction blocks in one symbol version. Most versions will
/// use blocks of differing sizes within one version, so, this encapsulates the parameters for
/// each set of blocks. It also holds the number of error-correction codewords per block since it
/// will be the same across all blocks within one version.</p>
/// </summary>
internal sealed class ECBlocks
{
    internal ECBlocks(int ecCodewordsPerBlock, params ECB[] ecBlocks)
    {
        this.ECCodewordsPerBlock = ecCodewordsPerBlock;
        this.Blocks = ecBlocks;
    }

    /// <summary> Gets the EC codewords per block. </summary>
    public int ECCodewordsPerBlock { get; private set;  }

    /// <summary> Gets the EC blocks. </summary>
    public ECB[] Blocks { get; private set; }

    /// <summary> Gets the total count of blocks. </summary>
    public int TotalBlocks => (from ecBlock in this.Blocks select ecBlock.Count).Sum();

    /// <summary> Gets the total count of EC codewords. </summary>
    public int TotalECCodewords => this.ECCodewordsPerBlock * this.TotalBlocks;
}

