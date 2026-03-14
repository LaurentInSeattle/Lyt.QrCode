namespace Lyt.QrCode.Content;

internal class Wifi
{
}

internal class WifiContent
{
}

/*
A WiFi QR code contains
specifically formatted text, usually following the MECARD or WIFI:S format, encoding the network name (SSID), password, and encryption type. This allows smartphones to scan the code and instantly connect without typing. Key details include SSID, Password, and Type (WPA/WPA2/WEP). 
Standard Wi-Fi QR Code Format:
The content inside the QR code follows this syntax, usually structured in a single string:
WIFI:S:<SSID>;T:<WPA|WEP|>;P:<PASSWORD>;H:<true|false|>;; 

    S: SSID (The name of your WiFi network).
    T: Encryption Type (e.g., WPA/WPA2, WEP, or blank for no password).
    P: Password (The network's access key).
    H: Hidden (Optional - true if the network is hidden, otherwise false or omitted). 

Key Points About WiFi QR Content:

    Case Sensitivity: The SSID and Password must be entered exactly as they appear on the router.
    Security Types: The most common secure format is WPA/WPA2.
    Encoding: Generators transform these text details into a scannable, high-density matrix format.
    Built-in Tools: Both iOS and Android allow you to generate this QR code format automatically in their settings. 

Example Content:
WIFI:S:MyHomeNetwork;T:WPA;P:Password123!;;
Using this format ensures that when a guest scans the code, their device directly parses the network name and password, reducing manual entry errors*/