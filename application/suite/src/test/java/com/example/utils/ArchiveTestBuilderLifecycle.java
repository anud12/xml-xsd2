package com.example.utils;

public interface ArchiveTestBuilderLifecycle extends ArchiveTestBuilderCommon {

    default void cleanup() {
        try {
            ArchiveRunner.cleanup(this.getState());
            CloseProcess.closeProcess(this.getState());
        } catch (Throwable ignored) {
        }
    }
}
