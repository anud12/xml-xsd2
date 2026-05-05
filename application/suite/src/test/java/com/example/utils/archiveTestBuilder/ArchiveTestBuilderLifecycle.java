package com.example.utils.archiveTestBuilder;

import com.example.utils.ArchiveRunner;
import com.example.utils.CloseProcess;

public interface ArchiveTestBuilderLifecycle extends ArchiveTestBuilderCommon {

    default void cleanup() {
        try {
            ArchiveRunner.cleanup(this.getState());
            CloseProcess.closeProcess(this.getState());
        } catch (Throwable ignored) {
        }
    }
}
