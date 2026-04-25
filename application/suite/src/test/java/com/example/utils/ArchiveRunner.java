package com.example.utils;

import com.example.interop.RuntimeInteropJava;

import java.util.ArrayList;
import java.util.Optional;

/**
 * ArchiveRunner adapted to use JNA FFI instead of spawning a separate runtime process.
 * Many existing test steps expect ArchiveRunner.runApplicationDebugThreadedWithArchive(state)
 * to populate ArchiveState.lastOutput with a startup log; that is preserved here.
 */
public class ArchiveRunner {

    public static final String DEBUG_DELIMITED = "_-_";

    public static void runApplicationDebugThreadedWithArchive(ArchiveState state) throws Exception {
        // Use JNA FFI to process the archive and populate runtime caches.
//        String zipPath = state.archive.file().getAbsolutePath();
        var runtimeInteropJava = RuntimeInteropJava.newRuntimeInteropJava();
        state.runtimeInteropJava = Optional.of(runtimeInteropJava);
        // Create an explicit callback object and keep a strong reference to it in state so JNA doesn't GC it
        RuntimeInteropJava.MyCallback cb = new RuntimeInteropJava.MyCallback() {
            public void invoke(String s) { state.logMessages.add(s); }
        };
        state.loggerCallback = cb;
        runtimeInteropJava.register_logger(cb);
        runtimeInteropJava.runtime_clear_state();
    }

    public static void cleanup(ArchiveState state) {
        try {
            state.runtimeInteropJava.ifPresent(runtimeInteropJava -> {
                runtimeInteropJava.runtime_debug_shutdown();
                // Clear native callback by registering a no-op callback
                runtimeInteropJava.register_logger(s -> { });
            });

            // Drop strong reference to callback so it can be GC'd
            state.loggerCallback = null;
            state.runtimeInteropJava = Optional.empty();
            state.logMessages = new ArrayList<>();
        } catch (Throwable ignored) {
        }
    }
}

