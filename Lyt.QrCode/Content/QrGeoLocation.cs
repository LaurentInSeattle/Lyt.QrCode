namespace Lyt.QrCode.Content;

using static Lyt.QrCode.Content.QrGeoLocation.GeoProtocol;

#region Documentation 

// https://www.bing.com/maps/search?style=r&cp=37.654833%7E-121.895432&lvl=18.1

// https://www.google.com/maps/@37.6557025,-121.8895928,15.46z?entry=ttu&g_ep=EgoyMDI2MDMxMS4wIKXMDSoASAFQAw%3D%3D

#endregion Documentation 

public class QrGeoLocation : QrContent<QrGeoLocation>
{
    /// <summary> The preferred protocol for encoding the location. </summary>
    public enum GeoProtocol
    {
        /// <summary> The regular "geo:" URI scheme. Default </summary>
        Geo,

        /// <summary> Convenience URL builders for Google and Bing. More later.</summary>
        GoogleMapsLink,
        BingMapsLink,
    }

    public QrGeoLocation(
        double latitude, double longitude, GeoProtocol geoProtocol = Geo)
        : base(isBinaryData: false)
    {
        if ((double.IsNaN(latitude)) ||
            (!double.IsFinite(latitude)) ||
            (latitude < -90.0) ||
            (latitude > 90.0))
        {
            throw new ArgumentException("Latitude is NaN, infinite or out of range", nameof(latitude));
        }

        if ((double.IsNaN(longitude)) ||
            (!double.IsFinite(longitude)) ||
            (longitude < -180.0) ||
            (longitude > 180.0))
        {
            throw new ArgumentException("Longitude is NaN, infinite or out of range", nameof(longitude));
        }

        this.Latitude = latitude;
        this.Longitude = longitude;
        this.Protocol = geoProtocol;
    }

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public GeoProtocol Protocol { get; private set; }

    public override string RawString
    {
        get
        {
            string latString = this.Latitude.ToString("F6");
            string longString = this.Longitude.ToString("F6");
            return this.Protocol switch
            {
                Geo => $"geo:{latString},{longString}",
                GoogleMapsLink => $"https://www.google.com/maps/@{latString},{longString}",
                BingMapsLink => $"https://www.bing.com/maps/search?style=r&cp={latString}%7E{longString}",
                _ => throw new NotImplementedException("Unsupported geo protocol"),
            };
        }
    }

    public static bool TryParse(string source, [NotNullWhen(true)] out QrGeoLocation? qrGeoLocation)
    {
        const string key = "geo:";
        qrGeoLocation = null;
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source string cannot be null, empty or white space", nameof(source));
        }

        try
        {
            if (!source.StartsWith(key))
            {
                return false;
            }

            source = source[key.Length..];
            string[] tokens = source.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length != 2)
            {
                return false;
            }

            if (!double.TryParse(tokens[0], out double latitude))
            {
                return false;

            }

            if (!double.TryParse(tokens[1], out double longitude))
            {
                return false;
            }

            qrGeoLocation = new QrGeoLocation(latitude, longitude);
            return true;
        }
        catch
        {
            // Swallow everything else 
        }

        return false;
    }
}
