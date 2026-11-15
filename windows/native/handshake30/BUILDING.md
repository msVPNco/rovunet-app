# Bounded authenticated protocol probe

Windows amd64, Go 1.27.3, CGO_ENABLED=0. Dependency versions are pinned in go.mod/go.sum;
they come from the existing reviewed dependency set used by the Mihomo adapter at
source 3f27ef39f8c96bbfbc8b0b9bb10f5c9ef60149d8. This helper does not import Mihomo.
No dependency version was floated for this build. The corresponding-source delivery
adds vendor/ (including licences); it is kept out of the ordinary Windows checkout.

With vendor/ present, use Go 1.27.3 and:

```
set CGO_ENABLED=0
set GOPROXY=off
go build -mod=vendor -trimpath -buildvcs=false -ldflags="-s -w" -o rovunet-handshake.exe .
go test -mod=vendor -count=1 -v -timeout=90s .
```

An ordinary -mod=readonly build with the pinned cache has equivalent protocol code but
different embedded Go dependency-checksum metadata, so its executable hash differs.
The shipped helper uses -mod=vendor for exact offline reproduction from this source
offer. scripts/build-handshake30.ps1 accepts -CorrespondingSourceDirectory pointing to
the extracted handshake30 directory, checks it against the checkout, and verifies the
compiler and resulting executable hashes. Current shipped executable SHA-256:
6bbc096a48a349de766d8ac5d84877d3981fe919649ca0b832acff33889b1b1a.

The tests create ephemeral loopback peers/keys/certificates and do not install a CA or
modify an OS adapter. To additionally verify the C# -> process boundary, set
ROVUNET_HANDSHAKE30_DOTNET to dotnet.exe and ROVUNET_HANDSHAKE30_RUNNER to the built
Rovunet.WindowsSmoke.dll beside Core/Platform DLLs and engines/rovunet-handshake.exe.
Its --handshake30-stdin branch accepts only redirected synthetic profiles in the tests.
Without these variables the native protocol tests still run, but the additional C#
interop path is not exercised. Final delivery receipts set both variables explicitly.

Production accepts one strict JSON input on stdin and no command-line arguments. Its
five-field JSON output contains version/authenticated/mechanism/elapsed_ms/error only.
No listener/proxy/TUN mode exists. Test CA pools are not accessible through production
stdin, flags or environment variables. The HY2 auth exchange is protocol HTTP/3; TUIC
uses a small matching DNS response over the authenticated relay. Neither contacts an
external HTTPS connectivity target. See docs/HANDSHAKE30.md for scope and limitations.
