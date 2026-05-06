package com.example.tests.stage3.entityMutation;

import com.example.utils.archiveTestBuilder.ArchiveTestBuilder;
import com.example.utils.archiveTestBuilder.assertions.AssertExportedEntities.Entity;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.MethodSource;

import java.util.List;
import java.util.stream.Stream;

public class EntityMutationTest {

    record TestData(String directory, Entity before, Entity after) {
    }

    static Stream<TestData> data() {
        return Stream.of(new TestData(
                        "numberModule",
                        new Entity().withNumberMapValue("value", 1L),
                        new Entity().withNumberMapValue("value", 2L)
                )
        );
    }

    @ParameterizedTest
    @MethodSource("data")
    void moduleLoadingWithScript(TestData testData) throws Exception {
        var builder = ArchiveTestBuilder.create();

        builder.runApplication()
                .addFile("./" + testData.directory() + "/manifest.json", "./manifest.json")
                .addFile("./" + testData.directory() + "/index.js", "./index.js")
                .loadArchive()
                .assertExportedEntities(List.of(testData.before))
                .runIterations(1)
                .assertExportedEntities(List.of(testData.before))
                .triggerAction("action")
                .assertExportedEntities(List.of(testData.after));
        builder.cleanup();
    }


}
