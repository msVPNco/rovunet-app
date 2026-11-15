package app.slipnet.service

import androidx.activity.compose.setContent
import androidx.lifecycle.ViewModelProvider
import androidx.test.core.app.ActivityScenario
import androidx.test.platform.app.InstrumentationRegistry
import app.slipnet.BuildConfig
import app.slipnet.core.*
import app.slipnet.domain.model.*
import app.slipnet.presentation.MainActivity
import app.slipnet.presentation.main.MainViewModel
import app.slipnet.tunnel.*
import app.slipnet.util.ConnectionDiagnostics
import com.jcraft.jsch.*
import kotlinx.coroutines.*
import kotlinx.coroutines.flow.first
import org.json.JSONObject
import org.junit.Assert.*
import org.junit.Test
import java.io.File
import java.net.Socket
import java.net.InetSocketAddress
import java.net.ServerSocket
import java.security.MessageDigest

/** Explicit opt-in diagnostic. Credentials arrive in app-private RAM/input, never instrumentation arguments. */
class Post49RuntimeParityDeviceTest {
    @Test fun observeIsolatedProtocolStages() = runBlocking<Unit> {
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        val context = instrumentation.targetContext
        check(BuildConfig.PRIVATE_CANDIDATE)
        val mode = InstrumentationRegistry.getArguments().getString("parityMode") ?: error("Missing diagnostic mode")
        require(mode in setOf("tuic", "mierutcp", "mieruudp", "slipssh", "slipsocks"))
        val input = File(context.filesDir, "private-test-source.url")
        check(input.isFile)
        val receipt = JSONObject().put("mode", mode).put("versionCode", BuildConfig.VERSION_CODE)
            .put("scope", "Isolated emulator protocol observation; no owner pin/store enrollment")
            .put("functionalPassed", false)
        val output = File(context.filesDir, "post49-parity-result.json")
        val phases = org.json.JSONArray()
        receipt.put("phases", phases)
        var stage by kotlin.properties.Delegates.observable("source") { _, _, value ->
            phases.put(JSONObject().put("stage", value).put("utcMillis", System.currentTimeMillis()))
            output.writeText(receipt.toString(2))
        }
        try {
            withTimeout(120000) {
                ActivityScenario.launch(MainActivity::class.java).use { scenario ->
                    var vm: MainViewModel? = null
                    scenario.onActivity { activity ->
                        check(activity.connectionManager.connectionState.value.isDisconnected) { "Existing session is owned elsewhere" }
                        activity.setContent {}; vm = ViewModelProvider(activity)[MainViewModel::class.java]
                    }
                    val model = requireNotNull(vm)
                    withTimeout(30000) { model.uiState.first { !it.isLoading } }
                    val preview = model.subscriptionRepository.prepare("Private parity diagnostic", input.readText().trim())
                    receipt.put("sourceProfiles", preview.profiles.size).put("unsupported", preview.unsupported)
                    val profiles = preview.profiles.map { it.second }.filter { profile -> when (mode) {
                        "tuic" -> profile.tunnelType == TunnelType.XRAY && runCatching { CoreLink.parse(profile.coreLink).outbound.optString("protocol") == "tuic" }.getOrDefault(false)
                        "mierutcp", "mieruudp" -> profile.tunnelType == TunnelType.XRAY && runCatching {
                            val outbound = CoreLink.parse(profile.coreLink).outbound
                            outbound.optString("protocol") == "mieru" && outbound.getJSONObject("settings").optString("transport") == if (mode == "mierutcp") "TCP" else "UDP"
                        }.getOrDefault(false)
                        "slipssh" -> profile.tunnelType == TunnelType.SLIPSTREAM_SSH
                        else -> profile.tunnelType == TunnelType.SLIPSTREAM
                    } }
                    receipt.put("matchingProfiles", profiles.size)
                    var profile = profiles.single()
                    val resolverMode = InstrumentationRegistry.getArguments().getString("resolverMode") ?: "feed"
                    receipt.put("resolverMode", resolverMode)
                    if (mode.startsWith("slip") && resolverMode == "local") {
                        stage = "physical-network-local-dns"
                        val cm = context.getSystemService(android.net.ConnectivityManager::class.java)
                        val physical = requireNotNull(PhysicalNetworkRoute.current(cm))
                        val ip = requireNotNull(cm.getLinkProperties(physical)?.dnsServers?.firstOrNull { it is java.net.Inet4Address }?.hostAddress)
                        profile = profile.copy(resolvers = listOf(DnsResolver(host = ip, port = 53)))
                    }
                    if (mode in setOf("tuic", "mierutcp", "mieruudp")) {
                        stage = "protocol-handshake"
                        val native = File(context.applicationInfo.nativeLibraryDir)
                        if (mode == "tuic") try {
                            val latency = ProtocolHandshake.measure(profile.coreLink, File(native, "librovunet_awg.so"), File(native, "libveyra_core_launcher.so"), File(context.filesDir, "parity-certificates"))
                            receipt.put("protocolHandshake", true).put("latencyMs", latency)
                        } catch (failure: Exception) {
                            receipt.put("protocolHandshake", false).put("handshakeFailureType", failure.javaClass.simpleName)
                                .put("typedHandshakeFailure", Regex("\\(([a-z:-]{1,80})\\)").find(failure.message.orEmpty())?.groupValues?.get(1) ?: "unclassified")
                        }
                        // Manual data-plane testing is independent of the quick probe outcome.
                        stage = "owned-core-start"
                        ConnectionDiagnostics.begin(900051, "XRAY")
                        val core = CoreProcess()
                        val endpoint = LocalSocksEndpoint.privateCore()
                        try {
                            val config = CoreLink.runtime(profile.coreLink, endpoint.port, endpoint.user, endpoint.password, null)
                            val effective = JSONObject(config).getJSONArray("outbounds").getJSONObject(0).getJSONObject("settings")
                            receipt.put("endpointHostAlias", MessageDigest.getInstance("SHA-256").digest(effective.getString("server").toByteArray(Charsets.UTF_8)).joinToString("") { "%02x".format(it.toInt() and 255) }.take(12))
                                .put("endpointPort", effective.getInt("port"))
                            runInterruptible(Dispatchers.IO) { core.start(File(native, "libveyra_xray.so"), config, File(native, "libveyra_core_launcher.so"), 30000,
                                workDirectory = context.cacheDir, certificateDirectory = File(context.filesDir, "parity-certificates")).getOrThrow() }
                            val outcomes = org.json.JSONArray()
                            var passed = false
                            for ((label, url) in listOf("default" to "https://www.gstatic.com/generate_204", "alternate" to "https://cp.cloudflare.com/generate_204", "control" to "https://github.com/robots.txt")) {
                                stage = "owned-core-https-$label"
                                val item = JSONObject().put("target", label)
                                try { val proof = ScannerHttpProbe.measure(endpoint.port, url, 12000, endpoint.user, endpoint.password)
                                    item.put("status", proof.status).put("httpsVerified", proof.httpsVerified); passed = passed || proof.httpsVerified
                                } catch (failure: Exception) {
                                    val message = generateSequence(failure as Throwable?) { it.cause }.joinToString(" ") { it.message.orEmpty() }.lowercase()
                                    val category = when {
                                        listOf("certificate", "certpath", "pkix", "trust anchor").any { it in message } -> "CERTIFICATE"
                                        listOf("closed", "terminated", "eof").any { it in message } -> "PEER_CLOSED"
                                        listOf("timeout", "timed out").any { it in message } -> "TIMEOUT"
                                        else -> "OTHER"
                                    }
                                    item.put("httpsVerified", false).put("failureType", failure.javaClass.simpleName).put("failureCategory", category)
                                }
                                outcomes.put(item)
                            }
                            receipt.put("httpsTargets", outcomes).put("functionalPassed", passed)
                        } finally { withContext(NonCancellable + Dispatchers.IO) { core.stop() }; receipt.put("typedStages", ConnectionDiagnostics.report().substringBefore("\nPREVIOUS PROCESS\n")) }
                    } else {
                        val resolver = profile.resolvers.first()
                        receipt.put("resolverCount", profile.resolvers.size).put("firstResolverLoopback", resolver.host in setOf("127.0.0.1", "::1"))
                            .put("authoritative", profile.authoritativeMode).put("inlineCertificatePresent", profile.slipstreamCertificate.isNotBlank())
                        stage = "slipstream-start"
                        SlipstreamProbePool.use(context) { client ->
                            val port = ServerSocket(0).use { it.localPort }
                            client.start(profile, resolver.host, resolver.port, port).getOrThrow()
                            stage = "quic-before-stream"
                            val before = android.os.SystemClock.elapsedRealtime()
                            var ready = false
                            while (android.os.SystemClock.elapsedRealtime() - before < 10000) {
                                if (client.isQuicReady()) { ready = true; break }
                                delay(100)
                            }
                            receipt.put("quicReadyBeforeStream", ready)
                            if (mode == "slipssh") {
                                stage = "raw-ssh-banner"
                                val banner = withContext(Dispatchers.IO) {
                                    Socket().use { socket -> socket.soTimeout = 15000; socket.connect(InetSocketAddress("127.0.0.1", client.getClientPort()), 2000)
                                        socket.getInputStream().bufferedReader().readLine()?.startsWith("SSH-2.0-") == true }
                                }
                                receipt.put("sshBanner", banner).put("quicReadyAfterBanner", client.isQuicReady())
                                stage = "ssh-auth"
                                val keys = JSONObject(File(context.filesDir, "private-expected-hostkeys.json").readText()).getJSONArray("keys")
                                val expected = (0 until keys.length()).map { keys.getJSONObject(it).getString("fingerprint") }.toSet()
                                val repository = object : HostKeyRepository {
                                    override fun check(host: String, key: ByteArray): Int {
                                        val actual = "SHA256:" + android.util.Base64.encodeToString(MessageDigest.getInstance("SHA-256").digest(key), android.util.Base64.NO_WRAP or android.util.Base64.NO_PADDING)
                                        val accepted = host == profile.sshTrustIdentity && actual in expected
                                        receipt.put("expectedHostKeyMatched", accepted)
                                        return if (accepted) HostKeyRepository.OK else HostKeyRepository.CHANGED
                                    }
                                    override fun add(key: HostKey, ui: UserInfo?) = error("Read-only repository")
                                    override fun remove(host: String, type: String?) = error("Read-only repository")
                                    override fun remove(host: String, type: String?, key: ByteArray?) = error("Read-only repository")
                                    override fun getKnownHostsRepositoryID() = "Private diagnostic expected fingerprints"
                                    override fun getHostKey(): Array<HostKey> = emptyArray()
                                    override fun getHostKey(host: String?, type: String?): Array<HostKey> = emptyArray()
                                }
                                val ssh = SshTunnelInstance("private-parity", repository)
                                ssh.trustIdentity = profile.sshTrustIdentity
                                val token = StartupCancellation(900050)
                                ssh.startupCancellation = token
                                ConnectionDiagnostics.begin(900050, "SLIPSTREAM_SSH")
                                val sshPort = ServerSocket(0).use { it.localPort }
                                try {
                                    withContext(Dispatchers.IO) { ssh.startOverProxy(profile.sshHost, profile.sshPort, profile.sshUsername, profile.sshPassword, "127.0.0.1", client.getClientPort(), sshPort,
                                        blockDirectDns = true, sshAuthType = profile.sshAuthType, sshPrivateKey = profile.sshPrivateKey, sshKeyPassphrase = profile.sshKeyPassphrase).getOrThrow() }
                                    receipt.put("sshAuthenticated", true)
                                    stage = "ssh-socks-https"
                                    val proof = ScannerHttpProbe.measure(sshPort, "https://www.gstatic.com/generate_204", 15000, null, null)
                                    receipt.put("httpsStatus", proof.status).put("functionalPassed", proof.success)
                                } finally { withContext(NonCancellable + Dispatchers.IO) { token.requestCancel().invoke(); ssh.stop() }; receipt.put("typedStages", ConnectionDiagnostics.report()) }
                            } else {
                                stage = "socks-https"
                                val proof = ScannerHttpProbe.measure(client.getClientPort(), "https://www.gstatic.com/generate_204", 15000, profile.socksUsername, profile.socksPassword)
                                receipt.put("httpsStatus", proof.status).put("functionalPassed", proof.success)
                            }
                        }
                    }
                }
            }
            receipt.put("observationCompleted", true)
        } catch (failure: Exception) {
            receipt.put("failureStage", stage).put("failureType", failure.javaClass.simpleName)
            if (mode == "tuic") receipt.put("typedFailure", Regex("\\(([a-z:-]{1,80})\\)").find(failure.message.orEmpty())?.groupValues?.get(1) ?: "unclassified")
            receipt.put("observationCompleted", true)
        } finally { input.delete(); File(context.filesDir, "private-expected-hostkeys.json").delete(); output.writeText(receipt.toString(2)) }
        // This asserts diagnostic completion; functionalPassed/protocolHandshake remain separate evidence.
        assertTrue(receipt.getBoolean("observationCompleted"))
        assertTrue("Source/profile preparation failed; this is not protocol evidence", receipt.optInt("sourceProfiles") > 0 && receipt.optInt("matchingProfiles") == 1)
    }
}
