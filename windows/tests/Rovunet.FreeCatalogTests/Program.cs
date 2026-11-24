using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Rovunet.Core;
using Rovunet.Desktop;

// Portable harness for the Free v2 client. It links the production verifier,
// makes no HTTP requests and never opens a user store. Fixture keys are the
// public RFC 8032 test vectors, never a production trust root.
const string seed = "9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60";
const string link = "vless://00000000-0000-4000-8000-000000000001@example.invalid:443?security=tls&type=ws";
var now = DateTimeOffset.UtcNow;
var aesKey = new byte[32];
for (int i = 0; i < aesKey.Length; i++) aesKey[i] = (byte)(i + 1);
string Time(DateTimeOffset value) => value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
string Hex(byte[] value) => Convert.ToHexString(value).ToLowerInvariant();

string Entry(string id, string donation, DateTimeOffset created, string sha, string keyId = "fixture-key") =>
    JsonSerializer.Serialize(new
    {
        id, donation_id = donation, key_id = keyId, quota_gib = 50,
        created_at = Time(created), expires_at = Time(created.AddDays(30)),
        path = "configs/" + id + ".json", sha256 = sha
    });

string Envelope(string id, string donation, DateTimeOffset created, string secret = link)
{
    byte[] nonce = new byte[12];
    for (int i = 0; i < nonce.Length; i++) nonce[i] = (byte)(0x40 + i);
    byte[] plain = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { link = secret }));
    byte[] aad = Encoding.UTF8.GetBytes($"rovunet-free-v2|{id}|{donation}|{Time(created)}|{Time(created.AddDays(30))}|fixture-key");
    byte[] ciphertext = new byte[plain.Length + 16];
    using (var gcm = new AesGcm(aesKey, 16))
        gcm.Encrypt(nonce, plain, ciphertext.AsSpan(0, plain.Length), ciphertext.AsSpan(plain.Length), aad);
    return JsonSerializer.Serialize(new
    {
        version = 2, algorithm = "AES-256-GCM", id, donation_id = donation,
        created_at = Time(created), expires_at = Time(created.AddDays(30)), key_id = "fixture-key", quota_gib = 50,
        nonce = Convert.ToBase64String(nonce), ciphertext = Convert.ToBase64String(ciphertext)
    });
}

string Catalog(DateTimeOffset generated, params string[] entries) =>
    "{\"version\":2,\"generated_at\":\"" + Time(generated) + "\",\"entries\":[" + string.Join(',', entries) + "]}\n";

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

// One active, well-formed item: catalog + signature + envelope + empty revoked list.
var goodId = new string('a', 32);
var goodDonation = new string('b', 32);
var goodCreated = now.AddDays(-1);
var goodEnvelope = Envelope(goodId, goodDonation, goodCreated);
var goodEntry = Entry(goodId, goodDonation, goodCreated, Hex(SHA256.HashData(Encoding.UTF8.GetBytes(goodEnvelope))));

async Task<SubscriptionClient.Payload> Fetch(
    string body,
    Func<string, string>? documents = null,
    string? detached = null,
    CancellationToken cancellation = default)
{
    string baseAddress = RovunetFreeCatalog.Url[..(RovunetFreeCatalog.Url.LastIndexOf('/') + 1)];
    string configAddress = baseAddress + "configs/" + goodId + ".json";
    string revokedAddress = baseAddress + "revoked.json";
    Task<SubscriptionClient.Payload> Transport(Uri uri, CancellationToken token, int port)
    {
        token.ThrowIfCancellationRequested();
        Check(port == 17891);
        string address = uri.AbsoluteUri;
        string payload = address == RovunetFreeCatalog.Url ? body
            : address == RovunetFreeCatalog.Url.Replace("catalog.json", "catalog.sig", StringComparison.Ordinal) ? detached ?? Signature(body)
            : address == revokedAddress ? "[]"
            : address == configAddress ? goodEnvelope
            : throw new Exception("UNEXPECTED_ADDRESS " + address);
        if (documents != null) payload = documents(payload);
        return Task.FromResult(new SubscriptionClient.Payload(payload, now, ""));
    }
    return await PublicFeed.FetchUsing28(RovunetFreeCatalog.Url, Transport, cancellation, 17891);
}

var tests = new List<(string Name, Func<Task> Run)> {
    ("pinned build values enable the official source", () =>
    {
        Check(RovunetFreeCatalog.Enabled);
        Check(PublicFeed.Presets.Any(preset => preset.Key == "rovunet"));
        return Task.CompletedTask;
    }),
    ("signed catalog decrypts into one importable path", async () =>
    {
        var payload = await Fetch(Catalog(now, goodEntry));
        var batch = PublicFeed.PreviewUpdate(RovunetFreeCatalog.Url, payload.Body, true);
        Check(batch.Total == 1 && batch.Profiles.Count == 1 && !PublicFeed.IsPartial(batch));
        Check(batch.Profiles[0].CanConnect);
    }),
    ("tampered catalog fails before any entry is read", async () =>
    {
        var original = Catalog(now, goodEntry);
        await Reject(async () => { await Fetch(Catalog(now, goodEntry + " "), detached: Signature(original)); }, "ROVUNET_FREE_UNVERIFIED");
    }),
    ("catalog without its trailing newline is rejected", async () =>
    {
        var body = Catalog(now, goodEntry).TrimEnd('\n');
        await Reject(async () => { await Fetch(body, detached: Signature(body)); }, "ROVUNET_FREE_UNVERIFIED");
    }),
    ("unsigned or malformed signature cannot retire profiles", async () =>
    {
        foreach (var signature in new[] { "", "not-base64", Convert.ToBase64String(new byte[64]) })
            await Reject(async () => { await Fetch(Catalog(now, goodEntry), detached: signature); }, "ROVUNET_FREE_UNVERIFIED");
    }),
    ("future-dated catalog is rejected", async () =>
    {
        await Reject(async () => { await Fetch(Catalog(now.AddMinutes(30), goodEntry)); }, "ROVUNET_FREE_UNVERIFIED");
    }),
    ("expired entry yields no links", async () =>
    {
        var id = new string('c', 32);
        var created = now.AddDays(-31);
        var envelope = Envelope(id, goodDonation, created);
        var entry = Entry(id, goodDonation, created, Hex(SHA256.HashData(Encoding.UTF8.GetBytes(envelope))));
        await Reject(async () => { await Fetch(Catalog(now, entry)); }, "ROVUNET_FREE_UNAVAILABLE");
    }),
    ("revoked donation drops every member", async () =>
    {
        string baseAddress = RovunetFreeCatalog.Url[..(RovunetFreeCatalog.Url.LastIndexOf('/') + 1)];
        await Reject(async () =>
        {
            await Fetch(Catalog(now, goodEntry), documents: payload =>
                payload == "[]" ? "[\"" + goodDonation + "\"]" : payload);
        }, "ROVUNET_FREE_UNAVAILABLE");
    }),
    ("envelope bytes that do not match the signed hash are skipped", async () =>
    {
        await Reject(async () =>
        {
            await Fetch(Catalog(now, goodEntry), documents: payload =>
                payload.StartsWith("{\"version\":2,\"algorithm\"", StringComparison.Ordinal) ? payload.Replace("\"version\":2", "\"version\":2 ", StringComparison.Ordinal) : payload);
        }, "ROVUNET_FREE_UNAVAILABLE");
    }),
    ("a wrong AES key cannot authenticate the envelope", () =>
    {
        var entry = Entry(goodId, goodDonation, goodCreated, Hex(SHA256.HashData(Encoding.UTF8.GetBytes(goodEnvelope))));
        var parsed = RovunetFreeCatalog.ParseCatalog(Catalog(now, entry), now).Single();
        var wrong = new byte[32];
        for (int i = 0; i < wrong.Length; i++) wrong[i] = (byte)(0xF0 + i);
        try { RovunetFreeCatalog.Decrypt(goodEnvelope, parsed, wrong); }
        catch (SafeError error) when (error.Code == "ROVUNET_FREE_UNVERIFIED") { return Task.CompletedTask; }
        throw new Exception("EXPECTED_ROVUNET_FREE_UNVERIFIED");
    }),
    ("entries must stay sorted by id", async () =>
    {
        var secondId = new string('d', 32);
        var secondEnvelope = Envelope(secondId, new string('e', 32), goodCreated);
        var second = Entry(secondId, new string('e', 32), goodCreated, Hex(SHA256.HashData(Encoding.UTF8.GetBytes(secondEnvelope))));
        await Reject(async () => { await Fetch(Catalog(now, second, goodEntry)); }, "ROVUNET_FREE_UNVERIFIED");
    }),
    ("third-party empty feeds retain their rejection", async () =>
    {
        foreach (var url in new[] { PublicFeed.MahsaUrl, PublicFeed.Presets.First(p => p.Key == "anonymous").Url, "https://example.invalid/feed" })
            await Reject(() => { PublicFeed.PreviewUpdate(url, "", true); return Task.CompletedTask; }, "INVALID_IMPORT");
    }),
    ("partial official catalog is rejected atomically", () =>
    {
        // The official source may never quietly drop a row it cannot parse.
        string mixed = link + "\nfuture-protocol://example.invalid";
        var ordinary = PublicFeed.PreviewUpdate(PublicFeed.Presets.First(p => p.Key == "anonymous").Url, mixed, true);
        Check(ordinary.Profiles.Count == 1 && PublicFeed.IsPartial(ordinary));
        try { PublicFeed.PreviewUpdate(RovunetFreeCatalog.Url, mixed, true); }
        catch (SafeError error) when (error.Code == "INVALID_IMPORT") { return Task.CompletedTask; }
        throw new Exception("EXPECTED_INVALID_IMPORT");
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
