package app.slipnet.presentation.theme

import android.app.Activity
import android.os.Build
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.dynamicDarkColorScheme
import androidx.compose.material3.dynamicLightColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.SideEffect
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalView
import androidx.core.view.WindowCompat
import app.slipnet.data.local.datastore.DarkMode

// Brand roles are explicit in both themes; cards must remain distinct from the background.
private val DarkColorScheme = darkColorScheme(
    primary = Color(0xFFC6BDDB), onPrimary = Color(0xFF292531),
    primaryContainer = Color(0xFF30313B), onPrimaryContainer = Color(0xFFEEEEF3),
    secondary = Color(0xFFB4CEC5), onSecondary = Color(0xFF243B32),
    secondaryContainer = Color(0xFF2E403B), onSecondaryContainer = Color(0xFFE3F1EB),
    tertiary = Color(0xFFBECBDC), onTertiary = Color(0xFF253449),
    background = Color(0xFF15171D), onBackground = Color(0xFFF1F2F5),
    surface = Color(0xFF252F3E), onSurface = Color(0xFFF1F2F5),
    surfaceContainerLowest = Color(0xFF15171D), surfaceContainerLow = Color(0xFF1E2129),
    surfaceContainer = Color(0xFF252F3E), surfaceContainerHigh = Color(0xFF2C3747),
    surfaceContainerHighest = Color(0xFF344357), surfaceVariant = Color(0xFF344357),
    onSurfaceVariant = Color(0xFFC6CBD4), outline = Color(0xFFAAA8B5), outlineVariant = Color(0xFF526074),
    error = Color(0xFFFFB8BE), onError = Color(0xFF650F21),
    errorContainer = Color(0xFF71262D), onErrorContainer = Color(0xFFFFDAD6)
)

private val AmoledColorScheme = DarkColorScheme.copy(
    background = Color.Black, surface = Color(0xFF171D27), surfaceContainerLowest = Color.Black,
    surfaceContainerLow = Color(0xFF101319), surfaceContainer = Color(0xFF171D27),
    primaryContainer = Color(0xFF222229), surfaceContainerHigh = Color(0xFF202938),
    surfaceContainerHighest = Color(0xFF283445), surfaceVariant = Color(0xFF283445),
    outlineVariant = Color(0xFF465165)
)

private val LightColorScheme = lightColorScheme(
    primary = Color(0xFF74678B), onPrimary = Color.White,
    primaryContainer = Color(0xFFE8E6EF), onPrimaryContainer = Color(0xFF2D2C36),
    secondary = Color(0xFF476B5C), onSecondary = Color.White,
    secondaryContainer = Color(0xFFE0EBE5), onSecondaryContainer = Color(0xFF243B32),
    tertiary = Color(0xFF52667E), onTertiary = Color.White,
    background = Color(0xFFF5F5F7), onBackground = Color(0xFF282B35),
    surface = Color(0xFFEDF1F6), onSurface = Color(0xFF282B35),
    surfaceContainerLowest = Color.White, surfaceContainerLow = Color(0xFFFAFAFB),
    surfaceContainer = Color(0xFFEDF1F6), surfaceContainerHigh = Color(0xFFE8EDF3),
    surfaceContainerHighest = Color(0xFFE5EBF2), surfaceVariant = Color(0xFFE8EDF3),
    onSurfaceVariant = Color(0xFF505966), outline = Color(0xFF7B8190), outlineVariant = Color(0xFFC1CAD6),
    error = Color(0xFFB3261E), onError = Color.White,
    errorContainer = Color(0xFFFFDAD6), onErrorContainer = Color(0xFF410002)
)

@Composable
fun SlipstreamTheme(
    darkMode: DarkMode = DarkMode.SYSTEM,
    dynamicColor: Boolean = false,
    content: @Composable () -> Unit
) {
    val darkTheme = when (darkMode) {
        DarkMode.LIGHT -> false
        DarkMode.DARK -> true
        DarkMode.AMOLED -> true
        DarkMode.SYSTEM -> isSystemInDarkTheme()
    }

    val colorScheme = when {
        darkMode == DarkMode.AMOLED -> AmoledColorScheme
        dynamicColor && Build.VERSION.SDK_INT >= Build.VERSION_CODES.S -> {
            val context = LocalContext.current
            if (darkTheme) dynamicDarkColorScheme(context) else dynamicLightColorScheme(context)
        }
        darkTheme -> DarkColorScheme
        else -> LightColorScheme
    }

    val view = LocalView.current
    if (!view.isInEditMode) {
        SideEffect {
            val window = (view.context as Activity).window
            // Control status bar and navigation bar icon colors based on theme
            val insetsController = WindowCompat.getInsetsController(window, view)
            insetsController.isAppearanceLightStatusBars = !darkTheme
            insetsController.isAppearanceLightNavigationBars = !darkTheme
        }
    }

    MaterialTheme(
        colorScheme = colorScheme,
        typography = Typography,
        content = content
    )
}
