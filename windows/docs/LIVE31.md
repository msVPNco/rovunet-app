# Local Windows 0.1.4-preview.5 / LIVE31

## Interface and cancellation

The ping button retains its location and switches to a white square on a red
background while either quick or accurate collection testing runs. Its accessible
label is the localized Stop action; cancellation restores the ping control. The
four UI languages and three themes retain their layout and centered flags.

The sticky hero now has a fixed 161-pixel flow slot. Its visible card contracts
from 148 to 88 pixels without changing scrollHeight. This removes the feedback
between shrinking height, browser scroll anchoring and scrollTop near 60 pixels.
No text transform or vertical type scaling is introduced. Frame samples cover
both scrolling directions, English/Persian and compact/wide widths. External
mouse-wheel input is suppressed only during controlled fixture sampling, then
restored. Actual pointer clicks exercise start/stop.

## Slipstream client paths

Connect acquires its public certificate before DNS pool verification. It shows a
cancellable Connecting state during acquisition. Explicit and automatic scanner
tunnel tests use that same acquisition path instead of opening a file picker.
Concurrent candidates serialize the initial fetch and reuse the saved result.
A detached chain layer resolves its subscription only when the raw-profile match
is unique. Ambiguous sources are not guessed. Manual certificate priority,
same-origin HTTPS, account/period/item/domain bindings, DER SHA256 checks, DPAPI
rollback and preservation of a different existing pin remain enforced.

Certificate acquisition is not a server account session. The Panel currently has
no Slipstream start/heartbeat/stop/device-ID/TTL API. No such endpoint is invented.
Neither a local listener nor QUIC readiness alone proves backend authentication
or internet access. The existing exact Windows Slipstream binary remains pinned.

## Actual owner-network observations on preview.4

Two authenticated quick attempts were made on each of seven private owner
profiles. No external HTTPS request was made in that quick-test phase:

| Account alias | Protocol | Quick result, ms | Separate accurate HTTPS |
| --- | --- | --- | --- |
| A | HY2 | timeout / timeout | failed, TLS-stage result |
| A | WireGuard | 203 / 191 | passed, 976 ms |
| A | AmneziaWG | 198 / 209 | passed, 829 ms |
| B | HY2 | 431 / 1041 | passed, 794 ms |
| B | WireGuard | timeout / timeout | timeout |
| B | AmneziaWG | timeout / 202 | passed, 1092 ms |
| B | TUIC | timeout / timeout | failed, TLS-stage result |

These are dated observations, not a guarantee that every endpoint works. A valid
1041-ms handshake remains valid but amber; green is <=1000 ms. Quick results do
not set the accurate HTTPS Verified flag. TLS-stage errors from the proxy core
do not by themselves prove that the remote server certificate was the cause.

The owner's Slipstream SOCKS profile also passed actual backend authentication
and accurate HTTPS (4333 ms) using the exact pinned Windows client and a valid
temporary public certificate supplied through the authorized Panel coordination.
It returned a genuine SOCKS5 method response in about 1.1 s. The SSH variant
established Slipstream QUIC but returned no SSH banner and closed after about
30.7 s with both imported 5000-ms and fast 400-ms timing. It is not reported as
working. A diagnostic direct-authoritative path did not establish successful
HTTPS, and is not substituted into the saved profiles.

## Panel dependencies and evidence boundaries

Panel contract reference: 7db0753a4fa657b2bdd0eaa5b2fc79bce8142342. The inspected
deployed runtime differs from the new Panel candidate. The current source B
status1 response for unlimited SQLite usage has limit=null, numeric usage and
partial coverage, which violates the legacy status1 contract. Its trust companion
returned 404. Source A status returned 404; this alone does not establish
expiry. Panel acknowledged the unlimited emitter mismatch and prepared a 2.0.1
correction that retains the independent SQLite/status2 accounting data.

Windows does not weaken status validation, guess another account/token, bypass
TLS, package owner-specific certificates, rotate a pin or modify the VPS to hide
these failures. The temporary public probe certificates are not inserted into
the owner's store. The existing trusted companion path must be tested again once
the server actually supplies the corrected status and trust resources.

Local final-package receipts, redacted network reports and the owner/manager
handoff are stored beside the LIVE31 delivery. Credentials, subscription URLs,
raw payloads, DPAPI files and private runtime data are excluded. Owner storage is
read only during the live probes; each report records its own before/after hash.
UI fixtures do not create a normal Agent or request system-proxy changes; an OS
proxy observation may change independently while the owner uses the computer.

Earlier installers/tags remain immutable. No public release, owner installation,
host TUN/route/firewall/DNS/SCM change or new reboot acceptance is part of LIVE31.
