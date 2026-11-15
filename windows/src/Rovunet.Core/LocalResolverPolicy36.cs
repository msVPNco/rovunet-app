using System.Net;
using System.Net.Sockets;
namespace Rovunet.Core;
public static class LocalResolverPolicy36
{
    // Preserve the existing validity checks and discovery order. This filter
    // only affects automatic Local discovery; manual IPv6 settings are retained.
    public static string[] SelectIpv4(IEnumerable<IPAddress> addresses)=>addresses
        .Where(a=>a.AddressFamily==AddressFamily.InterNetwork&&!IPAddress.IsLoopback(a)&&!a.Equals(IPAddress.Any))
        .Distinct().Take(16).Select(a=>new IPEndPoint(a,53).ToString()).ToArray();
}
