package com.example.stage1.call_action;

import com.example.utils.archiveTestBuilder.ArchiveTestBuilder;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.MethodSource;

import java.util.stream.Stream;

public class CallActionTest {

    static Stream<Example> callActionData() {
        return Stream.of(
                new Example("first", "action", "action called"),
                new Example("second", "second action", "second action called")
        );
    }

    @ParameterizedTest
    @MethodSource("callActionData")
    void callRegisteredAction(Example example) throws Exception {
        var builder = ArchiveTestBuilder.create();

        builder.runApplication()
                .addFile("./" + example.directory() + "/manifest.json", "./manifest.json")
                .addFile("./" + example.directory() + "/index.js", "./index.js")
                .loadArchive()
                .triggerAction(example.action())
                .assertLogLines(1L, example.log());
        builder.cleanup();
    }

    record Example(String directory, String action, String log) {
    }


}
