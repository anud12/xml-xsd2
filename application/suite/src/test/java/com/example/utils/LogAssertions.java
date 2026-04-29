package com.example.utils;

import java.util.regex.Pattern;

public class LogAssertions {

    public static void assertLogLineContainsRegex(ArchiveState state, long expectedCount, String arg0) {
        if (state.lastOutput == null) {
            throw new AssertionError("No output captured from runtime");
        }
        Pattern pattern = Pattern.compile(arg0);
        
        System.out.println("DEBUG ASSERTION: Looking for pattern: " + arg0);
        System.out.println("DEBUG ASSERTION: logMessages size: " + state.logMessages.size());
        
        var matchingLines = state.logMessages.stream()
                .filter(line -> {
                    boolean matches = pattern.matcher(line).find();
                    if (matches) {
                        System.out.println("DEBUG ASSERTION: MATCH: " + line);
                    }
                    return matches;
                })
                .toList();
        
        long found = matchingLines.size();

        if (expectedCount != 0 && found == 0) {
            throw new AssertionError("Output log line matching regex '" + arg0 + "' not found. Output:\n" + String.join("\n", state.logMessages));
        }

        if (found != expectedCount) {
            throw new AssertionError("Output log line matching regex '" + arg0 + "' found '" + found + "' more than expected '" + expectedCount + "'. Output:\n" + String.join("\n", state.logMessages));
        }
    }
}
