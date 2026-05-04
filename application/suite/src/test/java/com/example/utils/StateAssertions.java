package com.example.utils;

import java.io.File;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.Objects;
import java.util.regex.Pattern;
import java.util.stream.Collectors;

import com.example.interop.RuntimeInteropJava;
import com.sun.jna.Pointer;
import io.cucumber.core.internal.com.fasterxml.jackson.core.JsonProcessingException;
import io.cucumber.core.internal.com.fasterxml.jackson.databind.ObjectMapper;

public class StateAssertions {


    public static void assertExportedStateEmpty(ArchiveState state) throws Exception {
        RuntimeInteropJava lib = state.runtimeInteropJava.orElseGet(RuntimeInteropJava::newRuntimeInteropJava);
        com.sun.jna.Pointer p = lib.runtime_export_state_struct();
        if (p == null) {
            throw new AssertionError("exportStateStruct returned NULL pointer");
        }
        try {
            com.example.interop.exportedState.ExportedState es = new com.example.interop.exportedState.ExportedState(p);
            es.read();
            if (es.has_data != 0) {
                throw new AssertionError("Expected exported state to be empty");
            }
        } finally {
            lib.runtime_free_exported_state(p);
        }
    }

    private static void assertTableColumnsMatchesCsvImpl(ArchiveState state, String tableName, String csvFile, java.util.List<String> expectedColumns, java.util.List<java.util.Map<String, Pattern>> patternRows, java.util.List<java.util.Map<String, String>> actualRows) throws Exception {
        // Require exact count
        int expectedCount = patternRows.size();
        int actualCount = actualRows.size();
        if (actualCount != expectedCount) {
            StringBuilder msg = new StringBuilder();
            msg.append("Row count mismatch for table '").append(tableName).append("': expected ").append(expectedCount).append(" rows (from CSV), but found ").append(actualCount).append(" rows in exported state.\n");
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

        // Build match matrix and try to find perfect matching (same algorithm as SQL-based assertions)
        boolean[][] matches = new boolean[expectedCount][actualCount];
        for (int pidx = 0; pidx < expectedCount; pidx++) {
            for (int aidx = 0; aidx < actualCount; aidx++) {
                boolean ok = true;
                for (String col : expectedColumns) {
                    Pattern pat = patternRows.get(pidx).get(col);
                    String val = actualRows.get(aidx).getOrDefault(col, "");
                    if (pat == null) {
                        ok = false;
                        break;
                    }
                    if (!pat.matcher(val).matches()) {
                        ok = false;
                        break;
                    }
                }
                matches[pidx][aidx] = ok;
            }
        }

        int[] patternToActual = new int[expectedCount];
        int[] actualToPattern = new int[actualCount];
        java.util.Arrays.fill(patternToActual, -1);
        java.util.Arrays.fill(actualToPattern, -1);

        class AssignHelper {
            boolean tryAssign(int pidx, boolean[] seen) {
                for (int aidx = 0; aidx < actualCount; aidx++) {
                    if (!matches[pidx][aidx] || seen[aidx]) continue;
                    seen[aidx] = true;
                    if (actualToPattern[aidx] == -1 || tryAssign(actualToPattern[aidx], seen)) {
                        actualToPattern[aidx] = pidx;
                        patternToActual[pidx] = aidx;
                        return true;
                    }
                }
                return false;
            }
        }

        AssignHelper helper = new AssignHelper();
        for (int pidx = 0; pidx < expectedCount; pidx++) {
            boolean[] seen = new boolean[actualCount];
            if (!helper.tryAssign(pidx, seen)) {
                StringBuilder msg = new StringBuilder();
                msg.append("Unable to match CSV pattern row #").append(pidx).append(" to any exported-state row for table '").append(tableName).append("'.\n");
                msg.append("Pattern row: ").append(patternRows.get(pidx)).append("\n");
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
    }

    private static java.util.List<java.util.Map<String, Pattern>> buildPatternRows(ArchiveState state, String csvFile) throws Exception {
        // Read expected CSV
        File expected = Objects.requireNonNull(state.featureFiles.get(csvFile.replaceFirst("./", "")));
        String content = java.nio.file.Files.readString(expected.toPath()).replaceAll("\\R", "\n");
        String[] lines = content.split("\n", -1);
        if (lines.length <= 1) {
            throw new AssertionError("Expected CSV '" + csvFile + "' to contain header and at least one pattern row; path=" + expected.getAbsolutePath() + "; content='" + content + "'");
        }
        String headerLine = lines[0];
        java.util.List<String> expectedColumns = new java.util.ArrayList<>(java.util.Arrays.asList(headerLine.split(",", -1)));
        expectedColumns.removeIf(s -> s == null || s.isEmpty());

        // Build pattern rows (list of maps column->Pattern)
        java.util.List<java.util.Map<String, Pattern>> patternRows = java.util.stream.IntStream
                .range(1, lines.length)
                .filter(r -> !lines[r].trim().isEmpty())
                .mapToObj(r -> {
                    String ln = lines[r];
                    String[] cells = ln.split(",", -1);
                    java.util.List<String> cellList = new java.util.ArrayList<>(java.util.Arrays.asList(cells));
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

        return patternRows;
    }

    private static java.util.List<String> readCsvColumns(ArchiveState state, String csvFile) throws Exception {
        File expected = Objects.requireNonNull(state.featureFiles.get(csvFile.replaceFirst("./", "")));
        String content = java.nio.file.Files.readString(expected.toPath()).replaceAll("\\R", "\n");
        String[] lines = content.split("\n", -1);
        if (lines.length <= 1) {
            throw new AssertionError("Expected CSV '" + csvFile + "' to contain header and at least one pattern row; path=" + expected.getAbsolutePath() + "; content='" + content + "'");
        }
        String headerLine = lines[0];
        java.util.List<String> expectedColumns = new java.util.ArrayList<>(java.util.Arrays.asList(headerLine.split(",", -1)));
        expectedColumns.removeIf(s -> s == null || s.isEmpty());
        return expectedColumns;
    }

    public static void assertExportedStateEntityColumnsMatchesCsv(ArchiveState state, String csvFile) throws Exception {
        java.util.List<String> expectedColumns = readCsvColumns(state, csvFile);
        java.util.List<java.util.Map<String, Pattern>> patternRows = buildPatternRows(state, csvFile);

        java.util.List<java.util.Map<String, String>> actualRows = new java.util.ArrayList<>();
        RuntimeInteropJava lib = state.runtimeInteropJava.orElseGet(RuntimeInteropJava::newRuntimeInteropJava);
        com.sun.jna.Pointer p = lib.runtime_export_state_struct();
        if (p == null) throw new AssertionError("exportStateStruct returned NULL pointer");
        try {
            com.example.interop.exportedState.ExportedState es = new com.example.interop.exportedState.ExportedState(p);
            es.read();
            int len = es.entities.len == null ? 0 : es.entities.len.intValue();
            if (len > 0 && es.entities.data != null) {
                com.sun.jna.Pointer[] ptrs = es.entities.data.getPointerArray(0, len);
                for (com.sun.jna.Pointer q : ptrs) {
                    String v = q == null ? "" : q.getString(0);
                    java.util.Map<String, String> m = new java.util.HashMap<>();
                    m.put("textMap_name", v);
                    actualRows.add(m);
                }
            }
        } finally {
            lib.runtime_free_exported_state(p);
        }

        assertTableColumnsMatchesCsvImpl(state, "entity", csvFile, expectedColumns, patternRows, actualRows);
    }

    public static void assertExportedStateActionColumnsMatchesCsv(ArchiveState state, String csvFile) throws Exception {
        java.util.List<String> expectedColumns = readCsvColumns(state, csvFile);
        java.util.List<java.util.Map<String, Pattern>> patternRows = buildPatternRows(state, csvFile);

        java.util.List<java.util.Map<String, String>> actualRows = new java.util.ArrayList<>();
        RuntimeInteropJava lib = state.runtimeInteropJava.orElseGet(RuntimeInteropJava::newRuntimeInteropJava);
        com.sun.jna.Pointer p = lib.runtime_export_state_struct();
        if (p == null) throw new AssertionError("exportStateStruct returned NULL pointer");
        try {
            com.example.interop.exportedState.ExportedState es = new com.example.interop.exportedState.ExportedState(p);
            es.read();
            int len = es.actions.len == null ? 0 : es.actions.len.intValue();
            if (len > 0 && es.actions.data != null) {
                com.sun.jna.Pointer[] ptrs = es.actions.data.getPointerArray(0, len);
                for (com.sun.jna.Pointer q : ptrs) {
                    String v = q == null ? "" : q.getString(0);
                    java.util.Map<String, String> m = new java.util.HashMap<>();
                    m.put("name", v);
                    actualRows.add(m);
                }
            }
        } finally {
            lib.runtime_free_exported_state(p);
        }

        assertTableColumnsMatchesCsvImpl(state, "action", csvFile, expectedColumns, patternRows, actualRows);
    }

    public static void assertExportedStateEventsColumnsMatchesCsv(ArchiveState state, String csvFile) throws Exception {
        java.util.List<String> expectedColumns = readCsvColumns(state, csvFile);
        java.util.List<java.util.Map<String, Pattern>> patternRows = buildPatternRows(state, csvFile);

        java.util.List<java.util.Map<String, String>> actualRows = new java.util.ArrayList<>();
        RuntimeInteropJava lib = state.runtimeInteropJava.orElseGet(RuntimeInteropJava::newRuntimeInteropJava);
        com.sun.jna.Pointer p = lib.runtime_export_state_struct();
        if (p == null) throw new AssertionError("exportStateStruct returned NULL pointer");
        try {
            com.example.interop.exportedState.ExportedState es = new com.example.interop.exportedState.ExportedState(p);
            es.read();
            int len = es.events.len == null ? 0 : es.events.len.intValue();
            if (len > 0 && es.events.data != null) {
                com.sun.jna.Pointer[] ptrs = es.events.data.getPointerArray(0, len);
                for (com.sun.jna.Pointer q : ptrs) {
                    String v = q == null ? "" : q.getString(0);
                    java.util.Map<String, String> m = new java.util.HashMap<>();
                    m.put("name", v);
                    actualRows.add(m);
                }
            }
        } finally {
            lib.runtime_free_exported_state(p);
        }

        assertTableColumnsMatchesCsvImpl(state, "events", csvFile, expectedColumns, patternRows, actualRows);
    }

    public static void assertExportedStateModuleColumnsMatchesCsv(ArchiveState state, String csvFile) throws Exception {
        java.util.List<String> expectedColumns = readCsvColumns(state, csvFile);
        java.util.List<java.util.Map<String, Pattern>> patternRows = buildPatternRows(state, csvFile);

        java.util.List<java.util.Map<String, String>> actualRows = new java.util.ArrayList<>();
        RuntimeInteropJava lib = state.runtimeInteropJava.orElseGet(RuntimeInteropJava::newRuntimeInteropJava);
        com.sun.jna.Pointer p = lib.runtime_export_state_struct();
        if (p == null) throw new AssertionError("exportStateStruct returned NULL pointer");
        try {
            com.example.interop.exportedState.ExportedState es = new com.example.interop.exportedState.ExportedState(p);
            es.read();
            int len = es.modules.len == null ? 0 : es.modules.len.intValue();
            if (len > 0 && es.modules.data != null) {
                long structSize = new com.example.interop.exportedState.ModuleRow().size();
                for (int i = 0; i < len; i++) {
                    com.example.interop.exportedState.ModuleRow mr = new com.example.interop.exportedState.ModuleRow(es.modules.data.share(i * structSize));
                    mr.read();
                    String id = mr.id == null ? "" : mr.id.getString(0);
                    String name = mr.name == null ? "" : mr.name.getString(0);
                    String version = mr.version == null ? "" : mr.version.getString(0);
                    java.util.Map<String, String> m = new java.util.HashMap<>();
                    m.put("id", id);
                    m.put("name", name);
                    m.put("version", version);
                    actualRows.add(m);
                }
            }
        } finally {
            lib.runtime_free_exported_state(p);
        }

        assertTableColumnsMatchesCsvImpl(state, "module", csvFile, expectedColumns, patternRows, actualRows);
    }

    public static void assertExportedStatePanelColumnsMatchesCsv(ArchiveState state, String csvFile) throws Exception {
        java.util.List<String> expectedColumns = readCsvColumns(state, csvFile);
        java.util.List<java.util.Map<String, Pattern>> patternRows = buildPatternRows(state, csvFile);

        java.util.List<java.util.Map<String, String>> actualRows = new java.util.ArrayList<>();
        RuntimeInteropJava lib = state.runtimeInteropJava.orElseGet(RuntimeInteropJava::newRuntimeInteropJava);
        com.sun.jna.Pointer p = lib.runtime_export_state_struct();
        if (p == null) throw new AssertionError("exportStateStruct returned NULL pointer");
        try {
            com.example.interop.exportedState.ExportedState es = new com.example.interop.exportedState.ExportedState(p);
            es.read();
            int len = es.panels.len == null ? 0 : es.panels.len.intValue();
            if (len > 0 && es.panels.data != null) {
                com.example.interop.exportedState.PanelFfi template = new com.example.interop.exportedState.PanelFfi();
                int structSize = template.size();
                for (int i = 0; i < len; i++) {
                    com.example.interop.exportedState.PanelFfi panel =
                        new com.example.interop.exportedState.PanelFfi(es.panels.data.share((long) i * structSize));
                    String id = panel.id == null ? "" : panel.id.getString(0);
                    java.util.Map<String, String> m = new java.util.HashMap<>();
                    m.put("id", id);
                    m.put("offset__top", String.valueOf(panel.offset.top));
                    m.put("offset__left", String.valueOf(panel.offset.left));
                    m.put("offset__right", String.valueOf(panel.offset.right));
                    m.put("offset__bottom", String.valueOf(panel.offset.bottom));
                    actualRows.add(m);
                }
            }
        } finally {
            lib.runtime_free_exported_state(p);
        }

        assertTableColumnsMatchesCsvImpl(state, "panel", csvFile, expectedColumns, patternRows, actualRows);
    }

    public static void assertExportedStateTableColumnsMatchesCsv(ArchiveState state, String tableName, String csvFile) throws Exception {
        switch (tableName.toLowerCase()) {
            case "entity":
                assertExportedStateEntityColumnsMatchesCsv(state, csvFile);
                break;
            case "action":
                assertExportedStateActionColumnsMatchesCsv(state, csvFile);
                break;
            case "events":
                assertExportedStateEventsColumnsMatchesCsv(state, csvFile);
                break;
            case "module":
                assertExportedStateModuleColumnsMatchesCsv(state, csvFile);
                break;
            case "panel":
                assertExportedStatePanelColumnsMatchesCsv(state, csvFile);
                break;
            default:
                throw new AssertionError("Unsupported table for exported-state validation: " + tableName);
        }
    }

    public static void assertReturnedPanelNamesIsIn(ArchiveState state, String arg) {
        try {
            var objectMapper = new ObjectMapper();
            List<String> expectedNames = objectMapper.reader().readValue(arg, ArrayList.class);
            var runtimeInteropJava = state.runtimeInteropJava.get();

            String[] nameArray = runtimeInteropJava.get_panel_names().getStringArray(0);
            List<String> resultNames = Arrays.asList(nameArray);
            if (!(expectedNames.containsAll(resultNames)) || !(resultNames.containsAll(expectedNames))) {
                StringBuilder msg = new StringBuilder();
                msg.append("Unable to match result get_panel_names");
                msg.append("Expected : ").append(expectedNames.stream().collect(Collectors.joining(",", "[", "]"))).append("\n");
                msg.append("Actual:").append(resultNames.stream().collect(Collectors.joining(",", "[", "]"))).append("\n");
                throw new AssertionError(msg.toString());
            }

        } catch (JsonProcessingException e) {
            throw new RuntimeException(e);
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }
}





