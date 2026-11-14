package app.slipnet.data.subscription

import kotlinx.coroutines.currentCoroutineContext
import kotlinx.coroutines.ensureActive
import kotlinx.coroutines.withTimeoutOrNull

/** Built-in identities are separate from presentation and historical download addresses. */
internal object PublicProviders {
    enum class Provider(val address: String, val apiAddress: String) {
        ANONYMOUS("https://raw.githubusercontent.com/4n0nymou3/multi-proxy-config-fetcher/refs/heads/main/configs/proxy_configs.txt",
            "https://api.github.com/repos/4n0nymou3/multi-proxy-config-fetcher/contents/configs/proxy_configs.txt?ref=main"),
        MAHSA("https://raw.githubusercontent.com/mahsanet/MahsaFreeConfig/main/app/sub.txt",
            "https://api.github.com/repos/mahsanet/MahsaFreeConfig/contents/app/sub.txt?ref=main")
    }
    private val mahsaAliases = setOf(Provider.MAHSA.address,
        "https://raw.githubusercontent.com/mahsanet/MahsaFreeConfig/refs/heads/main/app/sub.txt",
        "https://raw.githubusercontent.com/hiddify/hiddify-app/refs/heads/main/test.configs/mahsa",
        "https://raw.githubusercontent.com/hiddify/hiddify-app/main/test.configs/mahsa")
    fun provider(address: String): Provider? = when (address.trim()) {
        in mahsaAliases -> Provider.MAHSA
        Provider.ANONYMOUS.address,
        "https://raw.githubusercontent.com/4n0nymou3/multi-proxy-config-fetcher/main/configs/proxy_configs.txt" -> Provider.ANONYMOUS
        else -> null
    }
    fun canonical(address: String): String = provider(address)?.address ?: address.trim()
    fun hidesAddress(sourceId: String?, address: String): Boolean = sourceId?.startsWith("public:") == true || provider(address) != null
    fun isDownloadEndpoint(address: String): Boolean = Provider.entries.any { address == it.address || address == it.apiAddress }
    fun isApiEndpoint(address: String): Boolean = Provider.entries.any { address == it.apiAddress }

    suspend fun download(address: String, budgetMs: Long = 25_000, fetch: suspend (String, Int) -> String): String {
        currentCoroutineContext().ensureActive()
        val target = canonical(address) // No request is ever made to a legacy alias.
        val provider = provider(target) ?: return fetch(target, PublicFeed.MAX_BYTES)
        val limit = if (provider == Provider.MAHSA) MahsaFeed.MAX_BYTES else PublicFeed.MAX_BYTES
        // One budget and one caller-owned transport for both addresses. Never retry
        // cancellation, schema/decode, size, rate-limit or redirect failures.
        val document = withTimeoutOrNull(budgetMs) {
            try { fetch(target, limit) }
            catch (e: PublicDownloadException) {
                currentCoroutineContext().ensureActive()
                if (!e.canFallback) throw e
                fetch(provider.apiAddress, limit)
            }
        } ?: throw PublicDownloadException(PublicDownloadException.Kind.TIMEOUT)
        if (provider != Provider.MAHSA) return document
        return try { MahsaFeed.decode(document) }
            catch (e: IllegalStateException) { if (e.message == "INVALID_FEED") error("PUBLIC_DECODE_FAILED") else throw e }
            catch (e: IllegalArgumentException) { if (e.message == "INVALID_FEED") error("PUBLIC_DECODE_FAILED") else throw e }
    }
}
