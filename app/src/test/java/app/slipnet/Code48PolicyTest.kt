package app.slipnet

import app.slipnet.data.subscription.*
import app.slipnet.domain.model.*
import org.json.JSONObject
import org.junit.Assert.*
import org.junit.Test

class Code48PolicyTest {
    private val time = java.time.Instant.parse("2026-11-14T12:00:00Z").toEpochMilli()
    private fun status() = AccountStatus("a".repeat(32), "b".repeat(32), "c".repeat(32), "active",
        time + 86_400_000L, 26_843_545_600L, null, "partial", time, time, 7,
        usageLowerBound = 123, contractVersion = 2, pendingBytes = 12, measurement = "accounted_lower_bound")
    private fun merge(previous: String, next: AccountStatus): String? {
        val current = PrivateStatusCache.read(previous).current
        return PrivateStatusCache.merge(PrivateStatusBinding("source", current.serverId, current.accountId, current.revision),
            "source", next, previous)
    }
    @Test fun status2FixturesFollowThePanelSchemaWithoutInventingRemainingQuota() {
        val fixture = JSONObject(requireNotNull(javaClass.getResourceAsStream("/status2-synthetic-fixtures.json")).bufferedReader().readText())
        val cases = fixture.getJSONArray("cases")
        for (i in 0 until cases.length()) {
            val item = cases.getJSONObject(i)
            if (!item.optBoolean("expected_parse", false)) continue
            val read = AccountStatus.read(item.getJSONObject("body").toString())
            assertEquals(item.getString("case"), 2, read.contractVersion)
            assertEquals(read, AccountStatus.read(read.json()))
            assertNull("Partial/unavailable counters cannot establish remaining bytes", read.remainingBytes)
        }
        val v = AccountStatusView.evaluate(status(), time, 1000, time, "", time, 1000)
        assertTrue(v.detailsCurrent)
        assertFalse(v.trustworthy)
        assertEquals(26_843_545_600L, v.status?.limitBytes)
        assertEquals(86_400_000L, v.remainingMillis)
        assertNull(v.status?.usedBytes)
    }
    @Test fun exactInt64ParsingRejectsOverflowFractionsStringsDuplicatesAndUnknownFields() {
        val original = status().copy(revision = Long.MAX_VALUE, usageLowerBound = Long.MAX_VALUE, pendingBytes = Long.MAX_VALUE).json()
        assertEquals(Long.MAX_VALUE, AccountStatus.read(original).revision)
        assertEquals(Long.MAX_VALUE, AccountStatus.read(original).usageLowerBound)
        assertEquals(123L, AccountStatus.read(status().json().replace("\"confirmed_bytes\":123", "\"confirmed_bytes\":1.23e2")).usageLowerBound)
        val invalid = listOf(
            original.replace(Long.MAX_VALUE.toString(), "9223372036854775808"),
            status().json().replace("\"revision\":7", "\"revision\":7.1"),
            status().json().replace("\"revision\":7", "\"revision\":\"7\""),
            status().json().dropLast(1) + ",\"revision\":7}",
            status().json().dropLast(1) + ",\"extra\":0}",
            status().json().replace("\"limit_kind\":\"finite\"", "\"limit_kind\":\"unlimited\""),
            status().json().replace("\"coverage\":\"partial\"", "\"coverage\":\"complete\"")
        )
        for (text in invalid) assertTrue(text, runCatching { AccountStatus.read(text) }.isFailure)
    }
    @Test fun renewalCanResetRevisionButRetiredPeriodsAndConfirmedRollbackAreRejected() {
        val first = requireNotNull(PrivateStatusCache.merge(null, "source", status(), null))
        assertNull(merge(first, status().copy(revision = 6)))
        assertNull(merge(first, status().copy(usageLowerBound = 122, revision = 8)))
        assertNull(merge(first, status().copy(pendingBytes = 13)))
        val unknown = requireNotNull(merge(first, status().copy(revision = 8, usageLowerBound = null, pendingBytes = null,
            metering = "unavailable", measurement = "unknown")))
        assertNull(merge(unknown, status().copy(revision = 9, usageLowerBound = 122)))
        val restored = requireNotNull(merge(unknown, status().copy(revision = 9)))
        val renewed = requireNotNull(merge(restored, status().copy(periodId = "d".repeat(32), revision = 1, usageLowerBound = 0)))
        assertNull(merge(renewed, status().copy(revision = 100)))
        assertEquals(1L, PrivateStatusCache.read(renewed).current.revision)
        assertEquals(2, PrivateStatusCache.read(renewed).periods.size)
        assertNull(PrivateStatusCache.merge(null, "other-source", status(), null, hasPriorSourceBinding = true))
        assertNull(merge(first, status().copy(accountId = "f".repeat(32), revision = 9)))
        assertTrue(runCatching { AccountStatus.read(first) }.isFailure)
    }
    @Test fun onlyAnExplicitV2FeedEnablesTheV2StatusEndpoint() {
        val token = "a".repeat(32)
        val feed = "https://panel.example.invalid/v2/sub/$token.windows.json"
        val target = "https://panel.example.invalid/v2/sub/$token.windows.status.json"
        assertEquals(target, PrivateStatusPolicy.endpoint(feed))
        assertNull(PrivateStatusPolicy.endpoint(feed, proposedV2 = false))
        assertTrue(PrivateStatusPolicy.redirectAllowed(target, target))
        for (bad in listOf(target.replace("https:", "http:"), target+"?x=1", target.replace("panel.example.invalid","other.invalid"), target.replace(".windows","")))
            assertFalse(PrivateStatusPolicy.redirectAllowed(target, bad))
        assertNull(PrivateStatusPolicy.endpoint(feed.replace(token, token.uppercase())))
    }
    @Test fun legacyPrivateCacheKeepsItsOriginalRevisionAndRenewalRules() {
        val legacy = status().copy(contractVersion = 1, usedBytes = 20, usageLowerBound = null,
            pendingBytes = null, measurement = null, metering = "complete")
        val raw = legacy.json()
        assertEquals(legacy, PrivateStatusCache.read(raw).current)
        val next = legacy.copy(periodId = "e".repeat(32), revision = 8, usedBytes = 0)
        val renewed = requireNotNull(merge(raw, next))
        assertEquals(next, PrivateStatusCache.read(renewed).current)
        assertFalse(renewed.contains("_cache_version"))
        assertNull(merge(renewed, legacy))
        assertNull(merge(raw, status().copy(revision = 8)))
    }
    @Test fun allTenDnsTypesAreDeferredAndCancellationOrUntestedFastPathsDoNotOfferDns() {
        val types = listOf(TunnelType.DNSTT,TunnelType.DNSTT_SSH,TunnelType.NOIZDNS,TunnelType.NOIZDNS_SSH,
            TunnelType.VAYDNS,TunnelType.VAYDNS_SSH,TunnelType.SLIPSTREAM,TunnelType.SLIPSTREAM_SSH,TunnelType.SOAR,TunnelType.SOAR_SSH)
        val dns = types.mapIndexed { i,t -> ServerProfile(id = i+10L, name = "DNS $i", domain = "example.invalid", tunnelType = t) }
        val fast = ServerProfile(id = 1, name = "Fast", domain = "example.invalid", tunnelType = TunnelType.SSH)
        val members = listOf(fast)+dns
        assertEquals(listOf(fast), CollectionTestPhase.candidates(members, false))
        assertEquals(dns, CollectionTestPhase.candidates(members, true))
        assertEquals(dns, CollectionTestPhase.candidates(dns, false, single = true))
        val failed = mapOf(1L to PingResult.Error("Test failure"))
        assertTrue(CollectionTestPhase.offerDns(members, failed, false, completed = true))
        assertFalse(CollectionTestPhase.offerDns(members, failed, false, completed = false))
        assertFalse(CollectionTestPhase.offerDns(members, emptyMap(), false, completed = true))
        assertFalse(CollectionTestPhase.offerDns(members, mapOf(1L to PingResult.Success(10, quick = true)), false, completed = true))
        assertFalse(CollectionTestPhase.offerDns(members, mapOf(1L to PingResult.ReviewIdentity), false, completed = true))
        assertFalse(CollectionTestPhase.offerDns(members, ProbeCache.historical(failed), false, completed = true))
    }
    @Test fun recencyOrdersCollectionsAndProfilesStablyUsingThePersistedConnectionTimestamp() {
        fun p(id: Long, collection: String, at: Long) = ServerProfile(id = id, name = "Path $id",
            domain = "example.invalid", collectionId = collection, collectionName = collection, lastConnectedAt = at)
        val old = p(1,"one",100); val newest = p(2,"one",300); val middle = p(3,"two",200)
        val neverA = p(4,"three",0); val neverB = p(5,"three",0)
        val groups = ConnectionRecency.collections(listOf(old,middle,neverA,neverB,newest))
        assertEquals(listOf(listOf(2L,1L),listOf(3L),listOf(4L,5L)), groups.map { group -> group.map { it.id } })
    }
}
