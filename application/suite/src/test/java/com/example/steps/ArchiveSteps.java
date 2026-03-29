package com.example.steps;

import io.cucumber.java.*;
import io.cucumber.java.en.And;
import io.cucumber.java.en.Given;
import io.cucumber.java.en.When;
import io.cucumber.java.en.Then;

import java.io.*;
import java.util.regex.Pattern;
import java.nio.charset.StandardCharsets;
import java.util.concurrent.TimeUnit;

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

    @After()
    public void afterScenario() {
        ArchiveRunner.cleanup(state);
    }

    @Then("assert output table {string} must be {string} csv")
    public void outputTableMustBeCsv(String tableName, String csvFile) throws Exception {
        ArchiveAssertions.assertOutputTableCsv(state, tableName, csvFile);
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
        try {
            Process p = state.runProcess;
            if (p == null) {
                throw new IllegalStateException("state.runProcess is null");
            }
            OutputStream os = p.getOutputStream();
            if (os == null) {
                throw new IllegalStateException("Process output stream is null");
            }
            String cmd = "DEBUG: shutdown" + System.lineSeparator();
            os.write(cmd.getBytes(StandardCharsets.UTF_8));
            os.flush();
            // Close stdin to signal EOF to the child process
            try {
                os.close();
            } catch (IOException ignored) {
            }

            // Wait for the process to exit (timeout after 60 seconds)
            boolean exited;
            try {
                exited = p.waitFor(60, TimeUnit.SECONDS);
            } catch (InterruptedException ie) {
                Thread.currentThread().interrupt();
                throw new RuntimeException("Interrupted while waiting for process to exit", ie);
            }
            if (!exited) {
                throw new AssertionError("Process did not exit within 60 seconds after shutdown signal");
            }
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }

    @Then("assert that application is closed")
    public void assertThatApplicationIsClosed() {
        // Write code here that turns the phrase above into concrete actions
        throw new PendingException();
    }
}
