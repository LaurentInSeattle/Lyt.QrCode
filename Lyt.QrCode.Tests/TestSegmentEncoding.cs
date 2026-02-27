namespace Lyt.QrCode.Tests;

using Lyt.QrCode.Render;

using System.Text;

[TestClass]
public sealed class TestSegmentEncoding
{
    private const string TextNumeric = "83930";

    private const int BitLengthNumeric = 17;

    private static readonly byte[] BitsNumeric = { 139, 243, 0 };

    private const string TextAlphanumeric = "$%*+-./ 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private const int BitLengthAlphanumeric = 242;

    private static readonly byte[] BitsAlphanumeric = {
            43, 63,240, 245, 223, 12, 64, 232,
            162, 147, 168, 116,228, 172,  40, 21,
            170, 67, 243, 58, 211, 175, 81, 76,
            109, 33, 107, 218, 193, 225, 2
        };

    private const string TextUtf8 = "😐ö€";

    private const int BitLengthUtf8 = 72;

    private static readonly byte[] BitsUtf8 = { 15, 249, 25, 9, 195, 109, 71, 65, 53 };

    [TestMethod]
    public void Test_UTF8()
    {
        //var segments = QrSegment.MakeSegments(TextUtf8);
        //Assert.AreEqual(segments.Count, 1);
        //var segment = segments[0];
        //Assert.AreEqual(segment.EncodingMode, Mode.Byte);
        //Assert.AreEqual(Encoding.UTF8.GetBytes(TextUtf8).Length, segment.NumChars);

        //var data = segment.GetData();
        //Assert.AreEqual(BitLengthUtf8, data.Length);


        //Assert.AreEqual(BitsUtf8, BitArrayToByteArray(data));
        //Assert.AreEqual(TextUtf8, segment.GetText());

        string text = "https://github.com/LaurentInSeattle/Lyt.Jigsaw"; 

        var qrCode = QrCode.EncodeText(text, Ecc.Quartile);
        // DrawQrCode(qrCode);
        byte[] image = PngBuilder.ToImage(qrCode, 16, 2); 
        File.WriteAllBytes("C:\\Users\\Laurent\\Desktop\\test.png", image);
    }


    public static void DrawQrCode (QrCode qrCode)
    {
        for (int y = 0; y < qrCode.Size; y++)
        {
            for (int x = 0; x < qrCode.Size; x++)
            {
                Debug.Write(qrCode.GetModule(y, x) ? "██" : "  ");
            }

            Debug.WriteLine("");
        }
    }
}
