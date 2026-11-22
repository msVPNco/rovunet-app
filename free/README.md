# Rovunet Free — donated public-access catalog (draft)

This **opt-in** channel is independent of private panel subscriptions and of
third-party public sources (Mahsa, Anonymous). The GitHub repository is a
**distribution catalog**, not a trusted traffic meter or a server credential store.

## Contract

- A donation is an **isolated account**, not another user's subscription.
- The panel provisions a **shared 50 GiB quota** and an expiry **30 × 24 hours
  after account creation**; neither resets when the candidate is uploaded.
- Only paths enforcing **server-side traffic accounting** are exported. The
  number of links is determined by the running eligible cores, not assumed to
  be seven/eight.
- The operator creates the account via Users > **Donated config** or
  `rovunet users --action donate`. When metered cores are available,
  the panel writes an owner-readable candidate to
  `/etc/rovunet/donations/<gift-user>.json` (0600).
- Review the candidate before committing to `free/donations/<id>.json` in a
  **separate PR**. **The links are intentionally public credentials**, not
  encrypted secrets. Do not include the panel config, original user's private
  feed, signing key, SSH keys or internal addresses.
- A publisher turns candidates into `free/catalog.json` (version 1,
  UTC `generated_at`, `donations[]` with timestamps, quota and links) and
  `free/catalog.sig` (Base64 Ed25519 signature of exact JSON bytes).
  No client should use this automatic source without a pinned, verified
  Ed25519 signing public key.

## Activation / safe deployment

1. Generate a **new, dedicated Ed25519 key** under the owner's control,
   independent of the Rovunet panel release signer. **Never commit the
   private PEM to GitHub or send it in a chat.**
2. In the GitHub repository's Actions secrets, set
   `ROVUNET_FREE_SIGNING_KEY_PEM_B64` to the Base64 of the full *private*
   PEM bytes; keep an offline recovery copy. The workflow refuses unsigned
   publication if the secret is absent or invalid.
3. Pin the matching **raw 32-byte Ed25519 public key encoded as Base64** in
   trusted app builds (Android Gradle property
   `rovunetFreePublicKeyBase64`). Android keeps Rovunet Free disabled
   until a valid key is embedded. A public key downloaded from the same
   catalog must **never** substitute for the pinned key.
4. Review the PR and CI, test signature verification on actual devices,
   then approve the app releases. Do not silently add this to a production
   build or downgrade verification to HTTPS-only.
5. When a donor account is exhausted early, an operator must remove its
   candidate and revoke/retire that account at the server; GitHub cannot
   infer real-time shared-byte usage.

## Expiry and failure behavior

- A daily GitHub Actions job signs the current active catalog and removes
  expired donation candidates from the current branch. Deleted Git files
  **remain in repository history**; revoking the server account is authoritative.
- **Clients must independently** check exact `created_at + 30 days` and
  `expires_at`, so a stale GitHub cache or missed scheduled workflow
  does not revive an expired link.
- If signature verification, parsing, transport validation or fetching
  fails, clients retain the last-good catalog and do **not** perform a
  destructive update.
- Public profile limits are **shared across every downloader** of a donation,
  not 50 GiB per app user. Retire a contribution if the aggregate quota is
  exhausted early, even if the 30-day period remains.
- Encrypted, base64-encoded or obscured links in a public repo are **not
  confidential** if every app contains their decryption key. The signature is
  for authenticity, not secrecy.
- Automatic refresh is opt-in and must not change third-party public source
  policy. Android's official refresh is scheduled independently from private
  subscription updates.

## Publisher validation

```bash
python3 -m unittest discover -s free/tools -p 'test_*.py' -v
python3 free/tools/publish.py --check
```

The publish step requires an Actions secret, runs the same tests, and writes
both catalog and signature before making a single Git commit. At present this
is a **draft contract**, not a live published Free source. Android, desktop and
macOS client acceptance and production key custody remain release gates.
