package app.slipnet.presentation.theme

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.ui.res.stringResource
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import app.slipnet.domain.model.ConnectionIndicator
import app.slipnet.R

data class ConnectionActionColors(val container: Color, val content: Color)

@Composable
fun connectionActionDescription(indicator: ConnectionIndicator): String = stringResource(when (indicator) {
    ConnectionIndicator.CONNECTED -> R.string.rv_connected_229655
    ConnectionIndicator.CONNECTING -> R.string.rv_connecting_5f04ae
    ConnectionIndicator.DISCONNECTING -> R.string.rv_disconnecting_cb5a85
    ConnectionIndicator.ERROR -> R.string.rv_connection_failed_42aa4e
    ConnectionIndicator.DISCONNECTED -> R.string.rv_not_connected_87d82c
})

/** Shared by the collection button and bottom connection button. */
@Composable
fun connectionActionColors(indicator: ConnectionIndicator): ConnectionActionColors = when (indicator) {
    ConnectionIndicator.CONNECTED -> ConnectionActionColors(Color(0xFF356A53), Color(0xFFF0FFF6))
    ConnectionIndicator.CONNECTING, ConnectionIndicator.DISCONNECTING ->
        ConnectionActionColors(Color(0xFFE2C16C), Color(0xFF342B13))
    ConnectionIndicator.ERROR -> ConnectionActionColors(Color(0xFFAF4C54), Color(0xFFFFF2F3))
    ConnectionIndicator.DISCONNECTED -> ConnectionActionColors(MaterialTheme.colorScheme.primary, MaterialTheme.colorScheme.onPrimary)
}

/** A 32dp flat face inside a separate 48dp touch target. */
@Composable
fun FlatCardIconButton(onClick: () -> Unit, modifier: Modifier = Modifier, enabled: Boolean = true,
    containerColor: Color = Color.Unspecified, contentColor: Color = Color.Unspecified,
    content: @Composable () -> Unit) {
    val tones = collectionTones
    val fill = if (containerColor == Color.Unspecified) tones.button else containerColor
    val foreground = if (contentColor == Color.Unspecified) MaterialTheme.colorScheme.primary else contentColor
    IconButton(onClick = onClick, enabled = enabled, modifier = modifier.size(48.dp),
        colors = IconButtonDefaults.iconButtonColors(contentColor = foreground, disabledContentColor = foreground.copy(alpha = .38f))) {
        Surface(shape = RoundedCornerShape(8.dp), color = fill.copy(alpha = if (enabled) 1f else .4f),
            contentColor = if (enabled) foreground else foreground.copy(alpha = .38f),
            border = BorderStroke(1.dp, if (containerColor == Color.Unspecified) tones.buttonOutline else fill)) {
            Box(Modifier.size(32.dp), contentAlignment = Alignment.Center) {
                Box(Modifier.size(18.dp), contentAlignment = Alignment.Center) { content() }
            }
        }
    }
}
