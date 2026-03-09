namespace Lyt.QrCode.Utilities;

/// <summary> 
/// Superclass of classes encapsulating types ECIs, according to "Extended Channel Interpretations" 5.3 of ISO 18004.
/// </summary>
public abstract class ECI
{
    /// <summary> the ECI value </summary>
    public virtual int Value { get; private set; }

    internal ECI(int val) => this.Value = val;

    /// Returns an <see cref="ECI"/> representing ECI of given value, or null if it is legal but unsupported.
    /// <param name="value">ECI value</param>
    /// <throws>ArgumentException if ECI value is invalid </throws>
    public static ECI? GetECIByValue(int value)
    {
        if (value < 0 || value > 999999)
        {
            throw new ArgumentException("Bad ECI value: " + value, nameof(value));
        }
        if (value < 900)
        {
            // Character set ECIs use 000000 - 000899
            return CharacterSetECI.GetCharacterSetECIByValue(value);
        }

        return null;
    }
}