// One bounded protocol probe, stdin configuration and redacted JSON result only.
package main

import (
	"context"
	"crypto/x509"
	"encoding/json"
	"errors"
	"io"
	"net"
	"os"
	"time"
)

type config struct {
	Version      int            `json:"version"`
	Protocol     string         `json:"protocol"`
	Server       string         `json:"server"`
	Port         int            `json:"port"`
	ServerName   string         `json:"server_name"`
	ALPN         []string       `json:"alpn"`
	Password     string         `json:"password"`
	UUID         string         `json:"uuid"`
	ObfsPassword string         `json:"obfs_password"`
	PrivateKey   string         `json:"private_key"`
	PublicKey    string         `json:"public_key"`
	PreSharedKey string         `json:"pre_shared_key"`
	AWG          map[string]any `json:"awg_options"`
	UDPMode      string         `json:"udp_mode"`
	TimeoutMS    int            `json:"timeout_ms"`
}
type result struct {
	Version       int    `json:"version"`
	Authenticated bool   `json:"authenticated"`
	Mechanism     string `json:"mechanism"`
	ElapsedMS     int64  `json:"elapsed_ms"`
	Error         string `json:"error"`
}
type probeError string

func (e probeError) Error() string { return string(e) }

const timeoutError = probeError("HANDSHAKE_TIMEOUT")
const rejectedError = probeError("HANDSHAKE_REJECTED")
const unconfirmedError = probeError("HANDSHAKE_UNCONFIRMED")

func probe(parent context.Context, c config, roots *x509.CertPool) result {
	r := result{Version: 1}
	if c.Version != 1 || c.Server == "" || c.Port < 1 || c.Port > 65535 || c.TimeoutMS < 100 || c.TimeoutMS > 10000 {
		r.Error = "INVALID_MESSAGE"
		return r
	}
	ctx, cancel := context.WithTimeout(parent, time.Duration(c.TimeoutMS)*time.Millisecond)
	defer cancel()
	if ctx.Err() != nil {
		r.Error = "CANCELLED"
		return r
	}
	var elapsed time.Duration
	var err error
	switch c.Protocol {
	case "hysteria2":
		r.Mechanism = "hysteria2-auth"
		elapsed, err = probeHY(ctx, c, roots)
	case "tuic":
		r.Mechanism = "tuic-authenticated-relay"
		elapsed, err = probeTUIC(ctx, c, roots)
	case "wireguard", "amneziawg":
		r.Mechanism = c.Protocol + "-noise"
		elapsed, err = probeWG(ctx, c)
	default:
		err = probeError("HANDSHAKE_UNSUPPORTED")
	}
	if err == nil {
		r.Authenticated = true
		r.ElapsedMS = max(1, elapsed.Milliseconds())
		return r
	}
	var fixed probeError
	var unknown x509.UnknownAuthorityError
	var hostname x509.HostnameError
	var invalid x509.CertificateInvalidError
	var network *net.DNSError
	switch {
	case errors.As(err, &fixed):
		r.Error = string(fixed)
	case errors.Is(ctx.Err(), context.DeadlineExceeded), errors.Is(err, context.DeadlineExceeded):
		r.Error = string(timeoutError)
	case errors.Is(ctx.Err(), context.Canceled):
		r.Error = "CANCELLED"
	case errors.As(err, &unknown), errors.As(err, &hostname), errors.As(err, &invalid):
		r.Error = "TLS_UNVERIFIED"
	case errors.As(err, &network):
		r.Error = "HANDSHAKE_DNS_FAILED"
	default:
		r.Error = "HANDSHAKE_FAILED"
	}
	return r
}
func run(input io.Reader, output io.Writer) {
	var c config
	decoder := json.NewDecoder(io.LimitReader(input, 65537))
	decoder.DisallowUnknownFields()
	if decoder.Decode(&c) != nil || decoder.Decode(new(any)) != io.EOF {
		json.NewEncoder(output).Encode(result{Version: 1, Error: "INVALID_MESSAGE"})
		return
	}
	// Production always uses system trust. Test roots are supplied only to probe() in Go tests.
	json.NewEncoder(output).Encode(probe(context.Background(), c, nil))
}
func main() {
	if len(os.Args) != 1 {
		os.Exit(12)
	}
	run(os.Stdin, os.Stdout)
}
