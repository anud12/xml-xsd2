package com.example.steps;

import java.io.File;
import java.util.Map;

public class ArchiveState {
    public Map<String, File> featureFiles;
    public ZipArchive archive;
    public volatile byte[] lastOutput = new byte[]{};
    public Process runProcess;
    public Thread runThread;
    public volatile boolean shouldStop = false;

    public ArchiveState() {
        this.archive = ZipArchive.createTemp();
    }
}
