namespace Lyt.QrCode.Content;

internal enum EncoderOutput
{
    Unsupported = 0,

    Image, // byte[]

    Vectors, // string 

    Modules, // bool[,]

    QrCode, // QrCode
}
