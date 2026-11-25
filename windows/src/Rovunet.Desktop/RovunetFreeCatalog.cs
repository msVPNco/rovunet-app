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
    internal static string? OverrideKeyId;

    /// <summary>
    /// Debug builds may read the shared debug values from a local, gitignored file
    /// instead of compiling them into the binary, so no package ever carries the
    /// key. Release builds compile this path out and never look at the file.
    /// </summary>
    internal static string DebugSecretsPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rovunet", "DebugSecrets", "free-v2.local.json");

    static readonly Lazy<(string KeyId, string Aes, string Public, string Address)> DebugSecrets = new(ReadDebugSecrets);

    static (string KeyId, string Aes, string Public, string Address) ReadDebugSecrets()
    {
#if DEBUG
        try
        {
            string path = DebugSecretsPath;
            if (File.Exists(path) && new FileInfo(path).Length is > 0 and <= 8192)
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                string Value(string name) => document.RootElement.TryGetProperty(name, out var node) &&
                                             node.ValueKind == JsonValueKind.String
                    ? (node.GetString() ?? "").Trim() : "";
                return (Value("key_id"), Value("aes_key_base64"), Value("public_key_base64"), Value("catalog_address"));
            }
        }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException
                                          or ArgumentException or NotSupportedException) { }
#endif
        return ("", "", "", "");
    }

    static string Pick(params string?[] values)
    {
        foreach (var value in values) if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
        return "";
    }

    /// <summary>Address compiled into this build; empty when the operator did not publish one.</summary>
    internal static string Url => Pick(OverrideAddress, DebugSecrets.Value.Address, Metadata("RovunetFreeCatalogAddress"));

    static string PublicKeyText => Pick(OverridePublicKey, DebugSecrets.Value.Public, Metadata("RovunetFreeEd25519PublicKey"));

    // The key id names which AES key an envelope was sealed with. When the build
    // pins one, envelopes that name a different key are skipped instead of being
    // tried against the wrong secret; empty means the contract key is unregistered.
    static string KeyIdText => Pick(OverrideKeyId, DebugSecrets.Value.KeyId, Metadata("RovunetFreeKeyId"));

    /// <summary>The quick path needs all three pinned values; otherwise it stays hidden.</summary>
    internal static bool Enabled
    {
        get
        {
            // Debug builds opt in explicitly with RovunetFreeDebug=1, which only the
            // gitignored local props file sets, or with a complete local Debug
            // secrets file. A public package therefore cannot carry the debug
            // catalog keys even if they leak into the environment.
            if (!string.IsNullOrEmpty(OverrideAesKey))
                return Url.Length > 0 && PublicKey().Length == 32 && AesKey().Length == 32;
            var local = DebugSecrets.Value;
            bool complete = local.Aes.Length > 0 && local.Public.Length > 0 && local.Address.Length > 0;
            if (!complete && Metadata("RovunetFreeDebug") != "1") return false;
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
            byte[] key = Convert.FromBase64String(Pick(OverrideAesKey, DebugSecrets.Value.Aes, Metadata("RovunetFreeAesKey")));
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

    // Debug-only seam: receives "<id prefix>:<stage>" per catalog entry so the
    // live harness can say why a config was skipped. Never carries a link, key,
    // hash or plaintext, and the shipped application leaves it unset.
    internal static Action<string>? Trace;

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

        var links = new List<(string Group, string Link)>();
        string pinnedKeyId = KeyIdText;
        void Report(Entry entry, string stage) => Trace?.Invoke(entry.Id[..8] + ":" + stage);
        foreach (var entry in entries)
        {
            cancellation.ThrowIfCancellationRequested();
            if (entry.Expires <= DateTimeOffset.UtcNow) { Report(entry, "expired"); continue; }
            if (revoked.Contains(entry.Id) || revoked.Contains(entry.DonationId)) { Report(entry, "revoked"); continue; }
            if (pinnedKeyId.Length > 0 && !string.Equals(entry.KeyId, pinnedKeyId, StringComparison.Ordinal)) { Report(entry, "key-id"); continue; }
            string address;
            try { address = ConfigAddress(baseAddress + entry.Path); }
            catch (SafeError) { Report(entry, "address"); continue; }
            SubscriptionClient.Payload envelope;
            try { envelope = await fetch(SubscriptionUrls.Validate(address), cancellation, proxyPort); }
            catch (SafeError) { Report(entry, "fetch"); continue; }
            catch (HttpRequestException) { Report(entry, "fetch"); continue; }
            string stage = TryDecrypt(envelope.Body, entry, aes, out string? link);
            Report(entry, stage.Length == 0 ? "ok" : stage);
            if (link != null) links.Add((entry.DonationId, link));
        }
        if (links.Count == 0) throw new SafeError("ROVUNET_FREE_UNAVAILABLE");
        // Every item in one donation group shares a single 50 GiB budget, so the
        // contract picks a random healthy group first and a random config inside
        // it; six paths of one donor must never look like six independent donors.
        var ordered = links
            .GroupBy(item => item.Group, StringComparer.Ordinal)
            .OrderBy(_ => Random.Shared.Next())
            .SelectMany(group => group.OrderBy(_ => Random.Shared.Next()))
            .Select(item => item.Link)
            .ToList();
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
        string stage = TryDecrypt(envelopeJson, entry, aesKey, out string? link, now);
        if (stage.Length > 0)
            throw new SafeError(stage == "expired" ? "ROVUNET_FREE_EXPIRED" : "ROVUNET_FREE_UNVERIFIED");
        return link!;
    }

    /// <summary>
    /// Same checks as <see cref="Decrypt"/>, but reports which stage refused the
    /// envelope instead of varying only by error code. The stage names carry no
    /// key, hash or plaintext material, so a debug run can say whether a catalog
    /// failed on its hash, its metadata, the GCM tag or the inner id.
    /// </summary>
    internal static string TryDecrypt(string envelopeJson, Entry entry, byte[] aesKey, out string? link,
        DateTimeOffset? now = null)
    {
        link = null;
        if (aesKey.Length != 32) return "key";
        if (entry.Expires <= (now ?? DateTimeOffset.UtcNow)) return "expired";
        byte[] raw = Encoding.UTF8.GetBytes(envelopeJson);
        if (raw.Length is < 1 or > MaxEnvelopeBytes) return "size";
        // The signed hash covers the exact ciphertext-file bytes.
        if (!string.Equals(Convert.ToHexString(SHA256.HashData(raw)).ToLowerInvariant(), entry.Sha256, StringComparison.Ordinal))
            return "hash";

        using var document = JsonDocument.Parse(envelopeJson);
        var env = document.RootElement;
        if (env.GetProperty("version").GetInt32() != 2) return "meta";
        if (env.GetProperty("algorithm").GetString() != "AES-256-GCM") return "meta";
        if (env.GetProperty("id").GetString() != entry.Id ||
            env.GetProperty("donation_id").GetString() != entry.DonationId ||
            env.GetProperty("created_at").GetString() != entry.CreatedAt ||
            env.GetProperty("expires_at").GetString() != entry.ExpiresAt ||
            env.GetProperty("key_id").GetString() != entry.KeyId ||
            env.GetProperty("quota_gib").GetInt32() != QuotaGiB)
            return "meta";

        byte[] nonce, ciphertext;
        try
        {
            nonce = Convert.FromBase64String(env.GetProperty("nonce").GetString() ?? "");
            ciphertext = Convert.FromBase64String(env.GetProperty("ciphertext").GetString() ?? "");
        }
        catch (FormatException) { return "encoding"; }
        if (nonce.Length != 12 || ciphertext.Length < 17) return "encoding";

        byte[] aad = Encoding.UTF8.GetBytes(
            $"rovunet-free-v2|{entry.Id}|{entry.DonationId}|{entry.CreatedAt}|{entry.ExpiresAt}|{entry.KeyId}");
        byte[] plain = new byte[ciphertext.Length - 16];
        try
        {
            using var gcm = new AesGcm(aesKey, 16);
            gcm.Decrypt(nonce, ciphertext.AsSpan(0, ciphertext.Length - 16), ciphertext.AsSpan(ciphertext.Length - 16), plain, aad);
        }
        catch (CryptographicException) { return "auth"; }

        using var item = JsonDocument.Parse(plain);
        // The plaintext carries the original panel item id next to the link; it is
        // deliberately not the random envelope id, so only its shape is checked.
        if (item.RootElement.GetProperty("id").GetString() is not { Length: >= 1 and <= 200 })
            return "inner-id";
        string value = item.RootElement.GetProperty("link").GetString() ?? "";
        if (value.Length is < 1 or > 65536 || value.Contains('\n') || value.Contains('\r'))
            return "link";
        link = value;
        return "";
    }
}
