package app.slipnet.service

import android.content.Intent
import android.net.ConnectivityManager
import android.net.NetworkCapabilities
import androidx.activity.compose.setContent
import androidx.lifecycle.ViewModelProvider
import androidx.test.core.app.ActivityScenario
import androidx.test.platform.app.InstrumentationRegistry
import app.slipnet.BuildConfig
import app.slipnet.core.CoreLink
import app.slipnet.core.ProtocolHandshake
import app.slipnet.core.TunnelProbe
import app.slipnet.data.local.datastore.PreferencesDataStore
import app.slipnet.di.DatabaseModule
import app.slipnet.domain.model.*
import app.slipnet.presentation.MainActivity
import app.slipnet.presentation.main.MainViewModel
import app.slipnet.tunnel.XrayBridge
import java.io.File
import java.net.URI
import kotlinx.coroutines.*
import kotlinx.coroutines.flow.first
import org.json.JSONObject
import org.junit.Assert.*
import org.junit.Assume.assumeTrue
import org.junit.Test

/** Opt-in on the disposable candidate only. The private URL arrives over adb stdin,
 * stays in app-private data, and is never an argument, source asset or test output. */
class OwnerSourceNetworkDeviceTest {
    @Test fun suppliedSourceRefreshQuickHy2AndManualHttpsUseRealAndroidPaths() = runBlocking<Unit> {
        val instrumentation = InstrumentationRegistry.getInstrumentation()
        val context = instrumentation.targetContext
        val input = File(context.filesDir, "private-test-source.url")
        assumeTrue(BuildConfig.PRIVATE_CANDIDATE && input.isFile)
        val result = JSONObject().put("versionCode", BuildConfig.VERSION_CODE).put("package", context.packageName)
            .put("androidApi", android.os.Build.VERSION.SDK_INT).put("abi", android.os.Build.SUPPORTED_ABIS.first())
            .put("scope", "Isolated Android emulator; app-owned SOCKS HTTPS after real VPN service setup")
        val cm = context.getSystemService(ConnectivityManager::class.java)
        val caps = cm.getNetworkCapabilities(cm.activeNetwork)
        result.put("network", when {
            caps?.hasTransport(NetworkCapabilities.TRANSPORT_WIFI) == true -> "emulated-wifi"
            caps?.hasTransport(NetworkCapabilities.TRANSPORT_ETHERNET) == true -> "emulated-ethernet"
            else -> "emulated-other"
        })
        val output = File(context.filesDir, "private-source-result.json")
        var stage = "input"
        var sourceId: String? = null
        var ownedSession = 0L
        var vm: MainViewModel? = null
        var manager: VpnConnectionManager? = null
        val preferences = PreferencesDataStore(context)
        val previousProxyOnly = preferences.proxyOnlyMode.first()
        val previousHttpProxy = preferences.appendHttpProxyToVpn.first()
        val previousSplit = preferences.splitTunnelingEnabled.first()
        var failed = false
        try {
            val address = input.readText().trim()
            val allowedHost = requireNotNull(InstrumentationRegistry.getArguments().getString("privateSourceHost"))
            require(URI(address).scheme == "https" && URI(address).host == allowedHost)
            preferences.setProxyOnlyMode(false)
            preferences.setAppendHttpProxyToVpn(false)
            preferences.setSplitTunnelingEnabled(false)
            ActivityScenario.launch<MainActivity>(Intent(context, MainActivity::class.java)).use { scenario ->
                scenario.onActivity {
                    it.setContent {}
                    vm = ViewModelProvider(it)[MainViewModel::class.java]
                    manager = it.connectionManager
                }
                val model = requireNotNull(vm)
                val connection = requireNotNull(manager)
                withTimeout(30_000) { model.uiState.first { !it.isLoading } }
                require(connection.connectionState.value.isDisconnected)
                val repository = model.subscriptionRepository
                stage = "download-and-parse"
                val preview = repository.prepare("Private network fixture", address)
                sourceId = preview.source.id
                result.put("previewProfiles", preview.profiles.size).put("unsupported", preview.unsupported)
                require(preview.profiles.isNotEmpty() && preview.unsupported == 0)
                stage = "import"
                repository.apply(preview)
                val rows = withTimeout(15_000) { model.uiState.first { state ->
                    state.profiles.count { it.collectionId == sourceId } == preview.profiles.size
                } }.profiles.filter { it.collectionId == sourceId }
                val ids = rows.map { it.id }.toSet()
                stage = "refresh"
                repository.apply(repository.prepare(preview.source.name, address, sourceId))
                val refreshed = model.profileRepository.getAllProfiles().first().filter { it.collectionId == sourceId }
                require(refreshed.map { it.id }.toSet() == ids)
                result.put("refreshPreservedIds", true).put("fastRoutes", CollectionTestPhase.quickCandidates(refreshed).size)
                    .put("dnsRoutes", refreshed.count(ProfileCollections::isDns))
                val hy2 = refreshed.filter { it.tunnelType == TunnelType.XRAY && runCatching {
                    CoreLink.parse(it.coreLink).outbound.optString("protocol") == "hysteria"
                }.getOrDefault(false) }
                result.put("hy2Profiles", hy2.size)
                require(hy2.isNotEmpty())
                val profile = hy2.first()
                val stream = CoreLink.parse(profile.coreLink).outbound.getJSONObject("streamSettings")
                result.put("hy2HasMask", stream.has("finalmask"))
                stage = "quick-hy2"
                result.put("quickSupported", ProtocolHandshake.supports(profile.coreLink))
                withContext(Dispatchers.Main) { model.pingSingleProfile(profile) }
                val quick = withTimeout(30_000) { model.uiState.first { state ->
                    !state.isPingRunning && state.pingResults[profile.id].let { it is PingResult.Success || it is PingResult.Error || it == PingResult.Skipped }
                } }.pingResults[profile.id]
                result.put("quickPassed", quick is PingResult.Success && quick.handshake && !quick.verified)
                if (quick is PingResult.Success) result.put("quickLatencyMs", quick.latencyMs)
                else {
                    // Retain the helper's typed stage on failure; never export its input/config.
                    val native = File(context.applicationInfo.nativeLibraryDir)
                    try {
                        ProtocolHandshake.measure(profile.coreLink, File(native, "librovunet_awg.so"),
                            File(native, "libveyra_core_launcher.so"), File(context.filesDir, "native-certificates"))
                        result.put("directProbeRetryPassed", true)
                    } catch (e: Exception) {
                        val code = Regex("\\(([a-z:-]{1,80})\\)").find(e.message.orEmpty())?.groupValues?.get(1)
                        result.put("quickFailure", code ?: "unclassified")
                    }
                }
                stage = "manual-vpn-setup"
                result.put("vpnPermissionGranted", android.net.VpnService.prepare(context) == null)
                    .put("proxyOnlyPreference", preferences.proxyOnlyMode.first())
                    .put("appendHttpProxyToVpn", preferences.appendHttpProxyToVpn.first())
                    .put("splitTunnelingEnabled", preferences.splitTunnelingEnabled.first())
                    .put("localAuthEnabled", preferences.proxyAuthEnabled.first())
                    .put("profileResolverCount", profile.resolvers.size)
                    .put("firstResolverLoopback", profile.resolvers.firstOrNull()?.host.let { it == "127.0.0.1" || it == "::1" })
                    .put("firstResolverUnspecified", profile.resolvers.firstOrNull()?.host.let { it == "0.0.0.0" || it == "::" })
                withContext(Dispatchers.Main) { ownedSession = connection.connect(profile) }
                require(ownedSession > 0)
                val connected = withTimeout(45_000) { connection.connectionState.first {
                    it is ConnectionState.Connected || it is ConnectionState.Error || it.isDisconnected
                } }
                result.put("vpnServiceReady", connected is ConnectionState.Connected)
                result.put("startupState", connected.javaClass.simpleName)
                if (connected is ConnectionState.Error) result.put("startupError", when (connected.message) {
                    "Enable local SOCKS authentication for Xray proxy-only mode" -> "LOCAL_PROXY_AUTH"
                    "Xray startup failed; check core binary and profile settings" -> "XRAY_SETUP"
                    context.getString(app.slipnet.R.string.vpn_tun_unavailable) -> "TUN_UNAVAILABLE"
                    else -> "OTHER"
                })
                require(connected is ConnectionState.Connected)
                stage = "https-through-live-core"
                val endpoint = requireNotNull(XrayBridge.activeEndpoint)
                val measurements = TunnelProbe.sample(onAttempt = {}, successTarget = 3) { host ->
                    TunnelProbe.measureCancellable(endpoint.port, endpoint.user, endpoint.password, host, 10_000,
                        diagnosticSession = app.slipnet.util.ConnectionDiagnostics.currentSessionId())
                }
                result.put("httpsAttempts", measurements.attempts).put("httpsSuccesses", measurements.summary?.successes ?: 0)
                    .put("httpsPassed", measurements.summary != null).put("httpsFailures", measurements.failures)
                if (measurements.summary == null) result.put("xrayInspection", XraySanitizedInspection.inspect(context, profile.coreLink))
                require(measurements.summary != null)
                if (InstrumentationRegistry.getArguments().getString("externalUidProbe") == "true") {
                    stage = "external-uid-tun-https"
                    val external = withContext(Dispatchers.IO) {
                        java.net.ServerSocket(0, 1, java.net.InetAddress.getByName("127.0.0.1")).use { listener ->
                            listener.soTimeout = 25_000
                            val nonce = java.util.UUID.randomUUID().toString().replace("-", "")
                            context.sendBroadcast(Intent().setComponent(android.content.ComponentName(
                                "app.veyra.networkprobe", "app.veyra.networkprobe.ProbeReceiver"))
                                .addFlags(Intent.FLAG_INCLUDE_STOPPED_PACKAGES)
                                .putExtra("replyPort", listener.localPort).putExtra("nonce", nonce))
                            listener.accept().use { socket ->
                                socket.soTimeout = 3000
                                val text = socket.getInputStream().bufferedReader().readText()
                                require(text.length <= 4096)
                                JSONObject(text).also { require(it.getString("nonce") == nonce); it.remove("nonce") }
                            }
                        }
                    }
                    result.put("externalUidProbe", external)
                    require(external.getInt("uid") != android.os.Process.myUid())
                    require(external.getBoolean("vpnDefaultNetwork") && external.getInt("httpsSuccesses") > 0)
                }
                stage = "connected-download-route"
                val during = repository.prepare("Private network fixture", address, sourceId)
                require(during.profiles.size == preview.profiles.size)
                result.put("connectedDownloadPassed", true)
                stage = "disconnect"
                withContext(Dispatchers.Main) { model.disconnect() }
                withTimeout(30_000) { connection.awaitSessionDisconnected(ownedSession) }
                ownedSession = 0
                require(result.optBoolean("quickPassed"))
                result.put("passed", true)
            }
        } catch (e: Throwable) {
            failed = true
            result.put("passed", false).put("failedStage", stage).put("testFailureType", e.javaClass.simpleName)
        } finally {
            withContext(NonCancellable) {
                val connection = manager
                if (connection != null && ownedSession > 0 && connection.ownsSession(ownedSession)) {
                    runCatching {
                        withContext(Dispatchers.Main) { connection.disconnect(expectedSessionId = ownedSession) }
                        withTimeout(30_000) { connection.awaitSessionDisconnected(ownedSession) }
                    }
                }
                sourceId?.let { id ->
                    val db = DatabaseModule.provideDatabase(context)
                    db.profileDao().getAllProfiles().first().filter { it.collectionId == id }.forEach {
                        db.profileDao().deleteProfile(it.id)
                    }
                    vm?.subscriptionRepository?.detach(id)
                }
                preferences.setProxyOnlyMode(previousProxyOnly)
                preferences.setAppendHttpProxyToVpn(previousHttpProxy)
                preferences.setSplitTunnelingEnabled(previousSplit)
                input.delete()
                result.put("diagnostics", app.slipnet.util.ConnectionDiagnostics.report())
                output.writeText(result.toString(2))
            }
        }
        if (failed) throw AssertionError("Private client test failed at $stage; inspect the redacted app-private result")
    }
}
