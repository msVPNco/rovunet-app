using System.Windows.Media;
using Rovunet.Core;

namespace Rovunet.Desktop;
public sealed partial class MainWindow
{
    Brush ConnectionInk28(string connection)=>connection switch{"connected"=>Green,"error" or "disconnected"=>Red,_=>Accent};
    Brush ProbeInk28(AgentReply result,bool fresh=true)
    {
        if(result.Verified)return fresh?Green:Muted;
        if(HttpsProbePolicy.Unconfirmed(result.Error))return Amber;
        return MessageInk28(result.Error);
    }
    Brush MessageInk28(string code)=>code switch{
        "" or "CANCELLED" or "CANCELLING" or "GROUP_WAITING" or "GROUP_WAIT_DIRECT" or "GROUP_WAIT_DNS" or "GROUP_TESTING" or "GROUP_DIRECT" or "GROUP_DNS" or "GROUP_DNS_SKIPPED" or "GROUP_NOT_TESTED" or "RESULT_STALE" or "HANDSHAKE_ACTIVE_CONFLICT"=>Muted,
        "SSH_TRUST_REQUIRED" or "SLIP_CERT_REQUIRED" or "DNS_POOL_FALLBACK" or "GLOBAL_RESOLVER_FALLBACK" or "HANDSHAKE_UNCONFIRMED"=>Amber,
        _=>Red
    };
}
