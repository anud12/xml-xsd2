package com.example.stage2.panel_initialization;

import com.example.utils.ArchiveTestBuilder;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.MethodSource;

import java.util.stream.Stream;

public class PanelInitializationTest {

    private ArchiveTestBuilder builder;

    @AfterEach
    void tearDown() {
        if (builder != null) builder.cleanup();
    }

    @ParameterizedTest
    @MethodSource("panelDirectories")
    void panelInitialization(String directory) throws Exception {
        builder = ArchiveTestBuilder.create("stage2/panel_initialization");

        String csvFile = "./" + directory + "/panel.csv";

        builder.runApplication()
                .addFile("./" + directory + "/manifest.json", "./manifest.json")
                .addFile("./" + directory + "/index.js", "./index.js")
                .loadArchive()
                .assertExportedPanels(csvFile);
    }

    static Stream<String> panelDirectories() {
        return Stream.of(
                "offset"
        );
    }
}
