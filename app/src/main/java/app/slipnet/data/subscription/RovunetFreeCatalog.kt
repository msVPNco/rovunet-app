package app.slipnet.data.subscription

import app.slipnet.BuildConfig
import android.util.Base64
import org.bouncycastle.crypto.params.Ed25519PublicKeyParameters
import org.bouncycastle.crypto.signers.Ed25519Signer
import org.json.JSONObject
import java.time.Duration
import java.time.Instant

/** Official, pinned signed public catalog. Never trust an unsigned public feed for unattended refreshes. */
internal object RovunetFreeCatalog {
    const val URL = "https://raw.githubusercontent.com/msVPNco/rovunet-app/main/free/catalog.json"
    private const val SIGNATURE_URL = "https://raw.githubusercontent.com/msVPNco/rovunet-app/main/free/catalog.sig"
    const val MAX_BYTES = 8 * 1024 * 1024

    fun enabled(): Boolean = runCatching {
        Base64.decode(BuildConfig.FREE_ED25519_PUBLIC_KEY, Base64.NO_WRAP).size == 32
    }.getOrDefault(false)

    suspend fun download(fetch: suspend (String, Int) -> String): String {
        require(enabled()) { "FREE_SOURCE_NOT_READY" }
        // The signature is checked against the exact UTF-8 bytes returned by the
        // official URL; it is independent of GitHub CDN caches and redirects.
        val document = fetch(URL, MAX_BYTES)
        val encodedSignature = fetch(SIGNATURE_URL, 256).trim()
        val bytes = document.toByteArray(Charsets.UTF_8)
        require(bytes.size <= MAX_BYTES) { "PUBLIC_FEED_LIMIT" }
        val signature = runCatching { Base64.decode(encodedSignature, Base64.DEFAULT) }.getOrNull()
        val publicKey = Base64.decode(BuildConfig.FREE_ED25519_PUBLIC_KEY, Base64.NO_WRAP)
        require(signature != null && signature.size == 64 &&
            verify(bytes, signature, publicKey)) { "FREE_SIGNATURE_INVALID" }
        return activeLinks(document, Instant.now())
    }

    private fun verify(message: ByteArray, signature: ByteArray, key: ByteArray): Boolean =
        runCatching {
            val verifier = Ed25519Signer()
            verifier.init(false, Ed25519PublicKeyParameters(key, 0))
            verifier.update(message, 0, message.size)
            verifier.verifySignature(signature)
        }.getOrDefault(false)

    /** Expiry is based on the *server account creation time*, never the GitHub publication date. */
    internal fun activeLinks(document: String, now: Instant): String {
        val catalog = JSONObject(document)
        require(catalog.getInt("version") == 1) { "FREE_CATALOG_VERSION" }
        val generated = Instant.parse(catalog.getString("generated_at"))
        require(!generated.isAfter(now.plus(Duration.ofMinutes(5))) &&
            !generated.isBefore(now.minus(Duration.ofDays(7)))) { "FREE_CATALOG_STALE" }
        val donors = catalog.getJSONArray("donations")
        require(donors.length() <= 500) { "PUBLIC_FEED_LIMIT" }
        val links = ArrayList<String>()
        val seen = HashSet<String>()
        for (i in 0 until donors.length()) {
            val donor = donors.getJSONObject(i)
            val donationId = donor.getString("donation_id")
            require(donationId.matches(Regex("[0-9a-f]{32}")) && seen.add(donationId)) { "FREE_DONATION_INVALID" }
            val created = Instant.parse(donor.getString("created_at"))
            val expires = Instant.parse(donor.getString("expires_at"))
            require(expires == created.plus(Duration.ofDays(30)) &&
                donor.getInt("quota_gb") == 50 && !created.isAfter(now.plus(Duration.ofMinutes(5)))) {
                "FREE_DONATION_INVALID"
            }
            if (!expires.isAfter(now)) continue
            val items = donor.getJSONArray("items")
            require(items.length() <= 128) { "PUBLIC_FEED_LIMIT" }
            for (j in 0 until items.length()) {
                val link = items.getJSONObject(j).getString("link")
                require(link.length <= 65536 && !link.contains('\n') && !link.contains('\r')) { "PUBLIC_FEED_LIMIT" }
                require(links.size + 1 <= PublicFeed.MAX_ENTRIES) { "PUBLIC_FEED_LIMIT" }
                links += link
            }
        }
        // The public feed importer performs its normal protocol, TLS and options
        // validation after signature verification. A signed empty catalog is valid.
        return links.joinToString("\n")
    }
}
