package main

import (
	"context"
	"encoding/base64"
	"encoding/hex"
	"errors"
	"fmt"
	awg "github.com/metacubex/amneziawg-go/device"
	awg1 "github.com/metacubex/amneziawg-go/device_v1"
	"github.com/metacubex/wireguard-go/conn"
	wg "github.com/metacubex/wireguard-go/device"
	"github.com/metacubex/wireguard-go/tun"
	"io"
	"net"
	"os"
	"sort"
	"strconv"
	"strings"
	"sync"
	"time"
)

// This is a userspace dummy interface. No OS adapter, address, route or DNS is created.
type nullTun struct {
	closed chan struct{}
	events chan tun.Event
	once   sync.Once
}

func newNullTun() *nullTun {
	return &nullTun{closed: make(chan struct{}), events: make(chan tun.Event, 1)}
}
func (t *nullTun) File() *os.File                         { return nil }
func (t *nullTun) Read([][]byte, []int, int) (int, error) { <-t.closed; return 0, io.EOF }
func (t *nullTun) Write(b [][]byte, _ int) (int, error)   { return len(b), nil }
func (t *nullTun) MTU() (int, error)                      { return 1280, nil }
func (t *nullTun) Name() (string, error)                  { return "rovunet-handshake-only", nil }
func (t *nullTun) Events() <-chan tun.Event               { return t.events }
func (t *nullTun) BatchSize() int                         { return 1 }
func (t *nullTun) Close() error                           { t.once.Do(func() { close(t.closed); close(t.events) }); return nil }

type plainBind struct{ conn.Bind } // Do not expose Windows route-notification extensions.
type probeDevice interface {
	IpcSet(string) error
	IpcGet() (string, error)
	Up() error
	Close()
}

func decodeKey(text string) ([]byte, error) {
	b, e := base64.StdEncoding.DecodeString(text)
	if e != nil || len(b) != 32 {
		return nil, probeError("INVALID_MESSAGE")
	}
	return b, nil
}
func wgConfig(c config, endpoint string) (string, [32]byte, error) {
	var public [32]byte
	private, e := decodeKey(c.PrivateKey)
	if e != nil {
		return "", public, e
	}
	p, e := decodeKey(c.PublicKey)
	if e != nil {
		return "", public, e
	}
	copy(public[:], p)
	ipc := "private_key=" + hex.EncodeToString(private) + "\n"
	allowed := map[string]string{"jc": "jc", "jmin": "jmin", "jmax": "jmax", "s1": "s1", "s2": "s2", "s3": "s3", "s4": "s4", "h1": "h1", "h2": "h2", "h3": "h3", "h4": "h4", "i1": "i1", "i2": "i2", "i3": "i3", "i4": "i4", "i5": "i5", "j1": "j1", "j2": "j2", "j3": "j3", "itime": "itime", "header-protection-key": "header_protection_key", "content-padding-addition": "content_padding_addition", "rekey-after-time": "rekey_after_time", "rekey-timeout": "rekey_timeout", "reject-after-time": "reject_after_time", "keepalive-timeout": "keepalive_timeout", "max-handshake-attempts": "max_handshake_attempts", "random-trailers": "random_trailers", "disable-cookies": "disable_cookies"}
	keys := make([]string, 0, len(c.AWG))
	for key := range c.AWG {
		keys = append(keys, key)
	}
	sort.Slice(keys, func(i, j int) bool {
		if keys[i] == "header-protection-key" {
			return false
		}
		if keys[j] == "header-protection-key" {
			return true
		}
		return keys[i] < keys[j]
	})
	for _, key := range keys {
		value := c.AWG[key]
		if key == "version" {
			continue
		}
		field, ok := allowed[key]
		if !ok {
			return "", public, probeError("HANDSHAKE_UNSUPPORTED")
		}
		text := fmt.Sprint(value)
		if key == "header-protection-key" {
			b, e := decodeKey(text)
			if e != nil {
				return "", public, e
			}
			text = hex.EncodeToString(b)
		}
		if len(text) > 2048 || strings.ContainsAny(text, "\r\n\x00") {
			return "", public, probeError("INVALID_MESSAGE")
		}
		ipc += field + "=" + text + "\n"
	}
	ipc += "public_key=" + hex.EncodeToString(p) + "\nendpoint=" + endpoint + "\n"
	if c.PreSharedKey != "" {
		b, e := decodeKey(c.PreSharedKey)
		if e != nil {
			return "", public, e
		}
		ipc += "preshared_key=" + hex.EncodeToString(b) + "\n"
	}
	return ipc, public, nil
}
func probeWG(ctx context.Context, c config) (time.Duration, error) {
	ips, e := net.DefaultResolver.LookupNetIP(ctx, "ip", c.Server)
	if e != nil {
		return 0, e
	}
	if len(ips) == 0 {
		return 0, probeError("HANDSHAKE_DNS_FAILED")
	}
	ipc, public, e := wgConfig(c, net.JoinHostPort(ips[0].Unmap().String(), strconv.Itoa(c.Port)))
	if e != nil {
		return 0, e
	}
	t := newNullTun()
	defer t.Close()
	bind := plainBind{conn.NewStdNetBind()}
	logger := wg.NewLogger(wg.LogLevelSilent, "")
	var d probeDevice
	var initiate func() error
	switch {
	case c.Protocol == "wireguard":
		dev := wg.NewDevice(t, bind, logger, 2)
		d = dev
		initiate = func() error {
			p := dev.LookupPeer(wg.NoisePublicKey(public))
			if p == nil {
				return errors.New("peer")
			}
			return p.SendHandshakeInitiation(false)
		}
	case c.AWG["version"] == float64(3):
		dev := awg.NewDevice(t, bind, logger, 2)
		d = dev
		initiate = func() error {
			p := dev.LookupPeer(awg.NoisePublicKey(public))
			if p == nil {
				return errors.New("peer")
			}
			return p.SendHandshakeInitiation(false)
		}
	default:
		dev := awg1.NewDevice(t, bind, logger, 2)
		d = dev
		initiate = func() error {
			p := dev.LookupPeer(awg1.NoisePublicKey(public))
			if p == nil {
				return errors.New("peer")
			}
			return p.SendHandshakeInitiation(false)
		}
	}
	defer d.Close()
	if e = d.IpcSet(ipc); e != nil {
		return 0, probeError("INVALID_MESSAGE")
	}
	if e = d.Up(); e != nil {
		return 0, e
	}
	start := time.Now()
	if e = initiate(); e != nil {
		return 0, e
	}
	ticker := time.NewTicker(2 * time.Millisecond)
	defer ticker.Stop()
	for {
		select {
		case <-ctx.Done():
			return 0, ctx.Err()
		case <-ticker.C:
			// This timestamp is updated only by cryptographically verified handshake completion.
			state, e := d.IpcGet()
			if e != nil {
				return 0, e
			}
			for _, line := range strings.Split(state, "\n") {
				if strings.HasPrefix(line, "last_handshake_time_sec=") {
					seconds, e := strconv.ParseInt(strings.TrimPrefix(line, "last_handshake_time_sec="), 10, 64)
					if e == nil && seconds > 0 {
						return time.Since(start), nil
					}
				}
			}
		}
	}
}
