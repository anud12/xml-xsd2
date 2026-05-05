package com.example.utils.archiveTestBuilder.assertions;

import com.example.tests.interop.RuntimeInteropJava;
import com.example.tests.interop.exportedState.ExportedState;
import com.example.tests.interop.exportedState.ModuleRow;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilder;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilderCommon;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;

public interface AssertExportedModules extends ArchiveTestBuilderCommon {

    class Module {
        private Optional<String> id = Optional.empty();
        private Optional<String> name = Optional.empty();
        private Optional<String> version = Optional.empty();

        public Module setId(String string) {
            id = Optional.of(string);
            return this;
        }
        public Module setName(String string) {
            name = Optional.of(string);
            return this;
        }
        public Module setVersion(String string) {
            version = Optional.of(string);
            return this;
        }
    }

    default ArchiveTestBuilder assertExportedModules(List<Module> expectedModules) throws Exception {
        RuntimeInteropJava lib = this.getState().runtimeInteropJava.orElseGet(RuntimeInteropJava::newRuntimeInteropJava);
        com.sun.jna.Pointer p = lib.runtime_export_state_struct();
        if (p == null) throw new AssertionError("exportStateStruct returned NULL pointer");
        try {
            ExportedState es = new ExportedState(p);
            es.read();
            int len = es.modules.len == null ? 0 : es.modules.len.intValue();

            List<Map<String, String>> actualRows = new ArrayList<>();
            if (len > 0 && es.modules.data != null) {
                long structSize = new ModuleRow().size();
                for (int i = 0; i < len; i++) {
                    ModuleRow mr = new ModuleRow(es.modules.data.share(i * structSize));
                    mr.read();
                    String idVal = mr.id == null ? "" : mr.id.getString(0);
                    String nameVal = mr.name == null ? "" : mr.name.getString(0);
                    String versionVal = mr.version == null ? "" : mr.version.getString(0);
                    Map<String, String> m = new java.util.HashMap<>();
                    m.put("id", idVal);
                    m.put("name", nameVal);
                    m.put("version", versionVal);
                    actualRows.add(m);
                }
            }

            boolean[] actualUsed = new boolean[actualRows.size()];
            StringBuilder failureMsg = new StringBuilder();

            for (int eidx = 0; eidx < expectedModules.size(); eidx++) {
                Module ep = expectedModules.get(eidx);
                boolean matched = false;

                for (int aidx = 0; aidx < actualRows.size(); aidx++) {
                    if (actualUsed[aidx]) continue;
                    Map<String, String> row = actualRows.get(aidx);
                    boolean ok = true;

                    if (ep.id.isPresent() && !row.getOrDefault("id", "").equals(ep.id.get())) ok = false;
                    if (ok && ep.name.isPresent() && !row.getOrDefault("name", "").equals(ep.name.get())) ok = false;
                    if (ok && ep.version.isPresent() && !row.getOrDefault("version", "").equals(ep.version.get())) ok = false;

                    if (ok) {
                        actualUsed[aidx] = true;
                        matched = true;
                        break;
                    }
                }

                if (!matched) {
                    failureMsg.append("Expected module #").append(eidx).append(" not found in exported state.\n");
                    failureMsg.append("  Expected: id=").append(ep.id).append(", name=").append(ep.name).append(", version=").append(ep.version).append("\n");
                }
            }

            if (failureMsg.length() > 0) {
                failureMsg.insert(0, "Module assertion failed:\n");
                throw new AssertionError(failureMsg.toString());
            }
        } finally {
            lib.runtime_free_exported_state(p);
        }

        return (ArchiveTestBuilder) this;
    }
}
