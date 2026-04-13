package com.example;

import com.example.steps.ArchiveState;
import com.example.steps.ArchiveRunner;
import com.example.steps.StateAssertions;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.File;

public class ExportedStateStructExample {
    @Test
    public void runExportedStateExample() throws Exception {
        ArchiveState state = new ArchiveState();
        try {
            ArchiveRunner.runApplicationDebugThreadedWithArchive(state);
            File out = StateAssertions.extractFileFromProcess(state);
            System.out.println("Exported SQLite written to: " + out.getAbsolutePath());
            assertTrue(out.exists(), "Export file should exist");
        } finally {
            ArchiveRunner.cleanup(state);
        }
    }
}
