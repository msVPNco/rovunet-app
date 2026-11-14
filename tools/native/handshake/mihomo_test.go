package ca

import (
	"bytes"
	"context"
	"crypto"
	"crypto/rand"
	"crypto/rsa"
	"crypto/x509"
	"crypto/x509/pkix"
	"io"
	"errors"
	"math/big"
	"net"
	"os"
	"path/filepath"
	"sync/atomic"
	"testing"
	"time"
	"github.com/metacubex/tls"
)

func TestRovunetMihomoPublicationGatesConcurrentIo(t *testing.T) {
	key,err:=rsa.GenerateKey(rand.Reader,2048);if err!=nil{t.Fatal(err)}
	template:=&x509.Certificate{SerialNumber:big.NewInt(1),DNSNames:[]string{"localhost"},NotBefore:time.Now().Add(-time.Hour),NotAfter:time.Now().Add(time.Hour),KeyUsage:x509.KeyUsageDigitalSignature,ExtKeyUsage:[]x509.ExtKeyUsage{x509.ExtKeyUsageServerAuth}}
	der,err:=x509.CreateCertificate(rand.Reader,template,template,&key.PublicKey,key);if err!=nil{t.Fatal(err)}
	parsed,err:=x509.ParseCertificate(der);if err!=nil{t.Fatal(err)}
	roots:=x509.NewCertPool();roots.AddCert(parsed)
	left,right:=net.Pipe();defer left.Close();defer right.Close()
	left.SetDeadline(time.Now().Add(3*time.Second));right.SetDeadline(time.Now().Add(3*time.Second))
	entered,release:=make(chan struct{}),make(chan struct{})
	defer func(){select{case <-release:default:close(release)}}()
	refused:=errors.New("synthetic publication conflict")
	client:=tls.Client(left,&tls.Config{ServerName:"localhost",RootCAs:roots,RovunetPostHandshake:func(state tls.ConnectionState)error{
		if !state.HandshakeComplete {t.Error("Publication hook preceded proof")};close(entered);<-release;return refused
	}})
	server:=tls.Server(right,&tls.Config{Certificates:[]tls.Certificate{{Certificate:[][]byte{der},PrivateKey:key}}})
	read:=make(chan int,1)
	go func(){if err:=server.Handshake();err!=nil{read<-0;return};n,_:=server.Read(make([]byte,32));read<-n}()
	handshake:=make(chan error,1);go func(){handshake<-client.Handshake()}()
	select{case <-entered:case <-time.After(2*time.Second):t.Fatal("Publication gate was not exercised")}
	written:=make(chan int,1);go func(){n,_:=client.Write([]byte("synthetic app data"));written<-n}()
	select{case n:=<-written:t.Fatalf("Concurrent I/O bypassed pending publication: %d bytes",n);case <-time.After(75*time.Millisecond):}
	close(release)
	if err:=<-handshake;!errors.Is(err,refused){t.Fatal("Publication conflict did not reject handshake",err)}
	if n:=<-written;n!=0{t.Fatal("Publication conflict released application data",n)}
	if n:=<-read;n!=0{t.Fatal("Server received application data despite publication conflict",n)}
}

type rovunetInvalidSigner struct { public crypto.PublicKey; secret crypto.Signer; called *atomic.Bool }
func (s rovunetInvalidSigner) Public() crypto.PublicKey { return s.public }
func (s rovunetInvalidSigner) Sign(r io.Reader, digest []byte, options crypto.SignerOpts) ([]byte,error) {
	s.called.Store(true);return s.secret.Sign(r,digest,options)
}

func TestRovunetMihomoCertificatePublicationRequiresOwnershipProof(t *testing.T) {
	makeCert:=func() tls.Certificate {
		key,err:=rsa.GenerateKey(rand.Reader,2048);if err!=nil { t.Fatal(err) }
		template:=&x509.Certificate{SerialNumber:big.NewInt(time.Now().UnixNano()),Subject:pkix.Name{CommonName:"Synthetic TLS"},DNSNames:[]string{"localhost"},NotBefore:time.Now().Add(-time.Hour),NotAfter:time.Now().Add(time.Hour),KeyUsage:x509.KeyUsageDigitalSignature,ExtKeyUsage:[]x509.ExtKeyUsage{x509.ExtKeyUsageServerAuth}}
		der,err:=x509.CreateCertificate(rand.Reader,template,template,&key.PublicKey,key);if err!=nil {t.Fatal(err)}
		return tls.Certificate{Certificate:[][]byte{der},PrivateKey:key}
	}
	first,second:=makeCert(),makeCert()
	var active atomic.Pointer[tls.Certificate]
	active.Store(&first)
	listener,err:=net.Listen("tcp","127.0.0.1:0");if err!=nil {t.Fatal(err)}
	defer listener.Close()
	go func() { for { raw,err:=listener.Accept();if err!=nil{return};go func(){defer raw.Close();raw.SetDeadline(time.Now().Add(3*time.Second));tls.Server(raw,&tls.Config{GetCertificate:func(*tls.ClientHelloInfo)(*tls.Certificate,error){return active.Load(),nil}}).Handshake()}() } }()
	oldDir,oldPeer:=RovunetCertificateDirectory,RovunetCertificateEndpoint
	defer func(){RovunetCertificateDirectory,RovunetCertificateEndpoint=oldDir,oldPeer}()
	RovunetCertificateDirectory,RovunetCertificateEndpoint=t.TempDir(),listener.Addr().String()
	connect:=func(host string,canceled bool)error {
		config:=&tls.Config{ServerName:host};rovunetAutomaticCertificate(config,Option{})
		raw,err:=net.DialTimeout("tcp",listener.Addr().String(),time.Second);if err!=nil{return err};defer raw.Close()
		raw.SetDeadline(time.Now().Add(2*time.Second))
		ctx,cancel:=context.WithTimeout(context.Background(),2*time.Second);defer cancel();if canceled{cancel()}
		return tls.Client(raw,config).HandshakeContext(ctx)
	}
	if err:=connect("localhost",true);err==nil {t.Fatal("Canceled handshake succeeded")}
	files,_:=os.ReadDir(RovunetCertificateDirectory);if len(files)!=0 {t.Fatal("Canceled handshake published a pin")}
	var signed atomic.Bool
	forged:=first;forged.PrivateKey=rovunetInvalidSigner{first.PrivateKey.(crypto.Signer).Public(),second.PrivateKey.(crypto.Signer),&signed}
	active.Store(&forged)
	if err:=connect("localhost",false);err==nil {t.Fatal("Invalid TLS ownership proof accepted")}
	if !signed.Load(){t.Fatal("Invalid-proof signer was not exercised")}
	files,_=os.ReadDir(RovunetCertificateDirectory);if len(files)!=0 {t.Fatal("Invalid proof enrolled a pin")}
	active.Store(&first)
	if err:=connect("wrong.example.invalid",false);err==nil {t.Fatal("Wrong certificate name accepted")}
	if err:=connect("localhost",false);err!=nil {t.Fatal("Verified enrollment failed",err)}
	files,_=os.ReadDir(RovunetCertificateDirectory);if len(files)!=1 {t.Fatal("Verified peer did not create exactly one pin")}
	path:=filepath.Join(RovunetCertificateDirectory,files[0].Name());stored,_:=os.ReadFile(path)
	if err:=connect("localhost",false);err!=nil {t.Fatal("Repeated verified connection failed",err)}
	active.Store(&forged);signed.Store(false)
	if err:=connect("localhost",false);err==nil || !signed.Load(){t.Fatal("Invalid proof did not fail after signing")}
	after,_:=os.ReadFile(path);if !bytes.Equal(stored,after){t.Fatal("Invalid proof rewrote the existing pin")}
	active.Store(&second)
	if err:=connect("localhost",false);err==nil {t.Fatal("Changed private identity accepted")}
	after,_=os.ReadFile(path);if !bytes.Equal(stored,after){t.Fatal("Changed identity rewrote the existing pin")}
}
