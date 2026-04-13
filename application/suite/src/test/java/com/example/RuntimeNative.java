package com.example;

import com.sun.jna.Library;
import com.sun.jna.Native;
import com.sun.jna.Pointer;

/**
 * JNA interface mapping to the native runtime exports.
 */
public interface RuntimeNative extends Library {


    Pointer runtime_process_archive(String path);

    /* Struct-based export: returns an allocated ExportedState* (caller must free with runtime_free_exported_state) */
    Pointer runtime_export_state_struct();

    /* Free an ExportedState* returned by runtime_export_state_struct */
    void runtime_free_exported_state(Pointer ptr);

    void runtime_free_string(Pointer s);

    boolean runtime_export_state(String path);
}
