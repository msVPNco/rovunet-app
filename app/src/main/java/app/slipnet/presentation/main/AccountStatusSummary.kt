package app.slipnet.presentation.main

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.luminance
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import app.slipnet.R
import app.slipnet.data.subscription.*
import app.slipnet.presentation.theme.collectionTones
import app.slipnet.presentation.common.components.MarqueeText
import java.text.NumberFormat
import java.text.DateFormat
import java.util.Date

/** Known entitlement and measurement confidence are independent. Never invent remaining quota. */
@Composable
internal fun AccountStatusSummary(view: AccountStatusView) {
    val status = view.status
    val color = when (view.severity) {
        AccountSeverity.CRITICAL -> MaterialTheme.colorScheme.error
        AccountSeverity.WARNING -> if (MaterialTheme.colorScheme.background.luminance() > .5f) Color(0xFF885000) else Color(0xFFFFCF70)
        else -> collectionTones.headerText.copy(alpha = .85f)
    }
    val volume = when {
        status == null || !status.volumeKnown -> stringResource(R.string.subscription_volume_unknown)
        status.limitBytes == null -> stringResource(R.string.account_status_unlimited_volume)
        else -> {
            val remaining = status.remainingBytes.takeIf { view.trustworthy }
            val bytes = remaining ?: status.limitBytes
            val gib = bytes >= 1024L * 1024 * 1024
            val number = NumberFormat.getNumberInstance().apply { maximumFractionDigits = 1 }
                .format(bytes / if (gib) 1073741824.0 else 1048576.0)
            stringResource(if (remaining != null) R.string.account_status_remaining_volume else R.string.account_status_volume_limit,
                number, if (gib) "GiB" else "MiB")
        }
    }
    val time = when {
        status == null || !status.expiryKnown -> stringResource(R.string.subscription_expiry_unknown)
        status.expiresAt == null -> stringResource(R.string.account_status_no_expiry)
        !view.detailsCurrent || view.remainingMillis == null -> stringResource(R.string.account_status_expires_at,
            DateFormat.getDateInstance(DateFormat.SHORT).format(Date(status.expiresAt)))
        view.remainingMillis == 0L -> stringResource(R.string.account_status_expired)
        else -> {
            val millis = requireNotNull(view.remainingMillis)
            when {
                millis > 86_400_000 -> stringResource(R.string.account_status_days, (millis + 86_399_999) / 86_400_000)
                millis > 3_600_000 -> stringResource(R.string.account_status_hours, (millis + 3_599_999) / 3_600_000)
                else -> stringResource(R.string.account_status_minutes, ((millis + 59_999) / 60_000).coerceAtLeast(1))
            }
        }
    }
    val advisory = when {
        status == null -> R.string.account_status_unavailable
        view.error.isNotEmpty() || !view.detailsCurrent -> R.string.account_status_stale
        status.state == "hold" -> R.string.account_status_hold
        status.state == "archived" -> R.string.account_status_archived
        status.state == "expired" -> R.string.account_status_expired
        status.state == "limit_reached" -> R.string.account_status_limit_reached
        status.metering == "partial" -> R.string.account_status_partial
        status.metering == "unavailable" -> R.string.account_status_waiting
        !view.trustworthy -> R.string.account_status_stale
        else -> null
    }
    Column(verticalArrangement = Arrangement.spacedBy(2.dp), modifier = Modifier.fillMaxWidth().padding(bottom = 3.dp)) {
        MarqueeText(stringResource(R.string.account_status_summary, volume, time),
            modifier = Modifier.testTag("account-entitlement"), style = MaterialTheme.typography.labelSmall, color = color)
        advisory?.let { MarqueeText(stringResource(it), style = MaterialTheme.typography.labelSmall,
            color = collectionTones.headerText.copy(alpha = .7f)) }
        if (status?.providerReported == true) MarqueeText(stringResource(R.string.subscription_provider_snapshot,
            DateFormat.getDateTimeInstance(DateFormat.SHORT, DateFormat.SHORT).format(Date(status.generatedAt))),
            style = MaterialTheme.typography.labelSmall, color = collectionTones.headerText.copy(alpha = .65f))
        if (view.trustworthy && status?.limitBytes != null && status.remainingBytes != null && status.state == "active") {
            LinearProgressIndicator(progress = { (requireNotNull(status.remainingBytes).toDouble() / status.limitBytes).coerceIn(0.0, 1.0).toFloat() },
                modifier = Modifier.fillMaxWidth().height(3.dp), color = color,
                trackColor = collectionTones.headerText.copy(alpha = .12f))
        }
    }
}
