namespace Lyt.Png.Internals; 

internal class Palette
{
    public bool HasAlphaValues { get; private set; }

    public byte[] Data { get; }

    /// <summary> Creates a palette object. Input palette data length from PLTE chunk must be a multiple of 3. </summary>
    public Palette(byte[] data)
    {
        this.Data = new byte[data.Length * 4 / 3];
        int dataIndex = 0;
        for (int i = 0; i < data.Length; i += 3)
        {
            this.Data[dataIndex++] = data[i];
            this.Data[dataIndex++] = data[i + 1];
            this.Data[dataIndex++] = data[i + 2];
            this.Data[dataIndex++] = 255;
        }
    }

    /// <summary> Adds transparency values from tRNS chunk. </summary>
    public void SetAlphaValues(byte[] bytes)
    {
        this.HasAlphaValues = true;

        for (int i = 0; i < bytes.Length; i++)
        {
            this.Data[i * 4 + 3] = bytes[i];
        }
    }

    public Pixel GetPixel(int index)
    {
        int start = index * 4;
        return new Pixel(this.Data[start], this.Data[start + 1], this.Data[start + 2], this.Data[start + 3], false);
    }
}