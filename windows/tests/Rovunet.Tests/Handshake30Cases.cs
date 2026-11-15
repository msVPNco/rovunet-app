using System.Text.Json.Nodes;
using Rovunet.Core;

static class Handshake30Cases
{
    static void Check(bool value){if(!value)throw new Exception("handshake contract");}
    static string Key(int seed)=>Convert.ToBase64String(Enumerable.Repeat((byte)seed,32).ToArray());
    static Profile Wg(int seed=1,string endpoint="127.0.0.1:51820")=>Importer.Single($"[Interface]\nPrivateKey={Key(seed)}\nAddress=10.0.0.2/32\n[Peer]\nPublicKey={Key(2)}\nEndpoint={endpoint}\nAllowedIPs=0.0.0.0/0");
    public static IEnumerable<(string,Action)> All()
    {
        yield return("handshake30-threshold-is-inclusive-1000",()=>Check(ProtocolHandshake30.GreenLatency(999)&&ProtocolHandshake30.GreenLatency(1000)&&!ProtocolHandshake30.GreenLatency(1001)&&!ProtocolHandshake30.GreenLatency(-1)));
        yield return("handshake30-hy2-keeps-auth-sni-salamander",()=>{var p=Importer.Single("hy2://fixture-pass@server.example.invalid:443?sni=cert.example.invalid&obfs=salamander&obfs-password=fixture-mask");var plan=ProtocolHandshake30.Plan(p)!;Check(plan.Protocol=="hysteria2"&&plan.Configuration["server_name"]!.ToString()=="cert.example.invalid"&&plan.Configuration["password"]!.ToString()=="fixture-pass"&&plan.Configuration["obfs_password"]!.ToString()=="fixture-mask");});
        yield return("handshake30-tuic-keeps-auth-and-udp-mode",()=>{var p=Importer.Single("tuic://00000000-0000-4000-8000-000000000001:fixture-pass@server.example.invalid:443?alpn=h3&udp_relay_mode=quic&sni=cert.example.invalid");var plan=ProtocolHandshake30.Plan(p)!;Check(plan.Mechanism=="tuic-authenticated-relay"&&plan.Configuration["uuid"]!.ToString()=="00000000-0000-4000-8000-000000000001"&&plan.Configuration["password"]!.ToString()=="fixture-pass"&&plan.Configuration["udp_mode"]!.ToString()=="quic");});
        yield return("handshake30-wg-uses-peer-and-psk",()=>{var p=Importer.Single(Wg().Raw+"\nPresharedKey="+Key(3));var plan=ProtocolHandshake30.Plan(p)!;Check(plan.Configuration["server"]!.ToString()=="127.0.0.1"&&plan.Configuration["port"]!.GetValue<int>()==51820&&plan.Configuration["private_key"]!.ToString()==Key(1)&&plan.Configuration["public_key"]!.ToString()==Key(2)&&plan.Configuration["pre_shared_key"]!.ToString()==Key(3));});
        yield return("handshake30-awg-retains-obfuscation",()=>{var p=Importer.Single(Wg().Raw.Replace("[Peer]","Jc=2\nJmin=10\nJmax=20\nS1=16\nS2=16\nH1=123456\nH2=234567\nH3=345678\nH4=456789\n[Peer]"));var plan=ProtocolHandshake30.Plan(p)!;Check(plan.Protocol=="amneziawg"&&plan.Configuration["awg_options"]!["jc"]!.GetValue<int>()==2&&plan.Configuration["awg_options"]!["h1"]!.ToString()=="123456");});
        yield return("handshake30-active-wg-same-keys-even-other-endpoint-conflict",()=>Check(ProtocolHandshake30.Conflicts(ProtocolHandshake30.Plan(Wg(endpoint:"127.0.0.2:51821"))!,Wg())));
        yield return("handshake30-other-wg-identity-is-independent",()=>Check(!ProtocolHandshake30.Conflicts(ProtocolHandshake30.Plan(Wg(4))!,Wg())));
        yield return("handshake30-redacted-plan-and-result-strings",()=>{var plan=ProtocolHandshake30.Plan(Wg())!;Check(!plan.ToString().Contains(Key(1))&&!new QuickProbe30(false,0,"fixture-secret","wireguard-noise").ToString().Contains("fixture-secret"));});
        foreach(string uri in new[]{"socks5://127.0.0.1:1080","trojan://fixture@server.example.invalid:443","anytls://fixture@server.example.invalid:443"}){string raw=uri;yield return("handshake30-tcp-fallback-"+new Uri(raw).Scheme,()=>Check(ProtocolHandshake30.Plan(Importer.Single(raw))==null));}
        yield return("handshake30-native-dns-stays-on-dns-fallback",()=>Check(ProtocolHandshake30.Plan(Importer.Single(ManualProfiles.Dns("dnstt","Fixture","t.example.invalid","127.0.0.1:53","udp",new string('a',64),"","","","")))==null));
        yield return("handshake30-timeout-is-bounded",()=>{foreach(int value in new[]{0,99,10001}){try{ProtocolHandshake30.Plan(Wg(),value);throw new Exception("accepted");}catch(SafeError e)when(e.Code=="INVALID_MESSAGE"){} }Check(ProtocolHandshake30.Plan(Wg(),100)!.Configuration["timeout_ms"]!.GetValue<int>()==100);});
    }
}
