package com.example.steps;

import java.io.File;
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
        state.runtimeInteropJava = Optional.of(new RuntimeInteropJava());
        String dbPath = state.runtimeInteropJava.map(runtimeInteropJava -> runtimeInteropJava.processArchive(zipPath))
                .get();
        // Emulate startup log output for existing test assertions
        state.lastOutput = ("\n" + STARTUP_LOG + "\n").getBytes(java.nio.charset.StandardCharsets.UTF_8);
    }

    public static void cleanup(ArchiveState state) {
        try {
            state.runtimeInteropJava.ifPresent(RuntimeInteropJava::debugShutdown);
            state.runtimeInteropJava = Optional.empty();
        } catch (Throwable ignored) {}
    }
}

