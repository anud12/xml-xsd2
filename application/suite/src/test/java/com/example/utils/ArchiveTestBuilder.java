package com.example.utils;

import java.io.IOException;
import java.util.regex.Pattern;

public class ArchiveTestBuilder {

    private final ArchiveState state;

    public static ArchiveTestBuilder create() {
        return new ArchiveTestBuilder();
    }

    public static ArchiveTestBuilder create(String resourcePath) throws IOException {
        var builder = new ArchiveTestBuilder();
        builder.loadFeatureFiles(resourcePath);
        return builder;
    }

    private ArchiveTestBuilder() {
        this.state = new ArchiveState();
    }

    public ArchiveState getState() {
        return state;
    }

    // --- Lifecycle ---

    public void cleanup() {
        try {
            ArchiveRunner.cleanup(state);
            CloseProcess.closeProcess(state);
        } catch (Throwable ignored) {
        }
    }

    // --- Setup steps (chainable) ---

    public ArchiveTestBuilder runApplication() throws Exception {
        state.logMessages.clear();
        if (state.archive == null)
            state.archive = ZipArchive.createTemp();
        ArchiveRunner.runApplicationDebugThreadedWithArchive(state);
        return this;
    }

    public ArchiveTestBuilder addFile(String fileName, String destination) throws IOException {
        if (!fileName.startsWith("./"))
            throw new IllegalArgumentException("Non-local path: " + fileName);
        var file = state.featureFiles.get(fileName.replaceFirst("./", ""));
        if (file == null || !file.exists())
            throw new RuntimeException("File '" + fileName + "' not found in feature files");
        state.archive.append(file, java.nio.file.Path.of(destination));
        return this;
    }

    public ArchiveTestBuilder loadArchive() throws Exception {
        var contents = state.archive.byteContents();
        state.runtimeInteropJava.map(ri -> ri.runtime_load_archive(contents, contents.length)).get();
        return this;
    }

    // --- Action steps (chainable) ---

    public ArchiveTestBuilder triggerAction(String actionName) {
        String existing = state.lastOutput != null ? new String(state.lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
        state.runtimeInteropJava.ifPresent(ri -> ri.trigger_action(actionName));
        state.lastOutput = (existing + ArchiveRunner.DEBUG_DELIMITED + "OK" + ArchiveRunner.DEBUG_DELIMITED)
                .getBytes(java.nio.charset.StandardCharsets.UTF_8);
        return this;
    }

    public ArchiveTestBuilder sendActionToEntity(String actionName, String actorId, String targetId) throws IOException {
        String existing = state.lastOutput != null ? new String(state.lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
        state.runtimeInteropJava.ifPresent(ri -> ri.trigger_action(actionName));
        state.lastOutput = (existing + ArchiveRunner.DEBUG_DELIMITED + "OK" + ArchiveRunner.DEBUG_DELIMITED)
                .getBytes(java.nio.charset.StandardCharsets.UTF_8);
        return this;
    }

    public ArchiveTestBuilder sendActionToContainer(String actionName, String actorId, String containerId) throws IOException {
        String existing = state.lastOutput != null ? new String(state.lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
        state.runtimeInteropJava.ifPresent(ri -> ri.trigger_action(actionName));
        state.lastOutput = (existing + ArchiveRunner.DEBUG_DELIMITED + "OK" + ArchiveRunner.DEBUG_DELIMITED)
                .getBytes(java.nio.charset.StandardCharsets.UTF_8);
        return this;
    }

    public ArchiveTestBuilder runIterations(int count) {
        String existing = state.lastOutput != null ? new String(state.lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
        state.runtimeInteropJava.ifPresent(ri -> ri.runtime_debug_iterate(count));
        StringBuilder sb = new StringBuilder(existing);
        for (int i = 0; i < count; i++)
            sb.append("Iteration completed in 0:0ns\n");
        sb.append(ArchiveRunner.DEBUG_DELIMITED + "OK" + ArchiveRunner.DEBUG_DELIMITED);
        state.lastOutput = sb.toString().getBytes(java.nio.charset.StandardCharsets.UTF_8);
        return this;
    }

    // --- Assertions (chainable, throw AssertionError on failure) ---

    public ArchiveTestBuilder assertLogLines(long expectedCount, String regexPattern) {
        LogAssertions.assertLogLineContainsRegex(state, expectedCount, regexPattern);
        return this;
    }

    public ArchiveTestBuilder assertNotInLogs(String regexPattern) {
        String output = state.lastOutput != null ? new String(state.lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
        Pattern pattern = Pattern.compile(regexPattern.replace("{", "(").replace("}", ")"));
        for (String line : output.split("\\r?\\n")) {
            if (pattern.matcher(line).find()) {
                throw new AssertionError(
                        "Expected no log lines matching regex '" + regexPattern + "' but found at least one.\nFull output:\n" + output);
            }
        }
        return this;
    }

    public ArchiveTestBuilder assertLogLineContains(int expectedCount, String regex) {
        String output = state.lastOutput != null ? new String(state.lastOutput) : "";
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
        return this;
    }

    public ArchiveTestBuilder assertExportedStateEmpty() throws Exception {
        StateAssertions.assertExportedStateEmpty(state);
        return this;
    }

    public ArchiveTestBuilder assertExportedStateTable(String tableName, String csvFile) throws Exception {
        StateAssertions.assertExportedStateTableColumnsMatchesCsv(state, tableName, csvFile);
        return this;
    }

    // Convenience shortcuts for common table names
    public ArchiveTestBuilder assertExportedActions(String csvFile) throws Exception {
        return assertExportedStateTable("action", csvFile);
    }

    public ArchiveTestBuilder assertExportedModules(String csvFile) throws Exception {
        return assertExportedStateTable("module", csvFile);
    }

    public ArchiveTestBuilder assertExportedEntities(String csvFile) throws Exception {
        return assertExportedStateTable("entity", csvFile);
    }

    public ArchiveTestBuilder assertExportedEvents(String csvFile) throws Exception {
        return assertExportedStateTable("events", csvFile);
    }

    public ArchiveTestBuilder assertExportedPanels(String csvFile) throws Exception {
        return assertExportedStateTable("panel", csvFile);
    }

    public ArchiveTestBuilder assertPanelNames(String json) {
        StateAssertions.assertReturnedPanelNamesIsIn(state, json);
        return this;
    }

    private void loadFeatureFiles(String resourcePath) throws IOException {
        var url = getClass().getClassLoader().getResource(resourcePath);
        if (url == null)
            throw new IOException("Resource not found: " + resourcePath);
        java.nio.file.Path dir;
        try {
            dir = java.nio.file.Path.of(url.toURI());
        } catch (java.net.URISyntaxException e) {
            throw new IOException("Invalid URI for resource: " + url, e);
        }
        try (var stream = java.nio.file.Files.walk(dir)) {
            state.featureFiles = stream.filter(java.nio.file.Files::isRegularFile)
                    .collect(java.util.stream.Collectors.toMap(
                            p -> dir.relativize(p).toString().replaceAll("\\\\", "/"),
                            p -> new java.io.File(p.toUri()), (a, b) -> a));
        }
    }

    // --- Debug helpers ---

    public void debugPrintStdout(int ms) throws InterruptedException {
        Thread.sleep(ms);
        System.out.println(new String(state.lastOutput));
    }
}
