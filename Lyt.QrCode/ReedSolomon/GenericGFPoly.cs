namespace Lyt.QrCode.ReedSolomon;

/// <summary>
/// <p>Represents a polynomial whose coefficients are elements of a GF.
/// Instances of this class are immutable.</p>
/// <p>Much credit is due to William Rucklidge since portions of this code are an indirect
/// port of his C++ Reed-Solomon implementation.</p>
/// </summary>
internal sealed class GenericGFPoly
{
    /// <summary> Initializes a new instance of the <see cref="GenericGFPoly"/> class. </summary>
    /// <param name="field">the {@link GenericGF} instance representing the field to use
    /// to perform computations</param>
    /// <param name="coefficients">coefficients as ints representing elements of GF(size), arranged
    /// from most significant (highest-power term) coefficient to least significant</param>
    /// <exception cref="ArgumentException">if argument is null or empty,
    /// or if leading coefficient is 0 and this is not a
    /// constant polynomial (that is, it is not the monomial "0")</exception>
    internal GenericGFPoly(GenericGF field, int[] coefficients)
    {
        if (coefficients.Length == 0)
        {
            throw new ArgumentException("coefficients Length is zero", nameof(coefficients));
        }

        this.Field = field;
        int coefficientsLength = coefficients.Length;
        if (coefficientsLength > 1 && coefficients[0] == 0)
        {
            // Leading term must be non-zero for anything except the constant polynomial "0"
            int firstNonZero = 1;
            while (firstNonZero < coefficientsLength && coefficients[firstNonZero] == 0)
            {
                firstNonZero++;
            }
            if (firstNonZero == coefficientsLength)
            {
                this.Coefficients = new int[] { 0 };
            }
            else
            {
                this.Coefficients = new int[coefficientsLength - firstNonZero];
                Array.Copy(coefficients,
                    firstNonZero,
                    this.Coefficients,
                    0,
                    this.Coefficients.Length);
            }
        }
        else
        {
            this.Coefficients = coefficients;
        }
    }

    internal GenericGF Field { get; private set; }

    internal int[] Coefficients {  get ; private set; }

    /// <summary> degree of this polynomial </summary>
    internal int Degree =>  this.Coefficients.Length - 1;

    /// <summary> Gets a value indicating whether this <see cref="GenericGFPoly"/> is zero. </summary>
    /// <value>true iff this polynomial is the monomial "0"</value>
    internal bool IsZero => this.Coefficients[0] == 0; 

    /// <summary> coefficient of x^degree term in this polynomial </summary>
    /// <param name="degree">The degree.</param>
    /// <returns>coefficient of x^degree term in this polynomial</returns>
    internal int getCoefficient(int degree) => this.Coefficients[this.Coefficients.Length - 1 - degree];

    /// <summary> Evaluation of this polynomial at the given point A </summary>
    internal int evaluateAt(int a)
    {
        int result = 0;
        if (a == 0)
        {
            // Just return the x^0 coefficient
            return getCoefficient(0);
        }
        if (a == 1)
        {
            // Just the sum of the coefficients
            foreach (var coefficient in this.Coefficients)
            {
                result = GenericGF.addOrSubtract(result, coefficient);
            }
            return result;
        }
        result = this.Coefficients[0];
        int size = this.Coefficients.Length;
        for (int i = 1; i < size; i++)
        {
            result = GenericGF.addOrSubtract(this.Field.multiply(a, result), this.Coefficients[i]);
        }

        return result;
    }

    internal GenericGFPoly addOrSubtract(GenericGFPoly other)
    {
        if (!this.Field.Equals(other.Field))
        {
            throw new ArgumentException("GenericGFPolys do not have same GenericGF field");
        }

        if (this.IsZero)
        {
            return other;
        }

        if (other.IsZero)
        {
            return this;
        }

        int[] smallerCoefficients = this.Coefficients;
        int[] largerCoefficients = other.Coefficients;
        if (smallerCoefficients.Length > largerCoefficients.Length)
        {
            int[] temp = smallerCoefficients;
            smallerCoefficients = largerCoefficients;
            largerCoefficients = temp;
        }

        int[] sumDiff = new int[largerCoefficients.Length];
        int lengthDiff = largerCoefficients.Length - smallerCoefficients.Length;
        // Copy high-order terms only found in higher-degree polynomial's coefficients
        Array.Copy(largerCoefficients, 0, sumDiff, 0, lengthDiff);

        for (int i = lengthDiff; i < largerCoefficients.Length; i++)
        {
            sumDiff[i] = GenericGF.addOrSubtract(smallerCoefficients[i - lengthDiff], largerCoefficients[i]);
        }

        return new GenericGFPoly(this.Field, sumDiff);
    }

    internal GenericGFPoly multiply(GenericGFPoly other)
    {
        if (!this.Field.Equals(other.Field))
        {
            throw new ArgumentException("GenericGFPolys do not have same GenericGF field");
        }

        if (this.IsZero || other.IsZero)
        {
            return this.Field.Zero;
        }

        int[] aCoefficients = this.Coefficients;
        int aLength = aCoefficients.Length;
        int[] bCoefficients = other.Coefficients;
        int bLength = bCoefficients.Length;
        int[] product = new int[aLength + bLength - 1];
        for (int i = 0; i < aLength; i++)
        {
            int aCoeff = aCoefficients[i];
            for (int j = 0; j < bLength; j++)
            {
                product[i + j] = 
                    GenericGF.addOrSubtract(product[i + j], this.Field.multiply(aCoeff, bCoefficients[j]));
            }
        }

        return new GenericGFPoly(this.Field, product);
    }

    internal GenericGFPoly Multiply(int scalar)
    {
        if (scalar == 0)
        {
            return this.Field.Zero;
        }

        if (scalar == 1)
        {
            return this;
        }
        
        int size = this.Coefficients.Length;
        int[] product = new int[size];
        for (int i = 0; i < size; i++)
        {
            product[i] = this.Field.multiply(this.Coefficients[i], scalar);
        }

        return new GenericGFPoly(this.Field, product);
    }

    internal GenericGFPoly multiplyByMonomial(int degree, int coefficient)
    {
        if (degree < 0)
        {
            throw new ArgumentException();
        }
        if (coefficient == 0)
        {
            return this.Field.Zero;
        }
        int size = this.Coefficients.Length;
        int[] product = new int[size + degree];
        for (int i = 0; i < size; i++)
        {
            product[i] = this.Field.multiply(this.Coefficients[i], coefficient);
        }
        return new GenericGFPoly(this.Field, product);
    }

    internal GenericGFPoly[] divide(GenericGFPoly other)
    {
        if (!this.Field.Equals(other.Field))
        {
            throw new ArgumentException("GenericGFPolys do not have same GenericGF field");
        }

        if (other.IsZero)
        {
            throw new ArgumentException("Divide by 0");
        }

        GenericGFPoly quotient = this.Field.Zero;
        GenericGFPoly remainder = this;

        int denominatorLeadingTerm = other.getCoefficient(other.Degree);
        int inverseDenominatorLeadingTerm = this.Field.inverse(denominatorLeadingTerm);

        while (remainder.Degree >= other.Degree && !remainder.IsZero)
        {
            int degreeDifference = remainder.Degree - other.Degree;
            int scale = this.Field.multiply(remainder.getCoefficient(remainder.Degree), inverseDenominatorLeadingTerm);
            GenericGFPoly term = other.multiplyByMonomial(degreeDifference, scale);
            GenericGFPoly iterationQuotient = this.Field.buildMonomial(degreeDifference, scale);
            quotient = quotient.addOrSubtract(iterationQuotient);
            remainder = remainder.addOrSubtract(term);
        }

        return [quotient, remainder];
    }

    public override string ToString()
    {
        if (this.IsZero)
        {
            return "0";
        }

        var sb = new StringBuilder(8 * this.Degree);
        for (int degree = this.Degree; degree >= 0; degree--)
        {
            int coefficient = getCoefficient(degree);
            if (coefficient != 0)
            {
                if (coefficient < 0)
                {
                    if (degree == this.Degree)
                    {
                        sb.Append("-");
                    }
                    else
                    {
                        sb.Append(" - ");
                    }
                    coefficient = -coefficient;
                }
                else
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(" + ");
                    }
                }
                
                if (degree == 0 || coefficient != 1)
                {
                    int alphaPower = this.Field.log(coefficient);
                    if (alphaPower == 0)
                    {
                        sb.Append('1');
                    }
                    else if (alphaPower == 1)
                    {
                        sb.Append('a');
                    }
                    else
                    {
                        sb.Append("a^");
                        sb.Append(alphaPower);
                    }
                }

                if (degree != 0)
                {
                    if (degree == 1)
                    {
                        sb.Append('x');
                    }
                    else
                    {
                        sb.Append("x^");
                        sb.Append(degree);
                    }
                }
            }
        }

        return sb.ToString();
    }
}