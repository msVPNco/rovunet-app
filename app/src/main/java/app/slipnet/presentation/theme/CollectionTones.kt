package app.slipnet.presentation.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.luminance

/** Separate surfaces make a collection's hierarchy readable in both appearances. */
data class CollectionTones(
    val header: Color, val raisedHeader: Color, val headerText: Color,
    val tray: Color, val child: Color, val selectedChild: Color, val childOutline: Color,
    val button: Color, val buttonOutline: Color
)

val collectionTones: CollectionTones
    @Composable get() {
        val colors = MaterialTheme.colorScheme
        return when {
            colors.background == Color.Black -> CollectionTones(
                Color(0xFF222229), Color(0xFF33323D), Color(0xFFEEEEF3),
                Color.Transparent, Color(0xFF171D27), Color(0xFF283445), Color(0xFF465165),
                Color(0xFF383640), Color(0xFF686371))
            colors.background.luminance() > 0.5f -> CollectionTones(
                Color(0xFFE8E6EF), Color(0xFFDFDCE9), Color(0xFF2D2C36),
                Color.Transparent, Color(0xFFEDF1F6), Color(0xFFE5EBF2), Color(0xFFC1CAD6),
                Color(0xFFF6F4F9), Color(0xFFC4BFCE))
            else -> CollectionTones(
                Color(0xFF30313B), Color(0xFF3B3B49), Color(0xFFEEEEF3),
                Color.Transparent, Color(0xFF252F3E), Color(0xFF344357), Color(0xFF526074),
                Color(0xFF44434F), Color(0xFF797583))
        }
    }
