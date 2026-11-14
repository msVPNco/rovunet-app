package main

import (
	"bytes"
	"context"
	"crypto/ecdh"
	"crypto/ecdsa"
	"crypto/elliptic"
	"crypto/rand"
	"crypto/subtle"
	"crypto/x509"
	"crypto/x509/pkix"
	"encoding/base64"
	"encoding/hex"
	"encoding/json"
	"encoding/pem"
	"fmt"
	awg "github.com/metacubex/amneziawg-go/device"
	awg1 "github.com/metacubex/amneziawg-go/device_v1"
	"github.com/metacubex/http"
	"github.com/metacubex/quic-go"
	"github.com/metacubex/quic-go/http3"
	hy "github.com/metacubex/sing-quic/hysteria2"
	"github.com/metacubex/tls"
	"github.com/metacubex/wireguard-go/conn"
	wg "github.com/metacubex/wireguard-go/device"
	"io"
	"math/big"
	"net"
	"os"
	"os/exec"
	"sort"
	"strconv"
	"strings"
	"sync"
	"testing"
	"time"
)

func testCertificate(t *testing.T) (*tls.Config, *x509.CertPool) {
	t.Helper()
	key, e := ecdsa.GenerateKey(elliptic.P256(), rand.Reader)
	if e != nil {
		t.Fatal(e)
	}
	template := &x509.Certificate{SerialNumber: big.NewInt(444), Subject: pkix.Name{CommonName: "localhost"}, DNSNames: []string{"localhost"}, IPAddresses: []net.IP{net.IPv4(127, 0, 0, 1)}, NotBefore: time.Now().Add(-time.Hour), NotAfter: time.Now().Add(time.Hour), KeyUsage: x509.KeyUsageDigitalSignature, ExtKeyUsage: []x509.ExtKeyUsage{x509.ExtKeyUsageServerAuth}, BasicConstraintsValid: true}
	der, e := x509.CreateCertificate(rand.Reader, template, template, &key.PublicKey, key)
	if e != nil {
		t.Fatal(e)
	}
	secret, _ := x509.MarshalECPrivateKey(key)
	certificate, e := tls.X509KeyPair(pem.EncodeToMemory(&pem.Block{Type: "CERTIFICATE", Bytes: der}), pem.EncodeToMemory(&pem.Block{Type: "EC PRIVATE KEY", Bytes: secret}))
	if e != nil {
		t.Fatal(e)
	}
	roots := x509.NewCertPool()
	parsed, _ := x509.ParseCertificate(der)
	roots.AddCert(parsed)
	return &tls.Config{Certificates: []tls.Certificate{certificate}, NextProtos: []string{"h3"}, MinVersion: tls.VersionTLS13}, roots
}
func udp(t *testing.T) net.PacketConn {
	t.Helper()
	pc, e := net.ListenPacket("udp4", "127.0.0.1:0")
	if e != nil {
		t.Fatal(e)
	}
	t.Cleanup(func() { pc.Close() })
	return pc
}
func TestHysteriaAuthenticatedHandshake(t *testing.T) {
	tlsConfig, roots := testCertificate(t)
	for _, obfs := range []string{"", "fixture-salamander"} {
		t.Run("obfs="+strconv.FormatBool(obfs != ""), func(t *testing.T) {
			pc := udp(t)
			port := pc.LocalAddr().(*net.UDPAddr).Port
			wrapped := pc
			if obfs != "" {
				wrapped = hy.NewSalamanderConn(pc, []byte(obfs))
			}
			var gate sync.Mutex
			requests := 0
			server := &http3.Server{TLSConfig: tlsConfig, QUICConfig: &quic.Config{EnableDatagrams: true}, Handler: http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
				gate.Lock()
				requests++
				gate.Unlock()
				if r.Method != "POST" || r.URL.Path != "/auth" || r.Host != "hysteria" {
					t.Error("not a HY2 auth request")
				}
				if r.Header.Get("Hysteria-Auth") != "fixture-pass" {
					w.WriteHeader(404)
					return
				}
				w.Header().Set("Hysteria-UDP", "true")
				w.Header().Set("Hysteria-CC-RX", "0")
				w.WriteHeader(233)
			})}
			go server.Serve(wrapped)
			defer server.Close()
			c := config{Version: 1, Protocol: "hysteria2", Server: "127.0.0.1", Port: port, ServerName: "localhost", Password: "fixture-pass", ObfsPassword: obfs, TimeoutMS: 1200}
			if r := probe(context.Background(), c, roots); !r.Authenticated || r.Mechanism != "hysteria2-auth" {
				t.Fatalf("positive: %+v", r)
			}
			c.Password = "wrong"
			if r := probe(context.Background(), c, roots); r.Authenticated || r.Error != "HANDSHAKE_REJECTED" {
				t.Fatalf("wrong password: %+v", r)
			}
			c.Password = "fixture-pass"
			c.ServerName = "wrong.invalid"
			if r := probe(context.Background(), c, roots); r.Authenticated || r.Error != "TLS_UNVERIFIED" {
				t.Fatalf("wrong SNI: %+v", r)
			}
			c.ServerName = "localhost"
			if r := probe(context.Background(), c, x509.NewCertPool()); r.Authenticated || r.Error != "TLS_UNVERIFIED" {
				t.Fatalf("untrusted certificate: %+v", r)
			}
			gate.Lock()
			n := requests
			gate.Unlock()
			if n != 2 {
				t.Fatalf("Only two protocol-auth requests expected, got %d", n)
			}
		})
	}
}
func tuicServer(t *testing.T, mode string) (int, *x509.CertPool) {
	t.Helper()
	tlsConfig, roots := testCertificate(t)
	pc := udp(t)
	listener, e := quic.Listen(pc, tlsConfig, &quic.Config{EnableDatagrams: true})
	if e != nil {
		t.Fatal(e)
	}
	t.Cleanup(func() { listener.Close() })
	ctx, cancel := context.WithCancel(context.Background())
	t.Cleanup(cancel)
	go func() {
		for {
			q, e := listener.Accept(ctx)
			if e != nil {
				return
			}
			go func(q *quic.Conn) {
				defer q.CloseWithError(0, "")
				stream, e := q.AcceptUniStream(ctx)
				if e != nil {
					return
				}
				auth, e := io.ReadAll(io.LimitReader(stream, 64))
				if e != nil || len(auth) != 50 {
					return
				}
				state := q.ConnectionState()
				token, e := state.TLS.ExportKeyingMaterial(string(auth[2:18]), []byte("fixture-pass"), 32)
				expectedUUID, _ := hex.DecodeString("00112233445566778899aabbccddeeff")
				if e != nil || !bytes.Equal(auth[2:18], expectedUUID) || subtle.ConstantTimeCompare(token, auth[18:]) != 1 {
					q.CloseWithError(0xfffffff1, "")
					return
				}
				respond := func(packet []byte) {
					if len(packet) < 29 || packet[0] != 5 || packet[1] != 2 {
						return
					}
					if mode == "silent" {
						q.SendDatagram([]byte{5, 4})
						return
					}
					reply := append([]byte{}, packet...)
					reply[19] |= 0x80
					if mode == "wrong-nonce" {
						reply[17] ^= 1
					}
					if mode == "quic" {
						s, e := q.OpenUniStreamSync(ctx)
						if e == nil {
							s.Write(reply)
							s.Close()
						}
					} else {
						q.SendDatagram(reply)
					}
				}
				if mode == "quic" {
					for {
						s, e := q.AcceptUniStream(ctx)
						if e != nil {
							return
						}
						b, e := io.ReadAll(io.LimitReader(s, 4096))
						if e == nil {
							respond(b)
						}
					}
				} else {
					for {
						b, e := q.ReceiveDatagram(ctx)
						if e != nil {
							return
						}
						respond(b)
					}
				}
			}(q)
		}
	}()
	return pc.LocalAddr().(*net.UDPAddr).Port, roots
}
func TestTUICRequiresAuthenticatedRelay(t *testing.T) {
	for _, mode := range []string{"native", "quic", "silent", "wrong-nonce"} {
		t.Run(mode, func(t *testing.T) {
			port, roots := tuicServer(t, mode)
			relay := "native"
			if mode == "quic" {
				relay = mode
			}
			c := config{Version: 1, Protocol: "tuic", Server: "127.0.0.1", Port: port, ServerName: "localhost", UUID: "00112233-4455-6677-8899-aabbccddeeff", Password: "fixture-pass", UDPMode: relay, TimeoutMS: 650}
			r := probe(context.Background(), c, roots)
			expected := mode == "native" || mode == "quic"
			if r.Authenticated != expected {
				t.Fatalf("relay proof: %+v", r)
			}
			if !expected && r.Error != "HANDSHAKE_UNCONFIRMED" {
				t.Fatalf("unconfirmed proof: %+v", r)
			}
			c.Password = "wrong"
			r = probe(context.Background(), c, roots)
			if r.Authenticated || r.Error != "HANDSHAKE_REJECTED" {
				t.Fatalf("auth rejection: %+v", r)
			}
			c.Password = "fixture-pass"
			c.UUID = "00000000-0000-4000-8000-000000000001"
			if r := probe(context.Background(), c, roots); r.Authenticated || r.Error != "HANDSHAKE_REJECTED" {
				t.Fatalf("unknown identity rejection: %+v", r)
			}
		})
	}
}
func pair(t *testing.T) ([]byte, []byte) {
	t.Helper()
	k, e := ecdh.X25519().GenerateKey(rand.Reader)
	if e != nil {
		t.Fatal(e)
	}
	return k.Bytes(), k.PublicKey().Bytes()
}
func wgPeer(t *testing.T, version string, clientPublic []byte, options map[string]any, psk string) (int, []byte) {
	t.Helper()
	private, public := pair(t)
	c := config{Protocol: "amneziawg", PrivateKey: base64.StdEncoding.EncodeToString(private), PublicKey: base64.StdEncoding.EncodeToString(clientPublic), AWG: options, PreSharedKey: psk}
	ipc, _, e := wgConfig(c, "127.0.0.1:9")
	if e != nil {
		t.Fatal(e)
	}
	ipc = strings.ReplaceAll(ipc, "endpoint=127.0.0.1:9\n", "")
	tun := newNullTun()
	logger := wg.NewLogger(wg.LogLevelSilent, "")
	bind := plainBind{conn.NewStdNetBind()}
	var d probeDevice
	switch version {
	case "wg":
		d = wg.NewDevice(tun, bind, logger, 2)
	case "3":
		d = awg.NewDevice(tun, bind, logger, 2)
	default:
		d = awg1.NewDevice(tun, bind, logger, 2)
	}
	t.Cleanup(d.Close)
	if e = d.IpcSet(ipc); e != nil {
		t.Fatal(e)
	}
	if e = d.Up(); e != nil {
		t.Fatal(e)
	}
	state, e := d.IpcGet()
	if e != nil {
		t.Fatal(e)
	}
	port := 0
	for _, line := range strings.Split(state, "\n") {
		if strings.HasPrefix(line, "listen_port=") {
			port, _ = strconv.Atoi(strings.TrimPrefix(line, "listen_port="))
		}
	}
	if port == 0 {
		t.Fatal("no UDP port")
	}
	t.Logf("fixture UDP port %d", port)
	return port, public
}
func TestWireGuardActualNoiseHandshake(t *testing.T) {
	for _, version := range []string{"wg", "1", "2", "3"} {
		t.Run(version, func(t *testing.T) {
			private, public := pair(t)
			options := map[string]any{}
			if version != "wg" {
				options = map[string]any{"jc": float64(2), "jmin": float64(10), "jmax": float64(20), "s1": float64(16), "s2": float64(16), "h1": "123456", "h2": "234567", "h3": "345678", "h4": "456789"}
			}
			if version == "2" {
				options["s3"] = float64(16)
				options["s4"] = float64(16)
				options["h1"] = "123456-123499"
			}
			if version == "3" {
				options["s3"] = float64(16)
				options["s4"] = float64(16)
				options["version"] = float64(3)
				protection, _ := pair(t)
				options["header-protection-key"] = base64.StdEncoding.EncodeToString(protection)
				options["disable-cookies"] = true
				options["random-trailers"] = true
			}
			port, serverPublic := wgPeer(t, version, public, options, "")
			kind := "amneziawg"
			if version == "wg" {
				kind = "wireguard"
			}
			c := config{Version: 1, Protocol: kind, Server: "127.0.0.1", Port: port, PrivateKey: base64.StdEncoding.EncodeToString(private), PublicKey: base64.StdEncoding.EncodeToString(serverPublic), AWG: options, TimeoutMS: 700}
			r := probe(context.Background(), c, nil)
			if !r.Authenticated || r.Mechanism != kind+"-noise" {
				t.Fatalf("positive: %+v", r)
			}
			if runner := os.Getenv("ROVUNET_HANDSHAKE30_RUNNER"); runner != "" {
				raw := fmt.Sprintf("[Interface]\nPrivateKey=%s\nAddress=10.0.0.2/32\n", c.PrivateKey)
				labels := map[string]string{"jc": "Jc", "jmin": "Jmin", "jmax": "Jmax", "s1": "S1", "s2": "S2", "s3": "S3", "s4": "S4", "h1": "H1", "h2": "H2", "h3": "H3", "h4": "H4", "version": "Version", "header-protection-key": "HeaderProtectionKey", "random-trailers": "RandomTrailers", "disable-cookies": "DisableCookies"}
				keys := make([]string, 0, len(options))
				for key := range options {
					keys = append(keys, key)
				}
				sort.Strings(keys)
				for _, key := range keys {
					raw += fmt.Sprintf("%s=%v\n", labels[key], options[key])
				}
				raw += fmt.Sprintf("[Peer]\nPublicKey=%s\nEndpoint=127.0.0.1:%d\nAllowedIPs=0.0.0.0/0\n", c.PublicKey, port)
				ctx, cancel := context.WithTimeout(context.Background(), 15*time.Second)
				defer cancel()
				command := exec.CommandContext(ctx, os.Getenv("ROVUNET_HANDSHAKE30_DOTNET"), runner, "--handshake30-stdin")
				command.Stdin = strings.NewReader(raw)
				output, err := command.Output()
				var actual result
				if err != nil || json.Unmarshal(output, &actual) != nil || !actual.Authenticated || actual.Mechanism != kind+"-noise" {
					t.Fatalf("C# importer -> pinned packaged child -> real peer failed: exit=%v", err)
				}
				t.Log("C# importer and platform launched the hash-pinned binary; real cryptographic handshake passed")
			}
			_, wrong := pair(t)
			c.PublicKey = base64.StdEncoding.EncodeToString(wrong)
			r = probe(context.Background(), c, nil)
			if r.Authenticated || r.Error != "HANDSHAKE_TIMEOUT" {
				t.Fatalf("wrong server key: %+v", r)
			}
			c.PublicKey = base64.StdEncoding.EncodeToString(serverPublic)
			key, _ := pair(t)
			c.PreSharedKey = base64.StdEncoding.EncodeToString(key)
			r = probe(context.Background(), c, nil)
			if r.Authenticated {
				t.Fatalf("wrong PSK passed: %+v", r)
			}
		})
	}
}
func TestTimeoutCancelAndRedaction(t *testing.T) {
	pc := udp(t)
	go func() {
		b := make([]byte, 4096)
		for {
			n, addr, e := pc.ReadFrom(b)
			if e != nil {
				return
			}
			pc.WriteTo(b[:n], addr)
		}
	}()
	private, _ := pair(t)
	_, public := pair(t)
	c := config{Version: 1, Protocol: "wireguard", Server: "127.0.0.1", Port: pc.LocalAddr().(*net.UDPAddr).Port, PrivateKey: base64.StdEncoding.EncodeToString(private), PublicKey: base64.StdEncoding.EncodeToString(public), TimeoutMS: 150}
	if r := probe(context.Background(), c, nil); r.Authenticated || r.Error != "HANDSHAKE_TIMEOUT" {
		t.Fatalf("UDP echo is not a handshake: %+v", r)
	}
	ctx, cancel := context.WithCancel(context.Background())
	cancel()
	r := probe(ctx, c, nil)
	if r.Authenticated || r.Error != "CANCELLED" {
		t.Fatalf("cancel: %+v", r)
	}
	var output bytes.Buffer
	run(strings.NewReader(`{"version":1,"protocol":"bad","password":"secret-fixture","extra":true}`), &output)
	if strings.Contains(output.String(), "secret-fixture") {
		t.Fatal("secret escaped")
	}
	var parsed result
	if json.Unmarshal(output.Bytes(), &parsed) != nil || parsed.Error != "INVALID_MESSAGE" {
		t.Fatal("strict contract")
	}

}
