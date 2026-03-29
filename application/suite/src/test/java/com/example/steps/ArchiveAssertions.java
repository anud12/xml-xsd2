package com.example.steps;

import java.io.File;
import java.nio.file.Files;
import java.util.Objects;
import java.util.regex.Pattern;

public class ArchiveAssertions {
    public static void assertStdoutContainsLine(ArchiveState state, String expectedLine) {
        if (state.lastOutput == null) {
            throw new AssertionError("No output captured from runtime");
        }
        var outputString = new String(state.lastOutput);
        if (!outputString.contains("\n" + expectedLine)) {
            throw new AssertionError("Expected output to contain: '" + expectedLine + "' but was:\n" + outputString);
        }
    }

    public static void assertOutputTableCsv(ArchiveState state, String tableName, String csvFile) throws Exception {
        String delimiter = "--SQLITE-START--";
        String outputStr = new String(state.lastOutput);
        int strIdx = outputStr.indexOf(delimiter);
        if (strIdx == -1) throw new AssertionError("Delimiter not found in output");
        int byteIdx = -1;
        for (int i = strIdx + delimiter.length(); i < outputStr.length(); i++) {
            char c = outputStr.charAt(i);
            if (c != '\n' && c != '\r') {
                byteIdx = outputStr.substring(0, i).getBytes().length;
                break;
            }
        }
        if (byteIdx == -1) throw new AssertionError("No SQLite data found after delimiter");
        File sqliteFile = File.createTempFile("testdb", ".sqlite");
        try (java.io.FileOutputStream fos = new java.io.FileOutputStream(sqliteFile)) {
            fos.write(state.lastOutput, byteIdx, state.lastOutput.length - byteIdx);
        }
        StringBuilder csvBuilder = new StringBuilder();
        try (java.sql.Connection conn = java.sql.DriverManager.getConnection("jdbc:sqlite:" + sqliteFile.getAbsolutePath());
             java.sql.Statement stmt = conn.createStatement()) {
            try (java.sql.ResultSet rs = stmt.executeQuery("SELECT * FROM '" + tableName + "'")) {
                java.sql.ResultSetMetaData meta = rs.getMetaData();
                int colCount = meta.getColumnCount();
                for (int i = 1; i <= colCount; i++) {
                    csvBuilder.append(meta.getColumnName(i));
                    if (i < colCount) csvBuilder.append(",");
                }
                csvBuilder.append("\n");
                while (rs.next()) {
                    for (int i = 1; i <= colCount; i++) {
                        csvBuilder.append(rs.getString(i));
                        if (i < colCount) csvBuilder.append(",");
                    }
                    csvBuilder.append("\n");
                }
            }
        }
        File expected = Objects.requireNonNull(state.featureFiles.get(csvFile.replaceFirst("./", "")));
        String expectedCsv = Files.readString(expected.toPath()).replaceAll("\r\n", "\n");
        if (!csvBuilder.toString().trim().equals(expectedCsv.trim())) {
            throw new AssertionError("CSV output mismatch:\nExpected:\n" + expectedCsv + "\nActual:\n" + csvBuilder);
        }
    }

    public static void assertLogLineContainsRegex(ArchiveState state, String arg0) {
        if (state.lastOutput == null) {
            throw new AssertionError("No output captured from runtime");
        }
        String output = new String(state.lastOutput);
        Pattern pattern = Pattern.compile(arg0);
        boolean found = output.lines().anyMatch(line -> pattern.matcher(line).find());
        if (!found) {
            throw new AssertionError("Output log line matching regex '" + arg0 + "' not found. Output:\n" + output);
        }
    }

    public static void waitUntilLogLineContainsRegex(ArchiveState state, String regex) throws InterruptedException {
        Pattern pattern = Pattern.compile(regex);
        long timeoutMillis = 10000; // 10 seconds max wait
        long pollInterval = 100;
        long start = System.currentTimeMillis();
        while (System.currentTimeMillis() - start < timeoutMillis) {
            if (state.lastOutput != null) {
                String output = new String(state.lastOutput);
                boolean found = output.lines().anyMatch(line -> pattern.matcher(line).find());
                if (found) return;
            }
            Thread.sleep(pollInterval);
        }
        throw new AssertionError("Timeout waiting for log line matching regex: " + regex);
    }
}
