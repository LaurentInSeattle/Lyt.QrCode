namespace Lyt.Png;

public partial class PngImage
{
    private readonly int backgroundColorInt;
    private readonly Dictionary<int, int> colorCounts = [];
    private readonly List<(string keyword, byte[] data)> storedStrings = [];

    /// <summary> Allows you to store arbitrary text data in the "iTXt" international textual data chunks of the generated PNG image. </summary>
    /// <param name="keyword">
    /// A keyword identifying the text data between 1-79 characters in length. Must not start with, end with or contain 
    /// consecutive whitespace characters. Only characters in the range 32 - 126 and 161 - 255 are permitted.
    /// </param>
    /// <param name="text"> The text data to store. Encoded as UTF-8 that may not contain zero (0) bytes but can be zero-length. </param>
    public void AddTextualMetadata(string keyword, string text)
    {
        if (keyword == string.Empty)
        {
            throw new ArgumentException("Keyword may not be empty.", nameof(keyword));
        }

        // trailing, leading and consecutive whitespaces are prohibited : Removing them
        keyword = keyword.Trim();
        keyword = keyword.Replace("  ", " ");
        if (keyword.Length > 79)
        {
            throw new ArgumentException(
                $"Keyword must be between 1 - 79 characters, provided keyword '{keyword}' has length of {keyword.Length} characters.",
                nameof(keyword));
        }

        for (int i = 0; i < keyword.Length; i++)
        {
            char c = keyword[i];
            bool isValid = (c >= 32 && c <= 126) || (c >= 161 && c <= 255);
            if (!isValid)
            {
                throw new ArgumentException(
                    "The keyword can only contain printable Latin 1 characters and spaces in the ranges 32 - 126 or 161 -255. " +
                    $"The provided keyword '{keyword}' contained an invalid character ({c}) at index {i}.",
                    nameof(keyword));
            }


        }

        byte[] bytes = Encoding.UTF8.GetBytes(text);
        for (int i = 0; i < bytes.Length; i++)
        {
            byte b = bytes[i];
            if (b == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(text),
                    "The provided text contained a null (0) byte when converted to UTF-8. Null bytes are not permitted. " +
                    $"Text was: '{text}'");
            }
        }

        // All checks passed, store the keyword and text data (as UTF-8 bytes) for later writing to the PNG file.
        this.storedStrings.Add((keyword, bytes));
    }
}
