# Lyt.QrCode

QrCode Library, Encode and Decode, zero dependencies, all written in C#.

Nuget: https://www.nuget.org/packages/Lyt.QrCode  

# Quick Start 

```csharp

using Lyt.QrCode.API;
using Lyt.QrCode.Content;

// Encoding 

    private const string link = "https://github.com/LaurentInSeattle/Lyt.QrCode";

    // Encode provided link as a PNG image using default parameters 
    var encodeImage = Qr.Encode<string, byte[]>(link);
    if (encodeImage.Success)
    {
        string fullPath = < .... >
        File.WriteAllBytes(fullPath, encodeImage.Result);
    }
```

See the Lyt.QrCode.**Encode**.Demo console application for more code examples. 

```csharp
// Decoding 

        var sourceImage = this.LoadSourceImage(filename + ".png");
        var result = Qr.Decode(sourceImage, OnDetect);
        if (result.Success)
        {
            Console.WriteLine("Decoded, Content:  " + result.Text);
            if (result.IsParsed)
            {
                Console.WriteLine("Parsed, Type:  " + result.ParsedType.FullName);
                if (result.TryGet(out QrVCard? qrCard))
                {
                    Console.WriteLine("Parsed as a 'VCard'");
                    Console.WriteLine($"{qrCard.FirstName} {qrCard.LastName}");
                }
            }
        }
```

See the Lyt.QrCode.**Decode**.Demo console application for more code examples. 

# Encoding API  

- EncodeParameters class

Holds the following properties to configure encoding: 
```csharp
    /// <summary> The minimum desired Error Correction Level, defaults to Medium.</summary>
    /// <remarks> Will be automatically increased as long as the data fits.</remarks>
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; init; } = QrErrorCorrectionLevel.Medium;

    /// <summary> The width and height, in pixels, of each module (QR code pixel), defaults to 16. </summary>
    public int Scale { get; init; } = 16;

    /// <summary> The border width, as a factor of the module size, defaults to 2. </summary>
    /// <remarks> Expressed in count of modules, actual border size in pixels will be: Border * Scale</remarks>
    public int Border { get; init; } = 2;

    /// <summary> The foreground color (dark modules), in RGB value, defaults to Black. </summary>
    public int Foreground { get; init; } = 0;

    /// <summary> The background color (light modules), in RGB value, defaults to White. </summary>
    public int Background { get; init; } = 0xFFFFFF;

    /// <summary> The Image Format, when the encoding output is byte[], defaults to PNG. </summary>
    public QrImageFormat ImageFormat { get; init; } = QrImageFormat.Png;

    /// <summary> The Vector Format, when the encoding output is string, defaults to SVG. </summary>
    public QrVectorFormat VectorFormat { get; init; } = QrVectorFormat.Svg;
```

- Encode Static Method 

Creates a QR Code: 

Ommiting or passing null for the parameters argument will actually use default parameters, listed above.

```csharp
    /// <summary> Creates a QR Code in TResult format type from provided content as TContent type. </summary>
    /// <typeparam name="TContent">string, byte array or any QrContent derived class.</typeparam>
    /// <typeparam name="TResult">byte array for immages, string for vectors, or bool[,] for modules data.</typeparam>
    /// <param name="content">The data to encoded.</param>
    /// <param name="encodeParameters">The encoding parameters, mostly used for the final steps of rendering.</param>
    /// <returns>An Encode Result instance.</returns>
    public static EncodeResult<TResult> Encode<TContent, TResult>(
        TContent content,
        EncodeParameters? encodeParameters = null)
        where TContent : class
        where TResult : class
    {
        ....
    }
```

- Encode Result class 

Holds the results of the encoding process.

```csharp
    /// <summary> True if encoding was succesful, otherwise false. </summary>
    public bool Success => ....

    /// <summary> The result object of type TResult of the encoding process. Not null if Success if true. </summary>
    public TResult? Result { get; set; }

    /// <summary> The version (size) of this QR code (between 1 for the smallest and 40 for the biggest). </summary>
    /// <remarks> Valid if and only if Success is true </remarks>
    public int QrCodeVersion { get; internal set; } = -1;

    /// <summary> The width and height of this QR code, in modules (pixels). The size is a value between 21 and 177.  </summary>
    /// <remarks> Valid if and only if Success is true </remarks>
    public int QrCodeDimension { get; internal set; } = -1;

```
 
- Message Log class 

The EncodeResult class inherits from MessageLog which contains a list of informational or error messages, and 
exception traces if any exception was thrown during the encoding process. 
A new list instance is created for each Encode invocation.

```csharp
    public List<string> Messages { get; set; } = [];
```

# Decoding API  

- The PngLoader class

Not implemented yet. 

Will **soon** load PNG images without any external dependencies. For now use SourceImage, documented below.


- The Source Image class

The Lyt.QrCode library runs without UI framework or imaging library dependencies, and therefore needs an internal represention for images to be decoded.
This is 'SourceImage' in the 'Lyt.QrCode.Image' namespace. Create a SourceImage by providing the canonical parameters of an 'in memory bitmap'.
 
```csharp

using Lyt.QrCode.Image;

public enum PixelFormat
{
    Gray8,
    Gray16,

    RGB24,
    BGR24,

    ARGB32,
    ABGR32,
    BGRA32,
    RGBA32,

    /// <summary> 2 bytes per pixel, 5 bit red, 6 bits green and 5 bits blue </summary>
    RGB565,

    /// <summary> 4 bytes for two pixels, UYVY formatted </summary>
    UYVY,

    /// <summary> 4 bytes for two pixels, YUYV formatted </summary>
    YUYV
}

    /// <summary> Creates a SourceImage instance from the provided information</summary>
    public SourceImage(int width, int height, int stride, PixelFormat format, byte[] pixels, bool isLocked = true)
    {
    } 

```
Sample code to create a SourceImage object using the ImageSharp library from SixLabors: 

```csharp

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

    public  SourceImage LoadSourceImage(string imagePath)
    {
        string fullPath = Path.Combine(rootPath, imagePath);
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(fullPath);
        byte[] pixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixels);
        return 
            new SourceImage(
                image.Width, image.Height, image.Width * 4, PixelFormat.RGBA32, pixels);
    }
```

Sample code to create a SourceImage object using Skia and Avalonia: 

```csharp

            ==> Unfinished !! <== 

            byte[] imageBytes = File.ReadAllBytes(path);
            if ((imageBytes is null) || (imageBytes.Length < 256))
            {
                throw new Exception("Failed to read image from disk: " + path);
            }

            var bitmap = WriteableBitmap.Decode(new MemoryStream(imageBytes));
            if (bitmap is not null)
            {
                using ( var frameBuffer = bitmap.Lock() ) 
                {
                     
                    int height = frameBuffer.Size.Height; 
                    int width = frameBuffer.Size.Width; 
                    int stride = frameBuffer.RowBytes; 

                    // NOT always 4 => Pixel Size 
                    PixelFormat pixelFormat = PixelFormat.????
                    int pixelSize = 4 ; 
                    byte[] pixels = new byte[height * stride * pixelSize];
                    bitmap.CopyPixels( frameBuffer.Address, ... TODO ... ) ; 
                    return new SourceImage (width, height, stride, pixelFormat, pixels) ;
                }

                throw new Exception("Failed to initialize puzzle with image: " + path);
            }
```

- DecodeParameters class

Contains a single boolean property allowing to skip parsing text content, trying to parse any canonical content.

```csharp
    /// <summary> True when it is not necessary to parse the Text result of the QR code.</summary>
    public bool SkipParsing { get; set; } = false;

```

- QrPixelPoint class

An immutable class holding a 2D point integer pixel coordinates X and Y on an image, with the origin located at the top left corner.

A QrPixelPoint may be invalid, for example if not correctly detected: client code should check the IsValid property. 

```csharp
    public int X { get; } 

    public int Y { get; } 

    public bool IsValid => ... ; 
```

- DetectorCallback delegate 

A Callback delegate which is invoked when a possible significant point in the QR code image, such as a corner, is found.
Designed to provide a hint on when taking a picture, this delegate is invoked on the current running thread, 
which is possibly NOT the UI thread. 

```csharp
public delegate void DetectorCallback(QrPixelPoint point);
```

The delegate is invoked if only if the detected QR Pixel Point is valid.

- Decode Static Method 

Ommiting or passing null for the delegate argument is the default option.
Ommiting or passing null for the parameters argument will actually use default parameters, listed above.

```csharp

    /// <summary> Tries to decode the provided SourceImage using the optional DetectorCallback and the optional DecodeParameters. </summary>
    /// <returns> A DecodeResult instance </returns>
    public static DecodeResult Decode(
        SourceImage sourceImage,
        DetectorCallback? detectorCallback = null,
        DecodeParameters? decodeParameters = null)
    {
        ....
    } 

```

- Decode Result 

Immutable class holding the results of the decoding process: 

```csharp
    /// <summary> True if decoding was succesful, otherwise false. </summary>
    public bool Success => ....

    /// <summary> Text encoded by the QR Code, if applicable, otherwise empty<summary> 
    public string Text { get; internal set; } = string.Empty;

    /// <summary> Bytes encoded by the QR Code, if applicable, otherwise null ><summary> 
    public byte[]? Bytes { get; internal set; } = null;

    /// <summary> True when a canonical object has been successfully parsed, otherwise false. ><summary> 
    public bool IsParsed => .... 

    /// <summary></summary> The type of the canonical object,if successfully parsed, otherwise null.<summary> 
    public Type? ParsedType { get; internal set; } = null;

    /// <summary> The actual canonical object, if successfully parsed, otherwise null.<summary> 
    public object? ParsedObject { get; internal set; } = null;

    /// <summary> True when a QR code has been successfully detected but possibly not aligned, otherwise false. ><summary> 
    public bool IsDetected => ....

    /// <summary> True when a QR code has been successfully detected AND aligned, otherwise false. ><summary> 
    public bool IsAligned =>  ....

    /// <summary> The finder pattern Top Left pixel point. Valid only if properly detected  </summary>
    public QrPixelPoint TopLeft { get; internal set; } 

    /// <summary> The finder pattern Top Right pixel point. Valid only if properly detected  </summary>
    public QrPixelPoint TopRight { get; internal set; } 

    /// <summary> The finder pattern Bottom Left pixel point. Valid only if properly detected  </summary>
    public QrPixelPoint BottomLeft { get; internal set; } 

    /// <summary> The best alignment pattern pixel point. Valid only if properly detected  </summary>
    public QrPixelPoint Alignment { get; internal set; } 
```

- Message Log class 

The DecodeResult class also inherits from MessageLog which contains a list of informational or error messages, and 
exception traces if any was thrown during the decoding process. 
A new list instance is created for each Decode invocation.

```csharp
    public List<string> Messages { get; set; } = [];
```
 
# Supported Content 

All these QR Content classes support **both** encoding and parsing. 

- **Bookmark** : Uses the 'MEBKM' protocol. See: https://github.com/LaurentInSeattle/Lyt.QrCode/blob/main/Lyt.QrCode/Content/QrBookmark.cs 

- **Calendar Event** : Uses the iCal protocol. See: https://github.com/LaurentInSeattle/Lyt.QrCode/blob/main/Lyt.QrCode/Content/QrCalendarEvent.cs

- **GeoLocation** : Uses the 'geo:' uri scheme. See: https://github.com/LaurentInSeattle/Lyt.QrCode/blob/main/Lyt.QrCode/Content/QrGeoLocation.cs

- **Email**: Supports either 'mailto:' , 'SMTP:' or 'MatMsg' protocols. See: https://github.com/LaurentInSeattle/Lyt.QrCode/blob/main/Lyt.QrCode/Content/QrMail.cs

- **V Card**: Uses the VCard 4.0 protocol. See: https://github.com/LaurentInSeattle/Lyt.QrCode/blob/main/Lyt.QrCode/Content/QrVCard.cs

- **Me Card**: The more compact Japanese version of VCard. See: https://github.com/LaurentInSeattle/Lyt.QrCode/blob/main/Lyt.QrCode/Content/QrMeCard.cs

- **Phone Number**: Uses the 'tel:' uri scheme. See: https://github.com/LaurentInSeattle/Lyt.QrCode/blob/main/Lyt.QrCode/Content/QrPhoneNumber.cs

- **Text Message**: Supports either 'sms:' , 'mms:' or 'mmsto:' protocols plus WhatsApp links. See: https://github.com/LaurentInSeattle/Lyt.QrCode/blob/main/Lyt.QrCode/Content/QrTextMessage.cs

- **Wifi**: Supports all variants of network authentication. See: https://github.com/LaurentInSeattle/Lyt.QrCode/blob/main/Lyt.QrCode/Content/QrWifi.cs

# Asynchrony 

Both Encode and Decode API's have an asynchronous version: EncodeAsync and DecodeAsync,
taking the same arguments and returning the same data.

```csharp

    public static async Task<EncodeResult<TResult>> EncodeAsync<TContent, TResult>(
        TContent content,
        EncodeParameters? encodeParameters = null)
        where TContent : class
        where TResult : class
        {
            ....
        }

    public static async Task<DecodeResult> DecodeAsync(
        SourceImage sourceImage,
        DetectorCallback? detectorCallback = null,
        DecodeParameters? decodeParameters = null)
        {
            ....
        }

```
 
# Convenience Encoding API  

Embedding the result type in the Encoding method name allows simplifications on the client code side because 
the content type can now be inferred by the C# compiler. 
Therefore it becomes possible to write: 

```csharp
using Lyt.QrCode.API;
using Lyt.QrCode.Content;

// Encoding 

    private const string link = "https://github.com/LaurentInSeattle/Lyt.QrCode";

    // Encode provided link as a PNG image using default parameters 
    var encodeImage = await Qr.EncodeToImageAsync(link);
    if (encodeImage.Success)
    {
        string fullPath = < .... >
        File.WriteAllBytes(fullPath, encodeImage.Result);
    }

```

Provided convenience methods: 
- EncodeToImage
- EncodeToImageAsync
- EncodeToVectors
- EncodeToVectorsAsync
- EncodeToModules
- EncodeToModulesAsync

# Access to the QR Code Modules

To access the module data you need to create the QR Code using the EncodeToModules variant or use the generic Encode witha bool[,] 
result parameter.
Then, if the encoding process is successful, the EncodeResult contains a 2D array of boolean values, one for each module, 
indexed from top left to bottom right. 

```csharp

        // content may be of type string, byte[] or any QrContent class
        var encodeModules = Qr.EncodeToModules(content);
        // or: var encodeModules = Qr.Encode<T, bool[,]>(content);

        if (encodeModules.Success)
        {
            // Access the modules as 2D array of booleans 
            bool[,] modules = encodeModules.Result;
            if (encodeModules.QrCodeDimension != modules.GetLength(0))
            {
                throw new Exception("Encoding problem with dimensions"); 
            }

            // Module at top left corner should be black since it is part of a 'finder' pattern
            bool corner = modules[0,0];
            if (!corner)
            {
                throw new Exception("Encoding problem with module value");
            }
        }

```

# Adding your own QR Content Classes

Any client code class, say of type 'TContent',  can become a data source for creating QrCode. 

You simply need to: 

- Make your class 'TContent' derive from QrContent<TContent>
- Declare your class 'TContent' as compliant to the IQrParsable<TContent>
- Override the QrString property get method that will provide the source data for the Qr Code.
- Implement the TryParse method that decodes a QrCode.
- In order to register and later execute your custom parser, you need to invoke: 

See the QrBookmark class in the source code repository.  

```csharp

    public interface IQrParsable<TSelf> where TSelf : IQrParsable<TSelf>
    {
        static abstract bool TryParse(string source, [NotNullWhen(true)] out TSelf? tself); 
    }

    // Example: Internal declaration of QrBookmark
    public class QrBookmark : QrContent<QrBookmark>, IQrParsable<QrBookmark>
    
    // Example: Internal implementation for QrBookmark
    public override string QrString => $"MEBKM:TITLE:{this.Title};URL:{this.Url};;";

    // Example: Internal implementation for QrBookmark
    public static bool TryParse(string source, [NotNullWhen(true)] out QrBookmark? qrBookmark)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source string cannot be null, empty or white space", nameof(source));
        }

        // Do more parsing stuff and return a typed object 

        ....

        return new QrBookmark ( .... ) ; 
    } 

    // Example: Registration of custom type TContent
     if ( ! Qr.TryAddCustomQrContentType(typeof(TContent)) 
     { 
        Console.WriteLine("Invalid QrCode Parser");
     } 

```

# Debugging and Troubleshooting 

Both EncodeResult and DecodeResult classes inherit from MessageLog which contains a list of informational or error messages, and 
exception traces, if any was thrown during the encoding or decoding process. 
A new list instance is created for each Encode or Decode invocation.

```csharp
    public List<string> Messages { get; set; } = [];
```
