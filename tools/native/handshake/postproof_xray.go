package tls

import (
	"context"
	"crypto/tls"
	"errors"
	"net"
	"net/http"
	"net/url"
	"runtime"
	"sync"
	"weak"
	"github.com/apernet/quic-go"
)

// Weak registration identifies only our policy, preserving other TLS callbacks.
var rovunetPostProof sync.Map

func RovunetCloneTLSConfig(config *tls.Config) *tls.Config {
	if config == nil { return nil }
	clone := config.Clone()
	if _, own := rovunetPostProof.Load(weak.Make(config)); own { rovunetRegisterPostHandshake(clone) }
	return clone
}

func RovunetStandardClient(ctx context.Context, raw net.Conn, config *tls.Config) (*tls.Conn, error) {
	connection := tls.Client(raw, config)
	if err := connection.HandshakeContext(ctx); err != nil { connection.Close(); return nil,err }
	if err := RovunetCertificateVerified(config,connection.ConnectionState()); err != nil { connection.Close();return nil,err }
	return connection,nil
}

func RovunetQuicVerified(ctx context.Context, config *tls.Config, connection *quic.Conn) error {
	select {
	case <-ctx.Done(): connection.CloseWithError(0,"");return ctx.Err()
	case <-connection.Context().Done(): return connection.Context().Err()
	case <-connection.HandshakeComplete():
	}
	if err := RovunetCertificateVerified(config,connection.ConnectionState().TLS); err != nil {connection.CloseWithError(0,"");return err}
	return nil
}

// A standard http.Transport otherwise clones TLS configs and creates crypto/tls
// clients internally, bypassing the post-proof publication hook.
func RovunetWrapHTTPTransport(transport *http.Transport) {
	config := transport.TLSClientConfig
	if _, own := rovunetPostProof.Load(weak.Make(config)); !own {return}
	if proxy := transport.Proxy; proxy != nil {
		transport.Proxy = func(request *http.Request) (*url.URL,error) {
			selected,err := proxy(request)
			if err==nil && selected!=nil && request.URL.Scheme=="https" {return nil,errors.New("automatic certificate proof unsupported for HTTP proxy CONNECT")}
			return selected,err
		}
	}
	if transport.DialTLSContext != nil {return}
	dial := transport.DialContext
	if dial == nil {dial = (&net.Dialer{}).DialContext}
	transport.DialTLSContext = func(ctx context.Context,network,address string)(net.Conn,error) {
		raw,err := dial(ctx,network,address);if err!=nil{return nil,err}
		return RovunetStandardClient(ctx,raw,RovunetCloneTLSConfig(config))
	}
}

func rovunetRegisterPostHandshake(config *tls.Config) {
	key := weak.Make(config)
	rovunetPostProof.Store(key, true)
	runtime.AddCleanup(config, func(key weak.Pointer[tls.Config]) { rovunetPostProof.Delete(key) }, key)
}

func RovunetCertificateVerified(config *tls.Config, state tls.ConnectionState) error {
	if config == nil { return nil }
	if _, own := rovunetPostProof.Load(weak.Make(config)); !own { return nil }
	if !state.HandshakeComplete { return errors.New("certificate ownership proof incomplete") }
	return config.VerifyConnection(state)
}

func (c *Conn) HandshakeContext(ctx context.Context) error {
	if err := c.Conn.HandshakeContext(ctx); err != nil { return err }
	c.rovunetFinalizeOnce.Do(func() { c.rovunetFinalizeError = RovunetCertificateVerified(c.rovunetConfig, c.Conn.ConnectionState()) })
	return c.rovunetFinalizeError
}
func (c *Conn) Handshake() error { return c.HandshakeContext(context.Background()) }
func (c *Conn) Read(p []byte) (int,error) {
	if err := c.Handshake(); err != nil { return 0,err }; return c.Conn.Read(p)
}
func (c *Conn) Write(p []byte) (int,error) {
	if err := c.Handshake(); err != nil { return 0,err }; return c.Conn.Write(p)
}
func (c *UConn) HandshakeContext(ctx context.Context) error {
	if err := c.UConn.HandshakeContext(ctx); err != nil { return err }
	state := c.UConn.ConnectionState()
	c.rovunetFinalizeOnce.Do(func() { c.rovunetFinalizeError = RovunetCertificateVerified(c.rovunetConfig, tls.ConnectionState{HandshakeComplete:state.HandshakeComplete,
		ServerName:state.ServerName,PeerCertificates:state.PeerCertificates,VerifiedChains:state.VerifiedChains}) })
	return c.rovunetFinalizeError
}
func (c *UConn) Handshake() error { return c.HandshakeContext(context.Background()) }
func (c *UConn) Read(p []byte) (int,error) {
	if err := c.Handshake(); err != nil { return 0,err }; return c.UConn.Read(p)
}
func (c *UConn) Write(p []byte) (int,error) {
	if err := c.Handshake(); err != nil { return 0,err }; return c.UConn.Write(p)
}
