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



    @When("I run the application in debug mode using archive")
    public void i_run_the_application_in_debug_mode_using_archive() throws IOException, InterruptedException {
        ArchiveRunner.runApplicationDebugThreadedWithArchive(state);
    }

    @When("I run the application in debug mode")
    public void i_run_the_application_in_debug_mode() throws IOException, InterruptedException {
        ArchiveRunner.runApplicationDebugThreadedWithArchive(state);
    }

    @Then("assert output table {string} must be {string} csv")
    public void outputTableMustBeCsv(String tableName, String csvFile) throws Exception {
        StateAssertions.assertOutputTableCsv(state, tableName, csvFile);
    }

    @And("assert log line containing {string} regex")
    public void hasLogLineContaining(String arg0) {
        ArchiveAssertions.assertLogLineContainsRegex(state, arg0);
    }

    @And("wait until log line contains {string} regex")
    public void waitUntilLogLineContainsRegex(String regex) throws InterruptedException {
        ArchiveAssertions.waitUntilLogLineContainsRegex(state, regex);
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
    public void iLoadCurrentArchive() {
        // Write code here that turns the phrase above into concrete actions
        throw new PendingException();
    }


    @Then("assert output table {string} columns matches {string}")
    public void assertOutputTableColumnsMatches(String tableName, String csvFile) throws Exception {
        StateAssertions.assertOutputTableColumnsMatchesCsv(state, tableName, csvFile);
    }
}
