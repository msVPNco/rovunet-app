package main

import (
	"bytes"
	"context"
	"crypto/rand"
	"crypto/x509"
	"encoding/binary"
	"encoding/hex"
	"errors"
	"github.com/metacubex/http"
	"github.com/metacubex/quic-go"
	"github.com/metacubex/quic-go/http3"
	hy "github.com/metacubex/sing-quic/hysteria2"
	"github.com/metacubex/tls"
	"io"
	"net"
	"net/url"
	"strings"
	"time"
)

func openQUIC(ctx context.Context, c config, roots *x509.CertPool) (*quic.Conn, func(), time.Time, error) {
	ip, err := net.DefaultResolver.LookupNetIP(ctx, "ip", c.Server)
	if err != nil {
		return nil, nil, time.Time{}, err
	}
	if len(ip) == 0 {
		return nil, nil, time.Time{}, probeError("HANDSHAKE_DNS_FAILED")
	}
	address := &net.UDPAddr{IP: ip[0].AsSlice(), Port: c.Port}
	network := "udp4"
	if address.IP.To4() == nil {
		network = "udp6"
	}
	var lc net.ListenConfig
	packet, err := lc.ListenPacket(ctx, network, ":0")
	if err != nil {
		return nil, nil, time.Time{}, err
	}
	if c.Protocol == "hysteria2" && c.ObfsPassword != "" {
		packet = hy.NewSalamanderConn(packet, []byte(c.ObfsPassword))
	}
	transport := &quic.Transport{Conn: packet}
	closeAll := func() { transport.Close(); packet.Close() }
	alpn := c.ALPN
	if len(alpn) == 0 {
		alpn = []string{"h3"}
	}
	name := c.ServerName
	if name == "" {
		name = c.Server
	}
	start := time.Now()
	q, err := transport.Dial(ctx, address, &tls.Config{ServerName: name, RootCAs: roots, MinVersion: tls.VersionTLS13, NextProtos: alpn}, &quic.Config{EnableDatagrams: true, HandshakeIdleTimeout: time.Duration(c.TimeoutMS) * time.Millisecond, MaxIdleTimeout: time.Duration(c.TimeoutMS) * time.Millisecond, MaxIncomingStreams: 4, MaxIncomingUniStreams: 4})
	if err != nil {
		closeAll()
		return nil, nil, start, err
	}
	return q, func() { q.CloseWithError(0, ""); closeAll() }, start, nil
}
func probeHY(ctx context.Context, c config, roots *x509.CertPool) (time.Duration, error) {
	if c.Password == "" {
		return 0, probeError("INVALID_MESSAGE")
	}
	q, closeAll, start, err := openQUIC(ctx, c, roots)
	if err != nil {
		return 0, err
	}
	defer closeAll()
	transport := &http3.Transport{Dial: func(context.Context, string, *tls.Config, *quic.Config) (*quic.Conn, error) { return q, nil }}
	defer transport.Close()
	// HTTP/3 auth is part of HY2 itself; this is not an external HTTPS connectivity probe.
	request := &http.Request{Method: "POST", URL: &url.URL{Scheme: "https", Host: "hysteria", Path: "/auth"}, Header: make(http.Header)}
	request.Header.Set("Hysteria-Auth", c.Password)
	request.Header.Set("Hysteria-CC-RX", "0")
	response, err := transport.RoundTrip(request.WithContext(ctx))
	if err != nil {
		return 0, err
	}
	defer response.Body.Close()
	if response.StatusCode != 233 {
		return 0, rejectedError
	}
	return time.Since(start), nil
}
func dnsQuestion() []byte {
	entropy := make([]byte, 8)
	rand.Read(entropy)
	name := hex.EncodeToString(entropy) + ".example.com"
	b := make([]byte, 12)
	rand.Read(b[:2])
	b[2] = 1
	b[5] = 1
	for _, label := range strings.Split(name, ".") {
		b = append(b, byte(len(label)))
		b = append(b, label...)
	}
	return append(b, 0, 0, 1, 0, 1)
}
func tuicPacket(assoc uint16, query []byte, ip net.IP) []byte {
	b := make([]byte, 10)
	b[0] = 5
	b[1] = 2
	binary.BigEndian.PutUint16(b[2:4], assoc)
	b[6] = 1
	binary.BigEndian.PutUint16(b[8:10], uint16(len(query)))
	b = append(b, 1)
	b = append(b, ip.To4()...)
	b = append(b, 0, 53)
	return append(b, query...)
}
func validReply(packet, query []byte, assoc uint16, ip net.IP) bool {
	if len(packet) < 17+len(query) || packet[0] != 5 || packet[1] != 2 || binary.BigEndian.Uint16(packet[2:4]) != assoc || packet[6] != 1 || packet[7] != 0 || packet[10] != 1 || !bytes.Equal(packet[11:15], ip.To4()) || binary.BigEndian.Uint16(packet[15:17]) != 53 {
		return false
	}
	size := int(binary.BigEndian.Uint16(packet[8:10]))
	if size != len(packet)-17 {
		return false
	}
	dns := packet[17:]
	return bytes.Equal(dns[:2], query[:2]) && dns[2]&0x80 != 0 && dns[2]&0x78 == 0 && dns[4] == 0 && dns[5] == 1 && bytes.Equal(dns[12:len(query)], query[12:])
}
func probeTUIC(ctx context.Context, c config, roots *x509.CertPool) (time.Duration, error) {
	uuid, err := hex.DecodeString(strings.ReplaceAll(c.UUID, "-", ""))
	if err != nil || len(uuid) != 16 || c.Password == "" || c.UDPMode != "" && c.UDPMode != "native" && c.UDPMode != "quic" {
		return 0, probeError("INVALID_MESSAGE")
	}
	q, closeAll, start, err := openQUIC(ctx, c, roots)
	if err != nil {
		return 0, err
	}
	defer closeAll()
	state := q.ConnectionState()
	token, err := state.TLS.ExportKeyingMaterial(string(uuid), []byte(c.Password), 32)
	if err != nil {
		return 0, err
	}
	auth, err := q.OpenUniStreamSync(ctx)
	if err != nil {
		return 0, err
	}
	if _, err = auth.Write(append(append([]byte{5, 0}, uuid...), token...)); err != nil {
		return 0, err
	}
	if err = auth.Close(); err != nil {
		return 0, err
	}
	// TUIC has no authentication-success ACK. A matching DNS response returned
	// through an authenticated relay is the proof; QUIC/heartbeat alone never pass.
	var id [2]byte
	rand.Read(id[:])
	assoc := binary.BigEndian.Uint16(id[:])
	query := dnsQuestion()
	answers := make(chan []byte, 4)
	go func() {
		for {
			b, e := q.ReceiveDatagram(ctx)
			if e != nil {
				return
			}
			select {
			case answers <- b:
			case <-ctx.Done():
				return
			}
		}
	}()
	go func() {
		for {
			stream, e := q.AcceptUniStream(ctx)
			if e != nil {
				return
			}
			b, e := io.ReadAll(io.LimitReader(stream, 4097))
			stream.CancelRead(0)
			if e == nil && len(b) <= 4096 {
				select {
				case answers <- b:
				case <-ctx.Done():
					return
				}
			}
		}
	}()
	ips := []net.IP{net.IPv4(1, 1, 1, 1), net.IPv4(8, 8, 8, 8)}
	send := func(ip net.IP) error {
		packet := tuicPacket(assoc, query, ip)
		if c.UDPMode != "quic" {
			return q.SendDatagram(packet)
		}
		stream, e := q.OpenUniStreamSync(ctx)
		if e != nil {
			return e
		}
		if _, e = stream.Write(packet); e != nil {
			return e
		}
		return stream.Close()
	}
	if err = send(ips[0]); err != nil {
		return 0, err
	}
	fallback := time.NewTimer(350 * time.Millisecond)
	defer fallback.Stop()
	for {
		select {
		case packet := <-answers:
			for _, ip := range ips {
				if validReply(packet, query, assoc, ip) {
					return time.Since(start), nil
				}
			}
		case <-fallback.C:
			if err = send(ips[1]); err != nil {
				return 0, err
			}
		case <-ctx.Done():
			if errors.Is(ctx.Err(), context.Canceled) {
				return 0, ctx.Err()
			}
			return 0, unconfirmedError
		case <-q.Context().Done():
			var app *quic.ApplicationError
			if errors.As(context.Cause(q.Context()), &app) && uint64(app.ErrorCode) == 0xfffffff1 {
				return 0, rejectedError
			}
			return 0, unconfirmedError
		}
	}
}
