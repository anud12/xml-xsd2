package com.example;

import com.sun.jna.*;
import java.util.*;

/**
 * Example showing how to read the ExportedState struct returned by the runtime via JNA.
 * Run as a simple main to dump exported tables (entities, actions, events, modules, files, created_by).
 */
public class ExportedStateStructExample {
    public static void main(String[] args) {
        RuntimeNative lib = loadLibrary();
        if (lib == null) {
            System.err.println("Failed to load native runtime library");
            return;
        }

        Pointer p = lib.runtime_export_state_struct();
        if (p == null) {
            System.out.println("runtime_export_state_struct returned NULL (no cached state)");
            return;
        }

        try {
            ExportedState es = new ExportedState(p);
            es.read();

            System.out.println("has_data: " + (es.has_data != 0));

            System.out.println("entities: " + readCStringArray(es.entities));
            System.out.println("actions: " + readCStringArray(es.actions));
            System.out.println("events: " + readCStringArray(es.events));
            System.out.println("entity_patterns: " + readCStringArray(es.entity_patterns));

            System.out.println("modules:");
            for (Map<String, String> m : readModules(es.modules)) {
                System.out.println("  " + m);
            }

            System.out.println("files (filename -> size):");
            for (Map<String, String> f : readFiles(es.files)) {
                String fname = f.get("filename");
                String contents = f.get("contents");
                System.out.println("  " + fname + " -> " + (contents == null ? 0 : contents.length()));
            }

            System.out.println("created_by mapping:");
            Map<String, List<String>> cb = readCreatedBy(es.created_by);
            for (Map.Entry<String, List<String>> e : cb.entrySet()) {
                System.out.println("  " + e.getKey() + " -> " + e.getValue());
            }

        } finally {
            lib.runtime_free_exported_state(p);
        }
    }

    // ------- helper: load library with same fallback logic used elsewhere -------
    private static RuntimeNative loadLibrary() {
        String jnaPath = System.getProperty("jna.library.path");
        String javaLibPath = System.getProperty("java.library.path");
        String[] candidates = new String[]{jnaPath, javaLibPath, System.getProperty("user.dir")};
        for (String p : candidates) {
            if (p == null) continue;
            java.io.File dll = new java.io.File(p, "libxml_xsd2.dll");
            if (dll.exists()) {
                try {
                    return (RuntimeNative) Native.load(dll.getAbsolutePath(), RuntimeNative.class);
                } catch (UnsatisfiedLinkError ex) {
                }
            }
            java.io.File so = new java.io.File(p, "libxml_xsd2.so");
            if (so.exists()) {
                try {
                    return (RuntimeNative) Native.load(so.getAbsolutePath(), RuntimeNative.class);
                } catch (UnsatisfiedLinkError ex) {
                }
            }
        }
        // fallback to system search path / JNA heuristics
        return (RuntimeNative) Native.load("libxml_xsd2", RuntimeNative.class);
    }

    // ------- JNA structure mappings (mirror the Rust #[repr(C)] types) -------
    public static class CStringArray extends Structure {
        public NativeLong len;
        public Pointer data; // *mut *mut c_char

        @Override
        protected List<String> getFieldOrder() {
            return Arrays.asList("len", "data");
        }

        public CStringArray() {
            super();
        }

        public CStringArray(Pointer p) {
            super(p);
            read();
        }
    }

    public static class ModuleRow extends Structure {
        public Pointer id;
        public Pointer name;
        public Pointer version;

        @Override
        protected List<String> getFieldOrder() {
            return Arrays.asList("id", "name", "version");
        }

        public ModuleRow() {
            super();
        }

        public ModuleRow(Pointer p) {
            super(p);
            read();
        }
    }

    public static class ModuleArray extends Structure {
        public NativeLong len;
        public Pointer data; // ModuleRow*

        @Override
        protected List<String> getFieldOrder() {
            return Arrays.asList("len", "data");
        }

        public ModuleArray() {
            super();
        }

        public ModuleArray(Pointer p) {
            super(p);
            read();
        }
    }

    public static class FileRow extends Structure {
        public Pointer filename;
        public Pointer contents;

        @Override
        protected List<String> getFieldOrder() {
            return Arrays.asList("filename", "contents");
        }

        public FileRow() {
            super();
        }

        public FileRow(Pointer p) {
            super(p);
            read();
        }
    }

    public static class FileArray extends Structure {
        public NativeLong len;
        public Pointer data; // FileRow*

        @Override
        protected List<String> getFieldOrder() {
            return Arrays.asList("len", "data");
        }

        public FileArray() {
            super();
        }

        public FileArray(Pointer p) {
            super(p);
            read();
        }
    }

    public static class CreatedByRow extends Structure {
        public Pointer key;
        public NativeLong values_len;
        public Pointer values; // *mut *mut c_char

        @Override
        protected List<String> getFieldOrder() {
            return Arrays.asList("key", "values_len", "values");
        }

        public CreatedByRow() {
            super();
        }

        public CreatedByRow(Pointer p) {
            super(p);
            read();
        }
    }

    public static class CreatedByArray extends Structure {
        public NativeLong len;
        public Pointer data; // CreatedByRow*

        @Override
        protected List<String> getFieldOrder() {
            return Arrays.asList("len", "data");
        }

        public CreatedByArray() {
            super();
        }

        public CreatedByArray(Pointer p) {
            super(p);
            read();
        }
    }

    public static class ExportedState extends Structure {
        public CStringArray entities;
        public CStringArray actions;
        public CStringArray events;
        public ModuleArray modules;
        public FileArray files;
        public CStringArray entity_patterns;
        public CreatedByArray created_by;
        public byte has_data; // maps to Rust's bool

        @Override
        protected List<String> getFieldOrder() {
            return Arrays.asList("entities", "actions", "events", "modules", "files", "entity_patterns", "created_by", "has_data");
        }

        public ExportedState() {
            super();
        }

        public ExportedState(Pointer p) {
            super(p);
            read();
        }
    }

    // ------- Readers for the C arrays/struct arrays returned by the runtime -------
    private static List<String> readCStringArray(CStringArray arr) {
        if (arr == null) return Collections.emptyList();
        int len = (int) arr.len.longValue();
        if (len == 0 || arr.data == null) return Collections.emptyList();
        Pointer[] ptrs = arr.data.getPointerArray(0, len);
        List<String> out = new ArrayList<>();
        for (Pointer p : ptrs) {
            out.add(p == null ? null : p.getString(0));
        }
        return out;
    }

    private static List<Map<String, String>> readModules(ModuleArray modules) {
        List<Map<String, String>> out = new ArrayList<>();
        if (modules == null) return out;
        int len = (int) modules.len.longValue();
        if (len == 0 || modules.data == null) return out;
        ModuleRow first = new ModuleRow(modules.data);
        ModuleRow[] rows = (ModuleRow[]) first.toArray(len);
        for (ModuleRow r : rows) {
            String id = r.id == null ? null : r.id.getString(0);
            String name = r.name == null ? null : r.name.getString(0);
            String version = r.version == null ? null : r.version.getString(0);
            Map<String, String> m = new HashMap<>();
            m.put("id", id);
            m.put("name", name);
            m.put("version", version);
            out.add(m);
        }
        return out;
    }

    private static List<Map<String, String>> readFiles(FileArray files) {
        List<Map<String, String>> out = new ArrayList<>();
        if (files == null) return out;
        int len = (int) files.len.longValue();
        if (len == 0 || files.data == null) return out;
        FileRow first = new FileRow(files.data);
        FileRow[] rows = (FileRow[]) first.toArray(len);
        for (FileRow r : rows) {
            String fname = r.filename == null ? null : r.filename.getString(0);
            String contents = r.contents == null ? null : r.contents.getString(0);
            Map<String, String> m = new HashMap<>();
            m.put("filename", fname);
            m.put("contents", contents);
            out.add(m);
        }
        return out;
    }

    private static Map<String, List<String>> readCreatedBy(CreatedByArray cba) {
        Map<String, List<String>> out = new LinkedHashMap<>();
        if (cba == null) return out;
        int len = (int) cba.len.longValue();
        if (len == 0 || cba.data == null) return out;
        CreatedByRow first = new CreatedByRow(cba.data);
        CreatedByRow[] rows = (CreatedByRow[]) first.toArray(len);
        for (CreatedByRow r : rows) {
            String key = r.key == null ? null : r.key.getString(0);
            int vlen = (int) r.values_len.longValue();
            List<String> vals = new ArrayList<>();
            if (vlen > 0 && r.values != null) {
                Pointer[] ptrs = r.values.getPointerArray(0, vlen);
                for (Pointer p : ptrs) vals.add(p == null ? null : p.getString(0));
            }
            out.put(key, vals);
        }
        return out;
    }

    @org.junit.jupiter.api.Test
    public void runExportedStateExample() {
        // Reuse the main method to execute the example logic inside a JUnit test so surefire can run it.
        main(new String[0]);
    }
}