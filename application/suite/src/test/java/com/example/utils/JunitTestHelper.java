package com.example.utils;

import java.io.File;
import java.io.IOException;
import java.net.URI;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Map;
import java.util.stream.Collectors;

public class JunitTestHelper {
    private final ArchiveState state;

    public JunitTestHelper() {
        this.state = new ArchiveState();
    }

    public ArchiveState getState() {
        return state;
    }

    public void setup() throws IOException {
        state.logMessages.clear();
        state.archive = ZipArchive.createTemp();
        loadAllFeatureFiles("features/stage1");
    }

    public void runApplication() throws Exception {
        ArchiveRunner.runApplicationDebugThreadedWithArchive(state);
    }

    public void teardown() {
        try {
            ArchiveRunner.cleanup(state);
            CloseProcess.closeProcess(state);
        } catch (Throwable ignored) {
        }
    }

    public void addFileToArchive(String fileName, String destination) throws IOException {
        if (!fileName.startsWith("./")) throw new RuntimeException("Non local path found");
        String name = fileName.replaceFirst("./", "");
        var file = state.featureFiles.get(name);
        if (file == null || !file.exists()) {
            throw new RuntimeException("File '" + name + "' not found in feature files: " + state.featureFiles.keySet());
        }
        state.archive.append(file, java.nio.file.Path.of(destination));
    }

    public void loadArchive() throws Exception {
        var contents = state.archive.byteContents();
        state.runtimeInteropJava.map(runtimeInteropJava -> runtimeInteropJava.runtime_load_archive(contents, contents.length))
                .get();
    }

    private void loadAllFeatureFiles(String resourcePath) throws IOException {
        var url = getClass().getClassLoader().getResource(resourcePath);
        if (url == null) throw new IOException("Resource not found: " + resourcePath);
        Path dir;
        try {
            dir = Path.of(url.toURI());
        } catch (java.net.URISyntaxException e) {
            throw new IOException("Invalid URI for resource: " + url, e);
        }
        try (var stream = Files.walk(dir)) {
            state.featureFiles = stream
                    .filter(Files::isRegularFile)
                    .collect(Collectors.toMap(path -> dir.relativize(path).toString().replaceAll("\\\\", "/"), path -> new File(path.toUri()), (a, b) -> a));
        }
    }

    public void triggerAction(String actionName) {
        state.runtimeInteropJava.ifPresent(runtimeInteropJava -> runtimeInteropJava.trigger_action(actionName));
    }
}
