package com.example.utils.archiveTestBuilder.assertions;

import com.google.gson.Gson;
import com.google.gson.reflect.TypeToken;
import com.sun.jna.Pointer;
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

        public Entity withTextMapName(String string) {
            textMap_name = Optional.of(string);
            return this;
        }

        public Entity withEmptyTextMap() {
            if (textMap.isEmpty()) {
                textMap = Optional.of(new HashMap<>());
            }
            return this;
        }

        public Entity withTextMapValue(String key, String value) {
            if (textMap.isEmpty()) {
                textMap = Optional.of(new HashMap<>());
            }
            textMap.ifPresent(hm -> hm.put(key, value));
            return this;
        }

        public Entity withEmptyNumberMap() {
            if (numberMap.isEmpty()) {
                numberMap = Optional.of(new HashMap<>());
            }
            return this;
        }

        public Entity withNumberMapValue(String key, Long value) {
            if (numberMap.isEmpty()) {
                numberMap = Optional.of(new HashMap<>());
            }
            numberMap.ifPresent(hm -> hm.put(key, value));
            return this;
        }
    }

    default ArchiveTestBuilder assertExportedEntities(List<Entity> expectedEntities) throws Exception {
        RuntimeInteropJava lib = this.getState().runtimeInteropJava.orElseGet(RuntimeInteropJava::newRuntimeInteropJava);
        Pointer p = lib.runtime_export_state_struct();
        if (p == null) throw new AssertionError("exportStateStruct returned NULL pointer");
        try {
            ExportedState es = new ExportedState(p);
            es.read();

            // Get entity names from entities array
            int len = es.entities.len == null ? 0 : es.entities.len.intValue();
            List<String> actualNames = new ArrayList<>();
            if (len > 0 && es.entities.data != null) {
                Pointer[] ptrs = es.entities.data.getPointerArray(0, len);
                for (Pointer q : ptrs) {
                    String v = q == null ? "" : q.getString(0);
                    actualNames.add(v);
                }
            }

            // Get entity map data as JSON from Rust side - avoids complex pointer chasing through nested C structs
            Map<String, Map<String, Object>> entityMapData = new HashMap<>();
            Pointer jsonPtr = lib.runtime_get_entity_maps_json();
            if (jsonPtr != null) {
                try {
                    String jsonString = jsonPtr.getString(0);
                    System.err.println("[DEBUG assertExportedEntities] JSON: " + jsonString);
                    entityMapData = parseJson(jsonString);
                } finally {
                    lib.runtime_free_entity_maps_json(jsonPtr);
                }
            }

            boolean[] actualUsed = new boolean[actualNames.size()];
            StringBuilder failureMsg = new StringBuilder();

            for (int eidx = 0; eidx < expectedEntities.size(); eidx++) {
                Entity ep = expectedEntities.get(eidx);
                boolean matched = false;

                for (int aidx = 0; aidx < actualNames.size(); aidx++) {
                    if (actualUsed[aidx]) continue;
                    String name = actualNames.get(aidx);
                    boolean ok = true;

                    if (ep.textMap_name.isPresent() && !name.equals(ep.textMap_name.get()))
                        ok = false;

                    if (ok) {
                        Map<String, Object> edata = entityMapData.get(name);

                        // Check textMap
                        if (ep.textMap.isPresent()) {
                            HashMap<String, String> expectedTextMap = ep.textMap.get();
                            if (expectedTextMap.isEmpty()) {
                                if (edata != null && edata.containsKey("textMap")) ok = false;
                            } else if (edata == null) {
                                ok = false;
                            } else {
                                Map<String, String> actualTextMap = (Map<String, String>) edata.get("textMap");
                                for (Map.Entry<String, String> entry : expectedTextMap.entrySet()) {
                                    if (!actualTextMap.containsKey(entry.getKey()) || !actualTextMap.get(entry.getKey()).equals(entry.getValue())) {
                                        ok = false; break;
                                    }
                                }
                            }
                        }

                        // Check numberMap
                        if (ok && ep.numberMap.isPresent()) {
                            HashMap<String, Long> expectedNumberMap = ep.numberMap.get();
                            if (expectedNumberMap.isEmpty()) {
                                Map<String, Object> e2 = edata == null ? new HashMap<>() : edata;
                                if (e2.containsKey("numberMap")) ok = false;
                            } else if (edata == null) {
                                ok = false;
                            } else {
                                Map<String, Number> actualNumMap = (Map<String, Number>) edata.get("numberMap");
                                for (Map.Entry<String, Long> entry : expectedNumberMap.entrySet()) {
                                    if (!actualNumMap.containsKey(entry.getKey())) { ok = false; break; }
                                    long actualVal = ((Number) actualNumMap.get(entry.getKey())).longValue();
                                    if (!Long.valueOf(actualVal).equals(entry.getValue())) { ok = false; break; }
                                }
                            }
                        }
                    }

                    if (ok) {
                        actualUsed[aidx] = true;
                        matched = true;
                        break;
                    }
                }

                if (!matched) {
                    failureMsg.append("Expected entity #").append(eidx).append(" not found in exported state.\n");
                    failureMsg.append("  Expected: textMap_name=").append(ep.textMap_name);
                    failureMsg.append(", textMap=").append(ep.textMap.orElseGet(HashMap::new));
                    failureMsg.append(", numberMap=").append(ep.numberMap.orElseGet(HashMap::new));
                    failureMsg.append("\n");
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

    /** Parse JSON string using Gson */
    @SuppressWarnings("unchecked")
    private static Map<String, Map<String, Object>> parseJson(String jsonStr) {
        Gson gson = new Gson();
        java.lang.reflect.Type type = new TypeToken<Map<String, Map<String, Object>>>(){}.getType();
        return gson.fromJson(jsonStr, type);
    }
}
