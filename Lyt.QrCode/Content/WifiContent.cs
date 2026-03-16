namespace Lyt.QrCode.Content;

using static Lyt.QrCode.Content.Wifi.AuthenticationMode;

#region Documentation 

//      A WiFi QR code contains specifically formatted text, usually following the MECARD (aka WIfI:T) or WIFI:S format, encoding 
//      the network name (SSID), password, and encryption type. This allows smartphones to scan the code and instantly 
//      connect without typing. Key details include SSID, Password, and Type (WPA/WPA2/WEP). 
//      Standard Wi-Fi QR Code Format:
//      The content inside the QR code follows this syntax, usually structured in a single string:
//      WIFI:S:<SSID>;T:<WPA|WEP|>;P:<PASSWORD>;H:<true|false|>;; 
//
//          S: SSID (The name of your WiFi network).
//          T: Encryption Type (e.g., WPA/WPA2, WEP, or blank for no password).
//          P: Password (The network's access key).
//          H: Hidden (Optional - true if the network is hidden, otherwise false or omitted). 
//
//      Key Points About WiFi QR Content:
//
//          Case Sensitivity: The SSID and Password must be entered exactly as they appear on the router.
//          Security Types: The most common secure format is WPA/WPA2.
//          Encoding: Generators transform these text details into a scannable, high-density matrix format.
//          Built-in Tools: Both iOS and Android allow you to generate this QR code format automatically in their settings. 
//
//      Example Content:
//          WIFI:S:MyHomeNetwork;T:WPA;P:Password123!;;

#endregion Documentation 

public class Wifi
{
    /// <summary> The authentication mode for the WiFi network. </summary>
    public enum AuthenticationMode
    {
        /// <summary> No password authentication mode </summary>
        None,

        /// <summary> WEP authentication mode </summary>
        WEP,

        /// <summary> WPA authentication mode </summary>
        WPA,

        /// <summary> WPA2 authentication mode </summary>
        WPA2
    }

    public Wifi(
        string ssid,
        string password = "",
        AuthenticationMode authenticationMode = WPA2,
        bool isHiddenNetwork = false,
        bool encodeUsingWifi_S = true)
    {
        if (string.IsNullOrWhiteSpace(ssid))
        {
            throw new ArgumentException("SsID is required, cannot be null, empty or white space", nameof(ssid));
        }

        if (authenticationMode != None && string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required, cannot be null, empty or white space", nameof(password));
        }

        this.Password = password;
        this.Ssid = ssid;
        this.Authentication = authenticationMode;
        this.IsHiddenNetwork = isHiddenNetwork;
        this.EncodeUsingWifi_S = encodeUsingWifi_S;
    }

    public string Ssid { get; }

    public string Password { get; }

    public AuthenticationMode Authentication { get; }

    public bool IsHiddenNetwork { get; }

    public bool EncodeUsingWifi_S { get; }
}

internal sealed class WifiContent(Wifi wifi) : QrContent<Wifi>(wifi)
{
    public override string RawString
    {
        get
        {
            var wifi = this.Content;
            string auth = wifi.Authentication == None ? "nopass" : wifi.Authentication.ToString();
            string password = $"P:{(wifi.Authentication == None ? string.Empty : wifi.Password)}" ;
            string hidden = wifi.IsHiddenNetwork ? "H:true" : string.Empty;

            // Handle WIFI:S or WIFI:T Format 
            return
                wifi.EncodeUsingWifi_S ?
                    $"WIFI:S:{wifi.Ssid};T:{auth};{password};{hidden};" :
                    $"WIFI:T:{auth};S:{wifi.Ssid};{password};{hidden};" ;
        }
    }
}
