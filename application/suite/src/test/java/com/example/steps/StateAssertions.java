package com.example.steps;

import java.io.File;
import java.io.IOException;
import java.util.Objects;
import java.util.regex.Pattern;

import static com.example.steps.ArchiveRunner.DEBUG_DELIMITED;

import com.example.interop.RuntimeInteropJava;

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
        boolean ok = state.runtimeInteropJava.map(runtimeInteropJava -> runtimeInteropJava.exportState(sqlFile.getAbsolutePath()))
                .get();
        long deadline = System.currentTimeMillis() + 5000;
        while (System.currentTimeMillis() < deadline) {
            if (sqlFile.exists() && sqlFile.length() > 0) break;
            try { Thread.sleep(10); } catch (InterruptedException e) { Thread.currentThread().interrupt(); throw new IOException("Interrupted", e); }
        }
        return sqlFile;
    }

    private static void extracted(ArchiveState state, String tableName, String csvFile, File sqliteFile) throws java.sql.SQLException, IOException {
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
        String expectedCsv = java.nio.file.Files.readString(expected.toPath()).replaceAll("\r\n", "\n");
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
            String headerLine = java.nio.file.Files.readString(expected.toPath()).replaceAll("\r\n", "\n").split("\n")[0];
            java.util.List<String> expectedColumns = new java.util.ArrayList<>(java.util.Arrays.asList(headerLine.split(",", -1)));
            expectedColumns.removeIf(s -> s == null || s.isEmpty());

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
        } catch (java.sql.SQLException e) {
            throw new RuntimeException(e);
        }
    }

    // rest of methods unchanged...
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
        String content = java.nio.file.Files.readString(expected.toPath()).replaceAll("\r\n", "\n");
        String[] lines = content.split("\n", -1);
        if (lines.length <= 1) {
            throw new AssertionError("Expected CSV '" + csvFile + "' to contain header and at least one pattern row");
        }
        String headerLine = lines[0];
        java.util.List<String> expectedColumns = new java.util.ArrayList<>(java.util.Arrays.asList(headerLine.split(",", -1)));
        expectedColumns.removeIf(s -> s == null || s.isEmpty());

        // Build pattern rows: list of maps column->Pattern (stream-based)
        java.util.List<java.util.Map<String, Pattern>> patternRows = java.util.stream.IntStream
                .range(1, lines.length)
                .filter(r -> !lines[r].trim().isEmpty())
                .mapToObj(r -> {
                    String ln = lines[r];
                    String[] cells = ln.split(",", -1);
                    java.util.List<String> cellList = new java.util.ArrayList<>(java.util.Arrays.asList(cells));
                    // Trim trailing empty columns which are often present in CSV patterns
                    while (cellList.size() > expectedColumns.size() && cellList.get(cellList.size() - 1).isEmpty()) {
                        cellList.remove(cellList.size() - 1);
                    }
                    if (cellList.size() != expectedColumns.size()) {
                        throw new AssertionError("CSV pattern row " + r + " has wrong number of columns (expected " + expectedColumns.size() + ", got " + cellList.size() + ")");
                    }
                    return java.util.stream.IntStream.range(0, expectedColumns.size())
                            .boxed()
                            .collect(java.util.stream.Collectors.toMap(i -> expectedColumns.get(i), i -> Pattern.compile(cellList.get(i), Pattern.DOTALL), (a, b) -> a, java.util.LinkedHashMap::new));
                })
                .collect(java.util.stream.Collectors.toList());

        // Require exact count: the CSV defines how many rows are expected
        int expectedCount = patternRows.size();
        int actualCount = actualRows.size();
        if (actualCount != expectedCount) {
            StringBuilder msg = new StringBuilder();
            msg.append("Row count mismatch for table '").append(tableName).append("': expected ").append(expectedCount).append(" rows (from CSV), but found ").append(actualCount).append(" rows in DB.\n");
            msg.append("Actual rows:\n");
            for (int i = 0; i < actualRows.size(); i++) {
                msg.append(i).append(": ").append(actualRows.get(i)).append("\n");
            }
            msg.append("CSV pattern rows:\n");
            for (int i = 0; i < patternRows.size(); i++) {
                msg.append(i).append(": ").append(patternRows.get(i).keySet()).append(" -> ").append(patternRows.get(i).values()).append("\n");
            }
            throw new AssertionError(msg.toString());
        }

        // Build match matrix: patternRows x actualRows
        boolean[][] matches = new boolean[expectedCount][actualCount];
        for (int p = 0; p < expectedCount; p++) {
            for (int a = 0; a < actualCount; a++) {
                boolean ok = true;
                for (String col : expectedColumns) {
                    Pattern pat = patternRows.get(p).get(col);
                    String val = actualRows.get(a).getOrDefault(col, "");
                    if (pat == null) {
                        // If CSV didn't include a column (shouldn't happen), treat as mismatch
                        ok = false;
                        break;
                    }
                    if (!pat.matcher(val).matches()) {
                        ok = false;
                        break;
                    }
                }
                matches[p][a] = ok;
            }
        }

        // Find a perfect matching between patternRows (left) and actualRows (right)
        int[] patternToActual = new int[expectedCount];
        int[] actualToPattern = new int[actualCount];
        java.util.Arrays.fill(patternToActual, -1);
        java.util.Arrays.fill(actualToPattern, -1);

        class AssignHelper {
            boolean tryAssign(int p, boolean[] seen) {
                for (int a = 0; a < actualCount; a++) {
                    if (!matches[p][a] || seen[a]) continue;
                    seen[a] = true;
                    if (actualToPattern[a] == -1 || tryAssign(actualToPattern[a], seen)) {
                        actualToPattern[a] = p;
                        patternToActual[p] = a;
                        return true;
                    }
                }
                return false;
            }
        }

        AssignHelper helper = new AssignHelper();
        for (int p = 0; p < expectedCount; p++) {
            boolean[] seen = new boolean[actualCount];
            if (!helper.tryAssign(p, seen)) {
                // Build diagnostic message showing which pattern couldn't be satisfied
                StringBuilder msg = new StringBuilder();
                msg.append("Unable to match CSV pattern row #").append(p).append(" to any DB row for table '").append(tableName).append("'.\n");
                msg.append("Pattern row: ").append(patternRows.get(p)).append("\n");
                msg.append("Actual rows:\n");
                for (int a = 0; a < actualRows.size(); a++) {
                    msg.append(a).append(": ").append(actualRows.get(a)).append("\n");
                }
                msg.append("Match matrix (pattern x actual):\n");
                for (int pi = 0; pi < expectedCount; pi++) {
                    msg.append("P").append(pi).append(": ");
                    for (int ai = 0; ai < actualCount; ai++) msg.append(matches[pi][ai] ? "1" : "0");
                    msg.append("\n");
                }
                throw new AssertionError(msg.toString());
            }
        }

        // If we reach here, a perfect one-to-one matching was found

    }
}

