package com.example.utils.archiveTestBuilder.assertions;

import com.example.tests.interop.RuntimeInteropJava;
import com.example.tests.interop.exportedState.ExportedState;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilder;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilderCommon;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;

public interface AssertExportedActions extends ArchiveTestBuilderCommon {

    class Action {
        private Optional<String> name = Optional.empty();

        public Action setName(String string) {
            name = Optional.of(string);
            return this;
        }
    }

    default ArchiveTestBuilder assertExportedActions(List<Action> expectedActions) throws Exception {
        RuntimeInteropJava lib = this.getState().runtimeInteropJava.orElseGet(RuntimeInteropJava::newRuntimeInteropJava);
        com.sun.jna.Pointer p = lib.runtime_export_state_struct();
        if (p == null) throw new AssertionError("exportStateStruct returned NULL pointer");
        try {
            ExportedState es = new ExportedState(p);
            es.read();
            int len = es.actions.len == null ? 0 : es.actions.len.intValue();

            List<Map<String, String>> actualRows = new ArrayList<>();
            if (len > 0 && es.actions.data != null) {
                com.sun.jna.Pointer[] ptrs = es.actions.data.getPointerArray(0, len);
                for (com.sun.jna.Pointer q : ptrs) {
                    String v = q == null ? "" : q.getString(0);
                    Map<String, String> m = new java.util.HashMap<>();
                    m.put("name", v);
                    actualRows.add(m);
                }
            }

            boolean[] actualUsed = new boolean[actualRows.size()];
            StringBuilder failureMsg = new StringBuilder();

            for (int eidx = 0; eidx < expectedActions.size(); eidx++) {
                Action ep = expectedActions.get(eidx);
                boolean matched = false;

                for (int aidx = 0; aidx < actualRows.size(); aidx++) {
                    if (actualUsed[aidx]) continue;
                    Map<String, String> row = actualRows.get(aidx);
                    boolean ok = true;

                    if (ep.name.isPresent() && !row.getOrDefault("name", "").equals(ep.name.get())) ok = false;

                    if (ok) {
                        actualUsed[aidx] = true;
                        matched = true;
                        break;
                    }
                }

                if (!matched) {
                    failureMsg.append("Expected action #").append(eidx).append(" not found in exported state.\n");
                    failureMsg.append("  Expected: name=").append(ep.name).append("\n");
                }
            }

            if (failureMsg.length() > 0) {
                failureMsg.insert(0, "Action assertion failed:\n");
                throw new AssertionError(failureMsg.toString());
            }
        } finally {
            lib.runtime_free_exported_state(p);
        }

        return (ArchiveTestBuilder) this;
    }
}
