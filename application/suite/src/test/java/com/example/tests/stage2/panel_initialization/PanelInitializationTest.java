package com.example.tests.stage2.panel_initialization;

import com.example.utils.archiveTestBuilder.ArchiveTestBuilder;
import com.example.utils.archiveTestBuilder.assertions.AssertExportedPanels.Panel;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.MethodSource;

import java.util.List;
import java.util.stream.Stream;

public class PanelInitializationTest {

    static Stream<String> panelDirectories() {
        return Stream.of(
                "offset"
        );
    }

    @ParameterizedTest
    @MethodSource("panelDirectories")
    void panelInitialization(String directory) throws Exception {
        var builder = ArchiveTestBuilder.create();

        List<Panel> expectedPanels = List.of(
                new Panel().setId("panel"),
                new Panel().setId("panel_2")
        );

        builder.runApplication()
                .addFile("./" + directory + "/manifest.json", "./manifest.json")
                .addFile("./" + directory + "/index.js", "./index.js")
                .loadArchive()
                .assertExportedPanels(expectedPanels);
        builder.cleanup();
    }


}
