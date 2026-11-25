# Rovunet Free v2 — integration contract (DRAFT / NOT LIVE)

Repository: https://github.com/msVPNco/rovunet-app

After review and merge, canonical contract: https://github.com/msVPNco/rovunet-app/blob/main/free/v2/CONTRACT.md

**Inactive until owner-approved keys, broker and client releases.** These URLs DO NOT exist yet:
- Signed index: https://raw.githubusercontent.com/msVPNco/rovunet-app/main/free/v2/catalog.json
- Detached signature: https://raw.githubusercontent.com/msVPNco/rovunet-app/main/free/v2/catalog.sig
- Individual encrypted config: https://raw.githubusercontent.com/msVPNco/rovunet-app/main/free/v2/configs/ITEM_ID.json

## Model and donation pool

The manually imported Freedom subscription has six connection configurations. The original .windows.json URL is a sensitive bearer capability; do not store it, its token, or a plaintext configuration in GitHub. The owner downloads its feed on a trusted machine OUTSIDE the repository and extracts each item separately. Preserve the existing native rovunet:// payload inside real encryption; that URL scheme is an encoding, not confidentiality.

Exactly one donation_id (32 lowercase hex), one server-side 50 GiB aggregate limit, and original account-created 30 x 24-hour expiry are shared across all six encrypted files. These cannot be authenticated from the URL or screenshot; require the panel donation receipt and authoritative accounting state. Never reset expiration when uploading or multiply the 50 GiB by six.

Each file stores version=2, random id, donation_id, created_at, expires_at, quota_gib=50, key_id, algorithm=AES-256-GCM, base64 nonce and base64 ciphertext (including GCM tag). Every item has a fresh 12-byte random nonce and random 16-byte ID. Authenticated associated data is:
rovunet-free-v2|ID|DONATION_ID|CREATED_AT|EXPIRES_AT|KEY_ID

Only the encrypted plaintext includes {"id":"original item id","link":"original rovunet://..."} or supported standard URI.

## Signed index

Catalog JSON contains version=2, generated_at, entries[]. Each entry includes id, donation_id, created_at, expires_at, quota_gib, key_id, a safe relative encrypted path and SHA256 of the exact ciphertext-file bytes. Entries are sorted by ID. An expired item is omitted; free/v2/revoked.json is an array of item IDs or donation IDs to exclude early. Revoking a donation ID disables all its paths.

Signature is Ed25519 over the exact UTF-8 catalog JSON including the trailing newline; catalog.sig contains the base64 detached signature. The client pins the FREE signing public key in its verified build. This key MUST differ from the panel production-release signing key. Never accept a downloaded index-supplied substitute public key.

The Git repository is NOT a live traffic-metering source. Exhaustion of the 50 GiB budget before expiry requires explicit group revocation and server-side enforcement. Deleted ciphertext remains in git history; never regard repository deletion as reliable revocation.

## Android, Windows, macOS, iOS client contract

1. An opt-in official Rovunet Free v2 source separate from manual Subscriptions, Mahsa and Anonymous. Never remove the user's existing manually imported Freedom group.
2. Fetch catalog.json plus catalog.sig; authenticate with a pinned Ed25519 public key BEFORE processing. Validate schema, uniqueness, sizes, path traversal, timestamp UTC, 30-day term, group binding, expiry and a signed monotonic update policy. A moving GitHub branch is not itself freshness proof; use a trusted last-good state and app-side expiration.
3. Fetch each encrypted file separately over HTTPS; verify exact SHA256, then check the envelope metadata against the signed entry. Decrypt only with valid AES-256-GCM tag and the prescribed AAD. Never log plaintext credentials.
4. Decrypted links go through the existing validated native rovunet:// and standard URI parsers; unsupported transports are skipped honestly and never silently downgraded.
5. Check actual authenticated tunnel handshake AND a bounded payload probe, not only TCP ping. Cache network-specific results with cooldown and capped retries; respect battery, data and privacy.
6. Pick a random HEALTHY DONATION GROUP first, then a random healthy config inside that group, to prevent a six-path donor from receiving six times the traffic of a single-path donor. Account/group expiry, exhaustion or authoritative revocation excludes every group member.
7. Failed fetch leaves last verified, UNEXPIRED state intact. A newer authenticated removal can retire official Free entries but never unrelated user-created collections.

## The limit of client-only encryption

A shared AES key embedded in Android or Windows apps can be extracted. Real app-only access would require a separate owner-controlled authenticated key broker, per-install authorization/attestation where available, short-lived grants, rate limits and prompt server-side revocation. The broker IS NOT implemented by this pull request. Until a broker and final client builds pass acceptance, do NOT enable official Free v2 in production.

## Owner operation and safety

Requires Python 3.12+ with cryptography >=46,<47. Keep raw source JSON and 32-byte AES secret OUTSIDE the repo, with mode 0600 on Linux. Source dates must come from the original panel donation receipt. Generate a fresh random 32-hex donation ID and a key-id registered in the future broker. Sample command uses FAKE placeholders and MUST NOT be run unchanged:

python3 free/tools/v2.py import --input /private/freedom.windows.json --key-file /private/free-aes.key --key-id broker-key-001 --donation-id 0123456789abcdef0123456789abcdef --created-at 2026-11-23T00:00:00Z --expires-at 2026-12-23T00:00:00Z

Importer refuses nonempty output folders; review encrypted files in a separate PR, not raw subscriptions. Run:
python3 free/tools/v2.py check

Only the manually dispatched workflow may publish the signed v2 index after the owner installs Actions secret ROVUNET_FREE_SIGNING_KEY_PEM_B64 (Base64 of the dedicated Ed25519 PKCS8 private PEM). Never send the private signing key or AES key into GitHub commits or this chat. The scheduled workflow validates only. The legacy plaintext v1 publisher remains as historic code but its public GitHub Actions publishing path is disabled; never interpret v1 as v2.

## Integration acceptance gate

Client agent must provide exact final Android/Windows/macOS/iOS build hashes, key-pinning tests, malformed/ciphertext-tamper rejection, shared-quota and group-revocation tests, real-tunnel validation, network-specific random selection and retry tests. The server must confirm the donation's real 50 GiB limit/30-day start and that all six paths share one account. This PR does not claim a real Freedom upload, a production encryption key, an active broker or a working Free app release.
