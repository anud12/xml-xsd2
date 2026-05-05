package com.example.utils.archiveTestBuilder.assertions;

import com.example.tests.interop.RuntimeInteropJava;
import com.example.tests.interop.exportedState.ExportedState;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilder;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilderCommon;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;

public interface AssertExportedEvents extends ArchiveTestBuilderCommon {

    class Event {
        private Optional<String> name = Optional.empty();

        public Event setName(String string) {
            name = Optional.of(string);
            return this;
        }
    }

    default ArchiveTestBuilder assertExportedEvents(List<Event> expectedEvents) throws Exception {
        RuntimeInteropJava lib = this.getState().runtimeInteropJava.orElseGet(RuntimeInteropJava::newRuntimeInteropJava);
        com.sun.jna.Pointer p = lib.runtime_export_state_struct();
        if (p == null) throw new AssertionError("exportStateStruct returned NULL pointer");
        try {
            ExportedState es = new ExportedState(p);
            es.read();
            int len = es.events.len == null ? 0 : es.events.len.intValue();

            List<Map<String, String>> actualRows = new ArrayList<>();
            if (len > 0 && es.events.data != null) {
                com.sun.jna.Pointer[] ptrs = es.events.data.getPointerArray(0, len);
                for (com.sun.jna.Pointer q : ptrs) {
                    String v = q == null ? "" : q.getString(0);
                    Map<String, String> m = new java.util.HashMap<>();
                    m.put("name", v);
                    actualRows.add(m);
                }
            }

            boolean[] actualUsed = new boolean[actualRows.size()];
            StringBuilder failureMsg = new StringBuilder();

            for (int eidx = 0; eidx < expectedEvents.size(); eidx++) {
                Event ep = expectedEvents.get(eidx);
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
                    failureMsg.append("Expected event #").append(eidx).append(" not found in exported state.\n");
                    failureMsg.append("  Expected: name=").append(ep.name).append("\n");
                }
            }

            if (failureMsg.length() > 0) {
                failureMsg.insert(0, "Event assertion failed:\n");
                throw new AssertionError(failureMsg.toString());
            }
        } finally {
            lib.runtime_free_exported_state(p);
        }

        return (ArchiveTestBuilder) this;
    }
}
