using System.Globalization;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Rovunet.Core;

namespace Rovunet.Desktop;

/// <summary>
/// Rovunet Free v2 client, implementing free/v2/CONTRACT.md the same way the
/// Android client does:
///   catalog.json      raw UTF-8 {version:2, generated_at, entries[]}
///   catalog.sig       base64 Ed25519 detached signature over the exact bytes,
///                     including the trailing newline
///   configs/&lt;id&gt;.json  one AES-256-GCM envelope per entry
///   revoked.json      item/donation IDs excluded early
///
/// The signature is verified before any entry is read; each envelope must match
/// its signed entry, its SHA-256 and its GCM tag with the prescribed AAD. The
/// public key is pinned by the build, never taken from the downloaded index.
/// Nothing here logs a decrypted link.
/// </summary>
internal static class RovunetFreeCatalog
{
    internal const int QuotaGiB = 50;
    internal const int MaxCatalogBytes = 2 * 1024 * 1024;
    internal const int MaxEnvelopeBytes = 200 * 1024;
    internal const int MaxRevokedBytes = 64 * 1024;

    static readonly Regex IdPattern = new("^[0-9a-f]{32}$", RegexOptions.CultureInvariant);
    static readonly Regex KeyIdPattern = new("^[a-z0-9][a-z0-9_-]{0,63}$", RegexOptions.CultureInvariant);
    static readonly Regex CatalogPath = new(
        "^/msvpnco/[A-Za-z0-9_.-]+/(?:[A-Za-z0-9_-]+/)+(?:catalog|revoked)\\.(?:json|sig)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    static readonly Regex ConfigPath = new(
        "^/msvpnco/[A-Za-z0-9_.-]+/(?:[A-Za-z0-9_-]+/)+configs/[0-9a-f]{32}\\.json$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    static readonly byte[] SpkiPrefix = [0x30, 0x2a, 0x30, 0x05, 0x06, 0x03, 0x2b, 0x65, 0x70, 0x03, 0x21, 0x00];

    static string Metadata(string key) => Assembly.GetExecutingAssembly()
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == key)?.Value ?? "";

    // Test seam for the offline harnesses that link this file into their own
    // assembly. The shipped application never assigns these, so its trust
    // anchor is always the value compiled into the build.
    internal static string? OverrideAddress;
    internal static string? OverridePublicKey;
    internal static string? OverrideAesKey;

    /// <summary>Address compiled into this build; empty when the operator did not publish one.</summary>
    internal static string Url => (OverrideAddress ?? Metadata("RovunetFreeCatalogAddress")).Trim();

    static string PublicKeyText => (OverridePublicKey ?? Metadata("RovunetFreeEd25519PublicKey")).Trim();

    /// <summary>The quick path needs all three pinned values; otherwise it stays hidden.</summary>
    internal static bool Enabled
    {
        get
        {
            // Debug builds opt in explicitly with RovunetFreeDebug=1, which only the
            // gitignored local props file sets. A public package therefore cannot
            // carry the debug catalog keys even if they leak into the environment.
            if (!string.IsNullOrEmpty(OverrideAesKey))
                return Url.Length > 0 && PublicKey().Length == 32 && AesKey().Length == 32;
            if (Metadata("RovunetFreeDebug") != "1") return false;
            try { return Url.Length > 0 && PublicKey().Length == 32 && AesKey().Length == 32; }
            catch { return false; }
        }
    }

    static byte[] PublicKey()
    {
        byte[] raw;
        try { raw = Convert.FromBase64String(PublicKeyText); }
        catch (FormatException) { throw new SafeError("ROVUNET_FREE_UNVERIFIED"); }
        return raw.Length switch
        {
            32 => raw,
            44 when raw.AsSpan(0, SpkiPrefix.Length).SequenceEqual(SpkiPrefix) => raw[SpkiPrefix.Length..],
            _ => throw new SafeError("ROVUNET_FREE_UNVERIFIED")
        };
    }

    static byte[] AesKey()
    {
        try
        {
            byte[] key = Convert.FromBase64String((OverrideAesKey ?? Metadata("RovunetFreeAesKey")).Trim());
            return key.Length == 32 ? key : throw new SafeError("ROVUNET_FREE_UNVERIFIED");
        }
        catch (FormatException) { throw new SafeError("ROVUNET_FREE_UNVERIFIED"); }
    }

    // ---------------------------------------------------------------- addresses

    internal static string CatalogAddress(string value) => RequireAddress(value, CatalogPath);

    internal static string ConfigAddress(string value) => RequireAddress(value, ConfigPath);

    static string RequireAddress(string value, Regex allowed)
    {
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme != "https" || !string.Equals(uri.Authority, "raw.githubusercontent.com", StringComparison.Ordinal) ||
            uri.Query.Length != 0 || uri.Fragment.Length != 0 || !allowed.IsMatch(uri.AbsolutePath))
            throw new SafeError("ROVUNET_FREE_UNVERIFIED");
        return uri.AbsoluteUri;
    }

    // ------------------------------------------------------------------ entries

    internal sealed record Entry(string Id, string DonationId, string CreatedAt, string ExpiresAt,
        DateTimeOffset Expires, string KeyId, string Path, string Sha256);

    // -------------------------------------------------------------------- fetch

    internal static async Task<SubscriptionClient.Payload> Fetch(
        Func<Uri,CancellationToken,int,Task<SubscriptionClient.Payload>> fetch,
        CancellationToken cancellation, int proxyPort)
    {
        if (!Enabled) throw new SafeError("ROVUNET_FREE_UNAVAILABLE");
        var key = PublicKey();
        var aes = AesKey();
        string origin = CatalogAddress(Url);

        var payload = await fetch(SubscriptionUrls.Validate(origin), cancellation, proxyPort);
        var detached = await fetch(SubscriptionUrls.Validate(origin.Replace("catalog.json", "catalog.sig", StringComparison.Ordinal)), cancellation, proxyPort);
        if (payload.Body.Length > MaxCatalogBytes || !payload.Body.EndsWith('\n'))
            throw new SafeError("ROVUNET_FREE_UNVERIFIED");

        byte[] body = Encoding.UTF8.GetBytes(payload.Body);
        byte[] signature;
        try { signature = Convert.FromBase64String(detached.Body.Trim()); }
        catch (FormatException) { throw new SafeError("ROVUNET_FREE_UNVERIFIED"); }
        if (signature.Length != 64) throw new SafeError("ROVUNET_FREE_UNVERIFIED");
        var verifier = new Ed25519Signer();
        verifier.Init(false, new Ed25519PublicKeyParameters(key, 0));
        verifier.BlockUpdate(body, 0, body.Length);
        if (!verifier.VerifySignature(signature)) throw new SafeError("ROVUNET_FREE_UNVERIFIED");

        var entries = ParseCatalog(payload.Body, DateTimeOffset.UtcNow);
        string baseAddress = origin[..(origin.LastIndexOf('/') + 1)];
        var revoked = await FetchRevoked(fetch, baseAddress, cancellation, proxyPort);

        var links = new List<string>();
        foreach (var entry in entries)
        {
            cancellation.ThrowIfCancellationRequested();
            if (entry.Expires <= DateTimeOffset.UtcNow) continue;
            if (revoked.Contains(entry.Id) || revoked.Contains(entry.DonationId)) continue;
            string address;
            try { address = ConfigAddress(baseAddress + entry.Path); }
            catch (SafeError) { continue; }
            SubscriptionClient.Payload envelope;
            try { envelope = await fetch(SubscriptionUrls.Validate(address), cancellation, proxyPort); }
            catch (SafeError) { continue; }
            catch (HttpRequestException) { continue; }
            string? link;
            try { link = Decrypt(envelope.Body, entry, aes); }
            catch (SafeError) { link = null; }
            if (link != null) links.Add(link);
        }
        if (links.Count == 0) throw new SafeError("ROVUNET_FREE_UNAVAILABLE");
        // Every item in one catalog shares a single 50 GiB donation group, so the
        // order is shuffled instead of always serving the same path first.
        var ordered = links.OrderBy(_ => Random.Shared.Next()).ToList();
        return new(string.Join('\n', ordered), payload.Date, "");
    }

    static async Task<HashSet<string>> FetchRevoked(
        Func<Uri,CancellationToken,int,Task<SubscriptionClient.Payload>> fetch,
        string baseAddress, CancellationToken cancellation, int proxyPort)
    {
        try
        {
            var document = await fetch(SubscriptionUrls.Validate(CatalogAddress(baseAddress + "revoked.json")), cancellation, proxyPort);
            if (Encoding.UTF8.GetByteCount(document.Body) > MaxRevokedBytes) return [];
            using var json = JsonDocument.Parse(document.Body);
            if (json.RootElement.ValueKind != JsonValueKind.Array) return [];
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in json.RootElement.EnumerateArray())
            {
                string id = item.GetString() ?? "";
                if (!IdPattern.IsMatch(id)) return [];
                set.Add(id);
            }
            return set;
        }
        catch (SafeError) { return []; }
        catch (JsonException) { return []; }
        catch (HttpRequestException) { return []; }
    }

    // -------------------------------------------------------------- verification

    internal static List<Entry> ParseCatalog(string catalogJson, DateTimeOffset now)
    {
        using var document = JsonDocument.Parse(catalogJson);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || root.GetProperty("version").GetInt32() != 2)
            throw new SafeError("ROVUNET_FREE_UNVERIFIED");
        var generated = ParseTime(root.GetProperty("generated_at").GetString());
        if (generated > now.AddMinutes(5)) throw new SafeError("ROVUNET_FREE_UNVERIFIED");
        var entriesJson = root.GetProperty("entries");
        if (entriesJson.ValueKind != JsonValueKind.Array || entriesJson.GetArrayLength() > 500)
            throw new SafeError("ROVUNET_FREE_UNVERIFIED");

        var entries = new List<Entry>(entriesJson.GetArrayLength());
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string last = "";
        foreach (var node in entriesJson.EnumerateArray())
        {
            var entry = ParseEntry(node);
            // The contract sorts entries by ID; a reorder is treated as tampering.
            if (entries.Count > 0 && string.CompareOrdinal(entry.Id, last) <= 0)
                throw new SafeError("ROVUNET_FREE_UNVERIFIED");
            last = entry.Id;
            if (!seen.Add(entry.Id)) throw new SafeError("ROVUNET_FREE_UNVERIFIED");
            entries.Add(entry);
        }
        return entries;
    }

    static Entry ParseEntry(JsonElement node)
    {
        string id = node.GetProperty("id").GetString() ?? "";
        string donation = node.GetProperty("donation_id").GetString() ?? "";
        string keyId = node.GetProperty("key_id").GetString() ?? "";
        string created = node.GetProperty("created_at").GetString() ?? "";
        string expires = node.GetProperty("expires_at").GetString() ?? "";
        if (!IdPattern.IsMatch(id) || !IdPattern.IsMatch(donation) || !KeyIdPattern.IsMatch(keyId))
            throw new SafeError("ROVUNET_FREE_UNVERIFIED");
        if (node.GetProperty("quota_gib").GetInt32() != QuotaGiB) throw new SafeError("ROVUNET_FREE_UNVERIFIED");
        var createdAt = ParseTime(created);
        var expiresAt = ParseTime(expires);
        if (expiresAt - createdAt != TimeSpan.FromDays(30)) throw new SafeError("ROVUNET_FREE_UNVERIFIED");
        if (node.GetProperty("path").GetString() != "configs/" + id + ".json")
            throw new SafeError("ROVUNET_FREE_UNVERIFIED");
        string sha = node.GetProperty("sha256").GetString() ?? "";
        if (sha.Length != 64 || !sha.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f'))
            throw new SafeError("ROVUNET_FREE_UNVERIFIED");
        return new(id, donation, created, expires, expiresAt, keyId, "configs/" + id + ".json", sha);
    }

    static DateTimeOffset ParseTime(string? raw) =>
        !string.IsNullOrEmpty(raw) && DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed.ToUniversalTime()
            : throw new SafeError("ROVUNET_FREE_UNVERIFIED");

    internal static string Decrypt(string envelopeJson, Entry entry, byte[] aesKey, DateTimeOffset? now = null)
    {
        if (aesKey.Length != 32) throw new SafeError("ROVUNET_FREE_UNVERIFIED");
        if (entry.Expires <= (now ?? DateTimeOffset.UtcNow)) throw new SafeError("ROVUNET_FREE_EXPIRED");
        byte[] raw = Encoding.UTF8.GetBytes(envelopeJson);
        if (raw.Length is < 1 or > MaxEnvelopeBytes) throw new SafeError("ROVUNET_FREE_UNVERIFIED");
        // The signed hash covers the exact ciphertext-file bytes.
        if (!string.Equals(Convert.ToHexString(SHA256.HashData(raw)).ToLowerInvariant(), entry.Sha256, StringComparison.Ordinal))
            throw new SafeError("ROVUNET_FREE_UNVERIFIED");

        using var document = JsonDocument.Parse(envelopeJson);
        var env = document.RootElement;
        if (env.GetProperty("version").GetInt32() != 2) throw new SafeError("ROVUNET_FREE_UNVERIFIED");
        if (env.GetProperty("algorithm").GetString() != "AES-256-GCM") throw new SafeError("ROVUNET_FREE_UNVERIFIED");
        if (env.GetProperty("id").GetString() != entry.Id ||
            env.GetProperty("donation_id").GetString() != entry.DonationId ||
            env.GetProperty("created_at").GetString() != entry.CreatedAt ||
            env.GetProperty("expires_at").GetString() != entry.ExpiresAt ||
            env.GetProperty("key_id").GetString() != entry.KeyId ||
            env.GetProperty("quota_gib").GetInt32() != QuotaGiB)
            throw new SafeError("ROVUNET_FREE_UNVERIFIED");

        byte[] nonce, ciphertext;
        try
        {
            nonce = Convert.FromBase64String(env.GetProperty("nonce").GetString() ?? "");
            ciphertext = Convert.FromBase64String(env.GetProperty("ciphertext").GetString() ?? "");
        }
        catch (FormatException) { throw new SafeError("ROVUNET_FREE_UNVERIFIED"); }
        if (nonce.Length != 12 || ciphertext.Length < 17) throw new SafeError("ROVUNET_FREE_UNVERIFIED");

        byte[] aad = Encoding.UTF8.GetBytes(
            $"rovunet-free-v2|{entry.Id}|{entry.DonationId}|{entry.CreatedAt}|{entry.ExpiresAt}|{entry.KeyId}");
        byte[] plain = new byte[ciphertext.Length - 16];
        try
        {
            using var gcm = new AesGcm(aesKey, 16);
            gcm.Decrypt(nonce, ciphertext.AsSpan(0, ciphertext.Length - 16), ciphertext.AsSpan(ciphertext.Length - 16), plain, aad);
        }
        catch (CryptographicException) { throw new SafeError("ROVUNET_FREE_UNVERIFIED"); }

        using var item = JsonDocument.Parse(plain);
        string link = item.RootElement.GetProperty("link").GetString() ?? "";
        if (link.Length is < 1 or > 65536 || link.Contains('\n') || link.Contains('\r'))
            throw new SafeError("ROVUNET_FREE_UNVERIFIED");
        return link;
    }
}
