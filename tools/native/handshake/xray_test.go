package tls

import (
	"bytes"
	"context"
	"crypto"
	"crypto/rand"
	"crypto/rsa"
	"crypto/tls"
	"crypto/x509"
	"crypto/x509/pkix"
	"encoding/pem"
	"io"
	"math/big"
	"net"
	"net/http"
	"net/http/httptest"
	"net/url"
	"os"
	"path/filepath"
	"sync"
	"sync/atomic"
	"testing"
	"time"

	utls "github.com/refraction-networking/utls"
	"github.com/apernet/quic-go"
)

func TestRovunetHttpTransportPreservesPlainProxyAndRefusesUngatedConnect(t *testing.T) {
	beforeDir,beforePeer:=RovunetCertificateDirectory,RovunetCertificateEndpoint
	defer func(){RovunetCertificateDirectory,RovunetCertificateEndpoint=beforeDir,beforePeer}()
	RovunetCertificateDirectory,RovunetCertificateEndpoint=t.TempDir(),"synthetic-peer:443"
	var requests atomic.Int32
	proxy:=httptest.NewServer(http.HandlerFunc(func(response http.ResponseWriter,request *http.Request){requests.Add(1);response.Write([]byte("synthetic proxy response"))}))
	defer proxy.Close()
	address,err:=url.Parse(proxy.URL);if err!=nil{t.Fatal(err)}
	transport:=&http.Transport{TLSClientConfig:(&Config{ServerName:"localhost"}).GetTLSConfig(),Proxy:http.ProxyURL(address)}
	defer transport.CloseIdleConnections()
	RovunetWrapHTTPTransport(transport)
	client:=&http.Client{Transport:transport,Timeout:2*time.Second}
	response,err:=client.Get("http://example.invalid/synthetic")
	if err!=nil{t.Fatal("Plain HTTP proxy was unexpectedly changed",err)}
	body,_:=io.ReadAll(response.Body);response.Body.Close()
	if string(body)!="synthetic proxy response" || requests.Load()!=1{t.Fatal("Plain request did not use its selected proxy")}
	if _,err:=client.Get("https://example.invalid/synthetic");err==nil{t.Fatal("Ungated HTTP CONNECT was silently allowed")}
	if requests.Load()!=1{t.Fatal("Ungated HTTP CONNECT reached the proxy")}
	files,_:=os.ReadDir(RovunetCertificateDirectory);if len(files)!=0{t.Fatal("Non-TLS/proxy-denied requests published a certificate")}
}

type rovunetWrongSigner struct {
	public crypto.PublicKey
	secret crypto.Signer
	called *atomic.Bool
}
func (s rovunetWrongSigner) Public() crypto.PublicKey { return s.public }
func (s rovunetWrongSigner) Sign(random io.Reader, digest []byte, options crypto.SignerOpts) ([]byte, error) {
	s.called.Store(true)
	return s.secret.Sign(random, digest, options)
}

func TestRovunetConcurrentQuicProofPublishesOnlyOneIdentity(t *testing.T) {
	beforeDir, beforePeer := RovunetCertificateDirectory, RovunetCertificateEndpoint
	defer func() { RovunetCertificateDirectory, RovunetCertificateEndpoint = beforeDir, beforePeer }()
	RovunetCertificateDirectory, RovunetCertificateEndpoint = t.TempDir(), "shared-synthetic-endpoint"
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()
	ready := make(chan struct{}, 2)
	publish := make(chan struct{})
	type outcome struct { certificate []byte; err error }
	results := make(chan outcome,2)
	for i:=0;i<2;i++ {
		certificate := rovunetTestCertificate(t)
		server, err := quic.ListenAddr("127.0.0.1:0", &tls.Config{NextProtos:[]string{"h3"},Certificates:[]tls.Certificate{certificate}},nil)
		if err != nil { t.Fatal(err) }
		defer server.Close()
		go func() { connection,err:=server.Accept(ctx); if err==nil { <-ctx.Done();connection.CloseWithError(0,"") } }()
		config := (&Config{ServerName:"localhost",NextProtocol:[]string{"h3"}}).GetTLSConfig()
		config.GetCertificate=nil
		go func() {
			connection,err:=quic.DialAddr(ctx,server.Addr().String(),config,&quic.Config{ChromeParrot:true})
			if err!=nil { results<-outcome{err:err};ready<-struct{}{};return }
			defer connection.CloseWithError(0,"")
			ready<-struct{}{}; <-publish
			results<-outcome{certificate.Certificate[0],RovunetCertificateVerified(config,connection.ConnectionState().TLS)}
		}()
	}
	for i:=0;i<2;i++ { select {case <-ready: case <-ctx.Done(): t.Fatal("Concurrent QUIC handshakes did not finish") } }
	files,_:=os.ReadDir(RovunetCertificateDirectory)
	if len(files)!=0 { t.Fatal("Handshake callback published before the explicit proof gate") }
	close(publish)
	var winner []byte;successful:=0
	for i:=0;i<2;i++ { out:=<-results;if out.err==nil { successful++;winner=out.certificate } }
	if successful!=1 { t.Fatal("Concurrent different proved identities must have exactly one winner",successful) }
	files,_=os.ReadDir(RovunetCertificateDirectory)
	if len(files)!=1 { t.Fatal("Concurrent proof publication created duplicate pins") }
	stored,err:=rovunetReadIdentity(filepath.Join(RovunetCertificateDirectory,files[0].Name()))
	if err!=nil || !bytes.Equal(stored.Certificate,winner) { t.Fatal("Published QUIC identity differs from the winning connection",err) }
}

func TestRovunetQuicChromeCertificateProofAndIdentity(t *testing.T) {
	first, second := rovunetTestCertificate(t), rovunetTestCertificate(t)
	var active atomic.Pointer[tls.Certificate]
	active.Store(&first)
	listener, err := quic.ListenAddr("127.0.0.1:0", &tls.Config{NextProtos: []string{"h3"},
		GetCertificate: func(*tls.ClientHelloInfo) (*tls.Certificate, error) { return active.Load(), nil }}, nil)
	if err != nil { t.Fatal(err) }
	defer listener.Close()
	ctx, cancel := context.WithTimeout(context.Background(), 20*time.Second)
	defer cancel()
	go func() {
		for {
			conn, err := listener.Accept(ctx)
			if err != nil { return }
			go func() {
				stream, err := conn.AcceptStream(ctx)
				if err != nil { return }
				io.Copy(stream, stream)
				stream.Close()
			}()
		}
	}()
	beforeDir, beforePeer := RovunetCertificateDirectory, RovunetCertificateEndpoint
	defer func() { RovunetCertificateDirectory, RovunetCertificateEndpoint = beforeDir, beforePeer }()
	RovunetCertificateDirectory, RovunetCertificateEndpoint = t.TempDir(), listener.Addr().String()
	var checks atomic.Int32
	connect := func(host string) error {
		config := (&Config{ServerName: host, NextProtocol: []string{"h3"}}).GetTLSConfig()
		verify := config.VerifyConnection
		if verify == nil { t.Fatal("Automatic certificate callback missing") }
		config.VerifyConnection = func(state tls.ConnectionState) error { checks.Add(1); return verify(state) }
		// Same as the pinned Hysteria dialer: this is a client-only QUIC config.
		config.GetCertificate = nil
		connection, err := quic.DialAddr(ctx, listener.Addr().String(), config, &quic.Config{ChromeParrot: true})
		if err != nil { return err }
		defer connection.CloseWithError(0, "")
		if err := RovunetCertificateVerified(config, connection.ConnectionState().TLS); err != nil { return err }
		stream, err := connection.OpenStreamSync(ctx)
		if err != nil { return err }
		payload := bytes.Repeat([]byte("synthetic-QUIC-proof"), 4096)
		go func() { stream.Write(payload); stream.Close() }()
		got := make([]byte, len(payload))
		if _, err = io.ReadFull(stream, got); err != nil { return err }
		if !bytes.Equal(got, payload) { t.Fatal("Encrypted QUIC echo changed the payload") }
		return nil
	}
	// The peer advertises the exact valid DER/name, then signs CertificateVerify
	// with another private key. Assert it actually signed; fixture startup failure
	// cannot satisfy this negative case.
	var signed atomic.Bool
	forged := first
	forged.PrivateKey = rovunetWrongSigner{first.PrivateKey.(crypto.Signer).Public(), second.PrivateKey.(crypto.Signer), &signed}
	active.Store(&forged)
	if err := connect("localhost"); err == nil { t.Fatal("QUIC invalid ownership proof was accepted") }
	if !signed.Load() { t.Fatal("QUIC invalid-proof signer was not exercised") }
	files, _ := os.ReadDir(RovunetCertificateDirectory)
	if len(files) != 0 { t.Fatal("QUIC invalid ownership proof enrolled an identity") }
	active.Store(&first)
	if err := connect("wrong.example.invalid"); err == nil { t.Fatal("QUIC wrong name was accepted") }
	files, _ = os.ReadDir(RovunetCertificateDirectory)
	if len(files) != 0 { t.Fatal("Rejected QUIC identity was stored") }
	if err := connect("localhost"); err != nil { t.Fatal("QUIC enrollment/relay failed", err) }
	files, _ = os.ReadDir(RovunetCertificateDirectory)
	if len(files) != 1 { t.Fatal("QUIC must store exactly one certificate") }
	path := filepath.Join(RovunetCertificateDirectory, files[0].Name())
	stored, _ := os.ReadFile(path)
	if err := connect("localhost"); err != nil { t.Fatal("Pinned QUIC repeat failed", err) }
	signed.Store(false)
	active.Store(&forged)
	if err := connect("localhost"); err == nil { t.Fatal("QUIC invalid ownership proof bypassed the existing pin") }
	if !signed.Load() { t.Fatal("QUIC invalid-proof signer was not exercised for the existing pin") }
	afterProof, _ := os.ReadFile(path)
	if !bytes.Equal(stored, afterProof) { t.Fatal("Invalid QUIC proof changed the existing pin") }
	active.Store(&second)
	if err := connect("localhost"); err == nil { t.Fatal("Changed QUIC identity was accepted") }
	after, _ := os.ReadFile(path)
	if !bytes.Equal(stored, after) { t.Fatal("Changed QUIC identity replaced the existing certificate") }
}

func TestRovunetConcurrentEnrollmentPreservesTheFirstIdentity(t *testing.T) {
	first, second := rovunetTestCertificate(t), rovunetTestCertificate(t)
	path := filepath.Join(t.TempDir(), "identity.json")
	start := make(chan struct{})
	winners := make(chan []byte, 8)
	failures := make(chan error, 8)
	var workers sync.WaitGroup
	for index := 0; index < 8; index++ {
		certificate := first.Certificate[0]
		if index%2 != 0 {
			certificate = second.Certificate[0]
		}
		workers.Add(1)
		go func() {
			defer workers.Done()
			<-start
			created, err := rovunetWriteIdentity(path, rovunetIdentity{Version: 1, Certificate: certificate}, false)
			if err != nil {
				failures <- err
			} else if created {
				winners <- certificate
			}
		}()
	}
	close(start)
	workers.Wait()
	close(winners)
	close(failures)
	for err := range failures {
		t.Fatal(err)
	}
	if len(winners) != 1 {
		t.Fatal("Enrollment did not have exactly one winner")
	}
	winner := <-winners
	stored, err := rovunetReadIdentity(path)
	if err != nil || string(stored.Certificate) != string(winner) || stored.PublicCA {
		t.Fatal("Concurrent enrollment replaced the winning identity", err)
	}
	files, err := os.ReadDir(filepath.Dir(path))
	if err != nil || len(files) != 1 {
		t.Fatal("Concurrent enrollment left duplicate certificates or temporary files", err)
	}
}

func rovunetTestCertificate(t *testing.T) tls.Certificate {
	t.Helper()
	key, err := rsa.GenerateKey(rand.Reader, 2048)
	if err != nil {
		t.Fatal(err)
	}
	template := &x509.Certificate{SerialNumber: big.NewInt(time.Now().UnixNano()), Subject: pkix.Name{CommonName: "Synthetic TLS"}, DNSNames: []string{"localhost"}, NotBefore: time.Now().Add(-time.Hour), NotAfter: time.Now().Add(time.Hour), KeyUsage: x509.KeyUsageDigitalSignature | x509.KeyUsageKeyEncipherment, ExtKeyUsage: []x509.ExtKeyUsage{x509.ExtKeyUsageServerAuth}}
	der, err := x509.CreateCertificate(rand.Reader, template, template, &key.PublicKey, key)
	if err != nil {
		t.Fatal(err)
	}
	result, err := tls.X509KeyPair(pem.EncodeToMemory(&pem.Block{Type: "CERTIFICATE", Bytes: der}), pem.EncodeToMemory(&pem.Block{Type: "RSA PRIVATE KEY", Bytes: x509.MarshalPKCS1PrivateKey(key)}))
	if err != nil {
		t.Fatal(err)
	}
	return result
}

func TestRovunetAutomaticCertificateRequiresProofNamesAndExistingIdentity(t *testing.T) {
	listener, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		t.Fatal(err)
	}
	defer listener.Close()
	var active atomic.Pointer[tls.Certificate]
	first := rovunetTestCertificate(t)
	active.Store(&first)
	go func() {
		for {
			raw, err := listener.Accept()
			if err != nil {
				return
			}
			go func() {
				defer raw.Close()
				raw.SetDeadline(time.Now().Add(3 * time.Second))
				server := tls.Server(raw, &tls.Config{GetCertificate: func(*tls.ClientHelloInfo) (*tls.Certificate, error) { return active.Load(), nil }})
				server.Handshake()
			}()
		}
	}()
	previousDirectory, previousEndpoint := RovunetCertificateDirectory, RovunetCertificateEndpoint
	defer func() { RovunetCertificateDirectory = previousDirectory; RovunetCertificateEndpoint = previousEndpoint }()
	RovunetCertificateEndpoint = listener.Addr().String()
	for _, fingerprint := range []bool{false, true} {
		t.Run(map[bool]string{false: "standard", true: "uTLS"}[fingerprint], func(t *testing.T) {
			RovunetCertificateDirectory = t.TempDir()
			connect := func(host string) error {
				config := (&Config{ServerName: host}).GetTLSConfig()
				raw, err := net.DialTimeout("tcp", listener.Addr().String(), 2*time.Second)
				if err != nil {
					return err
				}
				defer raw.Close()
				raw.SetDeadline(time.Now().Add(3 * time.Second))
				if fingerprint {
					fingerprint := utls.HelloChrome_Auto
					connection,_,err := NewGrpcUtls(config, &fingerprint).ClientHandshake(context.Background(),"localhost",raw)
					if connection != nil { connection.Close() }
					return err
				}
				return Client(raw, config).(Interface).HandshakeContext(context.Background())
			}
			var signed atomic.Bool
			forged := first
			wrongKey := rovunetTestCertificate(t)
			forged.PrivateKey = rovunetWrongSigner{first.PrivateKey.(crypto.Signer).Public(), wrongKey.PrivateKey.(crypto.Signer), &signed}
			active.Store(&forged)
			if err := connect("localhost"); err == nil { t.Fatal("Invalid ownership proof was accepted") }
			if !signed.Load() { t.Fatal("Invalid-proof signer was not exercised") }
			proofFiles, _ := os.ReadDir(RovunetCertificateDirectory)
			if len(proofFiles) != 0 { t.Fatal("Invalid ownership proof enrolled a certificate") }
			active.Store(&first)
			if err := connect("wrong.example.invalid"); err == nil {
				t.Fatal("Wrong name enrolled")
			}
			files, _ := os.ReadDir(RovunetCertificateDirectory)
			if len(files) != 0 {
				t.Fatal("Failed handshake stored a certificate")
			}
			if err := connect("localhost"); err != nil {
				t.Fatal(err)
			}
			files, _ = os.ReadDir(RovunetCertificateDirectory)
			if len(files) != 1 {
				t.Fatal("Missing private certificate")
			}
			path := filepath.Join(RovunetCertificateDirectory, files[0].Name())
			before, _ := os.ReadFile(path)
			if err := connect("localhost"); err != nil {
				t.Fatal(err)
			}
			changed := rovunetTestCertificate(t)
			active.Store(&changed)
			if err := connect("localhost"); err == nil {
				t.Fatal("Changed identity accepted")
			}
			after, _ := os.ReadFile(path)
			if string(before) != string(after) {
				t.Fatal("Stored certificate replaced")
			}
			active.Store(&first)
			if err := connect("localhost"); err != nil {
				t.Fatal(err)
			}
		})
	}
}

func TestRovunetPublicCaRenewalCannotDowngradeToUnknownPrivateIdentity(t *testing.T) {
	listener, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		t.Fatal(err)
	}
	defer listener.Close()
	first, second := rovunetTestCertificate(t), rovunetTestCertificate(t)
	var active atomic.Pointer[tls.Certificate]
	active.Store(&first)
	go func() {
		for {
			raw, err := listener.Accept()
			if err != nil {
				return
			}
			go func() {
				defer raw.Close()
				raw.SetDeadline(time.Now().Add(3 * time.Second))
				tls.Server(raw, &tls.Config{GetCertificate: func(*tls.ClientHelloInfo) (*tls.Certificate, error) { return active.Load(), nil }}).Handshake()
			}()
		}
	}()
	previousDirectory, previousEndpoint := RovunetCertificateDirectory, RovunetCertificateEndpoint
	defer func() { RovunetCertificateDirectory = previousDirectory; RovunetCertificateEndpoint = previousEndpoint }()
	RovunetCertificateEndpoint = listener.Addr().String()
	for _, fingerprint := range []bool{false, true} {
		t.Run(map[bool]string{false: "standard", true: "uTLS"}[fingerprint], func(t *testing.T) {
			active.Store(&first)
			RovunetCertificateDirectory = t.TempDir()
			connect := func(trusted *tls.Certificate) error {
				config := (&Config{ServerName: "localhost"}).GetTLSConfig()
				config.RootCAs = x509.NewCertPool()
				if trusted != nil {
					cert, err := x509.ParseCertificate(trusted.Certificate[0])
					if err != nil {
						t.Fatal(err)
					}
					config.RootCAs.AddCert(cert)
				}
				raw, err := net.DialTimeout("tcp", listener.Addr().String(), 2*time.Second)
				if err != nil {
					return err
				}
				defer raw.Close()
				raw.SetDeadline(time.Now().Add(3 * time.Second))
				if fingerprint {
					fingerprint := utls.HelloChrome_Auto
					return UClient(raw, config, &fingerprint).(Interface).HandshakeContext(context.Background())
				}
				return Client(raw, config).(Interface).HandshakeContext(context.Background())
			}
			if err := connect(&first); err != nil {
				t.Fatal("Trusted CA certificate failed", err)
			}
			files, _ := os.ReadDir(RovunetCertificateDirectory)
			if len(files) != 1 {
				t.Fatal("CA certificate was not cached")
			}
			path := filepath.Join(RovunetCertificateDirectory, files[0].Name())
			before, _ := os.ReadFile(path)
			stored, err := rovunetReadIdentity(path)
			if err != nil || !stored.PublicCA {
				t.Fatal("CA identity became a private pin", err)
			}
			active.Store(&second)
			if err := connect(nil); err == nil {
				t.Fatal("Known CA identity downgraded to an unknown self-signed key")
			}
			after, _ := os.ReadFile(path)
			if string(before) != string(after) {
				t.Fatal("Rejected downgrade replaced cached identity")
			}
			if err := connect(&second); err != nil {
				t.Fatal("CA-validated key renewal was blocked", err)
			}
			if err := connect(nil); err != nil {
				t.Fatal("Unchanged known key was forgotten", err)
			}
			stored, err = rovunetReadIdentity(path)
			if err != nil || !stored.PublicCA {
				t.Fatal("Known CA renewal was converted to a private pin", err)
			}
		})
	}
}
