namespace Lyt.QrCode.Utilities; 

/// <summary> Extension methods for the <see cref="BitArray"/> class. </summary>
public static class BitArrayExtensions
{
    /// <summary> 
    /// Appends the specified number bits of the specified value to this bit array.
    /// The least significant bits of the specified value are added. They are appended in reverse order,
    /// from the most significant to the least significant one, i.e. bits 0 to <i>len-1</i>
    /// are appended in the order <i>len-1</i>, <i>len-2</i> ... 1, 0.
    /// Requires 0 &#x2264; len &#x2264; 31, and 0 &#x2264; val &lt; 2<sup>len</sup>.
    /// </summary>
    /// <param name="bitArray">The BitArray instance that this method extends.</param>
    /// <param name="val">The value to append.</param>
    /// <param name="len">The number of low-order bits in the value to append.</param>
    /// <exception cref="ArgumentOutOfRangeException">Value or number of bits is out of range.</exception>
    public static void AppendBits(this BitArray bitArray, uint val, int len)
    {
        if (len < 0 || len > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(len), "'len' out of range");
        }

        if (val >> len != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(val), "'val' out of range");
        }

        int bitLength = bitArray.Length;
        bitArray.Length = bitLength + len;
        uint mask = 1U << (len - 1);
        for (int i = bitLength; i < bitLength + len; i++) // Append bit by bit
        {
            if ((val & mask) != 0)
            {
                bitArray.Set(i, true);
            }

            mask >>= 1;
        }
    }


    /// <summary> Appends the content of the specified bit array to the end of this array. </summary>
    /// <param name="bitArray">The BitArray instance that this method extends.</param>
    /// <param name="otherArray">The bit array to append</param>
    /// <exception cref="ArgumentNullException">If <c>bitArray</c> is <c>null</c>.</exception>
    public static void AppendData(this BitArray bitArray, BitArray otherArray)
    {
        int bitLength = bitArray.Length;
        bitArray.Length = bitLength + otherArray.Length;
        for (int i = 0; i < otherArray.Length; i++, bitLength++)  // Append bit by bit
        {
            if (otherArray[i])
            {
                bitArray.Set(bitLength, true);
            }
        }
    }

    /// <summary>
    /// Extracts the specified number of bits at the specified index in this bit array.
    /// The bit at index <paramref name="index"/> becomes the most significant bit of the result,
    /// The bit at index <paramref name="index"/> + <paramref name="len"/> - 1 becomes the least significant bit.
    /// Requires 0 &#x2264; <em>len</em> &#x2264; 31, 0 &#x2264; <em>index</em>, and <em>index + len</em> &#x2264; <em>bit array length</em>.
    /// </summary>
    /// <param name="bitArray">The BitArray instance that this method extends.</param>
    /// <param name="index">The index of the first bit to extract.</param>
    /// <param name="len">The number of bits to extract.</param>
    /// <returns>The extracted bits as an unsigned integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Index or length is out of range.</exception>
    public static uint ExtractBits(this BitArray bitArray, int index, int len)
    {
        if (len < 0 || len > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(len), "'len' out of range");
        }

        if (index < 0 || index + len > bitArray.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "'index' out of range");
        }

        uint result = 0;
        for (int i = 0; i < len; i++)
        {
            result <<= 1;
            if (bitArray.Get(index + i))
            {
                result |= 1;
            }
        }

        return result;
    }
}

