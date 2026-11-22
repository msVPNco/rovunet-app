using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Rovunet.Core;

namespace Rovunet.Desktop;

/// <summary>
/// Trust anchor is embedded by the reviewed Windows build, never fetched from GitHub.
/// Public catalogs contain intentionally shared account credentials, not private feeds.
/// </summary>
internal static class RovunetFreeCatalog
{
    internal const string Url = "https://raw.githubusercontent.com/msVPNco/rovunet-app/main/free/catalog.json";
    internal const string SigUrl = "https://raw.githubusercontent.com/msVPNco/rovunet-app/main/free/catalog.sig";

    static string Key => Assembly.GetExecutingAssembly()
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == "RovunetFreeEd25519PublicKey")?.Value ?? "";

    static byte[]? PinnedKey()
    {
        try
        {
            byte[] value = Convert.FromBase64String(Key);
            return value.Length == 32 ? value : null;
        }
        catch (FormatException) { return null; }
    }

    internal static bool Enabled => PinnedKey() != null;

    internal static async Task<SubscriptionClient.Payload> Fetch(
        Func<Uri,CancellationToken,int,Task<SubscriptionClient.Payload>> fetch,
        CancellationToken cancellation, int proxyPort)
    {
        var key = PinnedKey();
        if (key == null) throw new SafeError("FREE_SOURCE_NOT_READY");
        var payload = await fetch(SubscriptionUrls.Validate(Url), cancellation, proxyPort);
        var detached = await fetch(SubscriptionUrls.Validate(SigUrl), cancellation, proxyPort);
        byte[] body = Encoding.UTF8.GetBytes(payload.Body);
        if (body.Length > 8 * 1024 * 1024) throw new SafeError("IMPORT_TOO_LARGE");
        byte[] signature;
        try { signature = Convert.FromBase64String(detached.Body.Trim()); }
        catch (FormatException) { throw new SafeError("FREE_SIGNATURE_INVALID"); }
        if (signature.Length != 64) throw new SafeError("FREE_SIGNATURE_INVALID");
        var verifier = new Ed25519Signer();
        verifier.Init(false, new Ed25519PublicKeyParameters(key, 0));
        verifier.BlockUpdate(body, 0, body.Length);
        if (!verifier.VerifySignature(signature)) throw new SafeError("FREE_SIGNATURE_INVALID");
        return new SubscriptionClient.Payload(ActiveLinks(payload.Body, DateTimeOffset.UtcNow), payload.Date, "");
    }

    internal static string ActiveLinks(string json, DateTimeOffset now)
    {
        using var document = JsonDocument.Parse(json);
        var catalog = document.RootElement;
        if (catalog.GetProperty("version").GetInt32() != 1) throw new SafeError("FREE_CATALOG_VERSION");
        var generated = Parse(catalog.GetProperty("generated_at").GetString());
        if (generated > now.AddMinutes(5) || generated < now.AddDays(-7)) throw new SafeError("FREE_CATALOG_STALE");
        var donations = catalog.GetProperty("donations");
        if (donations.ValueKind != JsonValueKind.Array || donations.GetArrayLength() > 500)
            throw new SafeError("FREE_DONATION_INVALID");
        var ids = new HashSet<string>();
        var links = new List<string>();
        foreach (var donor in donations.EnumerateArray())
        {
            string id = donor.GetProperty("donation_id").GetString() ?? "";
            if (id.Length != 32 || !id.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f') || !ids.Add(id))
                throw new SafeError("FREE_DONATION_INVALID");
            var created = Parse(donor.GetProperty("created_at").GetString());
            var expires = Parse(donor.GetProperty("expires_at").GetString());
            if (expires-created != TimeSpan.FromDays(30) || donor.GetProperty("quota_gb").GetInt32() != 50 || created > now.AddMinutes(5))
                throw new SafeError("FREE_DONATION_INVALID");
            if (expires <= now) continue;
            var rows = donor.GetProperty("items");
            if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > 128)
                throw new SafeError("IMPORT_TOO_LARGE");
            foreach(var row in rows.EnumerateArray())
            {
                var link = row.GetProperty("link").GetString() ?? "";
                if (link.Length > 65536 || link.Contains('\n') || link.Contains('\r') || links.Count+1>10000)
                    throw new SafeError("IMPORT_TOO_LARGE");
                links.Add(link);
            }
        }
        return string.Join('\n',links);
    }

    static DateTimeOffset Parse(string? raw)
    {
        if (string.IsNullOrEmpty(raw) ||
            !DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            throw new SafeError("FREE_DONATION_INVALID");
        return parsed.ToUniversalTime();
    }
}
