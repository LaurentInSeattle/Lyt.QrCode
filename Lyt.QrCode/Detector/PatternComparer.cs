namespace Lyt.QrCode.Detector;

/// <summary> Orders Patterns by EstimatedModuleSize </summary>
internal sealed class PatternComparer : IComparer<Pattern>
{
    public int Compare(Pattern? center1, Pattern? center2)
    {
        if (center1 == null && center2 == null)
        {
            return 0;
        }

        if (center1 == null)
        {
            return -1;
        }

        if (center2 == null)
        {
            return 1;
        }

        if (center1.EstimatedModuleSize == center2.EstimatedModuleSize)
        {
            return 0;
        }

        if (center1.EstimatedModuleSize < center2.EstimatedModuleSize)
        {
            return -1;
        }

        return 1;
    }
}
