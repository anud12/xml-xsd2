package com.example.stage1.call_action;

import com.example.utils.JunitTestHelper;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.MethodSource;

import java.util.regex.Pattern;
import java.util.stream.Stream;

import static org.assertj.core.api.Assertions.assertThat;

public class CallActionTest {

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

    @ParameterizedTest
    @MethodSource("callActionData")
    void callRegisteredAction(Example example) throws Exception {
        helper.runApplication();
        helper.addFileToArchive("./" + example.directory() + "/manifest.json", "./manifest.json");
        helper.addFileToArchive("./" + example.directory() + "/index.js", "./index.js");
        helper.loadArchive();

        helper.triggerAction(example.action());

        var state = helper.getState();
        Pattern pattern = Pattern.compile(example.log());
        long actualMatches = state.logMessages.stream()
                .filter(line -> pattern.matcher(line).find())
                .count();

        assertThat(actualMatches)
                .as("Log line count after triggering action '%s' in directory %s", example.action(), example.directory())
                .isEqualTo(1L);
    }

    record Example(String directory, String action, String log) {}

    static Stream<Example> callActionData() {
        return Stream.of(
                new Example("call_action/first", "action", "action called"),
                new Example("call_action/second", "second action", "second action called")
        );
    }
}
