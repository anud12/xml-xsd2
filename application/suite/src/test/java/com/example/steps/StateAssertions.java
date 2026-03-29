package com.example.steps;

import java.io.File;
import java.nio.file.Files;
import java.util.Objects;

public class StateAssertions {

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

}
