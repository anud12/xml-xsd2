package com.example.stage1.action;

import com.example.utils.ArchiveTestBuilder;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.MethodSource;

import java.util.stream.Stream;

public class ActionRegistrationTest {

    private ArchiveTestBuilder builder;

    @AfterEach
    void tearDown() {
        if (builder != null) builder.cleanup();
    }

    @ParameterizedTest
    @MethodSource("actionDirectories")
    void registerAction(String directory) throws Exception {
        builder = ArchiveTestBuilder.create("stage1/action");

        String csvFile = "./" + directory + "/action.csv";

        builder.runApplication()
                .addFile("./" + directory + "/manifest.json", "./manifest.json")
                .addFile("./" + directory + "/index.js", "./index.js")
                .loadArchive()
                .assertExportedActions(csvFile);
    }

    static Stream<String> actionDirectories() {
        return Stream.of(
                "first",
                "second"
        );
    }
}
