package app.slipnet.core

import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

class ProcessCompatibilityTest {
    private fun child(live: Boolean): Process {
        if (System.getProperty("os.name").startsWith("Windows")) {
            val shell = File(System.getenv("SystemRoot"), "System32/WindowsPowerShell/v1.0/powershell.exe")
            // A single owned process, with no shell child left behind after destroy.
            return ProcessBuilder(shell.absolutePath, "-NoProfile", "-NonInteractive", "-Command",
                if (live) "[Console]::Write('ready'); Start-Sleep -Seconds 30" else "exit 0").start()
        }
        return ProcessBuilder("sh", "-c", if (live) "printf ready; exec sleep 30" else "exit 0").start()
    }
    private fun awaitExit(process: Process): Boolean {
        val deadline = System.nanoTime() + 2_000_000_000L
        while (ProcessCompatibility.isAlive(process) && System.nanoTime() < deadline) Thread.sleep(5)
        return !ProcessCompatibility.isAlive(process)
    }
    @Test fun liveChildAndTerminationUseTheLegacyCompatiblePath() {
        // Gradle uses mock SDK_INT=0; the portable host fixture uses API24. This exercises
        // the legacy destroy fallback on a real host child, not an Android device.
        val process = child(live = true)
        try {
            repeat(5) { process.inputStream.read() }
            assertTrue(ProcessCompatibility.isAlive(process))
            ProcessCompatibility.destroyForcibly(process)
            assertTrue(awaitExit(process))
            process.exitValue()
            assertFalse(ProcessCompatibility.isAlive(process))
        } finally {
            process.destroy()
        }
    }

    @Test fun anAlreadyExitedChildIsNotReportedAsRunning() {
        val process = child(live = false)
        assertTrue(awaitExit(process))
        assertFalse(ProcessCompatibility.isAlive(process))
    }
}
