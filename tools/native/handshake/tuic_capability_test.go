package outbound

import (
	"testing"
	"github.com/metacubex/mihomo/component/ca"
)

// Lock down the actual pinned TUIC constructor, not a general claim about QUIC.
func TestRovunetTuicEarlyDialHasNoResumptionCache(t *testing.T) {
	previousDirectory,previousEndpoint:=ca.RovunetCertificateDirectory,ca.RovunetCertificateEndpoint
	defer func(){ca.RovunetCertificateDirectory,ca.RovunetCertificateEndpoint=previousDirectory,previousEndpoint}()
	ca.RovunetCertificateDirectory,ca.RovunetCertificateEndpoint=t.TempDir(),"example.invalid:443"
	for _,early:=range []bool{false,true} {
		peer,err:=NewTuic(TuicOption{Name:"Synthetic TUIC",Server:"example.invalid",Port:443,
			UUID:"00000000-0000-4000-8000-000000000001",Password:"synthetic-only",ReduceRtt:early})
		if err!=nil {t.Fatal(err)}
		if peer.option.ReduceRtt!=early {t.Fatal("TUIC early option was silently changed")}
		if peer.tlsConfig.ClientSessionCache!=nil {t.Fatal("TUIC acquired a resumption cache: review early-data publication before changing this capability")}
		if peer.tlsConfig.VerifyConnection==nil || peer.tlsConfig.RovunetPostHandshake==nil {t.Fatal("TUIC lost its certificate validation/publication gates")}
		peer.Close()
	}
}
