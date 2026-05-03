package com.example.utils;

import java.io.IOException;

public interface ArchiveTestBuilderSetup extends ArchiveTestBuilderCommon {

    default ArchiveTestBuilder runApplication() throws Exception {
        this.getState().logMessages.clear();
        if (this.getState().archive == null)
            this.getState().archive = ZipArchive.createTemp();
        ArchiveRunner.runApplicationDebugThreadedWithArchive(this.getState());
        return (ArchiveTestBuilder) this;
    }

    default ArchiveTestBuilder addFile(String fileName, String destination) throws IOException {
        if (!fileName.startsWith("./"))
            throw new IllegalArgumentException("Non-local path: " + fileName);
        var file = this.getState().featureFiles.get(fileName.replaceFirst("./", ""));
        if (file == null || !file.exists())
            throw new RuntimeException("File '" + fileName + "' not found in feature files");
        this.getState().archive.append(file, java.nio.file.Path.of(destination));
        return (ArchiveTestBuilder) this;
    }

    default ArchiveTestBuilder loadArchive() throws Exception {
        var contents = this.getState().archive.byteContents();
        this.getState().runtimeInteropJava.map(ri -> ri.runtime_load_archive(contents, contents.length)).get();
        return (ArchiveTestBuilder) this;
    }
}
