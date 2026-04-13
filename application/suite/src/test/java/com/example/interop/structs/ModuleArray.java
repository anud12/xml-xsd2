package com.example.interop.structs;

import com.sun.jna.*;
import java.util.*;

public class ModuleArray extends Structure {
    public NativeLong len;
    public Pointer data; // ModuleRow*

    @Override
    protected List<String> getFieldOrder() {
        return Arrays.asList("len", "data");
    }

    public ModuleArray() { super(); }
    public ModuleArray(Pointer p) { super(p); read(); }
}
