package com.example;

import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;
import java.io.File;

/**
 * End-to-end smoke test that calls the native runtime via JNA.
 */
public class NativeInteropTest {
    @Test
    public void testRuntimeProcessArchive() {
        String tmpDir = System.getProperty("java.io.tmpdir");
        String zipPath = tmpDir + File.separator + "suite-module-test.zip";
        File zf = new File(zipPath);
        if (zf.exists()) zf.delete();

        String dbPath = RuntimeInteropJava.processArchive(zipPath);
        assertNotNull(dbPath, "DB path should not be null");

        File dbFile = new File(dbPath);
        if (!dbFile.exists()) {
            dbFile = new File(System.getProperty("user.dir"), dbPath);
        }
        assertTrue(dbFile.exists(), "Persisted DB file should exist: " + dbFile.getAbsolutePath());

        // Export state to a known location and assert file exists
        String out = System.getProperty("user.dir") + File.separator + "test-export.db";
        boolean exported = RuntimeInteropJava.exportState(out);
        assertTrue(exported, "Export state returned true");
        File exportedFile = new File(out);
        assertTrue(exportedFile.exists(), "Exported DB should exist: " + exportedFile.getAbsolutePath());
    }
}
