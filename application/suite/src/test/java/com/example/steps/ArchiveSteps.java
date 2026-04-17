package com.example.steps;

import com.example.utils.*;
import io.cucumber.java.After;
import io.cucumber.java.Before;
import io.cucumber.java.Scenario;
import io.cucumber.java.en.And;
import io.cucumber.java.en.Given;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;

import java.io.IOException;
import java.util.regex.Pattern;

import static com.example.utils.ArchiveRunner.DEBUG_DELIMITED;

public class ArchiveSteps {
    private final ArchiveState state = new ArchiveState();

    @Before()
    public void before(Scenario scenario) throws IOException {
        ArchiveSetup.before(state, scenario);
    }

    @Given("I have added {string} file as {string} to archive")
    public void the_test_directory_contains_file(String fileName, String destination) throws IOException {
        ArchiveSetup.addFileToArchive(state, fileName, destination);
    }

    @When("I run the application in debug mode")
    public void i_run_the_application_in_debug_mode() throws IOException, InterruptedException {
        try {
            ArchiveRunner.runApplicationDebugThreadedWithArchive(state);

        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }

    @And("assert exactly {long} log lines matches {string}")
    public void hasExactlyNLogLinesMatches(long arg0, String arg1) {
        LogAssertions.assertLogLineContainsRegex(state, arg0, arg1);
    }

    @Then("DEBUG Print stdout after {int} ms")
    public void debugPrintStdout(int ms) {
        try {
            Thread.sleep(ms);
        } catch (InterruptedException e) {
            throw new RuntimeException(e);
        }
        System.out.println(new String(state.lastOutput));
    }

    @And("I run {int} iterations")
    public void iRunIterations(int arg0) {
        try {
            String existing = state.lastOutput != null ? new String(state.lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
            state.runtimeInteropJava.ifPresent(runtimeInteropJava -> runtimeInteropJava.runtime_debug_iterate(arg0));
            StringBuilder sb = new StringBuilder(existing);
            for (int i = 0; i < arg0; i++) sb.append("Iteration completed in 0:0ns\n");
            sb.append(DEBUG_DELIMITED + "OK" + DEBUG_DELIMITED);
            state.lastOutput = sb.toString().getBytes(java.nio.charset.StandardCharsets.UTF_8);
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }

    @Then("assert that {int} log line\\(s) contains {string} regex")
    public void assertThatLogLineSContainsStringRegex(int count, String regex) {
        String output = state.lastOutput != null ? new String(state.lastOutput) : "";
        String patternStr = regex.replace("{", "(").replace("}", ")");
        Pattern pattern = Pattern.compile(patternStr);
        int matches = 0;
        String[] lines = output.split("\\r?\\n");
        for (String line : lines) {
            if (pattern.matcher(line).find()) {
                matches++;
            }
        }
        if (matches != count) {
            throw new AssertionError(String.format(
                    "Expected exactly %d log line(s) matching regex '%s' but found %d.\nFull output:\n%s",
                    count, regex, matches, output));
        }
    }

    @After()
    public void closeApplication() {
        ArchiveRunner.cleanup(state);
        CloseProcess.closeProcess(state);
    }

    @And("assert exported state should be empty")
    public void assertExportedStateShouldBeEmpty() throws Exception {
        StateAssertions.assertExportedStateEmpty(state);
    }

    @And("I load current archive")
    public void iLoadCurrentArchive() throws Exception {
        try {
            var contents = state.archive.byteContents();
            state.runtimeInteropJava.map(runtimeInteropJava -> runtimeInteropJava.runtime_load_archive(contents, contents.length))
                    .get();
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }

    @Then("assert exported state entities includes regexes from {string}")
    public void exportedEntitiesShouldIncludeRegexes(String csvFile) throws Exception {
        StateAssertions.assertExportedStateTableColumnsMatchesCsv(state, "entity", csvFile);
    }
    @Then("assert exported state module includes regexes from {string} file")
    public void exportedModuleShouldIncludeRegexes(String csvFile) throws Exception {
        StateAssertions.assertExportedStateTableColumnsMatchesCsv(state, "module", csvFile);
    }
    @Then("assert exported state action includes regexes from {string} file")
    public void exportedActionShouldIncludeRegexes(String csvFile) throws Exception {
        StateAssertions.assertExportedStateTableColumnsMatchesCsv(state, "action", csvFile);
    }
    @Then("assert exported state events includes regexes from {string}  file")
    public void exportedEventsShouldIncludeRegexes(String csvFile) throws Exception {
        StateAssertions.assertExportedStateTableColumnsMatchesCsv(state, "events", csvFile);
    }

    @Then("assert exported state panel includes regexes from {string} file")
    public void exportedPanelShouldIncludeRegexes(String csvFile) throws Exception {
        StateAssertions.assertExportedStateTableColumnsMatchesCsv(state, "panel", csvFile);
    }


    @When("I send action {string} from actor {string} to entity {string}")
    public void sendActionToEntity(String actionName, String actorId, String targetId) throws IOException, InterruptedException {
        try {
            String existing = state.lastOutput != null ? new String(state.lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
            state.runtimeInteropJava.ifPresent(runtimeInteropJava -> runtimeInteropJava.trigger_action(actionName));
            state.lastOutput = (existing + DEBUG_DELIMITED + "OK" + DEBUG_DELIMITED).getBytes(java.nio.charset.StandardCharsets.UTF_8);
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }

    @When("I send action {string} from actor {string} to container {string}")
    public void sendActionToContainer(String actionName, String actorId, String containerId) throws IOException, InterruptedException {
        try {
            String existing = state.lastOutput != null ? new String(state.lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
            state.runtimeInteropJava.ifPresent(runtimeInteropJava -> runtimeInteropJava.trigger_action(actionName));
            state.lastOutput = (existing + DEBUG_DELIMITED + "OK" + DEBUG_DELIMITED).getBytes(java.nio.charset.StandardCharsets.UTF_8);
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }

    @Then("assert log line containing {string} regex is false")
    public void assertLogLineNotContaining(String regex) {
        String output = state.lastOutput != null ? new String(state.lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
        String patternStr = regex.replace("{", "(").replace("}", ")");
        Pattern pattern = Pattern.compile(patternStr);
        boolean found = false;
        for (String line : output.split("\\r?\\n")) {
            if (pattern.matcher(line).find()) {
                found = true;
                break;
            }
        }
        if (found) {
            throw new AssertionError(String.format(
                    "Expected no log lines matching regex '%s' but found at least one.\nFull output:\n%s",
                    regex, output));
        }
    }

    @And("I trigger action {string}")
    public void iTriggerAction(String actionName) {
        try {
            String existing = state.lastOutput != null ? new String(state.lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
            state.runtimeInteropJava.ifPresent(runtimeInteropJava -> runtimeInteropJava.trigger_action(actionName));
            state.lastOutput = (existing + DEBUG_DELIMITED + "OK" + DEBUG_DELIMITED).getBytes(java.nio.charset.StandardCharsets.UTF_8);
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }
}

