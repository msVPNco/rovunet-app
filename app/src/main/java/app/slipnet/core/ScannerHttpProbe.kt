package app.slipnet.core

import java.io.DataInputStream
import java.net.InetSocketAddress
import java.net.Socket
import java.net.URI
import java.util.Timer
import java.util.TimerTask
import javax.net.ssl.SNIHostName
import javax.net.ssl.SSLSocket
import javax.net.ssl.SSLSocketFactory
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.withContext

/** One explicit SOCKS route and one deadline for CONNECT, TLS and the bounded HTTP response. */
internal object ScannerHttpProbe {
    data class Result(val status: Int, val latencyMs: Long, val secure: Boolean) {
        val success: Boolean get() = status in 200..299
        val httpsVerified: Boolean get() = secure && success
    }

    suspend fun measure(port: Int, url: String, timeoutMs: Long, username: String?, password: String?,
        tlsFactory: SSLSocketFactory = SSLSocketFactory.getDefault() as SSLSocketFactory): Result = withContext(Dispatchers.IO) {
        require(timeoutMs in 1..Int.MAX_VALUE.toLong())
        val target = URI(url)
        require(target.scheme in setOf("http", "https") && !target.host.isNullOrBlank() && target.userInfo == null)
        val host = target.host.removeSurrounding("[", "]")
        val secure = target.scheme == "https"
        val remotePort = if (target.port > 0) target.port else if (secure) 443 else 80
        val path = target.rawPath.orEmpty().ifEmpty { "/" } + (target.rawQuery?.let { "?$it" } ?: "")
        require(path.none { it == '\r' || it == '\n' })
        suspendCancellableCoroutine { continuation ->
            val socket = Socket()
            continuation.invokeOnCancellation { runCatching { socket.close() } }
            val timer = Timer("scanner-http-deadline", true)
            timer.schedule(object : TimerTask() { override fun run() { runCatching { socket.close() } } }, timeoutMs)
            try {
                if (continuation.isActive) continuation.resumeWith(runCatching {
                    socket.use {
                        socket.connect(InetSocketAddress("127.0.0.1", port), timeoutMs.toInt())
                        socket.soTimeout = timeoutMs.toInt()
                        val input = DataInputStream(socket.getInputStream())
                        val output = socket.getOutputStream()
                        val auth = !username.isNullOrBlank() && !password.isNullOrBlank()
                        output.write(if (auth) byteArrayOf(5, 2, 0, 2) else byteArrayOf(5, 1, 0)); output.flush()
                        check(input.readUnsignedByte() == 5) { "Invalid SOCKS greeting" }
                        when (input.readUnsignedByte()) {
                            0 -> Unit
                            2 -> {
                                check(auth) { "Unexpected SOCKS authentication" }
                                val user = username!!.toByteArray(Charsets.UTF_8)
                                val pass = password!!.toByteArray(Charsets.UTF_8)
                                require(user.size in 1..255 && pass.size in 1..255)
                                output.write(byteArrayOf(1, user.size.toByte()) + user + byteArrayOf(pass.size.toByte()) + pass); output.flush()
                                check(input.readUnsignedByte() == 1 && input.readUnsignedByte() == 0) { "SOCKS authentication failed" }
                            }
                            else -> error("SOCKS authentication rejected")
                        }
                        val domain = host.toByteArray(Charsets.US_ASCII)
                        require(domain.size in 1..255)
                        output.write(byteArrayOf(5, 1, 0, 3, domain.size.toByte()) + domain + byteArrayOf((remotePort shr 8).toByte(), remotePort.toByte())); output.flush()
                        check(input.readUnsignedByte() == 5 && input.readUnsignedByte() == 0 && input.readUnsignedByte() == 0) { "SOCKS CONNECT failed" }
                        val addressBytes = when (input.readUnsignedByte()) { 1 -> 4; 4 -> 16; 3 -> input.readUnsignedByte(); else -> error("Invalid SOCKS address") }
                        input.readFully(ByteArray(addressBytes + 2))
                        val start = System.nanoTime()
                        val remote = if (secure) tlsFactory.createSocket(socket, host, remotePort, true) as SSLSocket else socket
                        remote.use { connection ->
                            if (connection is SSLSocket) {
                                connection.sslParameters = connection.sslParameters.apply {
                                    endpointIdentificationAlgorithm = "HTTPS"
                                    if (!host.contains(':') && !host.matches(Regex("[0-9.]+"))) serverNames = listOf(SNIHostName(host))
                                }
                                connection.soTimeout = timeoutMs.toInt()
                                connection.startHandshake()
                            }
                            val authority = if (target.port > 0) "${target.host}:$remotePort" else target.host
                            connection.outputStream.write("HEAD $path HTTP/1.1\r\nHost: $authority\r\nConnection: close\r\n\r\n".toByteArray(Charsets.US_ASCII))
                            connection.outputStream.flush()
                            val response = connection.inputStream
                            val line = StringBuilder()
                            while (true) {
                                check(line.length < 1024) { "HTTP status line too long" }
                                val next = response.read(); check(next >= 0) { "Missing HTTP status line" }
                                if (next == 10) break
                                line.append(next.toChar())
                            }
                            val match = Regex("HTTP/1\\.[01] ([0-9]{3})(?: [^\\r\\n]*)?\\r").matchEntire(line.toString())
                                ?: error("Invalid HTTP status line")
                            Result(match.groupValues[1].toInt(), (System.nanoTime() - start) / 1_000_000, secure)
                        }
                    }
                })
            } finally { timer.cancel(); runCatching { socket.close() } }
        }
    }
}
