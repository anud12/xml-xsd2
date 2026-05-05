package com.example.utils.archiveTestBuilder.assertions;

import com.example.tests.interop.RuntimeInteropJava;
import com.example.tests.interop.exportedState.ExportedState;
import com.example.tests.interop.exportedState.PanelFfi;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilder;
import com.example.utils.archiveTestBuilder.ArchiveTestBuilderCommon;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;

public interface AssertExportedPanels extends ArchiveTestBuilderCommon {

    class Panel {
        private Optional<String> id = Optional.empty();
        private Optional<Integer> offsetTop = Optional.empty();
        private Optional<Integer> offsetLeft = Optional.empty();
        private Optional<Integer> offsetRight = Optional.empty();
        private Optional<Integer> offsetBottom = Optional.empty();

        public Panel setId(String string) {
            id = Optional.of(string);
            return this;
        }
        public Panel setOffsetTop(int i) {
            offsetTop = Optional.of(i);
            return this;
        }
        public Panel setOffsetLeft(int i) {
            offsetLeft = Optional.of(i);
            return this;
        }
        public Panel setOffsetRight(int i) {
            offsetRight = Optional.of(i);
            return this;
        }
        public Panel setOffsetBottom(int i) {
            offsetBottom = Optional.of(i);
            return this;
        }
    }

    default ArchiveTestBuilder assertExportedPanels(List<Panel> expectedPanels) throws Exception {
        RuntimeInteropJava lib = this.getState().runtimeInteropJava.orElseGet(RuntimeInteropJava::newRuntimeInteropJava);
        com.sun.jna.Pointer p = lib.runtime_export_state_struct();
        if (p == null) throw new AssertionError("exportStateStruct returned NULL pointer");
        try {
            ExportedState es = new ExportedState(p);
            es.read();
            int len = es.panels.len == null ? 0 : es.panels.len.intValue();

            List<Map<String, String>> actualRows = new ArrayList<>();
            if (len > 0 && es.panels.data != null) {
                PanelFfi template = new PanelFfi();
                int structSize = template.size();
                for (int i = 0; i < len; i++) {
                    PanelFfi panel =
                            new PanelFfi(es.panels.data.share((long) i * structSize));
                    String id = panel.id == null ? "" : panel.id.getString(0);
                    Map<String, String> m = new java.util.HashMap<>();
                    m.put("id", id);
                    m.put("offset__top", String.valueOf(panel.offset.top));
                    m.put("offset__left", String.valueOf(panel.offset.left));
                    m.put("offset__right", String.valueOf(panel.offset.right));
                    m.put("offset__bottom", String.valueOf(panel.offset.bottom));
                    actualRows.add(m);
                }
            }

            boolean[] actualUsed = new boolean[actualRows.size()];
            StringBuilder failureMsg = new StringBuilder();

            for (int eidx = 0; eidx < expectedPanels.size(); eidx++) {
                Panel ep = expectedPanels.get(eidx);
                boolean matched = false;

                for (int aidx = 0; aidx < actualRows.size(); aidx++) {
                    if (actualUsed[aidx]) continue;
                    Map<String, String> row = actualRows.get(aidx);
                    boolean ok = true;

                    if (ep.id.isPresent() && !row.getOrDefault("id", "").equals(ep.id.get())) ok = false;
                    if (ok && ep.offsetTop.isPresent() && !String.valueOf(row.getOrDefault("offset__top", "")).equals(String.valueOf(ep.offsetTop.get()))) ok = false;
                    if (ok && ep.offsetLeft.isPresent() && !String.valueOf(row.getOrDefault("offset__left", "")).equals(String.valueOf(ep.offsetLeft.get()))) ok = false;
                    if (ok && ep.offsetRight.isPresent() && !String.valueOf(row.getOrDefault("offset__right", "")).equals(String.valueOf(ep.offsetRight.get()))) ok = false;
                    if (ok && ep.offsetBottom.isPresent() && !String.valueOf(row.getOrDefault("offset__bottom", "")).equals(String.valueOf(ep.offsetBottom.get()))) ok = false;

                    if (ok) {
                        actualUsed[aidx] = true;
                        matched = true;
                        break;
                    }
                }

                if (!matched) {
                    failureMsg.append("Expected panel #").append(eidx).append(" not found in exported state.\n");
                    failureMsg.append("  Expected: id=").append(ep.id).append(", top=").append(ep.offsetTop).append(", left=").append(ep.offsetLeft).append(", right=").append(ep.offsetRight).append(", bottom=").append(ep.offsetBottom).append("\n");
                }
            }

            if (failureMsg.length() > 0) {
                failureMsg.insert(0, "Panel assertion failed:\n");
                throw new AssertionError(failureMsg.toString());
            }
        } finally {
            lib.runtime_free_exported_state(p);
        }

        return (ArchiveTestBuilder) this;
    }
}
