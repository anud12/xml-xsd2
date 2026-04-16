package com.example.utils;

import java.util.regex.Pattern;

public class LogAssertions {

    public static void assertLogLineContainsRegex(ArchiveState state, String arg0) {
        if (state.lastOutput == null) {
            throw new AssertionError("No output captured from runtime");
        }
        Pattern pattern = Pattern.compile(arg0);
        boolean found = state.logMessages.stream().anyMatch(line -> pattern.matcher(line).find());
        if (!found) {
            throw new AssertionError("Output log line matching regex '" + arg0 + "' not found. Output:\n" + String.join("\n", state.logMessages));
        }
    }
}
