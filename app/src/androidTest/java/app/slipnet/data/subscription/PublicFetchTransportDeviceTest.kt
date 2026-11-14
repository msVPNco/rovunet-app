package app.slipnet.data.subscription

import androidx.room.Room
import androidx.test.platform.app.InstrumentationRegistry
import app.slipnet.core.LocalSocksEndpoint
import app.slipnet.core.ProxySocketFactory
import app.slipnet.data.local.database.SlipNetDatabase
import java.io.*
import java.net.*
import java.security.KeyStore
import java.util.Collections
import java.util.concurrent.*
import javax.net.SocketFactory
import javax.net.ssl.*
import kotlin.concurrent.thread
import kotlinx.coroutines.*
import okhttp3.*
import org.junit.Assert.*
import org.junit.Assume.assumeTrue
import org.junit.Test

/** Test-only trust anchors and loopback sockets; never changes system CA/proxy/DNS. */
class PublicFetchTransportDeviceTest {
    private val context get() = InstrumentationRegistry.getInstrumentation().targetContext
    private val fixtureFile get() = File(context.filesDir, "public-feed-fixture.p12")
    private data class Reply(val status: Int = 200, val body: String = "synthetic", val delayMs: Long = 0,
        val chunked: Boolean = false, val extra: String = "")
    private class Peer(file: File) : Closeable {
        val requests = Collections.synchronizedList(mutableListOf<String>())
        val replies = LinkedBlockingQueue<Reply>()
        val seen = LinkedBlockingQueue<String>()
        val sockets = Collections.synchronizedList(mutableListOf<Socket>())
        val pool = Executors.newCachedThreadPool()
        private val store = KeyStore.getInstance("PKCS12").apply { file.inputStream().use { load(it, "synthetic-only".toCharArray()) } }
        private val managers = KeyManagerFactory.getInstance(KeyManagerFactory.getDefaultAlgorithm()).apply { init(store,"synthetic-only".toCharArray()) }
        private val anchors = KeyStore.getInstance(KeyStore.getDefaultType()).apply { load(null); setCertificateEntry("fixture",store.getCertificate("fixture")) }
        val trust = TrustManagerFactory.getInstance(TrustManagerFactory.getDefaultAlgorithm()).apply { init(anchors) }.trustManagers.filterIsInstance<X509TrustManager>().single()
        private val tls = SSLContext.getInstance("TLS").apply { init(managers.keyManagers,arrayOf(trust),null) }
        private val listener = tls.serverSocketFactory.createServerSocket(0,16,InetAddress.getByName("127.0.0.1")) as SSLServerSocket
        val port get() = listener.localPort
        init { pool.submit {
            while (!listener.isClosed) try {
                val socket = listener.accept(); sockets += socket
                pool.submit { try { socket.use { s ->
                    s.soTimeout = 5000
                    val reader = s.getInputStream().bufferedReader(Charsets.US_ASCII)
                    val lines = mutableListOf<String>()
                    while (true) { val line = reader.readLine() ?: break; if(line.isEmpty()) break; lines += line }
                    val request = lines.joinToString("\n"); requests += request; seen.put(request)
                    val reply = replies.poll(5,TimeUnit.SECONDS) ?: error("Missing synthetic response")
                    Thread.sleep(reply.delayMs)
                    val bytes = reply.body.toByteArray(Charsets.UTF_8)
                    val framing = if(reply.chunked) "Transfer-Encoding: chunked\r\n" else "Content-Length: ${bytes.size}\r\n"
                    val output = s.getOutputStream()
                    output.write(("HTTP/1.1 ${reply.status} Fixture\r\nContent-Type: text/plain\r\nConnection: close\r\n"+framing+reply.extra+"\r\n").toByteArray(Charsets.US_ASCII))
                    if(reply.chunked) { output.write((bytes.size.toString(16)+"\r\n").toByteArray()); output.write(bytes); output.write("\r\n0\r\n\r\n".toByteArray()) }
                    else output.write(bytes)
                    output.flush()
                } } catch (_: Exception) { /* cancellation/negative TLS is expected */ } }
            } catch (_: Exception) { if(!listener.isClosed) throw IllegalStateException("Fixture listener failed") }
        } }
        fun client(factory: SocketFactory = direct(), trusted: Boolean = true): OkHttpClient {
            val builder = OkHttpClient.Builder().followRedirects(false).followSslRedirects(false)
                .proxy(Proxy.NO_PROXY).retryOnConnectionFailure(false).connectionPool(ConnectionPool(0,1,TimeUnit.SECONDS))
                .socketFactory(factory).dns { host -> listOf(InetAddress.getByAddress(host,byteArrayOf(127,0,0,1))) }
                .connectTimeout(2,TimeUnit.SECONDS).readTimeout(5,TimeUnit.SECONDS).callTimeout(10,TimeUnit.SECONDS)
            if(trusted) builder.sslSocketFactory(tls.socketFactory,trust)
            return builder.build()
        }
        fun direct() = object : SocketFactory() {
            override fun createSocket(): Socket = object : Socket() {
                override fun connect(target: SocketAddress, timeout: Int) { super.connect(InetSocketAddress("127.0.0.1",this@Peer.port),timeout) }
            }
            override fun createSocket(h:String,p:Int)=createSocket().apply { connect(InetSocketAddress(h,p)) }
            override fun createSocket(h:InetAddress,p:Int)=createSocket().apply { connect(InetSocketAddress(h,p)) }
            override fun createSocket(h:String,p:Int,l:InetAddress?,lp:Int)=createSocket(h,p)
            override fun createSocket(h:InetAddress,p:Int,l:InetAddress?,lp:Int)=createSocket(h,p)
        }
        override fun close() { listener.close(); synchronized(sockets) { sockets.forEach { runCatching { it.close() } } }; pool.shutdownNow() }
    }
    private class Socks(private val targetPort:Int) : Closeable {
        private val listener = ServerSocket(0,16,InetAddress.getByName("127.0.0.1"))
        private val sockets = Collections.synchronizedList(mutableListOf<Socket>())
        val hosts = Collections.synchronizedList(mutableListOf<String>())
        val endpoint get() = LocalSocksEndpoint(listener.localPort,"fixture","synthetic-only")
        private val worker = thread(isDaemon=true) {
            while(!listener.isClosed) try {
                val client=listener.accept(); sockets += client
                thread(isDaemon=true) { try { client.use { socket ->
                    socket.soTimeout=5000
                    val input=DataInputStream(socket.getInputStream()); val output=socket.getOutputStream()
                    require(input.readUnsignedByte()==5); val methods=ByteArray(input.readUnsignedByte()); input.readFully(methods)
                    require(2.toByte() in methods); output.write(byteArrayOf(5,2)); output.flush()
                    require(input.readUnsignedByte()==1)
                    val user=ByteArray(input.readUnsignedByte()); input.readFully(user)
                    val password=ByteArray(input.readUnsignedByte()); input.readFully(password)
                    require(String(user)=="fixture" && String(password)=="synthetic-only")
                    output.write(byteArrayOf(1,0)); output.flush()
                    require(input.readUnsignedByte()==5 && input.readUnsignedByte()==1 && input.readUnsignedByte()==0 && input.readUnsignedByte()==3)
                    val host=ByteArray(input.readUnsignedByte()); input.readFully(host); hosts += String(host)
                    require(input.readUnsignedShort()==443)
                    val upstream=Socket("127.0.0.1",targetPort); sockets += upstream
                    upstream.use { peer ->
                        output.write(byteArrayOf(5,0,0,1,127,0,0,1,0,0)); output.flush()
                        val forward=thread(isDaemon=true) { runCatching { input.copyTo(peer.getOutputStream()) } }
                        peer.getInputStream().copyTo(output)
                        socket.close(); forward.join(1000)
                    }
                } } catch (_: Exception) { } }
            } catch (_: Exception) { }
        }
        override fun close() { listener.close(); synchronized(sockets) { sockets.forEach { runCatching { it.close() } } }; worker.join(1000) }
    }
    private suspend fun runWithRepository(block:suspend (SubscriptionRepository)->Unit) {
        assumeTrue(fixtureFile.isFile)
        val db=Room.inMemoryDatabaseBuilder(context,SlipNetDatabase::class.java).build()
        try { block(StatusReviewHarness.repository(context,db)) } finally { db.close() }
    }
    @Test fun officialFallbackKeepsTheSameDirectOrAuthenticatedProxyTransport() = runBlocking<Unit> {
        runWithRepository { repo -> for(proxied in listOf(false,true)) Peer(fixtureFile).use { peer ->
            Socks(peer.port).use { proxy ->
                val client=peer.client(if(proxied) ProxySocketFactory(proxy.endpoint) else peer.direct())
                peer.replies.put(Reply(404,""));peer.replies.put(Reply(body="socks5://fixture:synthetic@192.0.2.11:10001"))
                val calls=mutableListOf<String>();val preset=PublicProviders.Provider.ANONYMOUS
                val body=PublicProviders.download(preset.address) { target,limit -> calls += target;repo.fetchDocument(target,limit,false,client).text }
                assertTrue(body.startsWith("socks5://"));assertEquals(listOf(preset.address,preset.apiAddress),calls)
                assertEquals(2,peer.requests.size)
                assertTrue(peer.requests[1].contains("Accept: application/vnd.github.raw+json",true))
                assertTrue(peer.requests[1].contains("User-Agent: Rovunet-Android",true))
                if(proxied) assertEquals(listOf("raw.githubusercontent.com","api.github.com"),proxy.hosts.toList())
                else assertTrue(proxy.hosts.isEmpty())
            }
        } }
    }
    @Test fun redirectsRateLimitsAndBothBodyLengthBoundsDoNotBecomeFallbacks() = runBlocking<Unit> {
        runWithRepository { repo ->
            for(reply in listOf(Reply(302,"",extra="Location: https://example.invalid/never\r\n"),Reply(429),Reply(200,"x".repeat(65)),Reply(200,"x".repeat(65),chunked=true))) Peer(fixtureFile).use { peer ->
                peer.replies.put(reply);val calls=mutableListOf<String>();val client=peer.client()
                val error=runCatching { PublicProviders.download(PublicProviders.Provider.ANONYMOUS.address) { target,_ ->
                    calls += target;repo.fetchDocument(target,64,false,client).text
                } }.exceptionOrNull()
                assertNotNull(error);assertEquals(1,calls.size);assertEquals(1,peer.requests.size)
                assertEquals(if(reply.status==302) "PUBLIC_FETCH_REDIRECT" else if(reply.status==429) "PUBLIC_FETCH_RATE_LIMIT" else "FEED_TOO_LARGE",error?.message)
            }
        }
    }
    @Test fun externalCancellationClosesTheCallAndUntrustedTlsNeverReturnsAFeed() = runBlocking<Unit> {
        runWithRepository { repo ->
            Peer(fixtureFile).use { peer ->
                peer.replies.put(Reply(delayMs=5000));val calls=mutableListOf<String>();val client=peer.client()
                val work=launch { PublicProviders.download(PublicProviders.Provider.ANONYMOUS.address) { target,limit -> calls += target;repo.fetchDocument(target,limit,false,client).text } }
                withContext(Dispatchers.IO) { assertNotNull(peer.seen.poll(5,TimeUnit.SECONDS)) }
                withTimeout(2000) { work.cancelAndJoin() };assertEquals(1,calls.size)
            }
            Peer(fixtureFile).use { peer ->
                val client=peer.client(trusted=false)
                val error=runCatching { repo.fetchDocument(PublicProviders.Provider.ANONYMOUS.address,1024,false,client) }.exceptionOrNull()
                assertEquals("PUBLIC_FETCH_TLS",error?.message);assertTrue(peer.requests.isEmpty())
            }
        }
    }
}
