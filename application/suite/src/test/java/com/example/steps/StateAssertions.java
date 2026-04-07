package com.example.steps;

import java.io.File;
import java.io.IOException;
import java.io.OutputStream;
import java.nio.file.Files;
import java.sql.SQLException;
import java.util.Arrays;
import java.util.Objects;
import java.util.regex.Pattern;

import static com.example.steps.ArchiveRunner.DEBUG_DELIMITED;

public class StateAssertions {

    public static void assertOutputTableCsv(ArchiveState state, String tableName, String csvFile) throws Exception {
        File sqliteFile = extractFileFromProcess(state);
        extracted(state, tableName, csvFile, sqliteFile);
    }


    public static File extractFileFromProcess(ArchiveState state) throws IOException {
        File sqlFile = File.createTempFile("export", ".sqlite");
        sqlFile.delete(); // let the runtime create it fresh
        return extractFileFromProcess(state, sqlFile);
    }

    public static File extractFileFromProcess(ArchiveState state, File sqlFile) throws IOException {
        String cmd = "DEBUG: Export:" + sqlFile.getAbsolutePath() + System.lineSeparator();

        Process p = state.runProcess;
        if (p == null) throw new IllegalStateException("state.runProcess is null");
        OutputStream os = p.getOutputStream();
        if (os == null) throw new IllegalStateException("Process output stream is null");
        os.write(cmd.getBytes(java.nio.charset.StandardCharsets.UTF_8));
        os.flush();

        // Wait until the runtime has written the file
        long deadline = System.currentTimeMillis() + 5000;
        while (System.currentTimeMillis() < deadline) {
            if (sqlFile.exists() && sqlFile.length() > 0) break;
            try { Thread.sleep(10); } catch (InterruptedException ignored) {}
        }
        return sqlFile;
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



    public static void assertOutputTableColumnsMatchesCsv(ArchiveState state, String tableName, String csvFile) throws Exception {
        File sqliteFile = extractFileFromProcess(state);
        try (java.sql.Connection conn = java.sql.DriverManager.getConnection("jdbc:sqlite:" + sqliteFile.getAbsolutePath());
             java.sql.Statement stmt = conn.createStatement();
             java.sql.ResultSet rs = stmt.executeQuery("SELECT * FROM '" + tableName + "' LIMIT 0")) {
            java.sql.ResultSetMetaData meta = rs.getMetaData();
            int colCount = meta.getColumnCount();
            java.util.List<String> actualColumns = new java.util.ArrayList<>();
            for (int i = 1; i <= colCount; i++) {
                actualColumns.add(meta.getColumnName(i));
            }

            File expected = Objects.requireNonNull(state.featureFiles.get(csvFile.replaceFirst("./", "")));
            String headerLine = Files.readString(expected.toPath()).replaceAll("\r\n", "\n").split("\n")[0];
            java.util.List<String> expectedColumns = Arrays.asList(headerLine.split(","));

            if (!actualColumns.containsAll(expectedColumns)) {
                java.util.List<String> missing = new java.util.ArrayList<>(expectedColumns);
                missing.removeAll(actualColumns);
                throw new AssertionError("Column mismatch for table '" + tableName + "': missing columns " + missing + "\nExpected (from CSV): " + expectedColumns + "\nActual:              " + actualColumns);
            }

        }
        assertOutputTableRowsMatchRegexIncludesCsv(state,tableName,csvFile);
    }

    public static void assertEmptySqlFile(ArchiveState state) throws IOException {
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

    /**
     * For each row in the database table, assert that it is matched by at least one row in the
     * provided CSV file. The CSV's first line is treated as a header with column names. Each
     * subsequent line is treated as a set of regex patterns (one per header column). A DB row
     * matches a CSV pattern-row when, for every column named in the CSV header, the cell value
     * from the DB matches the corresponding regex (using full-match semantics).
     *
     * This implements an "includes" style check: the CSV may list a set of pattern-rows and the
     * table is considered valid when every actual row is included by at least one pattern-row.
     */
    public static void assertOutputTableRowsMatchRegexIncludesCsv(ArchiveState state, String tableName, String csvFile) throws Exception {
        File sqliteFile = extractFileFromProcess(state);

        // Read actual rows from the DB as a list of maps column->value
        java.util.List<java.util.Map<String, String>> actualRows = new java.util.ArrayList<>();
        try (java.sql.Connection conn = java.sql.DriverManager.getConnection("jdbc:sqlite:" + sqliteFile.getAbsolutePath());
             java.sql.Statement stmt = conn.createStatement();
             java.sql.ResultSet rs = stmt.executeQuery("SELECT * FROM '" + tableName + "'")) {
            java.sql.ResultSetMetaData meta = rs.getMetaData();
            int colCount = meta.getColumnCount();
            java.util.List<String> colNames = new java.util.ArrayList<>();
            for (int i = 1; i <= colCount; i++) colNames.add(meta.getColumnName(i));

            while (rs.next()) {
                java.util.Map<String, String> row = new java.util.HashMap<>();
                for (int i = 1; i <= colCount; i++) {
                    String val = rs.getString(i);
                    row.put(colNames.get(i - 1), val == null ? "" : val);
                }
                actualRows.add(row);
            }
        }

        // Read expected CSV and parse header + pattern rows
        File expected = Objects.requireNonNull(state.featureFiles.get(csvFile.replaceFirst("./", "")));
        String content = Files.readString(expected.toPath()).replaceAll("\r\n", "\n");
        String[] lines = content.split("\n", -1);
        if (lines.length <= 1) {
            throw new AssertionError("Expected CSV '" + csvFile + "' to contain header and at least one pattern row");
        }
        String headerLine = lines[0];
        java.util.List<String> expectedColumns = Arrays.asList(headerLine.split(","));

        // Build pattern rows: list of maps column->Pattern
        java.util.List<java.util.Map<String, Pattern>> patternRows = new java.util.ArrayList<>();
        for (int r = 1; r < lines.length; r++) {
            String ln = lines[r];
            if (ln.trim().isEmpty()) continue; // skip empty pattern lines
            String[] cells = ln.split(",", -1);
            if (cells.length != expectedColumns.size()) {
                throw new AssertionError("CSV pattern row " + r + " has wrong number of columns (expected " + expectedColumns.size() + ", got " + cells.length + ")");
            }
            java.util.Map<String, Pattern> prow = new java.util.HashMap<>();
            for (int i = 0; i < expectedColumns.size(); i++) {
                String col = expectedColumns.get(i);
                String pat = cells[i];
                Pattern p = Pattern.compile(pat, Pattern.DOTALL);
                prow.put(col, p);
            }
            patternRows.add(prow);
        }

        // For each actual row, ensure at least one pattern row matches
        java.util.List<java.util.Map<String, String>> missing = new java.util.ArrayList<>();
        for (java.util.Map<String, String> actual : actualRows) {
            boolean anyMatches = false;
            for (java.util.Map<String, Pattern> prow : patternRows) {
                boolean ok = true;
                for (String col : expectedColumns) {
                    Pattern p = prow.get(col);
                    String actualVal = actual.getOrDefault(col, "");
                    if (actualVal == null) actualVal = "";
                    if (!p.matcher(actualVal).matches()) { ok = false; break; }
                }
                if (ok) { anyMatches = true; break; }
            }
            if (!anyMatches) missing.add(actual);
        }

        if (!missing.isEmpty()) {
            StringBuilder b = new StringBuilder();
            b.append("The following rows from table '").append(tableName).append("' did not match any pattern from ").append(csvFile).append("\n");
            for (java.util.Map<String, String> row : missing) {
                b.append(row.toString()).append("\n");
            }
            throw new AssertionError(b.toString());
        }
    }

}
