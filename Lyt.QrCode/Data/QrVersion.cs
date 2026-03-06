namespace Lyt.QrCode.Data;

public sealed class QrVersion
{
    public QrVersion(int dimension)
    {
        int versionNumber = (dimension - 17) >> 2;
        this.VersionNumber = versionNumber;
        this.DimensionForVersion = 17 + 4 * versionNumber;
    }

    public int VersionNumber { get; }

    public int DimensionForVersion { get; }

    public static bool IsValidDimension(int dimension)
    {
        if (dimension % 4 != 1)
        {
            return false;
        }

        int versionNumber = (dimension - 17) >> 2;
        return IsValidVersionNumber(versionNumber);
    }

    public static bool IsValidVersionNumber(int versionNumber)
        => versionNumber >= 1 && versionNumber <= 40 ; 

    // LATER 
    // 
    ///// <summary> Returns a QrVersion object from a version number. </summary>
    ///// <param name="versionNumber">The version number.</param>
    //public static QrVersion GetVersionForNumber(int versionNumber)
    //{
    //    if (versionNumber < 1 || versionNumber > 40)
    //    {
    //        throw new ArgumentException(nameof(versionNumber));
    //    }

    //    return VERSIONS[versionNumber - 1];
    //}
}
