package app.slipnet

import app.slipnet.domain.model.*
import org.junit.Assert.*
import org.junit.Test

class Post48PingPresentationTest {
    private val now = 1_000_000L
    private val tcp = PingPresentation.Cohort("tcp", "example.invalid:443", "synthetic-network")
    private fun p(id: Long, group: String = "one", connectedAt: Long = 0) = ServerProfile(id = id,
        name = "Path $id", domain = "example.invalid", tunnelType = TunnelType.SSH,
        collectionId = group, collectionName = group, lastConnectedAt = connectedAt)
    private fun ok(ms: Long) = PingResult.Success(ms, measuredAt = now, quick = true)
    private fun order(profiles: List<ServerProfile>, results: Map<Long, PingResult>, samples: Map<Long, PingPresentation.Sample>,
        connected: Long? = null, at: Long = now) = PingPresentation.collections(profiles, results, samples, connected, at)
        .flatMap { it.map(ServerProfile::id) }

    @Test fun quickCandidatesExcludeAllTenDnsTypesEvenForASingleRouteAndNeverPadALimitedAccount() {
        val types = listOf(TunnelType.DNSTT, TunnelType.DNSTT_SSH, TunnelType.NOIZDNS, TunnelType.NOIZDNS_SSH,
            TunnelType.VAYDNS, TunnelType.VAYDNS_SSH, TunnelType.SLIPSTREAM, TunnelType.SLIPSTREAM_SSH, TunnelType.SOAR, TunnelType.SOAR_SSH)
        val dns = types.mapIndexed { i, type -> p(100L + i).copy(tunnelType = type) }
        val six = (1L..6L).map { p(it) }
        val twenty = (1L..20L).map { p(it) }
        assertEquals(six, CollectionTestPhase.quickCandidates(six + dns))
        assertEquals(twenty, CollectionTestPhase.quickCandidates(twenty + dns))
        for (route in dns) assertTrue(CollectionTestPhase.quickCandidates(listOf(route)).isEmpty())
        val failed = twenty.associate { it.id to PingResult.Error("Synthetic failure", now) }
        assertFalse(CollectionTestPhase.offerDns(twenty + dns, failed, detailed = false, completed = true))
        assertTrue(CollectionTestPhase.candidates(dns, dnsOnly = true).isNotEmpty())
    }

    @Test fun connectedRouteWinsAndFreshComparableSuccessesSortWithoutChangingProfilesOrTrust() {
        val profiles = listOf(p(1, connectedAt = 900), p(2), p(3), p(4), p(5), p(6), p(7), p(8))
        val results = mapOf(1L to ok(90), 2L to ok(40), 3L to ok(10), 4L to PingResult.Error("Failure", now),
            5L to ok(0).copy(currentNetwork = false), 6L to PingResult.Skipped, 7L to PingResult.Pending)
        val samples = results.mapValues { PingPresentation.Sample(it.value, tcp) }
        assertEquals(listOf(1L, 3L, 2L, 5L, 6L, 7L, 8L, 4L), order(profiles, results, samples, connected = 1))
        assertEquals(listOf(1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L), profiles.map { it.id })
        assertEquals(900L, profiles.first().lastConnectedAt)
        assertTrue(results.values.filterIsInstance<PingResult.Success>().none { it.verified })
        // The trust/auto-route ranking still rejects unverified quick results.
        assertEquals(profiles, ProfileRanking.sort(profiles, results))
    }

    @Test fun methodsTargetsAndRoutesAreSortedOnlyWithinTheirOwnStableSlots() {
        val profiles = (1L..8L).map { p(it) }
        val results = mapOf(1L to ok(80), 2L to ok(70).copy(handshake = true), 3L to ok(60), 4L to ok(50),
            5L to ok(20), 6L to ok(10).copy(handshake = true), 7L to ok(5), 8L to ok(1))
        val handshake = tcp.copy(method = "handshake:tuic")
        val target = tcp.copy(target = "other.invalid:443")
        val route = tcp.copy(route = "different-network")
        val cohorts = listOf(tcp, handshake, target, route, tcp, handshake, target, route)
        val samples = profiles.mapIndexed { i, p -> p.id to PingPresentation.Sample(results.getValue(p.id), cohorts[i]) }.toMap()
        assertEquals(listOf(5L, 6L, 7L, 8L, 1L, 2L, 3L, 4L), order(profiles, results, samples))
    }

    @Test fun expiredChangedOrUnknownMeasurementsNeverBecomeZeroLatencySuccesses() {
        val profiles = (1L..5L).map { p(it) }
        val results = mapOf(1L to ok(100), 2L to ok(0).copy(measuredAt = now - 300_001), 3L to ok(1),
            4L to PingResult.Error("Failure", now), 5L to PingResult.Testing("Synthetic test"))
        val samples = results.mapValues { PingPresentation.Sample(it.value, tcp) }.toMutableMap()
        samples[3] = PingPresentation.Sample(ok(9), tcp) // Replaced result: old sorting evidence is invalid.
        assertEquals(listOf(1L, 2L, 3L, 5L, 4L), order(profiles, results, samples))
        assertEquals(profiles.map { it.id }, order(profiles, results, samples, at = now + 300_001))
    }

    @Test fun httpsKeepsItsOwnCohortAndConnectedCollectionOverridesRecencyWithoutPersistingIt() {
        val profiles = listOf(p(1, "old", 900), p(2, "current"), p(3, "current"))
        val results = mapOf(2L to ok(80).copy(verified = true, quick = false), 3L to ok(20).copy(verified = true, quick = false))
        val https = tcp.copy(method = "https:3", target = "https://example.invalid/generate_204")
        val samples = results.mapValues { PingPresentation.Sample(it.value, https) }
        assertEquals(listOf(3L, 2L, 1L), order(profiles, results, samples, connected = 3))
        assertEquals(listOf(1L, 2L, 3L), ConnectionRecency.collections(profiles).flatten().map { it.id })
    }

    @Test fun verifiedAndUnverifiedEvidenceNeverBorrowEachOthersMeasurementMethod() {
        val profiles = (1L..4L).map { p(it) }
        val results = mapOf(1L to ok(90), 2L to ok(1).copy(verified = true), 3L to ok(5), 4L to ok(0))
        val https = tcp.copy(method = "https:3")
        val samples = mapOf(1L to PingPresentation.Sample(results.getValue(1), tcp),
            2L to PingPresentation.Sample(results.getValue(2), tcp), // A mismatched method cannot rank HTTPS as TCP.
            3L to PingPresentation.Sample(results.getValue(3), tcp),
            4L to PingPresentation.Sample(results.getValue(4), https)) // Nor can unverified ping borrow HTTPS trust.
        assertEquals(listOf(3L, 1L, 2L, 4L), order(profiles, results, samples))
    }
}
