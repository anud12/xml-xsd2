package com.example.utils;

import java.io.File;
import java.io.IOException;
import java.net.URI;
import java.nio.file.*;
import java.util.Map;

public record ZipArchive(FileSystem zipFs, Path zipPath) {

    public ZipArchive append(File fileToAdd, Path destination) throws IOException {

        // Convert the Path destination to a path inside the ZipFileSystem
        Path pathInZip = zipFs.getPath(destination.toString());
        // Copy from the local file to the internal ZIP path
        Files.copy(fileToAdd.toPath(), pathInZip, StandardCopyOption.REPLACE_EXISTING);
        return this;
    }

    public byte[] byteContents() {
        try {
            zipFs.close();
            return Files.readAllBytes(zipPath);
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }

    public static ZipArchive createTemp() {
        File temp = null;
        try {
            temp = File.createTempFile("archive_", ".zip");
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
        temp.delete();
        temp.deleteOnExit();  // still clean up on JVM exit
        URI zipUri = URI.create("jar:" + temp.toURI());
        try {
            FileSystem zipFs = FileSystems.newFileSystem(zipUri, Map.of("create", "true"));
            return new ZipArchive(zipFs, temp.toPath());
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }
}