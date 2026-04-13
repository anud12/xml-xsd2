package com.example;

import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;
import java.io.File;

import com.example.steps.ArchiveState;
import com.example.steps.ArchiveRunner;
import com.example.steps.StateAssertions;

public class NativeInteropTest {
    @Test
    public void testRuntimeProcessArchive() throws Exception {
        ArchiveState state = new ArchiveState();
        try {
            // Launch the runtime in debug mode with the test archive and interact via its stdio debug commands
            ArchiveRunner.runApplicationDebugThreadedWithArchive(state);

            // Export state to a known location and assert file exists
            String out = System.getProperty("java.io.tmpdir") + File.separator + "test-export.db";
            File exportedFile = StateAssertions.extractFileFromProcess(state, new File(out));
            assertTrue(exportedFile.exists(), "Exported DB should exist: " + exportedFile.getAbsolutePath());
        } finally {
            ArchiveRunner.cleanup(state);
        }
    }
}
