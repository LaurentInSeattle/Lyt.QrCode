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

- Encode Static Method 

- Encode Result class 
 
# Decoding API  

- DecodeParameters class

- QrPixelPoint class

A simple class holding pixel coordinates on an image, with the origin located at the top left corner.

- DetectorCallback delegate 

A Callback delegate which is invoked when a possible significant point in the QR code image, such as a corner, is found.

```csharp
public delegate void DetectorCallback(QrPixelPoint point);
```

- Decode Static Method 

- Encode Result 
 
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

TODO: Document how to 

# QR Code Modules

TODO: Document how to access the module data 

# Adding your own Content Classes

TODO: Document how to 

# Debugging and Troubleshooting 

TODO: Document how the Result classes.

# Contributing

Contributions are always welcome! Please feel free to submit a Pull Request. 

# License and Credits 

Zebra Crossing - Apache 2.0