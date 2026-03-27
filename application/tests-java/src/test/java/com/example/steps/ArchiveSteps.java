package com.example.steps;

import io.cucumber.java.en.Given;
import io.cucumber.java.en.When;
import io.cucumber.java.en.Then;
import org.junit.jupiter.api.Assertions;

import java.io.*;
import java.nio.file.*;
import java.util.*;
import java.util.zip.*;

public class ArchiveSteps {
    private Map<String, byte[]> files;
    private byte[] archive;
    private String lastOutput;

    @Given("^the test directory contains files named (.+) and (.+) with contents (.+) and (.+)$")
    public void the_test_directory_contains_files_named_and_with_contents(String file1, String file2, String content1, String content2) throws IOException {
        Path dir = Paths.get("src/test/resources/testdir");
        files = new HashMap<>();
        if (!Files.exists(dir)) {
            Files.createDirectories(dir);
        }
        // Clean up directory first
        try (DirectoryStream<Path> stream = Files.newDirectoryStream(dir)) {
            for (Path entry : stream) {
                if (Files.isRegularFile(entry)) {
                    Files.delete(entry);
                }
            }
        }
        // Create files as per scenario
        Files.write(dir.resolve(file1), content1.getBytes());
        Files.write(dir.resolve(file2), content2.getBytes());
        files.put(file1, content1.getBytes());
        files.put(file2, content2.getBytes());
    }

    @Given("the test directory contains files")
    public void the_test_directory_contains_files() throws IOException {
        Path dir = Paths.get("src/test/resources/testdir");
        files = new HashMap<>();
        if (!Files.exists(dir)) {
            Files.createDirectories(dir);
            // Create sample files for test
            Files.write(dir.resolve("file1.txt"), "Hello World".getBytes());
            Files.write(dir.resolve("file2.txt"), "Another File".getBytes());
        }
        try (DirectoryStream<Path> stream = Files.newDirectoryStream(dir)) {
            for (Path entry : stream) {
                if (Files.isRegularFile(entry)) {
                    files.put(entry.getFileName().toString(), Files.readAllBytes(entry));
                }
            }
        }
    }

    @When("I create an archive of all files in the directory")
    public void i_create_an_archive_of_all_files_in_the_directory() throws IOException {
        ByteArrayOutputStream baos = new ByteArrayOutputStream();
        try (ZipOutputStream zos = new ZipOutputStream(baos)) {
            for (Map.Entry<String, byte[]> entry : files.entrySet()) {
                ZipEntry zipEntry = new ZipEntry(entry.getKey());
                zos.putNextEntry(zipEntry);
                zos.write(entry.getValue());
                zos.closeEntry();
            }
        }
        archive = baos.toByteArray();
    }

    @When("I run the application on the archive")
    public void i_run_the_application_on_the_archive() throws IOException, InterruptedException {
        // Write archive to a temp file
        Path tempArchive = Files.createTempFile("test-archive", ".zip");
        Files.write(tempArchive, archive);
        // Build the runtime app if needed (assume cargo build --release)
        String runtimeDir = Paths.get("..", "runtime").toAbsolutePath().normalize().toString();
        String exe = System.getProperty("os.name").toLowerCase().contains("win") ? "target\\release\\xml-xsd2.exe" : "target/release/xml-xsd2";
        File exeFile = new File(runtimeDir, exe);
        if (!exeFile.exists()) {
            ProcessBuilder build = new ProcessBuilder("cargo", "build", "--release");
            build.directory(new File(runtimeDir));
            Process buildProcess = build.start();
            int buildExit = buildProcess.waitFor();
            if (buildExit != 0) throw new IOException("Failed to build runtime app");
            if (!exeFile.exists()) throw new IOException("Expected binary not found: " + exeFile.getAbsolutePath());
        }
        System.out.println("Running: " + exeFile.getAbsolutePath());
        ProcessBuilder run = new ProcessBuilder(exeFile.getAbsolutePath(), tempArchive.toAbsolutePath().toString());
        run.directory(new File(runtimeDir));
        run.redirectErrorStream(true);
        Process runProcess = run.start();
        lastOutput = new String(runProcess.getInputStream().readAllBytes());
        int exit = runProcess.waitFor();
        if (exit != 0) throw new IOException("Runtime app failed: " + lastOutput);
        // Optionally, store output for later assertions
        System.out.println("Runtime output: " + lastOutput);
    }


    @Then("the archive should contain all files with correct contents")
    public void the_archive_should_contain_all_files_with_correct_contents() throws IOException {
        Map<String, byte[]> archiveContents = new HashMap<>();
        try (ByteArrayInputStream bais = new ByteArrayInputStream(archive);
             ZipInputStream zis = new ZipInputStream(bais)) {
            ZipEntry entry;
            while ((entry = zis.getNextEntry()) != null) {
                ByteArrayOutputStream baos = new ByteArrayOutputStream();
                byte[] buffer = new byte[1024];
                int len;
                while ((len = zis.read(buffer)) > 0) {
                    baos.write(buffer, 0, len);
                }
                archiveContents.put(entry.getName(), baos.toByteArray());
            }
        }
        Assertions.assertEquals(files.size(), archiveContents.size());
        for (Map.Entry<String, byte[]> file : files.entrySet()) {
            Assertions.assertArrayEquals(file.getValue(), archiveContents.get(file.getKey()));
        }
    }

    @Then("the output must contain {string}")
    public void output_must_contain(String expectedLine) {
        if (lastOutput == null) {
            throw new AssertionError("No output captured from runtime");
        }
        if (!lastOutput.contains(expectedLine)) {
            throw new AssertionError("Expected output to contain: '" + expectedLine + "' but was:\n" + lastOutput);
        }
    }
}
