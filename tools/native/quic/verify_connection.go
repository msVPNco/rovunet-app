package handshake

import (
	"crypto/tls"
	utls "github.com/refraction-networking/utls"
)

// The Android certificate policy reads only public certificate/handshake fields.
// Preserve validation; identity publication has a separate full-handshake gate.
// This does not emulate ConnectionState.ExportKeyingMaterial's private exporter.
func rovunetCopyVerifyConnection(source *tls.Config, target *utls.Config) {
	if source.VerifyConnection == nil { return }
	verify := source.VerifyConnection
	target.VerifyConnection = func(state utls.ConnectionState) error {
		return verify(tls.ConnectionState{
			Version: state.Version, HandshakeComplete: state.HandshakeComplete,
			DidResume: state.DidResume, CipherSuite: state.CipherSuite,
			NegotiatedProtocol: state.NegotiatedProtocol,
			NegotiatedProtocolIsMutual: state.NegotiatedProtocolIsMutual,
			ServerName: state.ServerName, PeerCertificates: state.PeerCertificates,
			VerifiedChains: state.VerifiedChains,
			SignedCertificateTimestamps: state.SignedCertificateTimestamps,
			OCSPResponse: state.OCSPResponse,
		})
	}
}
