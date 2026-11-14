using System.Text.Json;
using System.Text.Json.Nodes;
using Rovunet.Core;
using Rovunet.Platform;
namespace Rovunet.WindowsSmoke;
static class Handshake30Contract
{
    internal static int Run(string path)
    {
        if(!Path.IsPathFullyQualified(path)||File.Exists(path))return 12;
        var checks=new Dictionary<string,bool>();var plan=new HandshakePlan30("wireguard","wireguard-noise","",new());
        string Good()=>"{\"version\":1,\"authenticated\":true,\"mechanism\":\"wireguard-noise\",\"elapsed_ms\":1000,\"error\":\"\"}";
        var valid=ProtocolHandshakeClient30.Parse(Good(),plan);checks["authenticated_result_is_explicit_and_has_threshold_latency"]=valid.Reachable&&valid.ElapsedMs==1000&&ProtocolHandshake30.GreenLatency(valid.ElapsedMs);
        foreach(var (name,json) in new[]{("wrong_mechanism",Good().Replace("wireguard-noise","fake-ready")),("error_with_success",Good().Replace("\"error\":\"\"","\"error\":\"HANDSHAKE_FAILED\"")),("negative_latency",Good().Replace(":1000",":-1")),("huge_latency",Good().Replace(":1000",":10001")),("unknown_field",Good().Replace("{","{\"ready\":true,")),("duplicate_field",Good().Replace("{","{\"authenticated\":false,")),("missing_authentication","{\"ready\":true}"), ("unbounded_error",Good().Replace("true","false").Replace("\"error\":\"\"","\"error\":\"raw-secret\""))}){bool rejected=false;try{ProtocolHandshakeClient30.Parse(json,plan);}catch(SafeError e){rejected=e.Code=="INVALID_MESSAGE";}checks["reject_"+name]=rejected;}
        var failure=ProtocolHandshakeClient30.Parse(Good().Replace("true","false").Replace("\"error\":\"\"","\"error\":\"HANDSHAKE_TIMEOUT\""),plan);checks["timeout_never_becomes_reachable"]=!failure.Reachable&&failure.ElapsedMs==0;
        bool passed=checks.Values.All(x=>x);Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllText(path,JsonSerializer.Serialize(new{passed,checks,realNetwork=false,ownerStoreTouched=false},new JsonSerializerOptions{WriteIndented=true}));return passed?0:1;
    }
}
