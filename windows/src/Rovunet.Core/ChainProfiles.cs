using System.Text;
using System.Text.Json;

namespace Rovunet.Core;

public sealed class ChainLayer
{
    public string Id {get;set;}="";
    public string Raw {get;set;}="";
    public override string ToString()=>"ChainLayer(redacted)";
}
public sealed class ChainIdentity
{
    public string HostKey {get;set;}="";
    public string SlipCertificate {get;set;}="";
    public string SshTlsCa {get;set;}="";
    public int? SlipInterval {get;set;}
    public override string ToString()=>"ChainIdentity(redacted)";
}
public static class ChainProfiles
{
    public const string Prefix="rovunet-chain://1.";
    public static bool IsChain(string raw)=>raw.StartsWith("rovunet-chain://",StringComparison.OrdinalIgnoreCase);
    // Native tokens are finer-grained than chain kinds: a VLESS path is Xray on the
    // wire (Android's TunnelType.XRAY), and `X_ssh` combo profiles are complete
    // two-stage paths that can only open a chain, never sit inside one.
    public static string Kind(Profile p)
    {
        if(ProfileEditing.IsNative(p.Raw)){
            string token=Importer.Fields(p.Raw)[1];
            return token switch{
                "vless" or "xray"=>"xray",
                "ss" or "slipstream"=>"slipstream",
                "sayedns"=>"dnstt",
                _ when token.EndsWith("_ssh",StringComparison.Ordinal)=>"combo",
                _=>token
            };
        }
        return p.Protocol=="SOCKS5"?"socks5":"xray";
    }
    // Android ChainValidation: 2–4 layers; only a directly reachable Xray
    // transport followed by SSH; Slipstream is outermost only; singleton
    // bridge groups cannot repeat (SSH and SOCKS5 can repeat).
    //
    // Rule() is the single source of truth. Validate() calls it with complete=true.
    // The editor calls AppendBlock() on every prefix, so each dropdown offers only
    // a layer that keeps the chain buildable instead of failing at save time.
    public static string? Rule(IReadOnlyList<Profile> layers,bool complete)
    {
        if(layers.Count>4)return "CHAIN_LAYER_COUNT";
        if(complete&&layers.Count<2)return "CHAIN_LAYER_COUNT";
        if(layers.Any(p=>IsChain(p.Raw)||!p.CanConnect))return "CHAIN_UNSUPPORTED";
        string[] kinds=layers.Select(Kind).ToArray();
        string[] allowed=["dnstt","sayedns","vaydns","soar","slipstream","ssh","naive","snowflake","doh","socks5","xray","combo"];
        if(kinds.Any(k=>!allowed.Contains(k)))return "CHAIN_SINGLE_LAYER_ONLY";
        // A combo profile already contains its own two stages; it can only be the
        // first layer, where it comes out as SOCKS5 for whatever follows.
        if(kinds.Contains("combo")&&(kinds[0]!="combo"||kinds.Length<2||(complete&&kinds.Length<2)))return "CHAIN_COMBO_FIRST_ONLY";
        if(kinds.Contains("xray")&&(kinds.Length>2||kinds[0]!="xray"||(kinds.Length>=2&&kinds[1]!="ssh")||(complete&&kinds.Length!=2)))return "XRAY_CHAIN_REQUIRES_OUTER_SSH";
        var groups=new HashSet<string>();
        for(int i=0;i<kinds.Length;i++){
            string kind=kinds[i];if(i>0&&kind is "slipstream")return "CHAIN_SLIP_OUTERMOST";
            string group=kind switch{"sayedns"=>"dnstt","ss"=>"slipstream",_=>kind};
            if(kind is not("ssh" or "socks5")&&!groups.Add(group))return "CHAIN_DUPLICATE_ENGINE";
        }
        return null;
    }
    public static void Validate(IReadOnlyList<Profile> layers)
    {
        string? error=Rule(layers,complete:true);
        if(error!=null)throw new SafeError(error);
    }
    /** Why [candidate] cannot be the next layer after [layers]; null when it can. */
    public static string? AppendBlock(IReadOnlyList<Profile> layers,Profile candidate)
    {
        if(layers.Count>=4)return "CHAIN_LAYER_COUNT";
        var next=new List<Profile>(layers.Count+1);next.AddRange(layers);next.Add(candidate);
        return Rule(next,complete:false);
    }
    public static List<Profile> Appendable(IReadOnlyList<Profile> layers,IEnumerable<Profile> candidates) =>
        candidates.Where(candidate=>AppendBlock(layers,candidate)==null).ToList();
    public static Profile Parse(string raw)
    {
        var (name,layers)=Read(raw);Validate(layers);
        return new(){Name=name,Raw=raw,Protocol="Chain",CollectionId="manual:"+Importer.Fingerprint(raw)};
    }
    public static (string Name,List<Profile> Layers) Read(string raw)
    {
        if(!raw.StartsWith(Prefix,StringComparison.Ordinal)||raw.Length>131072)throw new SafeError("INVALID_IMPORT");
        using var doc=JsonDocument.Parse(Importer.Decode(raw[Prefix.Length..]),new JsonDocumentOptions{MaxDepth=4});
        var root=doc.RootElement;Importer.Keys(root,"name","layers");string name=root.GetProperty("name").GetString()??"";
        if(name.Length is <1 or >200||name.Any(char.IsControl))throw new SafeError("CORE_NAME_INVALID");
        var items=root.GetProperty("layers");if(items.GetArrayLength() is <2 or >4)throw new SafeError("CHAIN_LAYER_COUNT");
        var layers=new List<Profile>();var ids=new HashSet<string>();
        foreach(var layer in items.EnumerateArray()){
            Importer.Keys(layer,"id","raw");string id=layer.GetProperty("id").GetString()??"",text=layer.GetProperty("raw").GetString()??"";
            if(id.Length is <1 or >128||id.Any(char.IsControl)||!ids.Add(id)||IsChain(text))throw new SafeError("INVALID_IMPORT");
            var p=Importer.Single(text);p.Id=id;layers.Add(p);
        }
        Validate(layers);return(name,layers);
    }
    public static string Create(string name,IReadOnlyList<Profile> layers)
    {
        Validate(layers);string raw=Prefix+Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{name=name.Trim(),layers=layers.Select(p=>new{id=p.Id,raw=p.Raw})})));
        _=Parse(raw);return raw;
    }
}
