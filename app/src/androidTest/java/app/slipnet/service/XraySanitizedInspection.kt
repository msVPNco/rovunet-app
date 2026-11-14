package app.slipnet.service

import android.content.Context
import app.slipnet.core.CoreLink
import app.slipnet.core.LocalSocksEndpoint
import app.slipnet.core.TunnelProbe
import java.io.File
import java.util.Collections
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.withContext
import org.json.JSONArray
import org.json.JSONObject

/** Opt-in diagnostic child. Only fixed phrase identifiers leave its pipe; never raw output/config. */
internal object XraySanitizedInspection {
    private val phrases = listOf(
        "failed to process outbound traffic", "failed to find an available destination", "failed to dial to dest",
        "failed to read request", "failed to resolve", "no such host", "auth failed code", "tls config is nil",
        "client is closed", "connection ends", "failed to write request", "failed to apply external controller",
        "permission denied", "operation not permitted", "network is unreachable", "no route to host",
        "operation not supported", "invalid argument", "not supported", "too many open files",
        "protocol not available", "no such file or directory", "bad file descriptor", "resource temporarily unavailable",
        "certificate", "timeout", "deadline exceeded", "connection refused", "connection reset", "unexpected eof",
        "tls handshake", "authentication failed", "invalid password", "no recent network activity",
        "invalid network", "unknown network", "unsupported network", "connection closed", "salamander",
        "quic: tls.Config.VerifyConnection is not supported with ChromeParrot".lowercase(),
        "failed", "socket", "setsockopt", "sendmsg", "udp", "tcp", "panic"
    )
    suspend fun inspect(context: Context, link: String): JSONObject = withContext(Dispatchers.IO) {
        val result = JSONObject().put("scope", "Diagnostic Xray child without routing rules, same app UID and current VPN state")
        val native = File(context.applicationInfo.nativeLibraryDir)
        val endpoint = LocalSocksEndpoint.privateCore()
        val config = JSONObject(CoreLink.runtime(link, endpoint.port, endpoint.user, endpoint.password))
            .put("log", JSONObject().put("loglevel", "info").put("access", "none"))
        val builder = ProcessBuilder(File(native,"libveyra_core_launcher.so").absolutePath,
            File(native,"libveyra_xray.so").absolutePath,"run","-c","stdin:").redirectErrorStream(true)
        builder.environment()["ROVUNET_CERTIFICATE_STORE"] = File(context.filesDir,"native-certificates").absolutePath
        val peer = config.getJSONArray("outbounds").getJSONObject(0).getJSONObject("settings")
        builder.environment()["ROVUNET_CERTIFICATE_ENDPOINT"] = peer.getString("address") + ":" + peer.getInt("port")
        val codes = Collections.synchronizedSet(linkedSetOf<Int>())
        val process = builder.start()
        val worker = Thread {
            runCatching {
                process.inputStream.use { input ->
                    val line = StringBuilder(); var discarded = false
                    while (true) {
                        val ch = input.read(); if (ch < 0) break
                        if (ch == 10) {
                            if (!discarded) {
                                val text = line.toString().lowercase()
                                phrases.forEachIndexed { index, phrase -> if (phrase in text) codes.add(index) }
                            }
                            line.setLength(0); discarded = false
                        } else if (line.length < 4096) line.append(ch.toChar()) else discarded = true
                    }
                }
            }
        }.apply { isDaemon = true; start() }
        try {
            process.outputStream.bufferedWriter().use { it.write(config.toString()) }
            delay(700)
            val measured = TunnelProbe.sample({}, successTarget=1) { host ->
                TunnelProbe.measureCancellable(endpoint.port,endpoint.user,endpoint.password,host,10_000)
            }
            result.put("httpsAttempts",measured.attempts).put("httpsSuccesses",measured.summary?.successes ?: 0)
                .put("httpsFailures",measured.failures)
            delay(100)
        } finally {
            process.destroy(); process.waitFor(); worker.join(2000)
            result.put("matchedFixedPhrases",JSONArray(synchronized(codes) { codes.map { phrases[it] } }))
        }
        result
    }
}
