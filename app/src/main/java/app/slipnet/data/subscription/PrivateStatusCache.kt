package app.slipnet.data.subscription

import org.json.JSONObject

/** Local-only envelope; never accepted as a server response. No Room schema change. */
internal object PrivateStatusCache {
    // Bound local metadata without ever forgetting a retired period to admit a replay.
    // Reaching this bound fails metadata refresh closed; it never gates a connection.
    private const val MAX_PERIODS = 256
    data class Watermark(val revision: Long, val confirmed: Long?)
    data class Receipt(val current: AccountStatus, val periods: Map<String, Watermark>)

    fun read(payload: String): Receipt {
        require(payload.length <= 131_072) { "UNTRUSTED_STATUS" }
        val root = JSONObject(payload)
        if (!root.has("_cache_version")) {
            val current = AccountStatus.read(payload)
            return Receipt(current, if (current.contractVersion == 2)
                mapOf(current.periodId to Watermark(current.revision, current.usageLowerBound)) else emptyMap())
        }
        require(root.getInt("_cache_version") == 1) { "UNTRUSTED_STATUS" }
        val current = AccountStatus.read(root.getJSONObject("current").toString())
        require(current.contractVersion == 2) { "UNTRUSTED_STATUS" }
        val entries = root.getJSONObject("periods")
        require(entries.length() in 1..MAX_PERIODS) { "UNTRUSTED_STATUS" }
        fun count(value: Any): Long {
            require(value is Number && value.toString().matches(Regex("0|[1-9][0-9]*"))) { "UNTRUSTED_STATUS" }
            return value.toString().toLong()
        }
        val periods = entries.keys().asSequence().associateWith { id ->
            require(id.matches(Regex("[0-9a-f]{32}"))) { "UNTRUSTED_STATUS" }
            val entry = entries.getJSONObject(id)
            Watermark(count(entry.get("revision")).also { require(it > 0) },
                if (entry.isNull("confirmed")) null else count(entry.get("confirmed")))
        }
        val mark = requireNotNull(periods[current.periodId])
        require(mark.revision == current.revision &&
            (current.usageLowerBound == null || mark.confirmed == current.usageLowerBound)) { "UNTRUSTED_STATUS" }
        return Receipt(current, periods)
    }

    /** Unknown usage may be displayed, but must not erase the last confirmed bound. */
    fun merge(binding: PrivateStatusBinding?, source: String, incoming: AccountStatus,
        previousPayload: String?, hasPriorSourceBinding: Boolean = false): String? = runCatching {
        if (binding == null) {
            require(previousPayload == null && !hasPriorSourceBinding && !incoming.providerReported)
            return@runCatching if (incoming.contractVersion == 2) encode(Receipt(incoming,
                mapOf(incoming.periodId to Watermark(incoming.revision, incoming.usageLowerBound)))) else incoming.json()
        }
        require(binding.sourceFingerprint == source && binding.serverId == incoming.serverId &&
            binding.accountId == incoming.accountId && !incoming.providerReported)
        val previous = read(requireNotNull(previousPayload))
        require(incoming.contractVersion == previous.current.contractVersion)
        if (incoming.contractVersion == 1) {
            require(PrivateStatusPolicy.accepts(binding, source, incoming, previous.current))
            return@runCatching incoming.json()
        }
        require(binding.highWater == previous.current.revision)
        val known = previous.periods[incoming.periodId]
        if (incoming.periodId == previous.current.periodId) {
            require(known != null && incoming.revision >= known.revision)
            require(incoming.revision != known.revision || incoming.accepts(previous.current))
            require(incoming.usageLowerBound == null || known.confirmed == null || incoming.usageLowerBound >= known.confirmed)
        } else {
            // A genuinely new period may reset revision; a retired period may not return.
            require(known == null && previous.periods.size < MAX_PERIODS)
        }
        encode(Receipt(incoming, previous.periods + (incoming.periodId to
            Watermark(incoming.revision, incoming.usageLowerBound ?: known?.confirmed))))
    }.getOrNull()

    private fun encode(receipt: Receipt): String {
        val periods = JSONObject()
        receipt.periods.forEach { (id, mark) -> periods.put(id, JSONObject()
            .put("revision", mark.revision).put("confirmed", mark.confirmed ?: JSONObject.NULL)) }
        return JSONObject().put("_cache_version", 1).put("current", JSONObject(receipt.current.json()))
            .put("periods", periods).toString()
    }
}
