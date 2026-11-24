using Rovunet.Core;

namespace Rovunet.Desktop;

/// <summary>
/// The Android client marks every path card with a four-step speed stripe. Windows had
/// no such marker, so the list looked flatter. The tiers follow the same order and the
/// same colours: fastest tunnels green, then olive, amber and the slowest orange.
/// </summary>
internal static class SpeedTier27
{
    public static string Color(Rovunet.Core.Profile profile)
    {
        string kind;
        try { kind = ChainProfiles.Kind(profile); } catch { return "#E17A30"; }
        return kind switch
        {
            "xray" or "naive" => "#31A354",                 // VLESS/VMess/Trojan/Reality/XHTTP, NaiveProxy
            "ssh" or "socks5" => "#87A532",                 // SSH and SOCKS5
            "snowflake" or "doh" => "#D1A309",              // Tor, DoH
            "dnstt" or "sayedns" or "vaydns" or "soar" => "#D1A309",
            "ss" or "slipstream" => "#E17A30",
            _ => "#E17A30"
        };
    }
}
