
namespace Lyt.QrCode.Utilities; 

/// <summary> 
/// Computes the Reed-Solomon error correction codewords for a sequence of data codewords at a given degree.
/// Note: All data blocks in a QR code share the same the divisor polynomial.
/// </summary>
internal class ReedSolomonGenerator
{
    // Coefficients of the divisor polynomial, stored from highest to lowest power, excluding the leading term which
    // is always 1. For example the polynomial x^3 + 255x^2 + 8x + 93 is stored as the uint8 array {255, 8, 93}.
    private readonly byte[] coefficients;

    /// <summary> Initializes a new Reed-Solomon ECC generator for the specified degree.  </summary>
    /// <param name="degree">The divisor polynomial degree (between 1 and 255).</param>
    /// <exception cref="ArgumentOutOfRangeException"><c>degree</c> &lt; 1 or <c>degree</c> &gt; 255</exception>
    internal ReedSolomonGenerator(int degree)
    {
        if (degree < 1 || degree > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(degree), "Degree out of range");
        }

        // Start with the monomial x^0
        this.coefficients = new byte[degree];
        this.coefficients[degree - 1] = 1;

        // Compute the product polynomial (x - r^0) * (x - r^1) * (x - r^2) * ... * (x - r^{degree-1}),
        // drop the highest term, and store the rest of the coefficients in order of descending powers.
        // Note that r = 0x02, which is a generator element of this field GF(2^8/0x11D).
        uint root = 1;
        for (int i = 0; i < degree; i++)
        {
            // Multiply the current product by (x - r^i)
            for (int j = 0; j < this.coefficients.Length; j++)
            {
                this.coefficients[j] = Multiply(this.coefficients[j], root);
                if (j + 1 < this.coefficients.Length)
                {
                    this.coefficients[j] ^= this.coefficients[j + 1];
                }
            }

            root = Multiply(root, 0x02);
        }
    }

    /// <summary> Computes the Reed-Solomon error correction codewords for the specified sequence of data codewords. </summary>
    /// <param name="data">The sequence of data codewords.</param>
    internal byte[] GetRemainder(byte[] data)
    {
        // Compute the remainder by performing polynomial division
        byte[] result = new byte[coefficients.Length];
        for (int k = 0 ; k < data.Length; ++k)
        {
            byte b = data[k]; 
            uint factor = (uint)(b ^ result[0]);
            Array.Copy(result, 1, result, 0, result.Length - 1);
            result[^1] = 0;
            for (int i = 0; i < result.Length; i++)
            {
                result[i] ^= Multiply(coefficients[i], factor);
            }
        }

        return result;
    }

    // Returns the product of the two given field elements modulo GF(2^8/0x11D). The arguments and result
    // are unsigned 8-bit integers. This could be implemented as a lookup table of 256*256 entries of uint8.
    private static byte Multiply(uint x, uint y)
    {
        Debug.Assert(x >> 8 == 0 && y >> 8 == 0);

        // Russian peasant multiplication
        // See:  https://en.wikipedia.org/wiki/Ancient_Egyptian_multiplication
        uint z = 0;
        for (int i = 7; i >= 0; i--)
        {
            z = (z << 1) ^ ((z >> 7) * 0x11D);
            z ^= ((y >> i) & 1) * x;
        }

        Debug.Assert(z >> 8 == 0);
        return (byte)z;
    }
}
