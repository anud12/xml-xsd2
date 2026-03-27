package com.example.steps;

import io.cucumber.java.Before;
import io.cucumber.java.PendingException;
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
    private byte[] lastOutput;

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
        build.inheritIO();
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
        lastOutput = runProcess.getInputStream().readAllBytes();
        int exit = runProcess.waitFor();
        if (exit != 0) throw new IOException("Runtime app failed: " + new String(lastOutput));
    }

    @Then("the stdout must contain line {string}")
    public void output_must_contain(String expectedLine) {
        if (lastOutput == null) {
            throw new AssertionError("No output captured from runtime");
        }
        var outputString = new String(lastOutput);
        if (!outputString.contains("\n" + expectedLine)) {
            throw new AssertionError("Expected output to contain: '" + expectedLine + "' but was:\n" + outputString);
        }
    }

    @Then("output table {string} must be {string} csv")
    public void outputTableMustBeCsv(String tableName, String csvFile) throws Exception {
        // 1. Find the delimiter in lastOutput
        String delimiter = "--SQLITE-START--";
        // Convert lastOutput to String for delimiter search
        String outputStr = new String(lastOutput);
        int strIdx = outputStr.indexOf(delimiter);
        if (strIdx == -1) throw new AssertionError("Delimiter not found in output");
        // Find the byte offset after the delimiter and any following newlines
        int byteIdx = -1;
        for (int i = strIdx + delimiter.length(); i < outputStr.length(); i++) {
            char c = outputStr.charAt(i);
            if (c != '\n' && c != '\r') {
                byteIdx = outputStr.substring(0, i).getBytes().length;
                break;
            }
        }
        if (byteIdx == -1) throw new AssertionError("No SQLite data found after delimiter");

        // 2. Write SQLite bytes to temp file
        File sqliteFile = File.createTempFile("testdb", ".sqlite");
        try (FileOutputStream fos = new FileOutputStream(sqliteFile)) {
            fos.write(lastOutput, byteIdx, lastOutput.length - byteIdx);
        }

        // 3. Query table and export as CSV
        StringBuilder csvBuilder = new StringBuilder();
        try (java.sql.Connection conn = java.sql.DriverManager.getConnection("jdbc:sqlite:" + sqliteFile.getAbsolutePath());
             java.sql.Statement stmt = conn.createStatement()) {
            // Log all tables and their structure
            try (java.sql.ResultSet tables = stmt.executeQuery("SELECT name FROM sqlite_master WHERE type='table'")) {
                System.out.println("--- SQLite Tables and Structure ---");
                while (tables.next()) {
                    String tName = tables.getString(1);
                    System.out.println("Table: " + tName);
                    try (java.sql.ResultSet pragma = stmt.executeQuery("PRAGMA table_info('" + tName + "')")) {
                        while (pragma.next()) {
                            System.out.println("  " + pragma.getString("name") + " " + pragma.getString("type"));
                        }
                    }
                }
                System.out.println("--- End SQLite Tables ---");
            }
            // Export requested table as CSV
            try (java.sql.ResultSet rs = stmt.executeQuery("SELECT * FROM '" + tableName + "'")) {
                java.sql.ResultSetMetaData meta = rs.getMetaData();
                int colCount = meta.getColumnCount();
                // Header
                for (int i = 1; i <= colCount; i++) {
                    csvBuilder.append(meta.getColumnName(i));
                    if (i < colCount) csvBuilder.append(",");
                }
                csvBuilder.append("\n");
                // Rows
                while (rs.next()) {
                    for (int i = 1; i <= colCount; i++) {
                        csvBuilder.append(rs.getString(i));
                        if (i < colCount) csvBuilder.append(",");
                    }
                    csvBuilder.append("\n");
                }
            }
        }

        // 4. Compare with expected CSV file
        File expected = java.util.Objects.requireNonNull(featureFiles.get(csvFile.replaceFirst("./", "")));
        String expectedCsv = java.nio.file.Files.readString(expected.toPath()).replaceAll("\r\n", "\n");
        if (!csvBuilder.toString().trim().equals(expectedCsv.trim())) {
            throw new AssertionError("CSV output mismatch:\nExpected:\n" + expectedCsv + "\nActual:\n" + csvBuilder);
        }
    }
}
