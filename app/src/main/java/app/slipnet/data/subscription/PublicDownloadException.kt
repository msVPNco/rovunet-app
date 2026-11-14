package app.slipnet.data.subscription

/** Safe stage codes only: no feed body, credentials, URL or raw exception message. */
internal class PublicDownloadException(val kind: Kind) : IllegalStateException("PUBLIC_FETCH_${kind.name}") {
    enum class Kind { NETWORK, TLS, TIMEOUT, NOT_FOUND, HTTP, RATE_LIMIT, REDIRECT }
    val canFallback get() = kind in setOf(Kind.NETWORK, Kind.TLS, Kind.TIMEOUT, Kind.NOT_FOUND)

    companion object {
        fun io(error: java.io.IOException) = PublicDownloadException(when (error) {
            is javax.net.ssl.SSLException -> Kind.TLS
            is java.net.SocketTimeoutException, is java.io.InterruptedIOException -> Kind.TIMEOUT
            else -> Kind.NETWORK
        })
        fun http(status: Int) = PublicDownloadException(when (status) {
            404 -> Kind.NOT_FOUND
            403, 429 -> Kind.RATE_LIMIT
            else -> Kind.HTTP
        })
    }
}
