package com.example.tests.interop.exportedState;

import com.sun.jna.*;
import java.util.*;

public class FileArray extends Structure {
    public NativeLong len;
    public Pointer data; // FileRow*

    @Override
    protected List<String> getFieldOrder() { return Arrays.asList("len", "data"); }
    public FileArray() { super(); }
    public FileArray(Pointer p) { super(p); read(); }
}
