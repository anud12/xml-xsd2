package com.example.utils;

import com.example.interop.RuntimeInteropJava;

import java.io.File;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;

public class ArchiveState {
    public Map<String, File> featureFiles;
    public ZipArchive archive;
    public volatile byte[] lastOutput = new byte[]{};
    public Process runProcess;
    public Thread runThread;
    public volatile boolean shouldStop = false;
    public Optional<RuntimeInteropJava> runtimeInteropJava = Optional.empty();
    public List<String> logMessages = new ArrayList<>();

    public ArchiveState() {
        this.archive = ZipArchive.createTemp();
    }
}
