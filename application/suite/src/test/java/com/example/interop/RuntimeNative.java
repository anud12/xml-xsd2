package com.example.interop;

import com.sun.jna.Library;
import com.sun.jna.Native;
import com.sun.jna.Pointer;

/**
 * JNA interface mapping to the native runtime exports.
 */
public interface RuntimeNative extends Library {

    Pointer runtime_process_archive(String path);

    Pointer runtime_debug_load_base64(String payload);

    void runtime_debug_iterate(int times);

    boolean runtime_debug_simulate_action(String actionName);

    void runtime_debug_shutdown();

    Pointer runtime_export_state_struct();

    void runtime_free_exported_state(Pointer ptr);

    void runtime_free_string(Pointer s);

    boolean runtime_export_state(String path);

    void runtime_clear_state();
}
