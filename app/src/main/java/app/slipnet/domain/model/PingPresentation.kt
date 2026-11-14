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
                else r.quick && (method == "tcp" && !r.handshake || method.startsWith("handshake:") && r.handshake)
            sample.cohort != null && r is PingResult.Success && comparableMethod && r.latencyMs >= 0 &&
                r == results[p.id] && ProbeCache.fresh(r, now)
        }
        val passed = profiles.filter { success(it) != null }
        // For quick tests the configured server is the variable being compared:
        // different server endpoints still measure the same TCP/protocol operation.
        // HTTPS destinations are control targets, so those remain separate.
        fun comparison(cohort: Cohort) = when {
            cohort.method == "tcp" || cohort.method.startsWith("handshake:") -> cohort.copy(target = "configured-endpoint")
            else -> cohort
        }
        // Keep each method/network group together; never imply a TCP connect and
        // a protocol-auth handshake or HTTPS request are the same speed metric.
        val ordered = passed.groupBy { comparison(success(it)!!.cohort!!) }.values.flatMap { group ->
            group.sortedBy { (samples.getValue(it.id).result as PingResult.Success).latencyMs }
        }
        val passedIds = passed.map { it.id }.toSet()
        val remaining = profiles.filterNot { it.id in passedIds }
        val (failed, unknown) = remaining.partition { results[it.id]?.let { r -> r is PingResult.Error && ProbeCache.fresh(r, now) } == true }
        return ordered + unknown + failed
    }
}
