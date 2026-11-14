package app.slipnet

import app.slipnet.data.subscription.*
import app.slipnet.data.export.ConfigImporter
import javax.crypto.Cipher
import javax.crypto.spec.IvParameterSpec
import javax.crypto.spec.SecretKeySpec
import kotlinx.coroutines.*
import org.junit.Assert.*
import org.junit.Test
import java.util.Base64

class PublicProvidersTest {
    private val old = "https://raw.githubusercontent.com/hiddify/hiddify-app/refs/heads/main/test.configs/mahsa"
    private fun encoded(json: String): String {
        val cipher = Cipher.getInstance("AES/CBC/PKCS5Padding")
        cipher.init(Cipher.ENCRYPT_MODE, SecretKeySpec("ca815ecfdb1f153a".toByteArray(Charsets.US_ASCII), "AES"),
            IvParameterSpec("lvcas56410c97lpb".toByteArray(Charsets.US_ASCII)))
        return Base64.getEncoder().encodeToString(cipher.doFinal(json.toByteArray()))
    }
    @Test fun mahsaDirectDecryptsDeduplicatesAndNeverFetchesHiddifyOrAds() = runBlocking {
        val link="socks5://synthetic:fixture@192.0.2.11:10001#fixture"
        val json=org.json.JSONObject().put("ads_url","https://example.invalid/advert")
            .put("mci",org.json.JSONArray().put(org.json.JSONObject().put("config",link)))
            .put("mtn",org.json.JSONArray().put(org.json.JSONObject().put("config",link)))
        for (address in listOf(old, PublicProviders.Provider.MAHSA.address)) {
            val calls=mutableListOf<String>()
            val result=PublicProviders.download(address) { target, limit ->
                calls += target; assertEquals(2*1024*1024,limit); encoded(json.toString())
            }
            assertEquals(listOf(PublicProviders.Provider.MAHSA.address),calls)
            assertEquals(link,result)
            assertEquals(1, PublicFeed.parse(result,ConfigImporter(),"synthetic").profiles.size)
        }
    }
    @Test fun malformedEncryptedOrJsonDataNeverYieldsPartialReplacement() = runBlocking {
        for (body in listOf("not-base64", encoded("{}"),encoded("{\"mci\":[],\"mtn\":[]}"),
            encoded("{\"mci\":[{\"config\":123}],\"mtn\":[]}"),
            encoded("{\"mci\":[{\"config\":\"socks5://first\\nsecond\"}],\"mtn\":[]}"),
            encoded("{\"mci\":[{\"config\":\"socks5://first\"},{}],\"mtn\":[]}"))) {
            assertTrue(runCatching { MahsaFeed.decode(body) }.isFailure)
        }
        val oversized="A".repeat(MahsaFeed.MAX_BYTES+1)
        assertTrue(runCatching { MahsaFeed.decode(oversized) }.isFailure)
    }
    @Test fun ordinaryPublicSourceNeverFollowsEmbeddedHttpsUrls() = runBlocking {
        var calls=0
        assertEquals("https://example.invalid/nested",PublicProviders.download(PublicProviders.Provider.ANONYMOUS.address) { _,_ -> calls++;"https://example.invalid/nested" })
        assertEquals(1,calls)
    }
    @Test fun aliasesResolveToOneSourceAndAllSavedFreeAddressesStayHidden() {
        assertEquals(PublicProviders.Provider.MAHSA.address,PublicProviders.canonical(old))
        assertTrue(PublicProviders.hidesAddress("legacy-id",old))
        assertTrue(PublicProviders.hidesAddress("public:custom","https://example.invalid/custom"))
        assertTrue(PublicProviders.hidesAddress(null,PublicProviders.Provider.ANONYMOUS.address))
        assertFalse(PublicProviders.hidesAddress("private-id","https://example.invalid/private"))
        assertEquals("https://example.invalid/private",PublicProviders.canonical("https://example.invalid/private"))
    }
    @Test fun failedOrCancelledDownloadPropagatesWithoutFollowingAnyOtherSource() = runBlocking {
        assertThrows(IllegalStateException::class.java) { runBlocking { PublicProviders.download(old) { _,_ -> error("DOWNLOAD_FAILED") } } }
        val child=launch(start=CoroutineStart.LAZY) { PublicProviders.download(old) { _,_ -> fail("Cancelled fetch started"); "" } }
        child.cancel();child.join();assertTrue(child.isCancelled)
    }

    @Test fun bothPresetsRetryOnlyTheirOwnOfficialFileAfterTransportOr404Failure() = runBlocking {
        val link = "socks5://synthetic:fixture@192.0.2.11:10001#fixture"
        val encrypted = encoded("{\"mci\":[{\"config\":\"$link\"}],\"mtn\":[]}")
        for (provider in PublicProviders.Provider.entries) for (kind in listOf(PublicDownloadException.Kind.NETWORK,
            PublicDownloadException.Kind.TLS, PublicDownloadException.Kind.TIMEOUT, PublicDownloadException.Kind.NOT_FOUND)) {
            val calls = mutableListOf<String>()
            val clear = PublicProviders.download(provider.address) { target, limit ->
                calls += target
                assertEquals(if (provider == PublicProviders.Provider.MAHSA) MahsaFeed.MAX_BYTES else PublicFeed.MAX_BYTES, limit)
                if (target == provider.address) throw PublicDownloadException(kind)
                if (provider == PublicProviders.Provider.MAHSA) encrypted else link
            }
            assertEquals(listOf(provider.address, provider.apiAddress), calls)
            assertEquals(link, clear) // Anonymous stays plaintext; Mahsa alone is decrypted.
            assertEquals(provider.address, PublicProviders.canonical(provider.address))
        }
    }

    @Test fun fallbackNeverHidesFormatSizeHttpOrRateLimitFailure() = runBlocking {
        val failures = listOf(IllegalStateException("INVALID_FEED"), IllegalArgumentException("FEED_TOO_LARGE")) +
            listOf(PublicDownloadException.Kind.HTTP, PublicDownloadException.Kind.RATE_LIMIT, PublicDownloadException.Kind.REDIRECT)
                .map(::PublicDownloadException)
        for (provider in PublicProviders.Provider.entries) for (failure in failures) {
            val calls = mutableListOf<String>()
            val error = runCatching { PublicProviders.download(provider.address) { target, _ -> calls += target; throw failure } }.exceptionOrNull()
            assertSame(failure, error)
            assertEquals(listOf(provider.address), calls)
        }
        var requests = 0
        val error = runCatching { PublicProviders.download(PublicProviders.Provider.MAHSA.address) { _, _ -> requests++; "malformed" } }.exceptionOrNull()
        assertEquals("PUBLIC_DECODE_FAILED", error?.message)
        assertEquals(1, requests)
    }

    @Test fun exhaustedSharedBudgetCancelsFallbackAndExternalCancellationNeverRetries() = runBlocking {
        val provider = PublicProviders.Provider.ANONYMOUS
        val started = CompletableDeferred<Unit>()
        val calls = mutableListOf<String>()
        val work = async {
            runCatching { PublicProviders.download(provider.address, budgetMs = 200) { target, _ ->
                calls += target
                if (target == provider.address) { delay(50); throw PublicDownloadException(PublicDownloadException.Kind.NETWORK) }
                started.complete(Unit)
                awaitCancellation()
            } }.exceptionOrNull()
        }
        withTimeout(5_000) { started.await() }
        assertEquals("PUBLIC_FETCH_TIMEOUT", work.await()?.message)
        assertEquals(listOf(provider.address, provider.apiAddress), calls)
        calls.clear()
        val rawStarted = CompletableDeferred<Unit>()
        val cancelled = launch { PublicProviders.download(provider.address) { target, _ ->
            calls += target; rawStarted.complete(Unit); awaitCancellation()
        } }
        withTimeout(5_000) { rawStarted.await() }; cancelled.cancelAndJoin()
        assertEquals(listOf(provider.address), calls)
    }

    @Test fun ordinarySourcesNeverAcquireGithubFallbackAndOnlyExactPresetEndpointsAreAllowlisted() = runBlocking {
        val custom = "https://example.invalid/public"
        val calls = mutableListOf<String>()
        assertTrue(runCatching { PublicProviders.download(custom) { target, _ -> calls += target; throw PublicDownloadException(PublicDownloadException.Kind.NETWORK) } }.isFailure)
        assertEquals(listOf(custom), calls)
        for (p in PublicProviders.Provider.entries) {
            assertTrue(PublicProviders.isDownloadEndpoint(p.address))
            assertTrue(PublicProviders.isApiEndpoint(p.apiAddress))
            assertFalse(PublicProviders.isDownloadEndpoint(p.apiAddress + "&other=1"))
            assertFalse(PublicProviders.isApiEndpoint(p.apiAddress.replace("api.github.com", "example.invalid")))
        }
    }
}
