package com.example.steps;

import java.io.File;
import java.io.IOException;
import java.net.URI;
import java.nio.file.*;
import java.util.Map;

public record ZipArchive(File file) {

    public ZipArchive append(File fileToAdd) throws IOException {
        URI zipUri = URI.create("jar:" + file.toURI());
        try (FileSystem zipFs = FileSystems.newFileSystem(zipUri, Map.of("create", "true"))) {
            Path dest = zipFs.getPath(fileToAdd.getName());
            Files.copy(fileToAdd.toPath(), dest, StandardCopyOption.REPLACE_EXISTING);
        }
        return this;
    }

    public static ZipArchive createTemp() {
        File temp = null;
        try {
            temp = File.createTempFile("archive_", ".zip");
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
        temp.delete();        // delete the stub — ZIP FS will create a fresh one
        temp.deleteOnExit();  // still clean up on JVM exit
        return new ZipArchive(temp);
    }
}