# Local Windows 0.1.4-preview.4

This continues UI30 and the owner's authenticated UDP quick-test request. No public
release, owner installation, host TUN, route, firewall, DNS, SCM or VM change is made.
Earlier local installers and public 0.1.3 Beta remain immutable.

## HTML controls

The UI30 dispatcher fix remains: WebView2 callbacks return before entering an existing
modal page loop. Back/title/save share one sticky compact row. Import tabs are real tabs.
UniformGrid cells now receive distinct row/column coordinates; counters, running resolver
slots and bottom filters no longer overlap. Resolver result checkboxes/details are a
horizontal row. Single-line native text keeps its own clipping/marquee boundary.

The group power icon turns green only for the active connection. Its enabled state and
label come from the existing C# control, including cancellation while connecting. The
collection menu derives Connect/Disconnect and Show/Hide paths from current state.
Dynamic resolver ellipsis menus use the same HTML menu bridge. Multi-check probe menus
retain their existing stay-open behavior. Disabled tabs/expanders/sliders stay disabled.

Resource text in an open page changes language without reconstructing its editable
controls. Only original localized resource-string objects are matched, by reference;
user-entered text, profile names, addresses and fixed English licence prose are preserved.
Combo-box display labels are localized without changing their underlying selected values.

Pending keystrokes are sent before Save or another button action. Rendering is deferred
while a pointer is held, so telemetry cannot remove the pressed button before release.
Marquee still runs only with application/document focus and respects reduced motion.
Button order, centered flags, three palettes and unscaled typography remain.

Each collection now has one persistent operation line. Subscription fetch/validation/
save, quick ping/handshake, accurate HTTPS and connecting/disconnecting report activity
on the target card until they finish, including a collapsed card. Unrelated footer
notices cannot erase it. Completion, cancellation or a red failure replaces the busy
message. Test/update overlap can show both operations; the latest completed operation
wins afterward. These transient operation states are not persisted as fake running
work across app restart, and no subscription URL is included in their text.
`--ui-card-activity30` uses delayed fetch/probe adapters and actual HTML commands to
check the full lifetime, collapse/expand, languages, success/failure/cancel, HTTPS
completion and preservation of unrelated profiles.

## Quick protocol tests

`ProtocolHandshake30` reparses the validated original profile. The separate hash-pinned
`rovunet-handshake.exe` reads bounded JSON through stdin and returns only a strict,
redacted result. It runs in an owned kill-on-close job, never receives credentials on its
command line, and creates no OS TUN adapter, proxy lease or temporary credential file.
Two handshake workers and a four-second protocol budget are used; process startup has
an independent twelve-second outer bound. Cancel kills only the owned helper.

* HY2: real QUIC/TLS and Hysteria2 authentication; only protocol status 233 passes.
  Salamander is retained. The protocol's own HTTP/3 authentication is not an external
  HTTPS connectivity test. System TLS trust/SNI validation stays enabled.
* TUIC v5: TLS exporter/UUID/password authentication followed by a nonce-matched DNS
  response through its UDP relay, supporting native datagrams and QUIC-stream relay.
  TUIC has no authentication-success ACK, so QUIC establishment or a heartbeat alone
  never passes. Without the matching relay response the result is unconfirmed.
* WireGuard: an authenticated Noise handshake, using a userspace dummy interface.
* AmneziaWG: the same authenticated proof with retained AWG 1/2/3 obfuscation options,
  including AWG3 header protection. A plain UDP echo is not a handshake.

Probing a WG/AWG peer with the same client/server keys as the app's active session can
roam the server's peer endpoint. Such a test is skipped with an explicit explanation;
the active connection and previous measurement are preserved. This guard includes
recognized chain layers; it cannot identify a session in an unrelated external VPN app.

TCP reachability remains a bare socket for TCP profiles. DNS-family fallback behavior
is preserved. Quick results have a separate persisted mechanism/fingerprint/timestamp;
they do not set HTTPS Verified. Valid fresh latency <=1000 ms is green, >1000 ms amber,
historical latency gray, rejection/failure red, and TUIC unconfirmed amber. An edit,
cancellation or failed save cannot publish a late false success.

## Evidence and remaining limits

`--ui-html-controls30` uses real pointer input in the product's own WebView and checks
hit targets, all 26 protocol forms, search, final-keystroke save, routing, deletion,
imports, settings, live language/state changes, About callbacks and resolver controls.
`--ui-html-navigation30` covers the four-language/three-theme matrix. `--ui-html29`
checks both sizes, actual palettes, centered flags, marquee/focus and bridge restrictions.
`--ui-handshake30` checks dispatch, concurrency, 999/1000/1001 colors, persistence,
cancellation, edited-profile rejection and active-session protection with an injected
probe adapter. That adapter is not cryptographic network evidence.

Native Go tests use actual encrypted loopback HY2/TUIC/WG/AWG peers and negative
credentials/keys/certificates/heartbeat/nonce cases. WG/AWG tests additionally call the
C# importer and Platform wrapper, which launches the exact pinned helper binary.
HY2/TUIC loopback tests supply test roots directly to Go test functions; production
stdin has no custom-root or insecure-TLS switch and Windows trust is not modified.
These tests do not certify every provider profile, remote public-CA behavior, reboot,
or TUN/kill-switch/service gates. About external launches are callbacks in UI evidence;
real donations, Telegram launch and installer execution are not performed by that suite.

Exact final file hashes, commands, failures retained from intermediate attempts and
source/archive equality are recorded with the local HANDSHAKE30 delivery. The owner's
preview.3 diagnostic contained PROBE_TLS_FAILED/SHARING_DISABLED, not a UI bridge trace.

Native source/build/dependency provenance: `native/handshake30/BUILDING.md` and
`licenses/Handshake30`. The accompanying native-source ZIP includes vendored dependency
sources and licences. Official protocol references:
[HY2](https://www.hy2.io/docs/developers/Protocol/),
[TUIC](https://github.com/tuic-protocol/tuic/blob/master/SPEC.md),
[WireGuard](https://www.wireguard.com/protocol/).
