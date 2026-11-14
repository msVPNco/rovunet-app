using System.Text;
using System.Text.RegularExpressions;
using Rovunet.Core;

namespace Rovunet.Desktop;

// منابع عمومی شخص ثالث. هیچ دانلودی بدون اقدام صریح کاربر انجام نمی‌شود
// و هیچ پروفایل قبلی هنگام خطا حذف نمی‌شود.
public static class PublicFeed
{
    public sealed record Preset(string Key, string Name, string Url, bool Expand)
    {
        public string DisplayName=>L.Text(Key=="mahsa"?"SOURCE_MAHSA":"SOURCE_ANONYMOUS");
        public override string ToString()=>Name;
    }
    public const string MahsaUrl="https://raw.githubusercontent.com/mahsanet/MahsaFreeConfig/main/app/sub.txt";
    internal const string MahsaApi32="https://api.github.com/repos/mahsanet/MahsaFreeConfig/contents/app/sub.txt?ref=main";

    public static readonly Preset[] Presets =
    [
        new("mahsa", "Mahsa", MahsaUrl, false),
        new("anonymous", "Anonymous", "https://raw.githubusercontent.com/4n0nymou3/multi-proxy-config-fetcher/refs/heads/main/configs/proxy_configs.txt", false),
    ];

    static readonly string[] MahsaAliases=[MahsaUrl,
        "https://raw.githubusercontent.com/mahsanet/MahsaFreeConfig/refs/heads/main/app/sub.txt",
        "https://github.com/mahsanet/MahsaFreeConfig/blob/main/app/sub.txt",
        "https://raw.githubusercontent.com/hiddify/hiddify-app/refs/heads/main/test.configs/mahsa",
        "https://raw.githubusercontent.com/hiddify/hiddify-app/main/test.configs/mahsa"];
    static readonly string[] AnonymousAliases=["https://raw.githubusercontent.com/4n0nymou3/multi-proxy-config-fetcher/main/configs/proxy_configs.txt","https://github.com/4n0nymou3/multi-proxy-config-fetcher/blob/main/configs/proxy_configs.txt"];
    public static Preset? Resolve(string url)=>MahsaAliases.Contains(url.Trim(),StringComparer.Ordinal)?Presets[0]:AnonymousAliases.Contains(url.Trim(),StringComparer.Ordinal)?Presets[1]:Presets.FirstOrDefault(p=>p.Url==url.Trim());
    public static bool IsRemoved(string url)=>Uri.TryCreate(url,UriKind.Absolute,out var uri)&&uri.Scheme=="https"&&(uri.IdnHost is "raw.githubusercontent.com" or "github.com")&&uri.AbsolutePath.StartsWith("/patterniha/Free-Configs/",StringComparison.OrdinalIgnoreCase);
    public static string CanonicalUrl(string url){if(IsRemoved(url))throw new SafeError("PUBLIC_SOURCE_REMOVED");return Resolve(url)?.Url??url;}

    public static bool IsKnown(string url) => Resolve(url)!=null;
    public static bool IsPartial(CompatibleImport batch)=>batch.Skipped.Values.Any(n=>n>0);

    public static string NameFor(string url) => Resolve(url)?.DisplayName??"";

    // The owner requires complete Mahsa validation before replacing its saved
    // collection. A compatible preview may report independent rejected rows,
    // but those rows must never silently turn into a destructive partial update.
    public static void ValidateForApply(string url,CompatibleImport batch)
    {
        if(Resolve(url)?.Key=="mahsa"&&batch.Skipped.Values.Any(count=>count>0))throw new SafeError("MAHSA_VALIDATION_FAILED");
    }

    public static async Task<SubscriptionClient.Payload> Fetch(string url, CancellationToken cancellation = default, int proxyPort = 0)
        =>await FetchUsing28(url,(uri,stop,port)=>SubscriptionClient.Fetch(uri,port,cancellation:stop,githubRaw32:uri.AbsoluteUri==MahsaApi32),cancellation,proxyPort);
    internal static async Task<SubscriptionClient.Payload> FetchUsing28(string url,Func<Uri,CancellationToken,int,Task<SubscriptionClient.Payload>> fetch,CancellationToken cancellation=default,int proxyPort=0)
    {
        string canonical=CanonicalUrl(url);
        if(Resolve(canonical)?.Key!="mahsa")return await fetch(SubscriptionUrls.Validate(canonical),cancellation,proxyPort);
        using var budget=CancellationTokenSource.CreateLinkedTokenSource(cancellation);budget.CancelAfter(TimeSpan.FromSeconds(25));
        try{
            SubscriptionClient.Payload first;
            try{first=await fetch(SubscriptionUrls.Validate(canonical),budget.Token,proxyPort);}
            catch(SafeError error)when(error.Code is "FETCH_FAILED" or "STATUS_NOT_FOUND"){
                // Same official repository and exact file, with the same chosen
                // proxy route. Never an ad URL, third-party mirror or TLS bypass.
                budget.Token.ThrowIfCancellationRequested();
                first=await fetch(SubscriptionUrls.Validate(MahsaApi32),budget.Token,proxyPort);
            }
            budget.Token.ThrowIfCancellationRequested();return new(string.Join('\n',MahsaFeed28.Decode(first.Body)),first.Date,"");
        }
        catch(OperationCanceledException)when(budget.IsCancellationRequested&&!cancellation.IsCancellationRequested){throw new SafeError("FETCH_FAILED");}
        catch(SafeError error)when(error.Code=="CANCELLED"&&budget.IsCancellationRequested&&!cancellation.IsCancellationRequested){throw new SafeError("FETCH_FAILED");}
    }

    public static CompatibleImport Preview(string body,bool experimental)
    {
        if(Encoding.UTF8.GetByteCount(body)>Importer.MaxBytes)throw new SafeError("IMPORT_TOO_LARGE");
        body=body.Trim().TrimStart('\uFEFF');if(body.Length==0||body.StartsWith('<'))throw new SafeError("INVALID_IMPORT");
        if(!body.Contains("://")&&!body.StartsWith('{')&&!body.StartsWith('['))body=Importer.Decode(body);
        if(body.StartsWith('{')||body.StartsWith('['))return Importer.ReadCompatible(body,experimental);
        var lines=body.Split('\n',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);if(lines.Length>10000)throw new SafeError("IMPORT_TOO_LARGE");
        var result=new CompatibleImport();
        foreach(string line in lines){
            if(line.StartsWith('#')||SubscriptionUsage20.MetadataLine(line,out _,out _))continue;
            CompatibleImport batch;
            try{batch=Importer.ReadCompatible(line,experimental);}
            catch(SafeError error)when(error.Code!="IMPORT_TOO_LARGE"){
                result.Reject(error.Code);continue;
            }
            result.Merge(batch);
        }
        return result;
    }
}
