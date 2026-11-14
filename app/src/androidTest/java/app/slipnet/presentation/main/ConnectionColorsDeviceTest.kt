package app.slipnet.presentation.main

import android.graphics.Bitmap
import androidx.compose.foundation.layout.Column
import androidx.compose.runtime.*
import androidx.compose.ui.graphics.toPixelMap
import androidx.compose.ui.semantics.SemanticsProperties
import androidx.compose.ui.test.*
import androidx.compose.ui.test.junit4.createComposeRule
import androidx.test.platform.app.InstrumentationRegistry
import app.slipnet.data.local.datastore.DarkMode
import app.slipnet.data.subscription.AccountStatus
import app.slipnet.data.subscription.AccountStatusView
import app.slipnet.domain.model.*
import app.slipnet.presentation.theme.SlipstreamTheme
import org.junit.Assert.*
import org.junit.Rule
import org.junit.Test
import java.io.File

class ConnectionColorsDeviceTest {
    @get:Rule val compose = createComposeRule()
    private val profile = ServerProfile(id = 4801, name = "Synthetic connection",
        domain = "example.invalid", tunnelType = TunnelType.SSH, collectionId = "code48-synthetic")

    @Test fun connectionCardAndBottomButtonShareGreenYellowAndRedStates() {
        var indicator by mutableStateOf(ConnectionIndicator.DISCONNECTED)
        compose.setContent { SlipstreamTheme(darkMode = DarkMode.DARK) {
            val state = when (indicator) {
                ConnectionIndicator.CONNECTED -> ConnectionState.Connected(profile)
                ConnectionIndicator.CONNECTING -> ConnectionState.Connecting
                ConnectionIndicator.ERROR -> ConnectionState.Error("Synthetic connection error")
                else -> ConnectionState.Disconnected
            }
            Column {
                CollectionCard(listOf(profile), emptyMap(), selected = true, expanded = false,
                    connectedProfileId = if (state.isConnected) profile.id else null, manualProfileId = profile.id,
                    connecting = state.isConnecting, busy = false, canConnect = true,
                    onConnect = {}, onDetails = {}, onRename = {}, onMove = {}, onTest = {},
                    onStabilityTest = {}, onExport = {}, onDelete = {}, canDelete = true,
                    connectionIndicator = indicator)
                ConnectFab(state, true, 100, {})
            }
        } }
        val stateLabels = mutableSetOf<String>()
        for (value in listOf(ConnectionIndicator.CONNECTING, ConnectionIndicator.CONNECTED, ConnectionIndicator.ERROR)) {
            compose.runOnIdle { indicator = value }
            compose.mainClock.advanceTimeBy(400)
            val descriptions = mutableListOf<String>()
            for (tag in listOf("collection-connect-code48-synthetic", "main-connect")) {
                val node = compose.onNodeWithTag(tag)
                descriptions += node.fetchSemanticsNode().config[SemanticsProperties.StateDescription]
                val image = node.captureToImage()
                val pixel = image.toPixelMap()[image.width / 4, image.height / 2]
                when (value) {
                    ConnectionIndicator.CONNECTED -> assertTrue(tag, pixel.green > pixel.red && pixel.green > pixel.blue)
                    ConnectionIndicator.CONNECTING -> assertTrue(tag, pixel.red > pixel.blue && pixel.green > pixel.blue)
                    ConnectionIndicator.ERROR -> assertTrue(tag, pixel.red > pixel.green && pixel.red > pixel.blue)
                    else -> error("Unexpected state")
                }
            }
            assertEquals(descriptions.first(), descriptions.last())
            assertTrue(descriptions.first().isNotBlank())
            stateLabels += descriptions.first()
            val instrumentation = InstrumentationRegistry.getInstrumentation()
            val bitmap = requireNotNull(instrumentation.uiAutomation.takeScreenshot())
            File(instrumentation.targetContext.filesDir, "connection-code48-${value.name.lowercase()}.png")
                .outputStream().use { assertTrue(bitmap.compress(Bitmap.CompressFormat.PNG, 100, it)) }
            bitmap.recycle()
        }
        assertEquals(3, stateLabels.size)
    }

    @Test fun partialUsageStillShowsKnownQuotaAndExpiryAndDoesNotClaimRemainingVolume() {
        val now = System.currentTimeMillis()
        val status = AccountStatus("a".repeat(32), "b".repeat(32), "c".repeat(32), "active",
            now + 24 * 86_400_000L, 26_843_545_600L, null, "partial", now, now, 1,
            usageLowerBound = 123, contractVersion = 2, pendingBytes = 10, measurement = "accounted_lower_bound")
        val view = AccountStatusView.evaluate(status, now, 1000, now, "", now, 1000)
        compose.setContent { SlipstreamTheme(darkMode = DarkMode.DARK) { AccountStatusSummary(view) } }
        val node = compose.onNodeWithTag("account-entitlement")
        node.assertIsDisplayed()
        val text = node.fetchSemanticsNode().config[SemanticsProperties.Text].joinToString { it.text }
            .map { it.digitToIntOrNull()?.digitToChar() ?: it }.joinToString("")
        assertTrue(text, text.contains("25") && text.contains("24"))
        assertFalse(view.trustworthy)
        assertNull(view.status?.remainingBytes)
    }
}
