package com.example.steps;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.sql.SQLException;
import java.util.Objects;
// ...existing code...

import static com.example.steps.ArchiveRunner.DEBUG_DELIMITED;

public class StateAssertions {

    public static void assertOutputTableCsv(ArchiveState state, String tableName, String csvFile) throws Exception {
        File sqliteFile = extractFileFromProcess(state);
        extracted(state, tableName, csvFile, sqliteFile);
    }
    
    

    private static File extractFileFromProcess(ArchiveState state) {
        // New logic: find the "--SQLITE-START--" marker first, then find the DEBUG_DELIMITED
        // The sqlite file bytes start at the "--SQLITE-START--" marker and end just before DEBUG_DELIMITED
        String startMarker = "--SQLITE-START--";
        String outputStr = new String(state.lastOutput);
        int startIdx = outputStr.indexOf(startMarker);
        if (startIdx == -1) throw new AssertionError("Start marker not found in output");
        int endIdx = outputStr.indexOf(DEBUG_DELIMITED, startIdx);
        if (endIdx == -1) throw new AssertionError("End marker (DEBUG_DELIMITED) not found after start marker");

        // compute byte offsets for the slice to write to the sqlite file
        int byteStart = outputStr.substring(0, startIdx).getBytes().length;
        int byteEnd = outputStr.substring(0, endIdx).getBytes().length;
        if (byteEnd <= byteStart) throw new AssertionError("No SQLite data found between markers");

        try {
            File sqliteFile = File.createTempFile("testdb", ".sqlite");
            try (java.io.FileOutputStream fos = new java.io.FileOutputStream(sqliteFile)) {
                fos.write(state.lastOutput, byteStart, byteEnd - byteStart);
            }
            return sqliteFile;
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }

    private static void extracted(ArchiveState state, String tableName, String csvFile, File sqliteFile) throws SQLException, IOException {
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

    public static void assertEmptySqlFile(ArchiveState state){
        var sqliteFile = extractFileFromProcess(state);

        try (java.sql.Connection conn = java.sql.DriverManager.getConnection("jdbc:sqlite:" + sqliteFile.getAbsolutePath());
             java.sql.PreparedStatement ps = conn.prepareStatement("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")) {
            try (java.sql.ResultSet rs = ps.executeQuery()) {
                java.util.List<String> tables = new java.util.ArrayList<>();
                while (rs.next()) {
                    tables.add(rs.getString(1));
                }
                if (!tables.isEmpty()) {
                    throw new AssertionError("Expected no tables defined in sqlite DB but found: " + tables);
                }
            }
        } catch (SQLException e) {
            throw new RuntimeException(e);
        }
    }

}
