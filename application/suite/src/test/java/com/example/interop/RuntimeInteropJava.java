package com.example.interop;

import com.sun.jna.Library;
import com.sun.jna.Pointer;

import static com.sun.jna.Native.load;

/**
 * Small Java wrapper that uses the RuntimeNative JNA interface for tests.
 */
public interface RuntimeInteropJava extends Library {

    public static RuntimeInteropJava newRuntimeInteropJava() {
        RuntimeInteropJava lib = null;
        String jnaPath = System.getProperty("jna.library.path");
        String javaLibPath = System.getProperty("java.library.path");
        String[] candidates = new String[]{jnaPath, javaLibPath, System.getProperty("user.dir")};
        for (String p : candidates) {
            if (p == null) continue;
            java.io.File dll = new java.io.File(p, "libxml_xsd2.dll");
            if (dll.exists()) {
                try {
                    lib = load(dll.getAbsolutePath(), RuntimeInteropJava.class);
                    break;
                } catch (UnsatisfiedLinkError e) {
                }
            }
            java.io.File so = new java.io.File(p, "libxml_xsd2.so");
            if (so.exists()) {
                try {
                    lib = load(so.getAbsolutePath(), RuntimeInteropJava.class);
                    break;
                } catch (UnsatisfiedLinkError e) {
                }
            }
        }
        if (lib == null) {
            try {
                lib = load("libxml_xsd2", RuntimeInteropJava.class);
            } catch (UnsatisfiedLinkError e1) {
                try {
                    lib = load("xml_xsd2", RuntimeInteropJava.class);
                } catch (UnsatisfiedLinkError e2) {
                    throw e2;
                }
            }
        }
        return lib;
    }

//    Pointer runtime_process_archive(String path);

    Pointer runtime_debug_load_base64(String payload);

    void runtime_debug_iterate(int times);

    boolean trigger_action(String actionName);

    void runtime_debug_shutdown();

    Pointer runtime_export_state_struct();

    void runtime_free_exported_state(Pointer ptr);

    boolean runtime_export_state(String path);

    void runtime_clear_state();

    // Callback interface for passing Java lambdas to native DLL that accept a single String argument
    interface MyCallback extends com.sun.jna.Callback {
        void invoke(String s);
    }

    // Register a callback in the native runtime accepting a single string
    void register_logger(MyCallback cb);

    /**
     * Native entrypoint: load an archive from raw bytes.
     * Exposes the native function that accepts a byte buffer
     */
    boolean runtime_load_archive(byte[] data, int length);


}
