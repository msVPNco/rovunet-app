package app.slipnet.data.subscription

import java.math.BigDecimal
import java.time.Instant
import org.json.JSONObject

/** Independent status2 DTO. Numeric lexemes never pass through Double. */
internal object PrivateStatusV2 {
    private val fields = setOf("version", "server_id", "account_id", "period_id", "revision", "account_status",
        "limit_kind", "limit_bytes", "expiry_kind", "expires_at", "confirmed_bytes", "pending_bytes",
        "coverage", "measurement", "sampled_at", "generated_at")
    private val numeric = Regex("""-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?""")

    fun read(text: String): AccountStatus {
        require(text.length <= 65_536) { "INVALID_STATUS" }
        var position = 0
        fun space() { while (position < text.length && text[position] in " \t\r\n") position++ }
        fun consume(character: Char) { space(); require(position < text.length && text[position++] == character) { "INVALID_STATUS" } }
        fun stringToken(): String {
            space()
            val start = position
            require(position < text.length && text[position++] == '"') { "INVALID_STATUS" }
            while (position < text.length) {
                when (val ch = text[position++]) {
                    '"' -> return text.substring(start, position)
                    '\\' -> {
                        require(position < text.length) { "INVALID_STATUS" }
                        val escape = text[position++]
                        if (escape == 'u') {
                            require(position + 4 <= text.length && text.substring(position, position + 4).all { it in "0123456789abcdefABCDEF" }) { "INVALID_STATUS" }
                            position += 4
                        } else require(escape in "\"\\/bfnrt") { "INVALID_STATUS" }
                    }
                    else -> require(ch >= ' ') { "INVALID_STATUS" }
                }
            }
            error("INVALID_STATUS")
        }
        fun decode(token: String) = JSONObject("""{"value":$token}""").getString("value")
        val values = linkedMapOf<String, String>()
        consume('{')
        while (true) {
            space()
            if (position < text.length && text[position] == '}') { position++; break }
            val key = decode(stringToken())
            require(key in fields && key !in values) { "INVALID_STATUS" }
            consume(':'); space()
            val token = when {
                position < text.length && text[position] == '"' -> stringToken()
                text.startsWith("null", position) -> "null".also { position += 4 }
                else -> numeric.find(text, position)?.takeIf { it.range.first == position }?.value
                    ?.also { position += it.length } ?: error("INVALID_STATUS")
            }
            values[key] = token
            space()
            require(position < text.length) { "INVALID_STATUS" }
            if (text[position] == '}') { position++; break }
            consume(',')
            space(); require(position < text.length && text[position] != '}') { "INVALID_STATUS" }
        }
        space()
        require(position == text.length && values.keys == fields) { "INVALID_STATUS" }
        fun string(key: String): String = values.getValue(key).let { token ->
            require(token.startsWith('"')) { "INVALID_STATUS" }; decode(token)
        }
        fun number(key: String, nullable: Boolean = false): Long? {
            val token = values.getValue(key)
            if (token == "null") { require(nullable) { "INVALID_STATUS" }; return null }
            require(token.length <= 128 && numeric.matches(token)) { "INVALID_STATUS" }
            val exponent = token.substringAfter('e', token.substringAfter('E', "0")).toIntOrNull()
            require(exponent != null && exponent in -64..64) { "INVALID_STATUS" }
            // BigDecimal.longValueExact is available on API1; BigInteger's equivalent needs API31.
            return BigDecimal(token).longValueExact().also { require(it >= 0) { "INVALID_STATUS" } }
        }
        fun time(key: String): Long? = if (values.getValue(key) == "null") null else Instant.parse(string(key)).toEpochMilli()
        fun identity(key: String) = string(key).also { require(it.matches(Regex("[0-9a-f]{32}"))) { "INVALID_STATUS" } }
        require(number("version") == 2L) { "INVALID_STATUS" }
        val revision = requireNotNull(number("revision")).also { require(it > 0) { "INVALID_STATUS" } }
        val state = string("account_status").also { require(it in setOf("active", "expired", "limit_reached", "hold", "archived")) { "INVALID_STATUS" } }
        val limitKind = string("limit_kind").also { require(it in setOf("finite", "unlimited", "unknown")) { "INVALID_STATUS" } }
        val expiryKind = string("expiry_kind").also { require(it in setOf("at", "none", "unknown")) { "INVALID_STATUS" } }
        val limit = number("limit_bytes", true)
        val expiry = time("expires_at")
        require(if (limitKind == "finite") limit != null && limit > 0 else limit == null) { "INVALID_STATUS" }
        require((expiryKind == "at") == (expiry != null)) { "INVALID_STATUS" }
        val coverage = string("coverage").also { require(it in setOf("partial", "unavailable", "not_metered")) { "INVALID_STATUS" } }
        val measurement = string("measurement").also { require(it in setOf("accounted_lower_bound", "none", "unknown")) { "INVALID_STATUS" } }
        return AccountStatus(identity("server_id"), identity("account_id"), identity("period_id"), state,
            expiry, limit, null, coverage, time("sampled_at"), requireNotNull(time("generated_at")), revision,
            volumeKnown = limitKind != "unknown", expiryKnown = expiryKind != "unknown",
            usageLowerBound = number("confirmed_bytes", true), contractVersion = 2,
            pendingBytes = number("pending_bytes", true), measurement = measurement)
    }
}
