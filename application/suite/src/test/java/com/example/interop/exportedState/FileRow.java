package com.example.interop.exportedState;

import com.sun.jna.*;
import java.util.*;

public class FileRow extends Structure {
    public Pointer filename;
    public Pointer contents;

    @Override
    protected List<String> getFieldOrder() { return Arrays.asList("filename", "contents"); }
    public FileRow() { super(); }
    public FileRow(Pointer p) { super(p); read(); }
}
