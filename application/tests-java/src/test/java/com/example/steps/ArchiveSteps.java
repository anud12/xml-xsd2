package com.example.steps;

import io.cucumber.java.Before;
import io.cucumber.java.Scenario;
import io.cucumber.java.en.Given;
import io.cucumber.java.en.When;
import io.cucumber.java.en.Then;
import org.junit.jupiter.api.Assertions;

import java.io.*;
import java.net.URI;
import java.nio.file.*;
import java.util.*;
import java.util.stream.Collectors;
import java.util.zip.*;

public class ArchiveSteps {
    private Map<String, byte[]> files;
    private Map<String, File> featureFiles;
    private ZipArchive archive = ZipArchive.createTemp();
    private String lastOutput;

    @Before()
    public void before(Scenario scenario) throws IOException {
        var rootPath = scenario.getUri().getPath().replace(Path.of(scenario.getUri()).getFileName().toString(), "")
                .replaceFirst("/", "");
        Path dir = Path.of(rootPath);
        try (var stream = Files.walk(dir)) {
            featureFiles = stream
                    .filter(Files::isRegularFile)
                    .collect(Collectors.toMap(path -> path.getFileName().toString(), path -> new File(path.toUri())));
        }
    }

    @Given("I have added {string} file to archive")
    public void the_test_directory_contains_file(String fileName) throws IOException {
        if(!fileName.startsWith("./")) {
            throw new RuntimeException("Non local path found");
        }
        fileName = fileName.replaceFirst("./", "");
        var file = Objects.requireNonNull(featureFiles.get(fileName), "File \"" + fileName + "\" not found in " + featureFiles.keySet());

        archive.append(file);
    }

    @When("I run the application using archive")
    public void i_run_the_application_on_the_archive() throws IOException, InterruptedException {
        // Build the runtime app if needed (assume cargo build --release)
        String runtimeDir = Paths.get("..", "runtime").toAbsolutePath().normalize().toString();
        String exe = System.getProperty("os.name").toLowerCase().contains("win") ? "target\\release\\xml-xsd2.exe" : "target/release/xml-xsd2";
        ProcessBuilder build = new ProcessBuilder("cargo", "build", "--release");
        build.directory(new File(runtimeDir));
        Process buildProcess = build.start();
        int buildExit = buildProcess.waitFor();
        if (buildExit != 0) throw new IOException("Failed to build runtime app");

        File exeFile = new File(runtimeDir, exe);
        if (!exeFile.exists()) throw new IOException("Expected binary not found: " + exeFile.getAbsolutePath());

        System.out.println("Running: " + exeFile.getAbsolutePath());
        ProcessBuilder run = new ProcessBuilder(exeFile.getAbsolutePath(), archive.file().toPath().toAbsolutePath().toString());
        run.directory(new File(runtimeDir));
        run.redirectErrorStream(true);
        Process runProcess = run.start();
        lastOutput = new String(runProcess.getInputStream().readAllBytes());
        int exit = runProcess.waitFor();
        if (exit != 0) throw new IOException("Runtime app failed: " + lastOutput);
    }

    @Then("the stdout must contain line {string}")
    public void output_must_contain(String expectedLine) {
        if (lastOutput == null) {
            throw new AssertionError("No output captured from runtime");
        }
        if (!lastOutput.contains("\n" + expectedLine)) {
            throw new AssertionError("Expected output to contain: '" + expectedLine + "' but was:\n" + lastOutput);
        }
    }
}
