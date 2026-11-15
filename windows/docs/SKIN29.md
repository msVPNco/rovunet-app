# Windows 0.1.4-preview.2 — HTML shell, quick ping, certificate acquisition

Owner-approved local build, 2026-11-14. The approved Mac video is a visual
reference: compact rows, bounded text, layered surfaces and clear button feedback.
Control order, connection-only recent ordering, one-open accordion and centered
language flags retain the Windows agreement. Public Windows 0.1.3 Beta is unchanged.

## Interface

The default interface is local HTML/CSS/JavaScript in WebView2; the existing C#
connection, validation, storage and subscription implementation remains authoritative.
Home, ellipsis menus and projected nested forms use the HTML shell. The native
control tree remains a controller and emergency fallback; this is not evidence that
the new implementation uses less memory than WPF. No font scaling/stretching is used.
Profile height stays 66 DIP; the sticky connection header collapses from 148 to 88 DIP.
Flags are mathematically centered, with a second header row when necessary.
All three themes and four languages are retained. Pressed controls change depth/color;
busy/test/cancel and connection states retain their existing actions.

Moving text is clipped within its own region and runs only while the application
and document have focus. The animation clock survives telemetry updates. It pauses
without a reset on deactivation. Reduced-motion preferences are respected. The local
font, icons, styles and scripts are bundled; no CDN or remote page is required.

The bridge accepts only existing typed actions from the exact mapped origin
`https://rovunet.local/index.html`. Host objects, external navigation, new windows,
downloads and permissions are disabled. CSP blocks remote scripts, frames and data
connections. User-controlled strings use textContent. Profile secrets are not in
the home model; editor fields expose only the data that their existing native
counterparts expose. Browser autofill/password saving and developer menus are off.
The WebView profile stays in the user's local application-data directory.

## Test meaning

The card test button and quick menu action perform bounded TCP connection timing:
at most four sockets concurrently, three seconds per endpoint. They start no proxy
engine and send no HTTP/TLS application data. Separate PingMeasurements persist the
profile fingerprint/time. They neither overwrite HTTPS verification nor change the
active connection. Fresh successes are green, historical results muted, failures red.
The accurate menu and stability tests remain HTTPS. Recent valid samples take
precedence; edited profiles invalidate old quick measurements.

TCP timing is endpoint reachability, not authentication, UDP viability or internet
verification. UDP-only HY2, TUIC, WireGuard/AWG and Mieru-UDP are not given fake TCP
success. A real protocol-handshake quick test is a separate pending capability:
HY2 must authenticate beyond the QUIC transport; TUIC's authentication cannot be
inferred from QUIC establishment; WireGuard needs its own authenticated response.
The current Xray runtime discards log text and exposes no attested per-peer handshake
measurement. A SOCKS-ready event, ICMP reply or UDP send is insufficient evidence.
The owner's message about continuing accurate tests on 20 paths is not a Windows
test receipt. Keep this item open until instrumented, bounded, cancellable handshake
evidence exists for each engine, with negative/authentication cases.

Protocol references: [HY2 specification](https://www.hy2.io/docs/developers/Protocol/),
[TUIC specification](https://github.com/tuic-protocol/tuic/blob/master/SPEC.md),
[WireGuard protocol](https://www.wireguard.com/protocol/).

## Slipstream public certificates

Use the Panel REVIEW25 trust-v1 companion contract, not a guessed URL or website
certificate. `/sub/TOKEN(.windows).json` and `/v2/sub/TOKEN(.windows).json` map to
the same-origin `TOKEN.trust.json`. Status is fetched first. Validate version,
server/account/period identity, item/profile ID, tunnel domain, one public certificate,
validity and DER SHA256. Fetches are bounded to twelve seconds and 262144 characters,
with normal HTTPS verification and redirects disabled. An active HTTP tunnel is used
when available. Manual valid pins remain authoritative. Chain layers use an unambiguous
saved subscription association. Manual sources without a companion contract cannot
be enrolled automatically.

First acquisition and saving require no confirmation. Existing different pins,
identity mismatch, invalid/expired certificates and download/save failure preserve
previous trust; no private keys or TLS-validation bypass are allowed. Stored pins
are scoped separately from manual certificates and protected by the existing DPAPI
store. Repeated acquisition uses the valid stored pin. A new period does not silently
authorize a different certificate; signed rotation/recovery authority is a separate gate.
The Panel agent's live-tunnel/reboot/1 MiB matching-hash report is Panel evidence,
not a Windows live/reboot certificate test.

## Installation and evidence

SDK pinned to Microsoft.Web.WebView2 1.0.4314.35. WebView2 Runtime 120+ is a prerequisite;
setup checks per-user/per-machine registrations. If absent, it verifies and executes
the bundled Microsoft-signed Evergreen Bootstrapper before replacing Rovunet. The
bootstrapper needs Microsoft download access. On failure the old Rovunet stays in
place. The real bootstrapper installation on a clean device remains untested; automated
prerequisite tests exercise the orchestration and embedded hash without installing it.
Notices accompany the package. [Microsoft distribution instructions](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution).

Final receipt outside the source tree records exact installer/source hashes and tests
run against the frozen payload. Target suites: actual WebView2 four-language/three-theme
compact/wide geometry and forms, focus/clock/status/bridge checks; loopback TCP with zero
application bytes; synthetic trust/DPAPI/rollback checks; retained connection/routing/
selection/accordion regressions and portable-domain checks. These do not certify physical
touch, mixed DPI, low-end GPU performance, clean-device WebView2 installation, live UDP
handshakes, public-server certificates, Windows reboot, TUN, kill switch or native service
acceptance. Owner installation, profiles, host networking and paused VM remain unchanged.
