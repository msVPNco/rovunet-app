using System.Text.Json.Nodes;
namespace Rovunet.Core;

public sealed record HandshakePlan30(string Protocol,string Mechanism,string SessionIdentity,JsonObject Configuration)
{
    public override string ToString()=>"HandshakePlan(redacted)";
}
public sealed record QuickProbe30(bool Reachable,long ElapsedMs,string Error,string Mechanism)
{
    public override string ToString()=>"QuickProbe("+Mechanism+",redacted)";
}
public static class ProtocolHandshake30
{
    public const int GreenMaximumMs=1000;
    public static bool GreenLatency(long ms)=>ms>=0&&ms<=GreenMaximumMs;
    public static HandshakePlan30? Plan(Profile profile,int timeoutMs=4000)
    {
        if(timeoutMs is <100 or >10000)throw new SafeError("INVALID_MESSAGE");
        profile=Importer.Single(profile.Raw);
        if(ProfileEditing.IsNative(profile.Raw)&&Importer.Fields(profile.Raw)[1]!="xray")return null;
        var parsed=Importer.Connection(profile);var outbound=parsed.Outbound;string protocol=parsed.Protocol switch{"Hysteria2"=>"hysteria2","TUIC"=>"tuic","WireGuard"=>"wireguard","AmneziaWG"=>"amneziawg",_=>""};if(protocol.Length==0)return null;
        var settings=outbound["settings"]!;var config=new JsonObject{["version"]=1,["protocol"]=protocol,["timeout_ms"]=timeoutMs};string mechanism,identity="";
        string Key(JsonNode? value)=>Convert.ToBase64String(Convert.FromBase64String(value?.ToString()??""));
        if(protocol=="hysteria2"){
            var stream=outbound["streamSettings"]!;if(stream["security"]?.ToString()!="tls"||stream["tlsSettings"]?["allowInsecure"]?.GetValue<bool>()!=false)throw new SafeError("TLS_UNVERIFIED");
            config["server"]=settings["address"]!.DeepClone();config["port"]=settings["port"]!.DeepClone();config["server_name"]=stream["tlsSettings"]!["serverName"]!.DeepClone();config["alpn"]=new JsonArray("h3");config["password"]=stream["hysteriaSettings"]!["auth"]!.DeepClone();
            if(stream["finalmask"]?["udp"]?[0] is {} mask){if(mask["type"]?.ToString()!="salamander")throw new SafeError("HANDSHAKE_UNSUPPORTED");config["obfs_password"]=mask["settings"]!["password"]!.DeepClone();}
            mechanism="hysteria2-auth";
        }else if(protocol=="tuic"){
            if(settings["skip-cert-verify"]?.GetValue<bool>()!=false)throw new SafeError("TLS_UNVERIFIED");
            foreach(var pair in new[]{("server","server"),("port","port"),("sni","server_name"),("uuid","uuid"),("password","password"),("alpn","alpn"),("udp-relay-mode","udp_mode")})if(settings[pair.Item1] is {} value)config[pair.Item2]=value.DeepClone();
            mechanism="tuic-authenticated-relay";
        }else{
            string privateKey,publicKey;
            if(protocol=="wireguard"){
                var peer=settings["peers"]![0]!;var endpoint=new Uri("udp://"+peer["endpoint"]!.ToString());config["server"]=endpoint.Host.Trim('[',']');config["port"]=endpoint.Port;privateKey=Key(settings["secretKey"]);publicKey=Key(peer["publicKey"]);if(peer["preSharedKey"] is {} key)config["pre_shared_key"]=Key(key);
            }else{
                config["server"]=settings["server"]!.DeepClone();config["port"]=settings["port"]!.DeepClone();privateKey=Key(settings["private-key"]);publicKey=Key(settings["public-key"]);config["awg_options"]=settings["amnezia-wg-option"]!.DeepClone();if(settings["pre-shared-key"] is {} key)config["pre_shared_key"]=Key(key);
            }
            config["private_key"]=privateKey;config["public_key"]=publicKey;identity=Importer.Fingerprint("wg-session/"+privateKey+"/"+publicKey);mechanism=protocol+"-noise";
        }
        return new(protocol,mechanism,identity,config);
    }
    public static bool Conflicts(HandshakePlan30 plan,Profile? active)
    {
        if(plan.SessionIdentity.Length==0||active==null)return false;
        try{return ChainProfiles.IsChain(active.Raw)?ChainProfiles.Read(active.Raw).Layers.Any(layer=>Conflicts(plan,layer)):Plan(active)?.SessionIdentity==plan.SessionIdentity;}catch(SafeError){return true;}
    }
}
