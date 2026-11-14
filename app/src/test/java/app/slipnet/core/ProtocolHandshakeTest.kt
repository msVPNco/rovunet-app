package app.slipnet.core

import org.junit.Assert.*
import org.junit.Test
import org.json.JSONObject

class ProtocolHandshakeTest {
    @Test fun quickProbeNeverReplacesUnsupportedUdpMasksWithPlainQuic() {
        assertTrue(ProtocolHandshake.supports("hy2://synthetic@example.invalid:443?sni=example.invalid"))
        assertTrue(ProtocolHandshake.supports("tuic://00000000-0000-4000-8000-000000000001:synthetic@example.invalid:443?sni=example.invalid"))
        assertFalse(ProtocolHandshake.supports("hy2://synthetic@example.invalid:443?mport=443,8443&hop-interval=30"))
        assertFalse(ProtocolHandshake.supports("vless://00000000-0000-4000-8000-000000000001@example.invalid:443?type=kcp&security=none"))
        assertFalse(ProtocolHandshake.supports("mierus://synthetic:secret@example.invalid?profile=fixture&port=8443&protocol=UDP"))
    }

    @Test fun salamanderIsMappedOnlyWhenItIsTheEntireUdpMask() {
        val link = "hy2://synthetic@example.invalid:443?obfs=salamander&obfs-password=synthetic-mask"
        assertTrue(ProtocolHandshake.supports(link))
        assertFalse(ProtocolHandshake.supports(link + "&mport=443,8443"))
        val extra = CoreSharing.enc("{\"tcp\":[{\"type\":\"fragment\",\"settings\":{\"packets\":\"tlshello\",\"length\":\"100-200\",\"interval\":\"10-20\"}}]}")
        assertFalse(ProtocolHandshake.supports(link + "&fm=" + extra))
        assertFalse(ProtocolHandshake.supports(link.replace("salamander", "unsupported-mask")))
        assertFalse(ProtocolHandshake.supports(link + "&insecure=1"))
    }

    @Test fun hy2UriEditorAndRuntimePreserveAuthObfsSniAndAlpnWithoutDoubleDecoding() {
        val link = "hy2://user%3Apa%2Bss%2540word%3A%2F@example.invalid:8443?sni=peer.example.invalid&obfs=salamander&obfs-password=mask%2B%2540%3A%E2%98%83&alpn=h3"
        for (text in listOf(link, CoreForm.decode(link).encode("Synthetic fixture"))) {
            val config = JSONObject(CoreLink.runtime(text, 10001, "local-fixture", "synthetic-only"))
            val out = config.getJSONArray("outbounds").getJSONObject(0)
            assertEquals("hysteria", out.getString("protocol"))
            assertEquals("example.invalid", out.getJSONObject("settings").getString("address"))
            assertEquals(8443, out.getJSONObject("settings").getInt("port"))
            val stream = out.getJSONObject("streamSettings")
            assertEquals("user:pa+ss%40word:/", stream.getJSONObject("hysteriaSettings").getString("auth"))
            assertEquals("peer.example.invalid", stream.getJSONObject("tlsSettings").getString("serverName"))
            assertEquals("h3", stream.getJSONObject("tlsSettings").getJSONArray("alpn").getString(0))
            assertEquals("mask+%40:☃", stream.getJSONObject("finalmask").getJSONArray("udp").getJSONObject(0)
                .getJSONObject("settings").getString("password"))
        }
    }

    @Test fun aLegacyHelperOrListenerCannotClaimASalamanderHandshake() {
        val legacy = JSONObject().put("kind", "protocol-handshake").put("latencyMs", 4)
        assertEquals(4L, ProtocolHandshake.confirmedLatency(legacy, false))
        assertTrue(runCatching { ProtocolHandshake.confirmedLatency(legacy, true) }.isFailure)
        assertEquals(4L, ProtocolHandshake.confirmedLatency(JSONObject(legacy.toString()).put("mappingRevision", 2), true))
        for (revision in listOf(0, 1, 3)) assertTrue(runCatching {
            ProtocolHandshake.confirmedLatency(JSONObject(legacy.toString()).put("mappingRevision", revision), true)
        }.isFailure)
        assertTrue(runCatching { ProtocolHandshake.confirmedLatency(JSONObject(legacy.toString())
            .put("mappingRevision", 2).put("kind", "local-listener"), true) }.isFailure)
    }
}
