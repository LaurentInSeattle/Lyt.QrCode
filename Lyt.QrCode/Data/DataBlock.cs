namespace Lyt.QrCode.Data;

/// <summary>
/// Encapsulates a block of data within a QR Code. QR Codes may split their data into
/// multiple blocks, each of which is a unit of data and error-correction codewords. Each
/// is represented by an instance of this class.
/// </summary>
internal sealed class DataBlock
{
    internal int NumDataCodewords { get; private set; }

    internal byte[] Codewords { get; private set; }

    private DataBlock(int numDataCodewords, byte[] codewords)
    {
        this.NumDataCodewords = numDataCodewords;
        this.Codewords = codewords;
    }

    /// <summary> 
    /// When QR Codes use multiple data blocks, they are actually interleaved.
    /// That is, the first byte of data block 1 to n is written, then the second bytes, and so on. This
    /// method will separate the data into original blocks.
    /// </summary>
    /// <param name="rawCodewords">bytes as read directly from the QR Code </param>
    /// <param name="version">version of the QR Code </param>
    /// <param name="ecLevel">error-correction level of the QR Code </param>
    /// <returns> DataBlock's containing original bytes, "de-interleaved" from representation in the QR Code. </returns>
    internal static DataBlock[] GetDataBlocks(byte[] rawCodewords, QrVersion version, ErrorCorrectionLevel ecLevel)
    {
        if (rawCodewords.Length != version.TotalCodewords)
        {
            throw new ArgumentException("rawCodewords.Length != version.TotalCodewords", nameof(rawCodewords));
        }

        // Figure out the number and size of data blocks used by this version and error correction level
        ECBlocks ecBlocks = version.ECBlocksForLevel(ecLevel);

        // First count the total number of data blocks
        int totalBlocks = 0;
        ECB[] ecBlockArray = ecBlocks.Blocks;
        foreach (var ecBlock in ecBlockArray)
        {
            totalBlocks += ecBlock.Count;
        }

        // Now establish DataBlocks of the appropriate size and number of data codewords
        var result = new DataBlock[totalBlocks];
        int numResultBlocks = 0;
        foreach (var ecBlock in ecBlockArray)
        {
            for (int i = 0; i < ecBlock.Count; i++)
            {
                int numDataCodewords = ecBlock.DataCodewords;
                int numBlockCodewords = ecBlocks.ECCodewordsPerBlock + numDataCodewords;
                result[numResultBlocks++] = new DataBlock(numDataCodewords, new byte[numBlockCodewords]);
            }
        }

        // All blocks have the same amount of data, except that the last n
        // (where n may be 0) have 1 more byte. Figure out where these start.
        int shorterBlocksTotalCodewords = result[0].Codewords.Length;
        int longerBlocksStartAt = result.Length - 1;
        while (longerBlocksStartAt >= 0)
        {
            int numCodewords = result[longerBlocksStartAt].Codewords.Length;
            if (numCodewords == shorterBlocksTotalCodewords)
            {
                break;
            }

            longerBlocksStartAt--;
        }

        longerBlocksStartAt++;
        int shorterBlocksNumDataCodewords = shorterBlocksTotalCodewords - ecBlocks.ECCodewordsPerBlock;
        
        // The last elements of result may be 1 element longer;
        // first fill out as many elements as all of them have
        int rawCodewordsOffset = 0;
        for (int i = 0; i < shorterBlocksNumDataCodewords; i++)
        {
            for (int j = 0; j < numResultBlocks; j++)
            {
                result[j].Codewords[i] = rawCodewords[rawCodewordsOffset++];
            }
        }

        // Fill out the last data block in the longer ones
        for (int j = longerBlocksStartAt; j < numResultBlocks; j++)
        {
            result[j].Codewords[shorterBlocksNumDataCodewords] = rawCodewords[rawCodewordsOffset++];
        }
        
        // Now add in error correction blocks
        int max = result[0].Codewords.Length;
        for (int i = shorterBlocksNumDataCodewords; i < max; i++)
        {
            for (int j = 0; j < numResultBlocks; j++)
            {
                int iOffset = j < longerBlocksStartAt ? i : i + 1;
                result[j].Codewords[iOffset] = rawCodewords[rawCodewordsOffset++];
            }
        }
        
        return result;
    }
}
