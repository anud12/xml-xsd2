package com.example.steps;

import io.cucumber.java.Scenario;
import java.io.File;
import java.io.IOException;
import java.net.URI;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Objects;
import java.util.stream.Collectors;

public class ArchiveSetup {
    public static void before(ArchiveState state, Scenario scenario) throws IOException {
        URI uri = scenario.getUri();
        Path dir;
        if ("classpath".equals(uri.getScheme())) {
            String resourcePath = uri.getSchemeSpecificPart();
            if (resourcePath.startsWith("/")) resourcePath = resourcePath.substring(1);
            var url = ArchiveSetup.class.getClassLoader().getResource(resourcePath);
            if (url == null) throw new IOException("Resource not found: " + resourcePath);
            try {
                dir = Path.of(url.toURI()).getParent();
            } catch (java.net.URISyntaxException e) {
                throw new IOException("Invalid URI for resource: " + url, e);
            }
        } else {
            dir = Path.of(uri).getParent();
        }
        try (var stream = Files.walk(dir)) {
            state.featureFiles = stream
                    .filter(Files::isRegularFile)
                    .collect(Collectors.toMap(path -> path.getFileName().toString(), path -> new File(path.toUri()), (a, b) -> a));
        }
    }

    public static void addFileToArchive(ArchiveState state, String fileName) throws IOException {
        if(!fileName.startsWith("./")) {
            throw new RuntimeException("Non local path found");
        }
        fileName = fileName.replaceFirst("./", "");
        var file = Objects.requireNonNull(state.featureFiles.get(fileName), "File '" + fileName + "' not found in " + state.featureFiles.keySet());
        state.archive.append(file);
    }
}
