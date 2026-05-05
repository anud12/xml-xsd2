package com.example.tests.interop.exportedState;

import com.sun.jna.*;
import java.util.*;

public class CStringArray extends Structure {
    public NativeLong len;
    public Pointer data; // *mut *mut c_char

    @Override
    protected List<String> getFieldOrder() {
        return Arrays.asList("len", "data");
    }

    public CStringArray() { super(); }
    public CStringArray(Pointer p) { super(p); read(); }
}
