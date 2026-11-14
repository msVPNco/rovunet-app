package app.slipnet.data.subscription

import androidx.room.Room
import androidx.test.platform.app.InstrumentationRegistry
import app.slipnet.BuildConfig
import app.slipnet.data.local.database.SlipNetDatabase
import java.io.File
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.runBlocking
import org.json.JSONArray
import org.json.JSONObject
import org.junit.Assume.assumeTrue
import org.junit.Test

/** Explicit live-network acceptance, isolated from the candidate's profile database. */
class PublicFeedNetworkDeviceTest {
    @Test fun bothOfficialPublicPresetsDownloadDecodeImportAndRefresh() = runBlocking<Unit> {
        assumeTrue(BuildConfig.PRIVATE_CANDIDATE &&
            InstrumentationRegistry.getArguments().getString("livePublicFeeds") == "true")
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val receipts = JSONArray()
        var allPassed = true
        for (provider in PublicProviders.Provider.entries) {
            val receipt = JSONObject().put("provider", provider.name)
            val db = Room.inMemoryDatabaseBuilder(context, SlipNetDatabase::class.java).build()
            var stage = "download-decode-preview"
            try {
                val repository = StatusReviewHarness.repository(context, db)
                val downloads = JSONArray()
                var lastBody = ""
                val preview = repository.prepareUsing(provider.name, provider.address, null, false) { target, limit ->
                    repository.fetchDocument(target, limit, false).also { response ->
                        lastBody = response.text
                        val bytes = response.text.toByteArray(Charsets.UTF_8)
                        val hash = java.security.MessageDigest.getInstance("SHA-256").digest(bytes).joinToString("") { "%02x".format(it) }
                        downloads.put(JSONObject().put("endpoint", if (PublicProviders.isApiEndpoint(target)) "official-contents-api" else "canonical-raw")
                            .put("route", "disconnected-direct").put("receivedAtMs", response.receivedAt)
                            .put("httpStatus", response.code).put("responseUtf8Bytes", bytes.size).put("responseUtf8Sha256", hash))
                    }
                }
                receipt.put("downloads", downloads).put("classification", JSONArray(preview.publicStats.map { stat ->
                    JSONObject().put("protocol", stat.protocol).put("accepted", stat.accepted).put("rejected", stat.rejected)
                        .put("reasons", JSONObject(stat.reasons.mapKeys { it.key.name }))
                }))
                val decoded = if (provider == PublicProviders.Provider.MAHSA) MahsaFeed.decode(lastBody) else lastBody
                val unverified = PublicFeed.expand(decoded).lineSequence().map(String::trim).count { line ->
                    val error = runCatching { app.slipnet.core.CoreLink.parse(line) }.exceptionOrNull()?.message.orEmpty()
                    error == "TLS_UNVERIFIED" || error.contains("Unverified TLS")
                }
                receipt.put("explicitlyUnverifiedEntries", unverified)
                lastBody = ""
                require(preview.profiles.isNotEmpty())
                receipt.put("previewProfiles", preview.profiles.size).put("unsupported", preview.unsupported)
                stage = "apply"
                receipt.put("partialReviewRequired", preview.unsupported > 0)
                if (preview.unsupported > 0) require(runCatching { repository.apply(preview) }.exceptionOrNull()?.message == "REVIEW_REQUIRED")
                repository.apply(preview.copy(reviewedUnsupported = true))
                val original = db.profileDao().getAllProfiles().first().filter { it.collectionId == preview.source.id }
                require(original.size == preview.profiles.size)
                val ids = original.map { it.id }.toSet()
                stage = "refresh"
                // Reapply the downloaded document to isolate apply/identity behavior from feed churn.
                repository.apply(preview.copy(expectedSource = db.subscriptionDao().get(preview.source.id), reviewedUnsupported = true))
                val refreshed = db.profileDao().getAllProfiles().first().filter { it.collectionId == preview.source.id }
                require(refreshed.map { it.id }.toSet() == ids)
                receipt.put("importedProfiles", refreshed.size).put("samePayloadRefreshPreservedIds", true).put("passed", true)
            } catch (e: Exception) {
                allPassed = false
                val safeCode = e.message?.takeIf { it.matches(Regex("[A-Z_]{3,70}")) } ?: e.javaClass.simpleName
                receipt.put("passed", false).put("failedStage", stage).put("safeFailure", safeCode)
            } finally {
                db.close()
                receipts.put(receipt)
            }
        }
        File(context.filesDir, "public-feed-network-result.json").writeText(JSONObject()
            .put("versionCode", BuildConfig.VERSION_CODE).put("scope", "Android live public HTTPS, disconnected direct route; isolated Room database")
            .put("providers", receipts).put("passed", allPassed).toString(2))
        if (!allPassed) throw AssertionError("Public feed acceptance failed; inspect the redacted receipt")
    }
}
