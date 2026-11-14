package main

import (
	"encoding/json"
	"testing"
)

func hy2MappingFixture(mask any) map[string]any {
	stream := map[string]any{"network": "hysteria", "security": "tls",
		"tlsSettings":      map[string]any{"serverName": "peer.example.invalid", "alpn": []string{"h3"}},
		"hysteriaSettings": map[string]any{"auth": "user:pa+ss%40word:/"}}
	if mask != nil {
		stream["finalmask"] = mask
	}
	return map[string]any{"protocol": "hysteria", "settings": map[string]any{"version": 2, "address": "example.invalid", "port": 8443}, "streamSettings": stream}
}

func salamanderFixture(password string) map[string]any {
	return map[string]any{"type": "salamander", "settings": map[string]any{"password": password}}
}

func TestHy2ProbePreservesExactlyDecodedCredentialsAndSalamander(t *testing.T) {
	out := hy2MappingFixture(map[string]any{"udp": []any{salamanderFixture("mask+%40:☃")}})
	data, err := json.Marshal(out)
	if err != nil {
		t.Fatal(err)
	}
	proxy, err := probeMapping(data)
	if err != nil {
		t.Fatal(err)
	}
	want := map[string]any{"type": "hysteria2", "server": "example.invalid", "port": float64(8443),
		"password": "user:pa+ss%40word:/", "sni": "peer.example.invalid", "skip-cert-verify": false,
		"obfs": "salamander", "obfs-password": "mask+%40:☃"}
	for key, value := range want {
		if proxy[key] != value {
			t.Fatalf("HY2 mapping changed field %s", key) // Never print credential values.
		}
	}
	alpn, ok := proxy["alpn"].([]any)
	if !ok || len(alpn) != 1 || alpn[0] != "h3" {
		t.Fatal("HY2 ALPN changed")
	}
}

func TestHy2ProbeRejectsAdditionalMasksHoppingAndWeakTls(t *testing.T) {
	layer := salamanderFixture("synthetic-mask")
	for _, mask := range []any{
		map[string]any{}, map[string]any{"udp": []any{}},
		map[string]any{"udp": []any{layer, layer}},
		map[string]any{"udp": []any{map[string]any{"type": "udphop", "settings": map[string]any{"remotePorts": "443,8443"}}}},
		map[string]any{"udp": []any{layer}, "tcp": []any{}},
		map[string]any{"udp": []any{salamanderFixture("")}},
		map[string]any{"udp": []any{map[string]any{"type": "salamander", "settings": map[string]any{"password": "synthetic", "unknown": true}}}},
	} {
		data, _ := json.Marshal(hy2MappingFixture(mask))
		if _, err := probeMapping(data); err == nil {
			t.Fatal("Unsupported mask became a simplified probe")
		}
	}
	for key, value := range map[string]any{"allowInsecure": true, "cipherSuites": "TLS_AES_128_GCM_SHA256", "alpn": []string{"unsupported"}} {
		out := hy2MappingFixture(nil)
		out["streamSettings"].(map[string]any)["tlsSettings"].(map[string]any)[key] = value
		data, _ := json.Marshal(out)
		if _, err := probeMapping(data); err == nil {
			t.Fatalf("Unsupported TLS field accepted: %s", key)
		}
	}
}
