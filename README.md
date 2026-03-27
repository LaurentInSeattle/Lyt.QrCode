# Lyt.QrCode

QrCode Library, Encode and Decode, zero dependencies, all written in C#.

Nuget: https://www.nuget.org/packages/Lyt.QrCode  

Complete Documentation: https://github.com/LaurentInSeattle/Lyt.QrCode/blob/main/Documentation.md

# Features

- Supports **Both** Encode and Decode.

- **Zero dependencies** for both encoding and decoding. Use **everywhere**: Absolutely no OS dependencies, no UI framework dependencies, just C#!

- **Many output formats**: PNG, BMP, SVG, XAML, aXAML, Raw data... With no dependencies, and with more to come...

- Built-in support for **both** encoding and decoding of **canonical content** such as: Links, GeoLocation, Wifi, VCard, Email, and more...

- **Nuget** now Available on Nuget.Org.

- **Fast**: Real time encoding and decoding, with multithreaded async/await support.

- **Modern**: Coded with .Net 10, taking advantage of all latest performance improvements of the recent .Net releases.

- **Simple, configurable and streamlined** API. 
 
 ## Installation

Install "Lyt.QrCode" via NuGet Package Manager or use "Manage Nuget Packages" in Visual Studio

```bash
PS C:....> Install-Package Lyt.QrCode
```

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

Complete Documentation: https://github.com/LaurentInSeattle/Lyt.QrCode/blob/main/Documentation.md

# Contributing

Contributions are always welcome! Please feel free to submit a Pull Request. 

# License and Credits 

Zebra Crossing - Apache 2.0