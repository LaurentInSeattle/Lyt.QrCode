namespace Lyt.QrCode.Content.Internal;

internal enum EncoderOutput
{
    Unsupported = 0,

    Image, // byte[]

    Vectors, // string 

    Modules, // bool[,]

    QrCode, // QrCode
}
