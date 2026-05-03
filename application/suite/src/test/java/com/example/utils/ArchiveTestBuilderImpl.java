package com.example.utils;

import java.io.File;
import java.io.IOException;
import java.net.URISyntaxException;

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

    public void debugPrintStdout(int ms) throws InterruptedException {
        Thread.sleep(ms);
        System.out.println(new String(state.lastOutput));
    }
}
