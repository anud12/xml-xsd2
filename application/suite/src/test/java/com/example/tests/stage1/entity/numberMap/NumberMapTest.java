package com.example.tests.stage1.entity.numberMap;

import com.example.utils.archiveTestBuilder.ArchiveTestBuilder;
import com.example.utils.archiveTestBuilder.assertions.AssertExportedEntities;
import org.junit.jupiter.api.Test;

import java.util.List;

public class NumberMapTest {
    static String folderName = "./module";

    @Test
    void assertionShouldFail() throws Exception {
        var builder = ArchiveTestBuilder.create();

        try {
            builder.runApplication()
                    .addFile(folderName + "/manifest.json", "./manifest.json")
                    .addFile(folderName + "/index.js", "./index.js")
                    .loadArchive()
                    .assertExportedEntities(List.of(new AssertExportedEntities.Entity()
                            .withEmptyNumberMap()
                    ));
            builder.cleanup();
        } catch (AssertionError error) {
            builder.cleanup();
            return;
        }
        throw new AssertionError("assertion should've failed failed with numberMap value mismatch from expected empty to actual 1");
    }

    @Test
    void assertionShouldFailWithValue() throws Exception {
        var builder = ArchiveTestBuilder.create();

        try {
            builder.runApplication()
                    .addFile(folderName + "/manifest.json", "./manifest.json")
                    .addFile(folderName + "/index.js", "./index.js")
                    .loadArchive()
                    .assertExportedEntities(List.of(new AssertExportedEntities.Entity()
                            .withNumberMapValue("value", 2L)
                    ));
            builder.cleanup();
        } catch (AssertionError error) {
            builder.cleanup();
            return;
        }
        throw new AssertionError("assertion should've failed failed with numberMap value mismatch from expected 2 to actual 1");
    }

    @Test
    void assertionShouldSucceed() throws Exception {
        var builder = ArchiveTestBuilder.create();
        builder.runApplication()
                .addFile(folderName + "/manifest.json", "./manifest.json")
                .addFile(folderName + "/index.js", "./index.js")
                .loadArchive()
                .assertExportedEntities(List.of(new AssertExportedEntities.Entity()
                        .withNumberMapValue("value", 1L)
                ));
        builder.cleanup();
    }

}
