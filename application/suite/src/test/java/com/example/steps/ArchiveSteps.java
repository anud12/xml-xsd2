package com.example.steps;

import io.cucumber.java.After;
import io.cucumber.java.Before;
import io.cucumber.java.PendingException;
import io.cucumber.java.Scenario;
import io.cucumber.java.en.And;
import io.cucumber.java.en.Given;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;

import java.io.IOException;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.util.regex.Pattern;

import static com.example.steps.ArchiveRunner.DEBUG_DELIMITED;

public class ArchiveSteps {
    private final ArchiveState state = new ArchiveState();

    @Before()
    public void before(Scenario scenario) throws IOException {
        ArchiveSetup.before(state, scenario);
    }

    @Given("I have added {string} file to archive")
    public void the_test_directory_contains_file(String fileName) throws IOException {
        ArchiveSetup.addFileToArchive(state, fileName);
    }

    @When("I run the application in debug mode")
    public void i_run_the_application_in_debug_mode() throws IOException, InterruptedException {
        ArchiveRunner.runApplicationDebugThreadedWithArchive(state);
    }


    @And("assert log line containing {string} regex")
    public void hasLogLineContaining(String arg0) {
        ArchiveAssertions.assertLogLineContainsRegex(state, arg0);
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
        String cmd = "DEBUG: ITERATE " + arg0 + System.lineSeparator();
        try {
            // Use state.runProcess stdio directly
            Process p = state.runProcess;
            if (p == null) {
                throw new IllegalStateException("state.runProcess is null");
            }
            OutputStream os = p.getOutputStream();
            if (os == null) {
                throw new IllegalStateException("Process output stream is null");
            }
            os.write(cmd.getBytes(StandardCharsets.UTF_8));
            os.flush();

            // Wait until the captured output contains the acknowledgement
            while (true) {
                String output = state.lastOutput != null ? new String(state.lastOutput, StandardCharsets.UTF_8) : "";
                if (output.contains(DEBUG_DELIMITED + "OK" + DEBUG_DELIMITED)) {
                    break;
                }
                Thread.sleep(10);
            }
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }

    @Then("assert that {int} log line\\(s) contains {string} regex")
    public void assertThatLogLineSContainsStringRegex(int count, String regex) {
        // Count how many log lines in the last captured output match the provided
        // regex and assert at least `count` such lines exist.
        String output = state.lastOutput != null ? new String(state.lastOutput) : "";
        String safeRegex = regex.replaceAll("(?<!\\\\)\\{", "\\\\{")
                .replaceAll("(?<!\\\\)\\}", "\\\\}");
        Pattern pattern = Pattern.compile(safeRegex);
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
    public void assertExportedStateShouldBeEmpty() {
        StateAssertions.assertEmptySqlFile(state);
    }

    @And("I load current archive")
    public void iLoadCurrentArchive() throws Exception {
        byte[] zipBytes = java.nio.file.Files.readAllBytes(state.archive.file().toPath());
        String encoded = java.util.Base64.getEncoder().encodeToString(zipBytes);
        String cmd = "DEBUG: Load:" + encoded + System.lineSeparator();

        Process p = state.runProcess;
        if (p == null) throw new IllegalStateException("state.runProcess is null");
        OutputStream os = p.getOutputStream();
        if (os == null) throw new IllegalStateException("Process output stream is null");
        os.write(cmd.getBytes(java.nio.charset.StandardCharsets.UTF_8));
        os.flush();

        long timeoutMillis = 10000;
        long start = System.currentTimeMillis();
        while (System.currentTimeMillis() - start < timeoutMillis) {
            String output = state.lastOutput != null ? new String(state.lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
            if (output.contains(DEBUG_DELIMITED + "OK" + DEBUG_DELIMITED)) break;
            Thread.sleep(10);
        }
    }


    @Then("assert exported state output table {string} includes regexes from {string}")
    public void exportedTableShouldIncludeRegexes(String tableName, String csvFile) throws Exception {
        StateAssertions.assertOutputTableColumnsMatchesCsv(state, tableName, csvFile);
    }

    @When("I send action {string} from actor {string} to entity {string}")
    public void sendActionToEntity(String actionName, String actorId, String targetId) throws IOException, InterruptedException {
        String cmd = String.format("DEBUG: ACTION %s %s entity %s%s", actionName, actorId, targetId, System.lineSeparator());
        writeDebugCommand(cmd);
    }

    @When("I send action {string} from actor {string} to container {string}")
    public void sendActionToContainer(String actionName, String actorId, String containerId) throws IOException, InterruptedException {
        String cmd = String.format("DEBUG: ACTION %s %s container %s%s", actionName, actorId, containerId, System.lineSeparator());
        writeDebugCommand(cmd);
    }

    @Then("assert log line containing {string} regex is false")
    public void assertLogLineNotContaining(String regex) {
        String output = state.lastOutput != null ? new String(state.lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
        String safeRegex = regex.replaceAll("(?<!\\\\)\\{", "\\\\{")
                .replaceAll("(?<!\\\\)\\}", "\\\\}");
        Pattern pattern = Pattern.compile(safeRegex);
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

    private void writeDebugCommand(String cmd) throws IOException, InterruptedException {
        Process p = state.runProcess;
        if (p == null) throw new IllegalStateException("state.runProcess is null");
        OutputStream os = p.getOutputStream();
        if (os == null) throw new IllegalStateException("Process output stream is null");
        os.write(cmd.getBytes(java.nio.charset.StandardCharsets.UTF_8));
        os.flush();

        // Wait for acknowledgement
        long timeoutMillis = 5000;
        long start = System.currentTimeMillis();
        while (System.currentTimeMillis() - start < timeoutMillis) {
            String output = state.lastOutput != null ? new String(state.lastOutput, java.nio.charset.StandardCharsets.UTF_8) : "";
            if (output.contains(DEBUG_DELIMITED + "OK" + DEBUG_DELIMITED)) break;
            Thread.sleep(10);
        }
    }

    @When("I export state to {string}")
    public void exportStateToFile(String fileName) throws IOException, InterruptedException {
        String cmd = String.format("DEBUG: EXPORT %s%s", fileName, System.lineSeparator());
        writeDebugCommand(cmd);
        Thread.sleep(500); // Give time for export to complete
    }

    @And("I send action {string} from actor {string}")
    public void iSendNoInputActionFromActor(String actionName, String actorId) throws IOException, InterruptedException {
        String cmd = String.format("DEBUG: ACTION %s %s%s", actionName, actorId, System.lineSeparator());
        writeDebugCommand(cmd);
    }
}
