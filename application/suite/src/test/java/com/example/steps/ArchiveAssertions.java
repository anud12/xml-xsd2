package com.example.steps;

import java.io.File;
import java.nio.file.Files;
import java.util.Objects;
import java.util.regex.Pattern;

public class ArchiveAssertions {
    public static void assertStdoutContainsLine(ArchiveState state, String expectedLine) {
        if (state.lastOutput == null) {
            throw new AssertionError("No output captured from runtime");
        }
        var outputString = new String(state.lastOutput);
        if (!outputString.contains("\n" + expectedLine)) {
            throw new AssertionError("Expected output to contain: '" + expectedLine + "' but was:\n" + outputString);
        }
    }

    public static void assertLogLineContainsRegex(ArchiveState state, String arg0) {
        if (state.lastOutput == null) {
            throw new AssertionError("No output captured from runtime");
        }
        String output = new String(state.lastOutput);
        Pattern pattern = Pattern.compile(arg0);
        boolean found = output.lines().anyMatch(line -> pattern.matcher(line).find());
        if (!found) {
            throw new AssertionError("Output log line matching regex '" + arg0 + "' not found. Output:\n" + output);
        }
    }

    public static void waitUntilLogLineContainsRegex(ArchiveState state, String regex) throws InterruptedException {
        Pattern pattern = Pattern.compile(regex);
        long timeoutMillis = 10000; // 10 seconds max wait
        long pollInterval = 100;
        long start = System.currentTimeMillis();
        while (System.currentTimeMillis() - start < timeoutMillis) {
            if (state.lastOutput != null) {
                String output = new String(state.lastOutput);
                boolean found = output.lines().anyMatch(line -> pattern.matcher(line).find());
                if (found) return;
            }
            Thread.sleep(pollInterval);
        }
        throw new AssertionError("Timeout waiting for log line matching regex: " + regex);
    }
}
