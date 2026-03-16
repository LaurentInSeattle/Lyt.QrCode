namespace Lyt.QrCode.Content;

using static Lyt.QrCode.Content.GeoLocation;

#region Documentation 

// https://www.bing.com/maps/search?style=r&cp=37.654833%7E-121.895432&lvl=18.1

// https://www.google.com/maps/@37.6557025,-121.8895928,15.46z?entry=ttu&g_ep=EgoyMDI2MDMxMS4wIKXMDSoASAFQAw%3D%3D

#endregion Documentation 

public class GeoLocation
{
    /// <summary> The preferred protocol for encoding the location. </summary>
    public enum GeoProtocol
    {
        /// <summary> The regular "geo:" URI scheme. Default </summary>
        Geo,

        GoogleMapsLink,
        BingMapsLink,
    }

    public GeoLocation(double latitude, double longitude, GeoProtocol geoProtocol = GeoProtocol.Geo)
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
}

public sealed class GeoLocationContent(GeoLocation location) : QrContent<GeoLocation>(location)
{
    public override string RawString
    {
        get
        {
            var geo = this.Content;
            string latString = geo.Latitude.ToString("F6");
            string longString = geo.Longitude.ToString("F6");
            return this.Content.Protocol switch
            {
                GeoProtocol.Geo => $"geo:{latString},{longString}",
                GeoProtocol.GoogleMapsLink => $"https://www.google.com/maps/@{latString},{longString}",
                GeoProtocol.BingMapsLink => $"https://www.bing.com/maps/search?style=r&cp={latString}%7E{longString}",
                _ => throw new NotImplementedException("Unsupported geo protocol"),
            };
        }
    }
}
