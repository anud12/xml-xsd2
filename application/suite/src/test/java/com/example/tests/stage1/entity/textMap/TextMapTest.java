package com.example.tests.stage1.entity.textMap;

import com.example.utils.archiveTestBuilder.ArchiveTestBuilder;
import com.example.utils.archiveTestBuilder.assertions.AssertExportedEntities;
import org.junit.jupiter.api.Test;

import java.util.List;

public class TextMapTest {
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
                            .withEmptyTextMap()
                    ));
            builder.cleanup();
        } catch (AssertionError error) {
            builder.cleanup();
            return;
        }
        throw new AssertionError("assertion should've failed failed with textMap value mismatch from expected empty to actual 1");
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
                            .withTextMapValue("value", "2")
                    ));
            builder.cleanup();
        } catch (AssertionError error) {
            builder.cleanup();
            return;
        }
        throw new AssertionError("assertion should've failed with textMap value mismatch from expected 2 to actual 1");
    }
    @Test
    void assertionShouldSucceed() throws Exception {
        var builder = ArchiveTestBuilder.create();
        builder.runApplication()
                .addFile(folderName + "/manifest.json", "./manifest.json")
                .addFile(folderName + "/index.js", "./index.js")
                .loadArchive()
                .assertExportedEntities(List.of(new AssertExportedEntities.Entity()
                        .withTextMapValue("value", "1")
                ));
        builder.cleanup();
    }

}
