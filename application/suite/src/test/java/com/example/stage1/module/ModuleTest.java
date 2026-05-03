package com.example.stage1.module;

import com.example.utils.ArchiveTestBuilder;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.MethodSource;

import java.util.stream.Stream;

public class ModuleTest {

    private ArchiveTestBuilder builder;

    @AfterEach
    void tearDown() {
        if (builder != null) builder.cleanup();
    }

    @ParameterizedTest
    @MethodSource("moduleLoadData")
    void moduleLoadingWithScript(Example example) throws Exception {
        builder = ArchiveTestBuilder.create();

        builder.runApplication()
                .addFile("./" + example.directory() + "/manifest.json", "./manifest.json")
                .addFile("./" + example.directory() + "/index.js", "./index.js")
                .loadArchive()
                .assertLogLines(example.expectedCount(), example.log());
    }

    @ParameterizedTest
    @MethodSource("missingEntrypointData")
    void missingEntrypoint(Example example) throws Exception {
        builder = ArchiveTestBuilder.create();

        builder.runApplication()
                .addFile("./" + example.directory() + "/manifest.json", "./manifest.json")
                .loadArchive()
                .assertLogLines(1L, example.log());
    }

    record Example(String directory, String log, long expectedCount) {
    }

    static Stream<Example> moduleLoadData() {
        return Stream.of(
                new Example("first", "First module loaded", 1L),
                new Example("second", "Second module loaded", 1L),
                new Example("if-guard", "if guard loaded", 0L)
        );
    }

    static Stream<Example> missingEntrypointData() {
        return Stream.of(
                new Example("missing_entrypoint", "Error: entrypoint.*not found in archive", 1L)
        );
    }
}
