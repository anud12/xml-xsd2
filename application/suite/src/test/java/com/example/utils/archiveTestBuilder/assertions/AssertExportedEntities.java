package com.example.utils.archiveTestBuilder.assertions;

import com.example.tests.interop.RuntimeInteropJava;
import com.example.tests.interop.exportedState.ExportedState;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilder;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilderCommon;

import java.util.*;

public interface AssertExportedEntities extends ArchiveTestBuilderCommon {

    class Entity {
        private Optional<String> textMap_name = Optional.empty();
        private Optional<HashMap<String, Long>> numberMap = Optional.empty();
        private Optional<HashMap<String, String>> textMap = Optional.empty();

        public Entity setTextMapName(String string) {
            textMap_name = Optional.of(string);
            return this;
        }

        public Entity setTextMap(String key, String value) {
            if (textMap.isEmpty()) {
                textMap = Optional.of(new HashMap<>());
            }
            textMap.ifPresent(stringLongHashMap -> {
                stringLongHashMap.put(key, value);
            });
            return this;
        }

        public Entity setNumberMap(String key, Long value) {
            if (numberMap.isEmpty()) {
                numberMap = Optional.of(new HashMap<>());
            }
            numberMap.ifPresent(stringLongHashMap -> {
                stringLongHashMap.put(key, value);
            });
            return this;
        }
    }

    default ArchiveTestBuilder assertExportedEntities(List<Entity> expectedEntities) throws Exception {
        RuntimeInteropJava lib = this.getState().runtimeInteropJava.orElseGet(RuntimeInteropJava::newRuntimeInteropJava);
        com.sun.jna.Pointer p = lib.runtime_export_state_struct();
        if (p == null) throw new AssertionError("exportStateStruct returned NULL pointer");
        try {
            ExportedState es = new ExportedState(p);
            es.read();
            int len = es.entities.len == null ? 0 : es.entities.len.intValue();

            List<Map<String, String>> actualRows = new ArrayList<>();
            if (len > 0 && es.entities.data != null) {
                com.sun.jna.Pointer[] ptrs = es.entities.data.getPointerArray(0, len);
                for (com.sun.jna.Pointer q : ptrs) {
                    String v = q == null ? "" : q.getString(0);
                    Map<String, String> m = new java.util.HashMap<>();
                    m.put("textMap_name", v);
                    actualRows.add(m);
                }
            }

            boolean[] actualUsed = new boolean[actualRows.size()];
            StringBuilder failureMsg = new StringBuilder();

            for (int eidx = 0; eidx < expectedEntities.size(); eidx++) {
                Entity ep = expectedEntities.get(eidx);
                boolean matched = false;

                for (int aidx = 0; aidx < actualRows.size(); aidx++) {
                    if (actualUsed[aidx]) continue;
                    Map<String, String> row = actualRows.get(aidx);
                    boolean ok = true;

                    if (ep.textMap_name.isPresent() && !row.getOrDefault("textMap_name", "").equals(ep.textMap_name.get()))
                        ok = false;

                    if (ok) {
                        actualUsed[aidx] = true;
                        matched = true;
                        break;
                    }
                }

                if (!matched) {
                    failureMsg.append("Expected entity #").append(eidx).append(" not found in exported state.\n");
                    failureMsg.append("  Expected: textMap_name=").append(ep.textMap_name).append("\n");
                }
            }

            if (failureMsg.length() > 0) {
                failureMsg.insert(0, "Entity assertion failed:\n");
                throw new AssertionError(failureMsg.toString());
            }
        } finally {
            lib.runtime_free_exported_state(p);
        }

        return (ArchiveTestBuilder) this;
    }
}
