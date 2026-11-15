# MAINT36 · 0.1.4-preview.9

This local maintenance source supersedes the unpublished Beta35 candidate.
Published tags, the frozen Beta35 artifacts and the owner installation remain
unchanged. A local test result is not a public-release or TUN acceptance claim.

## Changes

- Subscription input has one translated Paste action. Managed-source addresses
  stay hidden; consent, review-before-save and output Copy actions remain.
- Accurate HTTPS probes divide the existing round across the configured target
  and enabled alternatives (Cloudflare, GitHub, example.com), after accounting
  for engine setup. All use the same local proxy and normal TLS validation.
  Fallback-disabled probes retain their single-target allowance.
- Safe stage/deadline metadata distinguishes caller cancellation, round/target
  deadlines and TLS stream closure. A cancelled fresh result cannot inherit a
  previous successful verification; the established connection evidence remains.
  Inconclusive tests are amber, without relaxing changed-key/authentication errors.
- Local resolver discovery selects only IPv4 while retaining the existing
  interface priority, validity rules and deduplication. Manual/system IPv6 and
  subscription Local overrides are not disabled or erased. If no IPv4 is found,
  the prior field remains and a translated explanation appears.

## Development evidence

- 393 portable-domain cases passed. The Local UI fixture passed146 assertions
  and96 real pointer clicks across four languages, three themes and both forms,
  including dual-stack, IPv6-only and IPv4-only discovery without network writes.
- Clipboard UI passed117 checks/51 pointer clicks, including a native Windows
  text-clipboard paste with restoration. An earlier non-text clipboard run was
  safely skipped rather than replaced.
- Six fresh production IPC precise probes (Mieru TCP/UDP, Naive SOCKS/SSH and two
  SSH variants) returned strict HTTPS200/204 within their18/25-second overall
  allowances. Separate Mieru SOCKS controls returned GitHub HTTPS200; older
  target failures alone were not evidence of protocol/server failure.
- Six cancellation assertions passed through real Agent IPC after prior HTTPS
  success: each cancelled fresh reply was unverified while prior status retained
  the established verification. These receipts identify development DLL hashes;
  they must not be presented as installer-byte receipts.
- The owner confirmed Windows and Android Slipstream SSH/SOCKS website traffic
  after selecting Local. Historical companion404 is not the current server state.
  Independent TUIC and US WireGuard acceptance is recorded separately.

## Boundaries

Quick tests still exclude DNS tunnels and DNS-containing chains. HTTPS remains
accurate-only. Mieru UDP has no authenticated quick implementation and explicitly
reports non-applicability rather than inventing a TCP or HTTPS quick pass.
Existing concurrent WireGuard pair restrictions, certificate binding, TLS/SSH
checks and restricted-account controls remain. No owner store import, host TUN,
route, DNS, firewall, SCM or server mutation belongs to this maintenance stage.
Exact-source package results and remaining live limitations accompany delivery.
