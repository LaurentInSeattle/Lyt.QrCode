# Lyt.QrCode

QrCode Library, Encode and Decode, zero dependencies, all written in C#.

Nuget: https://www.nuget.org/packages/Lyt.QrCode  

# Features

- Supports **Both** Encode and Decode.

- **Zero dependencies** for both encoding and decoding. Use **everywhere**: Absolutely no OS dependencies, no UI framework dependencies, just C#!

- **Many output formats**: PNG, BMP, SVG, XAML, aXAML, Raw data... With no dependeincies, and with more to come...

- Built-in support for **both** encoding and decoding of **canonical content** such as: Links, GeoLocation, Wifi, VCard, Email, and more...

- **Nuget** ow Available on Nuget.Org.

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

using Lyt.QrCode;

// Encoding 

    private const string link = "https://github.com/LaurentInSeattle/Lyt.QrCode";

    // Encode provided link as a PNG image using default parameters 
    var encodeImage = Qr.Encode<string, byte[]>(link);
    if (encodeImage.Success)
    {
        string fullPath = < .... >
        File.WriteAllBytes(fullPath, encodeImage.Result);
    }

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
