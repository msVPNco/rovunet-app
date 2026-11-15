[English](README.md) · [فارسی](README.fa.md) · [Русский](README.ru.md) · [简体中文](README.zh-CN.md)

# Rovunet macOS 0.1.8 Beta

**Pre-release · internal build 0.1.7.20 · macOS 15+**

| Mac | Installer and checksum | Validation |
| --- | --- | --- |
| Apple Silicon · arm64 | [PKG](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-AppleSilicon-arm64.pkg) · [SHA-256](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/SHA256SUMS.txt) | Installed; UI and owner daily TUN use accepted |
| Intel · x64 | [PKG · NOT-TESTED](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-Intel-x64-NOT-TESTED.pkg) · [SHA-256](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/SHA256SUMS.txt) | **NOT TESTED on physical Intel hardware** |

Preview for **macOS 15+**, with separate Apple Silicon and Intel installers. Public version: **0.1.8 Beta**. Internal build: **0.1.7.20**.

### Changes

- Compact DNS Scanner results with expandable technical details. DNS, tunnel, HTTPS, progress and errors remain visible; expanding a result does not select its resolver.
- Separate quick checks and precise HTTPS tests. Supported UDP quick checks use authenticated protocol responses; a successful ping alone does not establish data transfer.
- Retains earlier interface, connection-setting and panel-subscription compatibility fixes. Build 20's final product change is scanner result layout.

### Validation

Installed and launched on Apple Silicon; all **492 installed files** matched the package. **2,139 UI checks passed, zero failures.** The owner accepted the appearance and daily TUN operation. Synthetic UI fixtures do not establish interoperability on every network.

Earlier build 19 tests transferred a complete HTTPS body through **18/20 fast paths**, then through **all eight DNS paths**, including SSH variants, using TCP resolvers. These are historical build 19 results, not a new full build 20 protocol sweep. The owner reports success in Canada with Panel 2.0.2 except TUIC, also unsuccessful in other clients; panel/server investigation remains necessary. Historical WireGuard failure on another server is separate evidence.

### Installation and limits

Disconnect, quit Rovunet with Command-Q, then install the processor-matching PKG. Preserve profiles and Keychain items when upgrading. The app has a local **ad hoc signature, without Apple Developer ID or notarization**. macOS may display a security warning; do not disable Gatekeeper globally.

**Intel has not been tested on physical hardware.** Forced-exit recovery, DNS/route restoration, sleep/wake, kill-switch and long-duration energy use do not yet have full acceptance. Independent Smart comparison under global TUN remains limited. Slipstream trust delivery on the current panel needs fresh confirmation; TLS/pin checks must not be bypassed. Diagnostic WG/TUIC success nested inside HY2 is not direct connectivity acceptance.

## Source / منشأ / Исходники / 源码

[Exact source ZIP](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-source.zip) · [Verification](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/VERIFICATION.json) · [Release notes](https://github.com/msVPNco/rovunet-app/releases/tag/macos-v0.1.8-beta) · [Four-language record](releases/0.1.8-beta.md)

Exact local source snapshot: `71adb8900c68fb3b97f97903877902db718f586a`. This snapshot is not an upstream-published application commit. The attached source ZIP corresponds to these binaries; GitHub's automatic source archive is not asserted to match it. All eleven native engines and the TUN helper are reused unchanged from internal 0.1.7.19. No fresh full native rebuild or exhaustive protocol acceptance is claimed. Use the macOS release channel rather than repository-wide Latest. PKG installers only; no DMG is provided.

[Main project](../../README.md) · [Previous macOS preview](https://github.com/msVPNco/rovunet-app/releases/tag/macos-v0.1.7-beta)
