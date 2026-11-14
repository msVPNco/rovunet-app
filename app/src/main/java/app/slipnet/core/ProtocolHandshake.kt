package app.slipnet.core

import java.io.File
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.withContext
import org.json.JSONObject

/** Isolated protocol probe. A listening local SOCKS port is never handshake evidence. */
object ProtocolHandshake {
    fun supports(link: String): Boolean = runCatching {
        val out = CoreLink.parse(link).outbound
        out.getString("protocol") in setOf("tuic", "wireguard", "amneziawg") ||
            out.getString("protocol") == "hysteria" && supportsHysteria(out)
    }.getOrDefault(false)

    private fun supportsHysteria(out: JSONObject): Boolean {
        val stream = out.getJSONObject("streamSettings")
        if (stream.getJSONObject("tlsSettings").has("cipherSuites")) return false
        if (!stream.has("finalmask")) return true
        val mask = stream.getJSONObject("finalmask")
        if (mask.length() != 1) return false
        val layers = mask.getJSONArray("udp")
        if (layers.length() != 1) return false
        val layer = layers.getJSONObject(0)
        val settings = layer.getJSONObject("settings")
        return layer.length() == 2 && layer.getString("type") == "salamander" && settings.length() == 1 &&
            settings.getString("password").isNotEmpty()
    }

    internal fun confirmedLatency(result: JSONObject, requireMappingV2: Boolean): Long {
        require(result.getString("kind") == "protocol-handshake") { "Protocol handshake not confirmed" }
        if (requireMappingV2) require(result.optInt("mappingRevision") == 2) { "Protocol probe runtime update required" }
        return result.getLong("latencyMs").also { require(it in 0..12_000) { "Invalid handshake time" } }
    }

    suspend fun measure(link: String, binary: File, launcher: File, certificateDirectory: File): Long = withContext(Dispatchers.IO) {
        require(supports(link)) { "Protocol handshake test unavailable" }
        require(binary.isFile && launcher.isFile) { "Protocol probe runtime missing" }
        check(certificateDirectory.isDirectory || certificateDirectory.mkdirs()) { "Certificate storage unavailable" }
        val outbound = CoreLink.parse(link).outbound
        val requireMappingV2 = outbound.optString("protocol") == "hysteria" &&
            outbound.optJSONObject("streamSettings")?.has("finalmask") == true
        val config = outbound.toString()
        val process = ProcessBuilder(launcher.absolutePath, binary.absolutePath, "--probe")
            .directory(certificateDirectory.parentFile).redirectErrorStream(true).apply {
                environment()["ROVUNET_CERTIFICATE_STORE"] = certificateDirectory.absolutePath
                environment()["TMPDIR"] = certificateDirectory.parentFile.absolutePath
            }.start()
        suspendCancellableCoroutine { continuation ->
            continuation.invokeOnCancellation { process.destroy() }
            try {
                process.outputStream.bufferedWriter(Charsets.UTF_8).use { it.write(config) }
                val deadline = System.nanoTime() + 12_000_000_000L
                while (runCatching { process.exitValue(); false }.getOrDefault(true)) {
                    check(continuation.isActive && System.nanoTime() < deadline) { "Protocol handshake timed out" }
                    Thread.sleep(20)
                }
                val reader = process.inputStream.reader(Charsets.UTF_8)
                val output = CharArray(4097)
                var size = 0
                while (size < output.size) {
                    val count = reader.read(output, size, output.size - size)
                    if (count < 0) break
                    size += count
                }
                check(size < output.size) { "Invalid protocol probe response" }
                val result = runCatching { JSONObject(String(output, 0, size)) }.getOrNull()
                if (process.exitValue() != 0) {
                    val code = result?.optString("error").orEmpty().takeIf { it.matches(Regex("[a-z:-]{1,80}")) }
                        ?: "exit-${process.exitValue()}"
                    error("Protocol handshake failed ($code)")
                }
                requireNotNull(result) { "Invalid protocol probe response" }
                val elapsed = confirmedLatency(result, requireMappingV2)
                if (continuation.isActive) continuation.resume(elapsed)
            } catch (error: Exception) {
                if (continuation.isActive) continuation.resumeWithException(error)
            } finally {
                process.destroy()
                runCatching { process.inputStream.close() }
                runCatching { process.errorStream.close() }
                runCatching { process.outputStream.close() }
                val cleanup = System.nanoTime() + 2_000_000_000L
                while (runCatching { process.exitValue(); false }.getOrDefault(true) && System.nanoTime() < cleanup) Thread.sleep(20)
                if (runCatching { process.exitValue(); false }.getOrDefault(true))
                    runCatching { Process::class.java.getMethod("destroyForcibly").invoke(process) }
            }
        }
    }
}
