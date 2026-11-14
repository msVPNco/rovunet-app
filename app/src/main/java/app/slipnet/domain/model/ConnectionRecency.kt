package app.slipnet.domain.model

/** Successful user connections are already persisted; probes never update these timestamps. */
object ConnectionRecency {
    fun profiles(profiles: List<ServerProfile>): List<ServerProfile> =
        profiles.sortedByDescending { it.lastConnectedAt.coerceAtLeast(0) }

    fun collections(profiles: List<ServerProfile>): List<List<ServerProfile>> =
        profiles.groupBy(ProfileCollections::key).values
            .sortedByDescending { group -> group.maxOf { it.lastConnectedAt.coerceAtLeast(0) } }
            .map(ConnectionRecency::profiles)
}
