package com.example.interop;

import com.sun.jna.Pointer;

import static com.sun.jna.Native.*;

/**
 * Small Java wrapper that uses the RuntimeNative JNA interface for tests.
 */
public class RuntimeInteropJava {
    private final RuntimeNative LIB;

    public RuntimeInteropJava() {
        RuntimeNative lib = null;
        String jnaPath = System.getProperty("jna.library.path");
        String javaLibPath = System.getProperty("java.library.path");
        String[] candidates = new String[] {jnaPath, javaLibPath, System.getProperty("user.dir")};
        for (String p : candidates) {
            if (p == null) continue;
            java.io.File dll = new java.io.File(p, "libxml_xsd2.dll");
            if (dll.exists()) {
                try {
                    lib = load(dll.getAbsolutePath(), RuntimeNative.class);
                    break;
                } catch (UnsatisfiedLinkError e) { }
            }
            java.io.File so = new java.io.File(p, "libxml_xsd2.so");
            if (so.exists()) {
                try {
                    lib = load(so.getAbsolutePath(), RuntimeNative.class);
                    break;
                } catch (UnsatisfiedLinkError e) { }
            }
        }
        if (lib == null) {
            try {
                lib = load("libxml_xsd2", RuntimeNative.class);
            } catch (UnsatisfiedLinkError e1) {
                try {
                    lib = load("xml_xsd2", RuntimeNative.class);
                } catch (UnsatisfiedLinkError e2) {
                    throw e2;
                }
            }
        }
        LIB = lib;
    }

    public String processArchive(String path) {
        Pointer p = LIB.runtime_process_archive(path);
        if (p == null) return null;
        try {
            String s = p.getString(0);
            return s;
        } finally {
            LIB.runtime_free_string(p);
        }
    }

    public String debugLoadBase64(String payloadB64) {
        Pointer p = LIB.runtime_debug_load_base64(payloadB64);
        if (p == null) return null;
        try {
            return p.getString(0);
        } finally {
            LIB.runtime_free_string(p);
        }
    }

    public void debugIterate(int times) {
        LIB.runtime_debug_iterate(times);
    }

    public boolean debugSimulateAction(String actionName) {
        return LIB.runtime_debug_simulate_action(actionName);
    }

    public void debugShutdown() {
        LIB.runtime_debug_shutdown();
    }

    public boolean exportState(String path) {
        return LIB.runtime_export_state(path);
    }

    public com.sun.jna.Pointer exportStateStruct() {
        return LIB.runtime_export_state_struct();
    }

    public void freeExportedState(com.sun.jna.Pointer ptr) {
        LIB.runtime_free_exported_state(ptr);
    }
}
