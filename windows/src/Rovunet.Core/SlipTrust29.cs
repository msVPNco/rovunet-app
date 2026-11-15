using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace Rovunet.Core;
public sealed class SlipTrustPin29
{
    public string Origin {get;set;}="";
    public string ServerId {get;set;}="";
    public string AccountId {get;set;}="";
    public string PeriodId {get;set;}="";
    public string ProfileId {get;set;}="";
    public string EndpointBinding {get;set;}="";
    public string CertificatePem {get;set;}="";
    public string CertificateSha256 {get;set;}="";
    public override string ToString()=>"SlipTrustPin(redacted)";
}
public static class SlipTrust29
{
    public static Uri Companion(string source)
    {
        var uri=SubscriptionUrls.Validate(source);
        if(uri.Query.Length>0)throw new SafeError("CERT_AUTO_UNAVAILABLE");
        var match=Regex.Match(uri.AbsolutePath,@"^(/(?:v2/)?sub/)([A-Za-z0-9_-]{16,256})(?:\.windows)?\.json$");
        if(!match.Success)throw new SafeError("CERT_AUTO_UNAVAILABLE");
        return new UriBuilder(uri){Path=match.Groups[1].Value+match.Groups[2].Value+".trust.json"}.Uri;
    }
    public static string Key(Subscription sub,Profile profile)=>Importer.Fingerprint(string.Join('|',sub.Id,Companion(sub.Url).GetLeftPart(UriPartial.Authority),profile.ItemId,SubscriptionEndpoint.Binding(profile)));
    public static SlipTrustPin29 Read(string body,Uri companion,Profile profile,AccountStatus expected)
    {
        try{
            if(body.Length>262144||!TransportPlan.UsesSlip(profile.Raw)||profile.ItemId.Length==0)throw new SafeError("SLIP_CERT_INVALID");
            using var doc=JsonDocument.Parse(body,new JsonDocumentOptions{MaxDepth=8});var root=doc.RootElement;
            void Keys(JsonElement value,params string[] keys){if(value.ValueKind!=JsonValueKind.Object||value.EnumerateObject().Count()!=keys.Length||value.EnumerateObject().Any(p=>!keys.Contains(p.Name)))throw new SafeError("SLIP_CERT_INVALID");}
            Keys(root,"version","server_id","account_id","period_id","items");
            string Field(string key)=>root.GetProperty(key).GetString()??"";
            if(root.GetProperty("version").GetInt32()!=1||new[]{"server_id","account_id","period_id"}.Any(key=>!Regex.IsMatch(Field(key),"^[0-9a-f]{32}$")))throw new SafeError("SLIP_CERT_INVALID");
            if(Field("server_id")!=expected.ServerId||Field("account_id")!=expected.AccountId||Field("period_id")!=expected.PeriodId)throw new SafeError("ACCOUNT_BINDING_CHANGED");
            var items=root.GetProperty("items");if(items.ValueKind!=JsonValueKind.Array||items.GetArrayLength()>512)throw new SafeError("SLIP_CERT_INVALID");
            var matches=items.EnumerateArray().Where(item=>item.GetProperty("profile_id").GetString()==profile.ItemId).ToArray();if(matches.Length!=1)throw new SafeError("CERT_AUTO_UNAVAILABLE");
            var row=matches[0];Keys(row,"profile_id","transport","domain","certificate_pem","certificate_sha256","not_after");
            string Get(string key)=>row.GetProperty(key).GetString()??"";
            if(Get("transport")!="slipstream"||!Get("domain").TrimEnd('.').Equals(Importer.Fields(profile.Raw)[3].TrimEnd('.'),StringComparison.OrdinalIgnoreCase))throw new SafeError("SLIP_CERT_INVALID");
            string raw=Get("certificate_pem").Trim();
            if(Regex.Matches(raw,"-----BEGIN CERTIFICATE-----").Count!=1||!raw.StartsWith("-----BEGIN CERTIFICATE-----",StringComparison.Ordinal)||!raw.EndsWith("-----END CERTIFICATE-----",StringComparison.Ordinal)||Regex.Matches(raw,"-----BEGIN ").Count!=1)throw new SafeError("SLIP_CERT_INVALID");
            string pem=SlipCertificate.ReadPublic(raw);using var cert=X509Certificate2.CreateFromPem(pem);string sha=Convert.ToHexStringLower(SHA256.HashData(cert.RawData));
            if(Get("certificate_sha256")!=sha||!DateTimeOffset.TryParse(Get("not_after"),System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.RoundtripKind,out var expiry)||Math.Abs((expiry-cert.NotAfter.ToUniversalTime()).TotalSeconds)>1)throw new SafeError("SLIP_CERT_INVALID");
            return new(){Origin=companion.GetLeftPart(UriPartial.Authority),ServerId=expected.ServerId,AccountId=expected.AccountId,PeriodId=expected.PeriodId,ProfileId=profile.ItemId,EndpointBinding=SubscriptionEndpoint.Binding(profile),CertificatePem=pem,CertificateSha256=sha};
        }catch(SafeError){throw;}catch{throw new SafeError("SLIP_CERT_INVALID");}
    }
}
