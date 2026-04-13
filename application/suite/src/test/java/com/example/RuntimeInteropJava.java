package com.example;

import com.sun.jna.Pointer;

/**
 * Small Java wrapper that uses the RuntimeNative JNA interface for tests.
 */
public class RuntimeInteropJava {
    private static final RuntimeNative LIB;
    static {
        RuntimeNative lib = null;
        String jnaPath = System.getProperty("jna.library.path");
        String javaLibPath = System.getProperty("java.library.path");
        String[] candidates = new String[] {jnaPath, javaLibPath, System.getProperty("user.dir")};
        for (String p : candidates) {
            if (p == null) continue;
            java.io.File dll = new java.io.File(p, "libxml_xsd2.dll");
            if (dll.exists()) {
                try {
                    lib = (RuntimeNative) com.sun.jna.Native.load(dll.getAbsolutePath(), RuntimeNative.class);
                    break;
                } catch (UnsatisfiedLinkError e) { }
            }
            java.io.File so = new java.io.File(p, "libxml_xsd2.so");
            if (so.exists()) {
                try {
                    lib = (RuntimeNative) com.sun.jna.Native.load(so.getAbsolutePath(), RuntimeNative.class);
                    break;
                } catch (UnsatisfiedLinkError e) { }
            }
        }
        if (lib == null) {
            try {
                lib = (RuntimeNative) com.sun.jna.Native.load("libxml_xsd2", RuntimeNative.class);
            } catch (UnsatisfiedLinkError e1) {
                try {
                    lib = (RuntimeNative) com.sun.jna.Native.load("xml_xsd2", RuntimeNative.class);
                } catch (UnsatisfiedLinkError e2) {
                    throw e2;
                }
            }
        }
        LIB = lib;
    }

    public static String processArchive(String path) {
        Pointer p = LIB.runtime_process_archive(path);
        if (p == null) return null;
        try {
            String s = p.getString(0);
            return s;
        } finally {
            LIB.runtime_free_string(p);
        }
    }

    public static boolean exportState(String path) {
        return LIB.runtime_export_state(path);
    }
}
