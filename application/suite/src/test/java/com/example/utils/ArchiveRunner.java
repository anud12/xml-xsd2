package com.example.utils;

import java.util.Optional;

import com.example.interop.RuntimeInteropJava;

/**
 * ArchiveRunner adapted to use JNA FFI instead of spawning a separate runtime process.
 * Many existing test steps expect ArchiveRunner.runApplicationDebugThreadedWithArchive(state)
 * to populate ArchiveState.lastOutput with a startup log; that is preserved here.
 */
public class ArchiveRunner {

    public static final String STARTUP_LOG = "Runtime launched";
    public static final String DEBUG_DELIMITED = "_-_";

    public static void runApplicationDebugThreadedWithArchive(ArchiveState state) throws Exception {
        // Use JNA FFI to process the archive and populate runtime caches.
        String zipPath = state.archive.file().getAbsolutePath();
        var runtimeInteropJava = RuntimeInteropJava.newRuntimeInteropJava();
        state.runtimeInteropJava = Optional.of(runtimeInteropJava);
        runtimeInteropJava.runtime_clear_state();
        runtimeInteropJava.runtime_process_archive(zipPath);
        state.lastOutput = ("\n" + STARTUP_LOG + "\n").getBytes(java.nio.charset.StandardCharsets.UTF_8);
    }

    public static void cleanup(ArchiveState state) {
        try {
            state.runtimeInteropJava.ifPresent(RuntimeInteropJava::runtime_debug_shutdown);
            state.runtimeInteropJava = Optional.empty();
        } catch (Throwable ignored) {}
    }
}

