WebView2 Evergreen Bootstrapper, downloaded 2026-11-14 from the official Microsoft link:
https://go.microsoft.com/fwlink/p/?LinkId=2124703

SHA256: cf2f2445fef0c5c14e372045f84998771eeb8bb01d59be4afd00692fa160082e
Bytes: 1897688
Authenticode checked before embedding: Valid; Microsoft Corporation.

Distribution reference: https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution
The setup embeds this exact bootstrapper and verifies its hash before running it.
It runs only when WebView2 Runtime 120+ is absent, before replacing Rovunet.
The bootstrapper needs access to Microsoft servers. Failed prerequisites leave the
installed Rovunet untouched. No runtime installation was executed on the owner host
by the automated test; an existing runtime was detected there.
SDK notices: licenses/WebView2-LICENSE.txt and licenses/WebView2-NOTICE.txt.
