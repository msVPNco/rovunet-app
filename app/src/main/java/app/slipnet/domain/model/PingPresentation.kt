package app.slipnet.domain.model

/** Ephemeral display order. Never promotes quick results to verified or writes connection history/order. */
object PingPresentation {
    data class Cohort(val method: String, val target: String, val route: String)
    data class Sample(val result: PingResult, val cohort: Cohort?)

    fun collections(profiles: List<ServerProfile>, results: Map<Long, PingResult>, samples: Map<Long, Sample>,
        connectedId: Long?, now: Long = System.currentTimeMillis()): List<List<ServerProfile>> =
        ConnectionRecency.collections(profiles)
            .map { group ->
                val ordered = if (group.any { it.id in samples }) paths(group, results, samples, now) else group
                ordered.sortedBy { if (it.id == connectedId) 0 else 1 }
            }
            .sortedBy { group -> if (group.any { it.id == connectedId }) 0 else 1 }

    private fun paths(profiles: List<ServerProfile>, results: Map<Long, PingResult>, samples: Map<Long, Sample>,
        now: Long): List<ServerProfile> {
        fun success(p: ServerProfile): Sample? = samples[p.id]?.takeIf { sample ->
            val r = sample.result
            val method = sample.cohort?.method.orEmpty()
            val comparableMethod = r is PingResult.Success && if (r.verified) method.startsWith("https:")
                else r.quick && (method == "tcp" || method.startsWith("handshake:"))
            sample.cohort != null && r is PingResult.Success && comparableMethod && r.latencyMs >= 0 &&
                r == results[p.id] && ProbeCache.fresh(r, now)
        }
        val passed = profiles.filter { success(it) != null }
        // Sort only within comparable slots. TCP, each handshake protocol, target
        // and effective network/connection route retain separate measurement cohorts.
        val queues = passed.groupBy { success(it)!!.cohort }.mapValues { (_, group) ->
            ArrayDeque(group.sortedBy { (samples.getValue(it.id).result as PingResult.Success).latencyMs })
        }
        val ordered = passed.map { queues.getValue(success(it)!!.cohort).removeFirst() }
        val passedIds = passed.map { it.id }.toSet()
        val remaining = profiles.filterNot { it.id in passedIds }
        val (failed, unknown) = remaining.partition { results[it.id]?.let { r -> r is PingResult.Error && ProbeCache.fresh(r, now) } == true }
        return ordered + unknown + failed
    }
}
