using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Rovunet.Agent;
using Rovunet.Core;
using Rovunet.Desktop;

// Live check for Rovunet Free v2 (debug catalog only).
//
// It downloads the signed catalog through the production client, decrypts every
// envelope, then opens each path with the real Agent runtime and fetches a known
// HTTPS page through it. Counts are reported separately: fetched, decrypted,
// connected, payload-verified. No link, credential or IP address is printed.
// Nothing here is part of a packaged release.

var report = new Dictionary<string, object>();
string[] checks = ["https://example.com/", "https://api.ipify.org"];

// Debug trust values come from the owner's local secret files, passed as
// arguments: <aes-key-file> <public-key-file> <catalog-base-url> [key-id].
// The AES file may be the 32 raw bytes the import tool uses or its base64 text;
// the public key file is base64 or an SPKI DER export. Nothing is compiled in
// and nothing is printed.
if (args.Length < 3)
{
    Console.WriteLine(JsonSerializer.Serialize(new { suite = "free-v2-live", error = "usage: <aes-key-file> <public-key-file> <catalog-base-url> [key-id]" }));
    return 2;
}
static string KeyText(string path)
{
    byte[] bytes = File.ReadAllBytes(path);
    return bytes.Length == 32 ? Convert.ToBase64String(bytes) : System.Text.Encoding.UTF8.GetString(bytes).Trim();
}
RovunetFreeCatalog.OverrideAesKey = KeyText(args[0]);
RovunetFreeCatalog.OverridePublicKey = KeyText(args[1]);
RovunetFreeCatalog.OverrideAddress = args[2].TrimEnd('/') + "/catalog.json";
if (args.Length > 3) RovunetFreeCatalog.OverrideKeyId = args[3].Trim();
var stages = new List<string>();
RovunetFreeCatalog.Trace = stage => stages.Add(stage);
if (!RovunetFreeCatalog.Enabled)
{
    Console.WriteLine(JsonSerializer.Serialize(new { suite = "free-v2-live", error = "free catalog is not enabled with these values" }));
    return 2;
}

// 1. Fetch and decrypt through the exact production path.
List<string> links;
try
{
    var payload = await PublicFeed.Fetch(RovunetFreeCatalog.Url, CancellationToken.None);
    links = payload.Body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    report["fetched_decrypted"] = links.Count;
}
catch (SafeError error)
{
    Console.WriteLine(JsonSerializer.Serialize(new { suite = "free-v2-live", error = error.Code, fetched_decrypted = 0, stages }));
    return 2;
}

// 2. One connection attempt per decrypted config, with a real HTTPS payload.
int connected = 0, verified = 0;
var outcomes = new List<object>();
foreach (string raw in links)
{
    var row = new Dictionary<string, object>();
    var clock = Stopwatch.StartNew();
    Profile profile;
    try { profile = Importer.Single(raw); }
    catch (SafeError error) { row["result"] = "parse:" + error.Code; outcomes.Add(row); continue; }
    // Scheme and host are enough to identify a row; no credentials are reported.
    row["scheme"] = profile.Protocol;
    using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(75));
    using var runtime = new Runtime(probeOnly: true);
    try
    {
        var ready = await runtime.Connect(profile, false, budget.Token);
        if (ready.State != "connected") { row["result"] = "connect:" + (ready.Error is { Length: > 0 } ? ready.Error : ready.State); outcomes.Add(row); continue; }
        connected++;
        row["ports"] = ready.HttpPort > 0 && ready.SocksPort > 0;
        using var handler = new SocketsHttpHandler
        {
            Proxy = new WebProxy("http://127.0.0.1:" + ready.HttpPort),
            UseProxy = true,
            AllowAutoRedirect = false,
            ConnectTimeout = TimeSpan.FromSeconds(12)
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
        bool ok = true;
        var probes = new List<string>();
        foreach (string target in checks)
        {
            try
            {
                using var response = await client.GetAsync(target, budget.Token);
                bool good = response.IsSuccessStatusCode;
                probes.Add((good ? "ok:" : "status:" + (int)response.StatusCode) + new Uri(target).Host);
                ok &= good;
            }
            catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
            {
                probes.Add("fail:" + new Uri(target).Host);
                ok = false;
            }
        }
        var stats = await runtime.Telemetry(budget.Token);
        row["probes"] = probes;
        row["traffic"] = stats.TrafficAvailable && stats.UploadedBytes > 0 && stats.DownloadedBytes > 0;
        if (ok && stats.TrafficAvailable && stats.DownloadedBytes > 0) verified++;
        row["result"] = ok ? "payload-verified" : "payload-failed";
    }
    catch (OperationCanceledException) { row["result"] = "timeout"; }
    catch (SafeError error) { row["result"] = "error:" + error.Code; }
    finally { runtime.Stop(); }
    row["elapsedMs"] = clock.ElapsedMilliseconds;
    outcomes.Add(row);
}

report["connected"] = connected;
report["payload_verified"] = verified;
report["total"] = links.Count;
report["stages"] = stages;
report["rows"] = outcomes;
Console.WriteLine(JsonSerializer.Serialize(new { suite = "free-v2-live", report }));
return verified > 0 ? 0 : 1;

namespace Rovunet.Desktop
{
    internal static class L { internal static string Text(string key) => key; }
}
