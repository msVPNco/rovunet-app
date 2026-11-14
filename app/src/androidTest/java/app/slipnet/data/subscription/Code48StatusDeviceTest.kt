package app.slipnet.data.subscription

import androidx.room.Room
import androidx.test.platform.app.InstrumentationRegistry
import app.slipnet.data.local.DeviceSecrets
import app.slipnet.data.local.database.SlipNetDatabase
import kotlinx.coroutines.runBlocking
import org.junit.Assert.*
import org.junit.Test
import java.security.MessageDigest
import java.util.UUID

/** Real Room receipts with injected HTTPS responses; no account, URL or live traffic. */
class Code48StatusDeviceTest {
    @Test fun status2ReceiptsSurviveReopenUnknownCountersRenewalAndRollback() = runBlocking<Unit> {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val name = "code48-status-${UUID.randomUUID()}.db"
        val sourceId = "code48-status"
        val address = "https://subscription.example.invalid/v2/sub/" + "a".repeat(32) + ".windows.json"
        val fingerprint = MessageDigest.getInstance("SHA-256").digest(address.toByteArray()).joinToString("") { "%02x".format(it) }
        val now = System.currentTimeMillis()
        var status = AccountStatus("b".repeat(32), "c".repeat(32), "d".repeat(32), "active",
            now + 24 * 86_400_000L, 26_843_545_600L, null, "partial", now, now, 7,
            usageLowerBound = 123, contractVersion = 2, pendingBytes = 12, measurement = "accounted_lower_bound")
        var body = status.json()
        var db = Room.databaseBuilder(context, SlipNetDatabase::class.java, name).build()
        fun store(): AccountStatusStore = AccountStatusStore(db, StatusReviewHarness.repository(context, db)) { target ->
            assertTrue(target.endsWith(".windows.status.json"))
            SubscriptionRepository.Download(200, null, body, now, System.currentTimeMillis())
        }
        suspend fun row() = requireNotNull(db.privateAccountStatusDao().get(sourceId, fingerprint))
        try {
            db.subscriptionDao().put(SubscriptionEntity(sourceId, "Synthetic subscription",
                DeviceSecrets.seal(address, "subscription:$sourceId")))
            store().refresh(true)
            val first = row()
            assertEquals(status, PrivateStatusCache.read(first.payload).current)
            assertEquals(26_843_545_600L, first.display().view().status?.limitBytes)
            assertFalse(first.display().view().trustworthy)
            assertTrue(first.display().view().detailsCurrent)
            assertNull(first.display().view().status?.remainingBytes)
            db.close()
            db = Room.databaseBuilder(context, SlipNetDatabase::class.java, name).build()
            assertEquals(first, row())
            status = status.copy(periodId = "e".repeat(32), revision = 1, usageLowerBound = 10)
            body = status.json()
            store().refresh(true)
            val renewed = row()
            assertEquals(1L, renewed.highWater)
            assertEquals(2, PrivateStatusCache.read(renewed.payload).periods.size)
            body = status.copy(periodId = "d".repeat(32), revision = 100).json()
            store().refresh(true)
            assertEquals("A retired period replay replaced the current receipt", renewed, row())
            body = status.copy(revision = 2, usageLowerBound = null, pendingBytes = null,
                metering = "unavailable", measurement = "unknown").json()
            store().refresh(true)
            val unknown = row()
            assertNull(unknown.display().view().status?.usageLowerBound)
            body = status.copy(revision = 3, usageLowerBound = 9).json()
            store().refresh(true)
            assertEquals("Unknown usage erased the previous confirmed high-water", unknown, row())
            body = status.copy(contractVersion = 1, usedBytes = 10, usageLowerBound = null,
                pendingBytes = null, measurement = null, metering = "complete").json()
            store().refresh(true)
            assertEquals(unknown.payload, row().payload)
            assertEquals("UNTRUSTED_STATUS", row().error)
        } finally { db.close(); context.deleteDatabase(name) }
    }
}
