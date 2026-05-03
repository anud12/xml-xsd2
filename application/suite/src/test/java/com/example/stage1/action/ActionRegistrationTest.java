package com.example.stage1.action;

import com.example.utils.JunitTestHelper;
import com.example.utils.StateAssertions;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.MethodSource;

import java.util.stream.Stream;

import static org.assertj.core.api.Assertions.assertThatCode;

public class ActionRegistrationTest {

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

    @ParameterizedTest(name = "[{index}] directory={0}")
    @MethodSource("actionDirectories")
    void registerAction(String directory) throws Exception {
        helper.runApplication();
        helper.addFileToArchive("./" + directory + "/manifest.json", "./manifest.json");
        helper.addFileToArchive("./" + directory + "/index.js", "./index.js");
        helper.loadArchive();

        String csvFile = "./" + directory + "/action.csv";

        assertThatCode(() -> StateAssertions.assertExportedStateTableColumnsMatchesCsv(helper.getState(), "action", csvFile))
                .as("Exported state action should match CSV patterns from %s", csvFile)
                .doesNotThrowAnyException();
    }

    static Stream<String> actionDirectories() {
        return Stream.of(
                "action/first",
                "action/second"
        );
    }
}
