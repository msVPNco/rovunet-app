using System.Globalization;
using System.Text;
using System.Text.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Rovunet.Core;
using Rovunet.Desktop;

// Link the production download/verifier/parser classes into a portable harness.
// It performs no HTTP requests and never opens or modifies a user's store.
const string seed = "9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60";
const string link = "vless://00000000-0000-4000-8000-000000000001@example.invalid:443?security=tls&type=ws";
var now = DateTimeOffset.UtcNow;
string Time(DateTimeOffset value) => value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
object Donation(DateTimeOffset created, params string[] links) => new {
    donation_id = new string('a', 32), created_at = Time(created), expires_at = Time(created.AddDays(30)),
    quota_gb = 50, items = links.Select((value, i) => new { id = "item-" + i, link = value }).ToArray()
};
string Catalog(DateTimeOffset generated, params object[] donations) =>
    JsonSerializer.Serialize(new { version = 1, generated_at = Time(generated), donations }) + "\n";
string Signature(string body)
{
    var signer = new Ed25519Signer();
    signer.Init(true, new Ed25519PrivateKeyParameters(Convert.FromHexString(seed), 0));
    var bytes = Encoding.UTF8.GetBytes(body);
    signer.BlockUpdate(bytes, 0, bytes.Length);
    return Convert.ToBase64String(signer.GenerateSignature());
}
void Check(bool value) { if (!value) throw new Exception("ASSERTION_FAILED"); }
async Task Reject(Func<Task> action, string expected)
{
    try { await action(); }
    catch (SafeError error) when (error.Code == expected) { return; }
    throw new Exception("EXPECTED_" + expected);
}
async Task<SubscriptionClient.Payload> Fetch(string body, string? detached = null, CancellationToken cancellation = default)
{
    var requests = new List<string>();
    Task<SubscriptionClient.Payload> Transport(Uri uri, CancellationToken token, int port)
    {
        token.ThrowIfCancellationRequested();
        Check(port == 17891);
        requests.Add(uri.AbsoluteUri);
        return Task.FromResult(new SubscriptionClient.Payload(
            uri.AbsoluteUri == RovunetFreeCatalog.Url ? body : detached ?? Signature(body), now, ""));
    }
    var result = await PublicFeed.FetchUsing28(RovunetFreeCatalog.Url, Transport, cancellation, 17891);
    Check(requests.SequenceEqual(new[] { RovunetFreeCatalog.Url, RovunetFreeCatalog.SigUrl }));
    return result;
}
var tests = new List<(string Name, Func<Task> Run)> {
    ("signed empty catalog reaches empty replacement preview", async () => {
        var payload = await Fetch(Catalog(now));
        var batch = PublicFeed.PreviewUpdate(RovunetFreeCatalog.Url, payload.Body, true);
        Check(payload.Body == "" && batch.Total == 0 && batch.Profiles.Count == 0);
    }),
    ("expired final donation reaches empty replacement preview", async () => {
        var payload = await Fetch(Catalog(now, Donation(now.AddDays(-31), link)));
        var batch = PublicFeed.PreviewUpdate(RovunetFreeCatalog.Url, payload.Body, true);
        Check(batch.Total == 0 && batch.Profiles.Count == 0);
    }),
    ("valid signed catalog retains the normal protocol parser", async () => {
        var payload = await Fetch(Catalog(now, Donation(now.AddDays(-1), link)));
        var batch = PublicFeed.PreviewUpdate(RovunetFreeCatalog.Url, payload.Body, true);
        Check(batch.Total == 1 && batch.Profiles.Count == 1 && !PublicFeed.IsPartial(batch));
    }),
    ("tampered empty catalog fails before replacement preview", async () => {
        var original = Catalog(now, Donation(now.AddDays(-1), link));
        await Reject(async () => { await Fetch(Catalog(now), Signature(original)); }, "FREE_SIGNATURE_INVALID");
    }),
    ("unsigned or malformed signature cannot retire profiles", async () => {
        foreach (var signature in new[] { "", "not-base64", Convert.ToBase64String(new byte[64]) })
            await Reject(async () => { await Fetch(Catalog(now), signature); }, "FREE_SIGNATURE_INVALID");
    }),
    ("stale signed empty catalog is rejected", async () => {
        await Reject(async () => { await Fetch(Catalog(now.AddDays(-8))); }, "FREE_CATALOG_STALE");
    }),
    ("third-party empty feeds retain their rejection", async () => {
        foreach (var url in new[] { PublicFeed.MahsaUrl, PublicFeed.Presets[1].Url, "https://example.invalid/feed" })
            await Reject(() => { PublicFeed.PreviewUpdate(url, "", true); return Task.CompletedTask; }, "INVALID_IMPORT");
    }),
    ("partial official catalog is rejected atomically", async () => {
        var payload = await Fetch(Catalog(now, Donation(now.AddDays(-1), link, "future-protocol://example.invalid")));
        await Reject(() => { PublicFeed.PreviewUpdate(RovunetFreeCatalog.Url, payload.Body, true); return Task.CompletedTask; }, "INVALID_IMPORT");
        var ordinary = PublicFeed.PreviewUpdate(PublicFeed.Presets[1].Url, payload.Body, true);
        Check(ordinary.Profiles.Count == 1 && PublicFeed.IsPartial(ordinary));
    }),
};
var failed = 0;
foreach (var test in tests)
{
    try { await test.Run(); Console.WriteLine("PASS " + test.Name); }
    catch (Exception error) { failed++; Console.WriteLine("FAIL " + test.Name + ": " + error.Message); }
}
Console.WriteLine($"{tests.Count - failed}/{tests.Count} signed-catalog contract tests passed; no network, user store or WPF UI executed.");
return failed == 0 ? 0 : 1;

namespace Rovunet.Desktop
{
    // Presentation translation is not part of this portable protocol test.
    internal static class L { internal static string Text(string key) => key; }
}
