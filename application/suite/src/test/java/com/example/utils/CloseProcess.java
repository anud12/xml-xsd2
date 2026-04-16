package com.example.utils;

import com.example.interop.RuntimeInteropJava;

public class CloseProcess {
    public static void closeProcess(ArchiveState state) {
        try {
            state.runtimeInteropJava.ifPresent(RuntimeInteropJava::runtime_debug_shutdown);
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }
}
