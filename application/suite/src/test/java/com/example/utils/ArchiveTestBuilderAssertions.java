package com.example.utils;

import java.util.regex.Pattern;

public interface ArchiveTestBuilderAssertions extends ArchiveTestBuilderCommon {

    default ArchiveTestBuilder assertLogLines(long expectedCount, String regexPattern) {
        LogAssertions.assertLogLineContainsRegex(this.getState(), expectedCount, regexPattern);
        return (ArchiveTestBuilder) this;
    }

    default ArchiveTestBuilder assertNotInLogs(String regexPattern) {
        String output = this.getState().lastOutput != null ? new String(this.getState().lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
        Pattern pattern = Pattern.compile(regexPattern.replace("{", "(").replace("}", ")"));
        for (String line : output.split("\\r?\\n")) {
            if (pattern.matcher(line).find()) {
                throw new AssertionError(
                        "Expected no log lines matching regex '" + regexPattern + "' but found at least one.\nFull output:\n" + output);
            }
        }
        return (ArchiveTestBuilder) this;
    }

    default ArchiveTestBuilder assertLogLineContains(int expectedCount, String regex) {
        String output = this.getState().lastOutput != null ? new String(this.getState().lastOutput) : "";
        Pattern pattern = Pattern.compile(regex.replace("{", "(").replace("}", ")"));
        int matches = 0;
        for (String line : output.split("\\r?\\n")) {
            if (pattern.matcher(line).find())
                matches++;
        }
        if (matches != expectedCount) {
            throw new AssertionError(
                    "Expected exactly %d log line(s) matching regex '%s' but found %d.\nFull output:\n%s"
                            .formatted(expectedCount, regex, matches, output));
        }
        return (ArchiveTestBuilder) this;
    }

    default ArchiveTestBuilder assertExportedStateEmpty() throws Exception {
        StateAssertions.assertExportedStateEmpty(this.getState());
        return (ArchiveTestBuilder) this;
    }

    // Convenience shortcuts for common table names
    default ArchiveTestBuilder assertExportedActions(String csvFile) throws Exception {
        StateAssertions.assertExportedStateActionColumnsMatchesCsv(this.getState(), csvFile);
        return (ArchiveTestBuilder) this;
    }

    default ArchiveTestBuilder assertExportedModules(String csvFile) throws Exception {
        StateAssertions.assertExportedStateModuleColumnsMatchesCsv(this.getState(), csvFile);
        return (ArchiveTestBuilder) this;
    }

    default ArchiveTestBuilder assertExportedEntities(String csvFile) throws Exception {
        StateAssertions.assertExportedStateEntityColumnsMatchesCsv(this.getState(), csvFile);
        return (ArchiveTestBuilder) this;
    }

    default ArchiveTestBuilder assertExportedEvents(String csvFile) throws Exception {
        StateAssertions.assertExportedStateEventsColumnsMatchesCsv(this.getState(), csvFile);
        return (ArchiveTestBuilder) this;
    }

    default ArchiveTestBuilder assertExportedPanels(String csvFile) throws Exception {
        StateAssertions.assertExportedStatePanelColumnsMatchesCsv(this.getState(), csvFile);
        return (ArchiveTestBuilder) this;
    }

    default ArchiveTestBuilder assertPanelNames(String json) {
        StateAssertions.assertReturnedPanelNamesIsIn(this.getState(), json);
        return (ArchiveTestBuilder) this;
    }
}
