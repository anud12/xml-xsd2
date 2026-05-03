package com.example.stage1.module;

import com.example.utils.JunitTestHelper;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.MethodSource;

import java.util.regex.Pattern;
import java.util.stream.Stream;

import static org.assertj.core.api.Assertions.assertThat;

public class ModuleTest {

    private JunitTestHelper helper;

    @BeforeEach
    void setUp() throws Exception {
        helper = new JunitTestHelper();
        helper.setup();
    }

    @AfterEach
    void tearDown() {
        if (helper != null) helper.teardown();
    }

    @ParameterizedTest
    @MethodSource("moduleLoadData")
    void moduleLoadingWithScript(Example example) throws Exception {
        helper.runApplication();
        helper.addFileToArchive("./" + example.directory() + "/manifest.json", "./manifest.json");
        helper.addFileToArchive("./" + example.directory() + "/index.js", "./index.js");
        helper.loadArchive();

        var state = helper.getState();
        Pattern pattern = Pattern.compile(example.log());
        long actualMatches = state.logMessages.stream()
                .filter(line -> pattern.matcher(line).find())
                .count();

        assertThat(actualMatches)
                .as("Log line count for directory %s matching '%s'", example.directory(), example.log())
                .isEqualTo(example.expectedCount());
    }

    @ParameterizedTest
    @MethodSource("missingEntrypointData")
    void missingEntrypoint(Example example) throws Exception {
        helper.runApplication();
        helper.addFileToArchive("./" + example.directory() + "/manifest.json", "./manifest.json");
        helper.loadArchive();

        var state = helper.getState();
        Pattern pattern = Pattern.compile(example.log());
        long actualMatches = state.logMessages.stream()
                .filter(line -> pattern.matcher(line).find())
                .count();

        assertThat(actualMatches)
                .as("Log line count for directory %s matching '%s'", example.directory(), example.log())
                .isEqualTo(1L);
    }

    record Example(String directory, String log, long expectedCount) {}

    static Stream<Example> moduleLoadData() {
        return Stream.of(
                new Example("module/first", "First module loaded", 1L),
                new Example("module/second", "Second module loaded", 1L),
                new Example("module/if-guard", "if guard loaded", 0L)
        );
    }

    static Stream<Example> missingEntrypointData() {
        return Stream.of(
                                new Example("module/missing_entrypoint", "Error: entrypoint.*not found in archive", 1L)
        );
    }
}
