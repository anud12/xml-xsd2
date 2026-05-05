package com.example.utils;

import com.example.tests.interop.RuntimeInteropJava;

// Strong reference to a native callback so it isn't GC'd while native code may call back
import com.example.tests.interop.RuntimeInteropJava.MyCallback;

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
    // Keep a strong reference to the callback to prevent it being GC'd while native code holds its pointer
    public MyCallback loggerCallback = null;
    public List<String> logMessages = new ArrayList<>();

    public ArchiveState() {
        this.archive = ZipArchive.createTemp();
    }
}
