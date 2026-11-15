package app.slipnet.tunnel

import com.jcraft.jsch.HostKeyRepository
import app.slipnet.util.ConnectionDiagnostics.Event

/** Trace stages without changing the repository's approval or replacement decisions. */
internal object SshHostKeyTrace {
    fun wrap(repository: HostKeyRepository, record: (Event) -> Unit): HostKeyRepository =
        object : HostKeyRepository by repository {
            override fun check(host: String, key: ByteArray): Int {
                record(Event.SSH_HOSTKEY_CHECK)
                return try {
                    repository.check(host, key).also {
                        record(if (it == HostKeyRepository.OK) Event.SSH_HOSTKEY_ACCEPTED else Event.SSH_HOSTKEY_REJECTED)
                    }
                } catch (failure: Exception) {
                    record(Event.SSH_HOSTKEY_REJECTED)
                    throw failure
                }
            }
        }
}
