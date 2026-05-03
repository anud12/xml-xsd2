package com.example.stage1.call_action;

import com.example.utils.ArchiveTestBuilder;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.MethodSource;

import java.util.stream.Stream;

public class CallActionTest {

    private ArchiveTestBuilder builder;

    @AfterEach
    void tearDown() {
        if (builder != null) builder.cleanup();
    }

    @ParameterizedTest
    @MethodSource("callActionData")
    void callRegisteredAction(Example example) throws Exception {
        builder = ArchiveTestBuilder.create();

        builder.runApplication()
                .addFile("./" + example.directory() + "/manifest.json", "./manifest.json")
                .addFile("./" + example.directory() + "/index.js", "./index.js")
                .loadArchive()
                .triggerAction(example.action())
                .assertLogLines(1L, example.log());
    }

    record Example(String directory, String action, String log) {
    }

    static Stream<Example> callActionData() {
        return Stream.of(
                new Example("first", "action", "action called"),
                new Example("second", "second action", "second action called")
        );
    }
}
