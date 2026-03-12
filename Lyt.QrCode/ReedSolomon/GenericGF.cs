namespace Lyt.QrCode.ReedSolomon; 

/// <summary>
/// This class contains utility methods for performing mathematical operations over
/// the Galois Fields. Operations use a given primitive polynomial in calculations.
/// Throughout this package, elements of the GF are represented as an {@code int}
/// for convenience and speed (but at the cost of memory).
/// The size of the GF is assumed to be a power of two.
/// </summary>

internal sealed class GenericGF
{
    /// <summary> QR Code </summary>
    internal static GenericGF QR_CODE_FIELD_256 = new(0b100011101, 256, 0); // x^8 + x^4 + x^3 + x^2 + 1

    #region Not used - Kept for future reference 
    ///// <summary> Aztec data 12 </summary>
    //public static GenericGF AZTEC_DATA_12 = new (0b1000001101001, 4096, 1); // x^12 + x^6 + x^5 + x^3 + 1
    ///// <summary> Aztec data 10 </summary>
    //public static GenericGF AZTEC_DATA_10 = new (0b10000001001, 1024, 1); // x^10 + x^3 + 1
    ///// <summary> Aztec data 6 </summary>
    //public static GenericGF AZTEC_DATA_6 = new (0b1000011, 64, 1); // x^6 + x + 1
    ///// <summary> Aztec param </summary>
    //public static GenericGF AZTEC_PARAM = new (0b10011, 16, 1); // x^4 + x + 1
    ///// <summary> Data Matrix </summary>
    //public static GenericGF DATA_MATRIX_FIELD_256 = new (0b100101101, 256, 1); // x^8 + x^5 + x^3 + x^2 + 1
    ///// <summary> Aztec data 8 </summary>
    //public static GenericGF AZTEC_DATA_8 = DATA_MATRIX_FIELD_256;
    ///// <summary> Maxicode </summary>
    //public static GenericGF MAXICODE_FIELD_64 = AZTEC_DATA_6;
    #endregion Not used - Kept for future reference 

    private readonly int[] expTable;
    private readonly int[] logTable;
    private readonly GenericGFPoly zero;
    private readonly GenericGFPoly one;
    private readonly int size;
    private readonly int primitive;
    private readonly int generatorBase;

    /// <summary> Create a representation of GF(size) using the given primitive polynomial. </summary>
    /// <param name="primitive">irreducible polynomial whose coefficients are represented by
    /// *  the bits of an int, where the least-significant bit represents the constant
    /// *  coefficient</param>
    /// <param name="size">the size of the field</param>
    /// <param name="genBase">the factor b in the generator polynomial can be 0- or 1-based
    /// *  (g(x) = (x+a^b)(x+a^(b+1))...(x+a^(b+2t-1))).
    /// *  In most cases it should be 1, but for QR code it is 0.</param>
    internal GenericGF(int primitive, int size, int genBase)
    {
        this.primitive = primitive;
        this.size = size;
        this.generatorBase = genBase;

        this.expTable = new int[size];
        this.logTable = new int[size];
        int x = 1;
        for (int i = 0; i < size; i++)
        {
            this.expTable[i] = x;
            x <<= 1; // x *= 2; 2 (the polynomial x) is a primitive element
            if (x >= size)
            {
                x ^= primitive;
                x &= size - 1;
            }
        }

        for (int i = 0; i < size - 1; i++)
        {
            this.logTable[this.expTable[i]] = i;
        }

        // logTable[0] == 0 but this should never be used
        this.zero = new GenericGFPoly(this, [0]);
        this.one = new GenericGFPoly(this, [1]);
    }

    /// <summary> Gets this instance's size. </summary>
    internal int Size => this.size;

    /// <summary> Gets the generator base. </summary>
    internal int GeneratorBase => this.generatorBase; 

    internal GenericGFPoly Zero => this.zero;

    internal GenericGFPoly One => this.one;

    /// <summary> Builds and returns the monomial representing coefficient * x^degree. </summary>
    /// <param name="degree">The degree.</param>
    /// <param name="coefficient">The coefficient.</param>
    internal GenericGFPoly BuildMonomial(int degree, int coefficient)
    {
        if (degree < 0)
        {
            throw new ArgumentException("Galois Polynomial Degree is negative ", nameof(degree));
        }

        if (coefficient == 0)
        {
            return this.zero;
        }

        int[] coefficients = new int[degree + 1];
        coefficients[0] = coefficient;
        return new GenericGFPoly(this, coefficients);
    }

    /// <summary> Implements both addition and subtraction -- they are the same in GF(size). </summary>
    /// <returns>sum/difference of a and b</returns>
    internal static int AddOrSubtract(int a, int b) =>  a ^ b;

    /// <summary> Exponentiates the specified integer. </summary>
    /// <returns>2 to the power of a in GF(size)</returns>
    internal int Exp(int a) => this.expTable[a];

    /// <summary> Returns base 2 log of a in GF(size), the provided integer. </summary>
    internal int Log(int a)
    {
        if (a == 0)
        {
            throw new ArithmeticException();
        }

        return this.logTable[a];
    }

    /// <summary> Returns the multiplicative inverse of the provided integer </summary>
    internal int Inverse(int a)
    {
        if (a == 0)
        {
            throw new ArithmeticException();
        }

        return this.expTable[size - this.logTable[a] - 1];
    }

    /// <summary> Returns the product of provided integers a and b in GF(size) </summary>
    internal int Multiply(int a, int b)
    {
        if (a == 0 || b == 0)
        {
            return 0;
        }

        return this.expTable[(this.logTable[a] + this.logTable[b]) % (size - 1)];
    }

    /// <summary> Returns a <see cref="string"/> that represents this instance. </summary>
    public override string ToString() => "GF(0x" + this.primitive.ToString("X") + ',' + this.size + ')';    
}
