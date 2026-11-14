package app.slipnet.domain.model

/** HTTPS collection tests may offer DNS separately; quick ping never runs DNS. */
object CollectionTestPhase {
    enum class FastState { SUCCESS, INCOMPLETE, NEEDS_REVIEW, FAILED }
    fun candidates(members: List<ServerProfile>, dnsOnly: Boolean, single: Boolean = false) =
        if (single) members else members.filter { ProfileCollections.isDns(it) == dnsOnly }

    // No fixed count: a limited account tests only its own authorized fast routes.
    fun quickCandidates(members: List<ServerProfile>) = members.filterNot(ProfileCollections::isDns)

    fun fastState(members: List<ServerProfile>, results: Map<Long, PingResult>, detailed: Boolean): FastState {
        val fast = candidates(members, false)
        if (fast.any { p -> (results[p.id] as? PingResult.Success)?.let {
                ProbeCache.fresh(it) && (!detailed || it.verified)
            } == true }) return FastState.SUCCESS
        if (fast.any { results[it.id] == PingResult.ReviewIdentity }) return FastState.NEEDS_REVIEW
        if (fast.any { p -> when (val r = results[p.id]) {
                is PingResult.Error -> !ProbeCache.fresh(r)
                is PingResult.Success -> !ProbeCache.fresh(r) || detailed && !r.verified
                PingResult.Skipped -> false
                else -> true
            } }) return FastState.INCOMPLETE
        return FastState.FAILED
    }

    fun offerDns(members: List<ServerProfile>, results: Map<Long, PingResult>, detailed: Boolean,
        completed: Boolean, dnsOnly: Boolean = false): Boolean = detailed && completed && !dnsOnly &&
        members.any(ProfileCollections::isDns) && fastState(members, results, detailed) == FastState.FAILED
}
