package com.example.interop.exportedState;

import com.sun.jna.*;
import java.util.*;

public class CreatedByRow extends Structure {
    public Pointer key;
    public NativeLong values_len;
    public Pointer values; // *mut *mut c_char

    @Override
    protected List<String> getFieldOrder() { return Arrays.asList("key", "values_len", "values"); }
    public CreatedByRow() { super(); }
    public CreatedByRow(Pointer p) { super(p); read(); }
}
