# Freedom — Android encrypted download test (NOT production)
 
Six actual independently encrypted Rovunet native items extracted from the supplied Freedom strict feed; the original subscription URL, bearer token and plaintext configs are NOT committed.
 
- Index: `free/v2/catalog.json` — version 2, six entries.
- Signature: `free/v2/catalog.sig` — Base64 detached Ed25519 over the exact UTF-8 catalog bytes (including newline).
- Individual encrypted files: `free/v2/configs/<id>.json` — AES-256-GCM, random nonce and item ID.
- Debug public signer (raw 32-byte Ed25519 public key, Base64): `XOTdPAKvXNVn1amoxMG7hgZ6cYpwD+S/kmtVIuNgrUA=`.
- Debug AES key ID: `freedom-debug-20261123`. The AES key is **NOT in GitHub**; owner receives it separately.
- Synthetic packaging timestamps: `2026-11-23T16:29:44Z` to `2026-12-23T16:29:44Z`. These are ONLY test envelope timestamps, **NOT verified panel account expiration**.
- Shared donation group ID: `c620a96363e9ab1dacc2f778fb633924`. Six files share ONE pool, not six 50-GiB allowances.
- Content availability and server quota enforcement have NOT been tested on Android or against the live panel.
 
Client debug sequence: download index+signature; verify using the pinned debug public key; fetch EACH relative file and compare exact SHA-256 bytes with its signed entry; check envelope metadata against entry, decrypt with the separately supplied owner test AES key using AAD `rovunet-free-v2|id|donation_id|created_at|expires_at|key_id`; parse decrypted `{id, link}` using the existing `rovunet://` parser. Do real tunnel and payload tests locally. Never log plaintext links or place the debug AES key in a public release. Keep production Free v2 disabled until real timestamps, revocation/key broker and device tests are verified.
