namespace Lyt.Png;

public partial class PngImage
{
    private readonly int backgroundColorInt;
    private readonly Dictionary<int, int> colorCounts = [];
    private readonly List<(string keyword, byte[] data)> textualMetadata = [];

    internal (int Width, int Height, int Channels, byte[] Pixels) ToRgba32Bitmap()
    {
        byte[] pixels = new byte[this.Width * this.Height * 4];
        if (this.Header.IsRgba32)
        {
            Array.Copy(this.data, pixels, pixels.Length);
            return (this.Width, this.Height, 4, pixels);
        }

        throw new Exception("Not a RGBA 32 image ");
    }

    internal (int Width, int Height, int Channels, byte[] Pixels) ToRgb24Bitmap()
    {
        byte[] pixels = new byte[this.Width * this.Height * 3];
        if (this.Header.IsRgb24)
        {
            Array.Copy(this.data, pixels, pixels.Length);
            return (this.Width, this.Height, 3, pixels);
        }

        throw new Exception("Not a RGB 24 image ");
    }

    /*
        Text data encoded in PNG files typically exists as metadata within tEXt, zTXt, or iTXt chunks to 
        describe image properties, authorship, and usage rights. 
        Common examples include image titles, creator names, copyright notices, creation times, software used, 
        and comments. These text chunks use keywords (e.g., "Description," "Author") and are often stored 
        in ISO 8859-1 (Latin-1) or UTF-8. 

        Examples of Text Data Metadata in PNG Chunks

            Title: A short description of the image.
            Author: The name of the creator.
            Description: A detailed explanation of the image content.
            Copyright: Legal notice regarding ownership.
            Creation Time: The date and time the image was created.
            Software: The software used to create or edit the image (e.g., "Adobe Photoshop").
            Comment: General user-supplied notes.
            Disclaimer: Legal warnings or disclaimers.
            Source: The device or source used to produce the image.
            Warning: Warnings regarding the nature of the content.
            URL: A link to the author or source.
    
     */

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
        this.textualMetadata.Add((keyword, bytes));
    }

    /// <summary> Sets the RGB pixel value for the given column (x) and row (y). </summary>
    public void SetPixel(byte r, byte g, byte b, int x, int y) => this.SetPixel(new Pixel(r, g, b), x, y);

    /// <summary> Set the pixel value for the given column (x) and row (y). </summary>
    public void SetPixel(Pixel pixel, int x, int y)
    {
        if (!this.hasTooManyColorsForPalette)
        {
            int colorIntValue = Pixel.ToColorInt(pixel);
            if (colorIntValue != this.backgroundColorInt)
            {
                if (!this.colorCounts.TryGetValue(colorIntValue, out int value))
                {
                    this.colorCounts[colorIntValue] = 1;
                }
                else
                {
                    this.colorCounts[colorIntValue] = ++value;
                }

                this.colorCounts[backgroundColorInt]--;
                if (this.colorCounts[backgroundColorInt] == 0)
                {
                    this.colorCounts.Remove(backgroundColorInt);
                }
            }

            if (this.colorCounts.Count > 256)
            {
                this.hasTooManyColorsForPalette = true;
            }
        }

        int start = (y * ((this.width * this.bytesPerPixel) + 1)) + 1 + (x * this.bytesPerPixel);
        this.data[start++] = pixel.R;
        this.data[start++] = pixel.G;
        this.data[start++] = pixel.B;

        if (this.hasAlphaChannel)
        {
            this.data[start] = pixel.A;
        }
    }
}
