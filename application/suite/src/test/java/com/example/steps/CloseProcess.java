package com.example.steps;

import java.io.IOException;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.util.concurrent.TimeUnit;

public class CloseProcess {
    public static void closeProcess(ArchiveState state) {
        try {
            Process p = state.runProcess;
            if (p == null) {
                throw new IllegalStateException("state.runProcess is null");
            }
            OutputStream os = p.getOutputStream();
            if (os == null) {
                throw new IllegalStateException("Process output stream is null");
            }
            String cmd = "DEBUG: shutdown" + System.lineSeparator();
            os.write(cmd.getBytes(StandardCharsets.UTF_8));
            // Close stdin to signal EOF to the child process
            try {
                os.close();
            } catch (IOException ignored) {
            }

            // Wait for the process to exit (timeout after 60 seconds)
            boolean exited;
            try {
                exited = p.waitFor(60, TimeUnit.SECONDS);
            } catch (InterruptedException ie) {
                Thread.currentThread().interrupt();
                throw new RuntimeException("Interrupted while waiting for process to exit", ie);
            }
            if (!exited) {
                throw new AssertionError("Process did not exit within 60 seconds after shutdown signal");
            }
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }
}
