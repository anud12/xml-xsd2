package com.example.utils;

import java.io.File;
import java.io.IOException;

public class ArchiveTestBuilderImpl implements ArchiveTestBuilder {

    private final ArchiveState state;

    public ArchiveTestBuilderImpl() {
        this.state = new ArchiveState();
    }

    @Override
    public ArchiveState getState() {
        return state;
    }

    void loadFeatureFiles(String resourcePath) throws IOException {
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

    void loadFeatureFilesFromCaller() throws IOException {
        // Walk stack to find the calling test class (first frame outside this impl)
        var callerClass = findCallerClass();
        if (callerClass == null)
            throw new IOException("Could not detect calling test class");

        // Get URL of caller's package directory (e.g., target/test-classes/com/example/stage1/module/)
        java.net.URL pkgUrl = callerClass.getResource("");
        if (pkgUrl == null || !"file".equals(pkgUrl.getProtocol()))
            throw new IOException("Cannot resolve resource path for class: " + callerClass.getName());

        java.nio.file.Path baseDir;
        try {
            baseDir = java.nio.file.Path.of(pkgUrl.toURI());
        } catch (java.net.URISyntaxException e) {
            throw new IOException("Invalid URI for package URL: " + pkgUrl, e);
        }

        try (var stream = java.nio.file.Files.walk(baseDir)) {
            state.featureFiles = stream.filter(java.nio.file.Files::isRegularFile)
                    .filter(p -> !p.toString().endsWith(".class"))
                    .collect(java.util.stream.Collectors.toMap(
                            p -> baseDir.relativize(p).toString().replaceAll("\\\\", "/"),
                            p -> new java.io.File(p.toUri()), (a, b) -> a));
        }
    }

    private Class<?> findCallerClass() {
        var stack = Thread.currentThread().getStackTrace();
        for (var frame : stack) {
            var className = frame.getClassName();
            if (!className.contains("ArchiveTestBuilder")
                    && !className.contains("java.lang.")
                    && !className.contains("org.junit.")) {
                try {
                    return Class.forName(className);
                } catch (ClassNotFoundException ignore) {}
            }
        }
        return null;
    }

    public void debugPrintStdout(int ms) throws InterruptedException {
        Thread.sleep(ms);
        System.out.println(new String(state.lastOutput));
    }
}
