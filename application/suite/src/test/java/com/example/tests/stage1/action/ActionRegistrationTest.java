package com.example.tests.stage1.action;

import com.example.utils.archiveTestBuilder.ArchiveTestBuilder;
import com.example.utils.archiveTestBuilder.assertions.AssertExportedActions.Action;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.MethodSource;

import java.util.List;
import java.util.stream.Stream;

public class ActionRegistrationTest {

    record TestCase(String directory, List<Action> expectedActions) {
    }

    static Stream<TestCase> actionTests() {
        return Stream.of(
                new TestCase("first", List.of(
                        new Action().setName("action"),
                        new Action().setName("second action")
                )),
                new TestCase("second", List.of(
                        new Action().setName("second action"),
                        new Action().setName("second second action")
                ))
        );
    }

    @ParameterizedTest
    @MethodSource("actionTests")
    void registerAction(TestCase tc) throws Exception {
        var builder = ArchiveTestBuilder.create();

        builder.runApplication()
                .addFile("./" + tc.directory() + "/manifest.json", "./manifest.json")
                .addFile("./" + tc.directory() + "/index.js", "./index.js")
                .loadArchive()
                .assertExportedActions(tc.expectedActions());
        builder.cleanup();
    }


}
