package com.example.steps;

import java.io.*;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.List;

public class ArchiveRunner {

    public static final String STARTUP_LOG = "Runtime launched";
    public static final String DEBUG_DELIMITED = "_-_";

    public static void runApplicationDebugThreadedWithArchive(ArchiveState state) throws IOException, InterruptedException {
        runApplicationDebugThreaded(state, List.of(
                "--stdioDebugWithDelimiterWrap=" + DEBUG_DELIMITED,
                state.archive.file().toPath().toAbsolutePath().toString()
        ));
        ArchiveAssertions.waitUntilLogLineContainsRegex(state, STARTUP_LOG);
    }

    private static void runApplicationDebugThreaded(ArchiveState state, List<String> runArguments) throws IOException, InterruptedException {
        String runtimeDir = Paths.get("..", "runtime").toAbsolutePath().normalize().toString();
        String exe = System.getProperty("os.name").toLowerCase().contains("win") ? "target\\release\\xml-xsd2.exe" : "target/release/xml-xsd2";
        ProcessBuilder build = new ProcessBuilder("cargo", "build", "--release");
        build.inheritIO();
        build.directory(new File(runtimeDir));
        Process buildProcess = build.start();
        int buildExit = buildProcess.waitFor();
        if (buildExit != 0) throw new IOException("Failed to build runtime app");

        File exeFile = new File(runtimeDir, exe);
        if (!exeFile.exists()) throw new IOException("Expected binary not found: " + exeFile.getAbsolutePath());
        var command = new ArrayList<String>();
        command.add(exeFile.getAbsolutePath());
        command.addAll(runArguments);
        ProcessBuilder run = new ProcessBuilder(command);
        run.directory(new File(runtimeDir));
        run.redirectErrorStream(false);
        state.shouldStop = false;
        state.runProcess = run.start();
        state.runThread = new Thread(() -> {
            try (InputStream in = state.runProcess.getInputStream();
                 OutputStream err = OutputStream.nullOutputStream()) {
                new Thread(() -> { try { state.runProcess.getErrorStream().transferTo(err); } catch (Exception ignored) {} }).start();
                ByteArrayOutputStream buffer = new ByteArrayOutputStream();
                byte[] data = new byte[4096];
                int nRead;
                while (!state.shouldStop && (nRead = in.read(data, 0, data.length)) != -1) {
                    buffer.write(data, 0, nRead);
                    state.lastOutput = buffer.toByteArray();
                }
            } catch (IOException e) {
                // Optionally log or handle
            }
        });
        state.runThread.start();
    }

    public static void cleanup(ArchiveState state) {
        state.shouldStop = true;
        if (state.runProcess != null && state.runProcess.isAlive()) {
            state.runProcess.destroy();
            try { state.runProcess.waitFor(); } catch (InterruptedException ignored) {}
        }
        if (state.runThread != null && state.runThread.isAlive()) {
            try { state.runThread.join(1000); } catch (InterruptedException ignored) {}
        }
    }
}
