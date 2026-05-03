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

    void loadFeatureFilesFromCaller() throws IOException {
        // Walk stack to find the calling test class (first frame outside this impl)
        var callerClass = findCallerClass();
        if (callerClass == null)
            throw new IOException("Could not detect calling test class");

        // Find directory containing compiled .class via ProtectionDomain
        java.security.ProtectionDomain pd = callerClass.getProtectionDomain();
        Object csLoc = pd.getCodeSourceLocation();
        java.nio.file.Path baseDir;
        if (csLoc != null) {
            try {
                var url = new java.net.URL("file:" + csLoc);
                baseDir = java.nio.file.Path.of(url.toURI());
            } catch (Exception e) {
                throw new IOException("Invalid code source location: " + csLoc, e);
            }
        } else {
            // Fallback: derive from caller class's package name
            var pkgName = callerClass.getPackage().getName();
            var relPath = pkgName.replace('.', java.io.File.separatorChar);
            baseDir = java.nio.file.Paths.get("target/test-classes").resolve(relPath);
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
