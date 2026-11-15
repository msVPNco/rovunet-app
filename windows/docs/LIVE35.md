# Local LIVE35 / 0.1.4-preview.8

Quick tests now cover non-DNS paths only. A collection with 20 eligible paths and
10 DNS tunnel paths reports 20 tests and 10 exclusions. Failure of all eligible
paths does not start DNS fallback, and excluded paths get neither a failed ping
nor a fabricated success. DNS-containing chains are excluded as well.
Automatic failover/smart candidate selection uses the same boundary; manual
selection and the resolver scanner remain available. An existing healthy DNS
origin can still be restored if a smart reconnect fails.

The card's quick action changes to Stop during work. DNS-only cards disable it;
their profile menus retain the resolver scan action. Labels and scope counts
follow all four languages without changing the approved control positions.
Certificate companion404 and fetch failures now have specific translated
Slipstream messages. Account-status errors and identity/pin failures stay
distinct; standard TLS, identity/DER validation and save ordering are unchanged.

Development checks: 390 portable-domain assertions passed. A focused WPF/HTML
fixture exercised 20 real loopback TCP connects with zero HTTP/TLS bytes,
all-direct failure without DNS fallback, pointer Start/Stop, DNS-only controls,
automatic candidate exclusion and the four-language/three-theme labels. These
are local fixtures, not remote DNS or TUN acceptance. Exact execution receipts
and subsequent candidate changes are in versions/LIVE35; the final delivery
must have its own source/binary identity and receipts.

Separate live observations on immutable preview.7: a fresh owner-provided feed
imported all30 profiles and valid status1. HY2 authenticated in370ms and verified
accurate HTTPS in1061ms. TUIC did not prove its authenticated relay within4s.
The initial accurate run had12 HTTPS passes,7 attempted unverified paths and11
skips. Its20-second HTTPS cap was shorter than the product DNS budget; a retry
under the actual per-profile policy is recorded separately. WG/AWG were skipped
by a conservative concurrent-session guard; the active owner's identity was
not independently established. SSH lacked an independently verified new host
key, and Slipstream was skipped before transport because companion was404.
These observations do not identify all unsuccessful paths as client defects.

Panel identified a legacy trust-route omission and tested a narrow correction
locally. Its isolated VM/Caddy success is not deployment on the owner's source.
After authorized deployment, the same-origin status/trust binding and DER must
pass before live Slipstream SOCKS/SSH authentication, banner and HTTPS testing.
No temporary historical PEM, inherited pin, v2 activation or TLS bypass is a
substitute. Published artifacts, the owner's installation and host networking
are preserved. Native/TUN/SCM/reboot acceptance remains independently scoped.
