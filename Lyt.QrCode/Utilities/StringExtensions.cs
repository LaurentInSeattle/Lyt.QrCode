namespace Lyt.QrCode.Utilities;

internal static partial class StringExtensions
{
    [GeneratedRegex("^[0-9]*$", RegexOptions.Compiled)]
    private static partial Regex CompiledNumericRegex();

    // Describes precisely all strings that are encodable in numeric mode.
    private static readonly Regex NumericRegex = CompiledNumericRegex();

    [GeneratedRegex("^[A-Z0-9 $%*+./:-]*$", RegexOptions.Compiled)]
    private static partial Regex CompiledAlphanumericRegex();

    // Describes precisely all strings that are encodable in alphanumeric mode.
    private static readonly Regex AlphanumericRegex = CompiledAlphanumericRegex();

    // The set of all legal characters in alphanumeric mode, where
    // each character value maps to the index in the string.
    internal const string AlphanumericCharset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    internal static void ThrowIfNullOrWhiteSpace(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("String must not be null or empty.", nameof(text));
        }
    }

    internal static uint IndexOfAlphanumeric(this char x)
        => (uint)AlphanumericCharset.IndexOf(x); 

    /// <summary> 
    /// Tests whether the specified string can be encoded as a segment in numeric mode. 
    /// A string is encodable iff each character is in the range "0" to "9".
    /// </summary>
    /// <param name="text">the string to test for encodability (not <c>null</c>)</param>
    /// <returns><c>true</c> iff each character is in the range "0" to "9".</returns>
    internal static bool IsNumeric(this string text)
        =>  NumericRegex.IsMatch(text);

    /// <summary> 
    /// Tests whether the specified string can be encoded as a segment in alphanumeric mode.
    /// A string is encodable iff each character is in the range "0" to "9", "A" to "Z" (uppercase only),
    /// space, dollar, percent, asterisk, plus, hyphen, period, slash, colon.
    /// </summary>
    /// <param name="text">the string to test for encodability (not <c>null</c>)</param>
    /// <returns><c>true</c> iff each character is in the alphanumeric mode character set.</returns>
    internal static bool IsAlphanumeric(this string text)
        => AlphanumericRegex.IsMatch(text);

}
