package com.example.interop.structs;

import com.sun.jna.*;
import java.util.*;

public class CreatedByArray extends Structure {
    public NativeLong len;
    public Pointer data; // CreatedByRow*

    @Override
    protected List<String> getFieldOrder() { return Arrays.asList("len", "data"); }
    public CreatedByArray() { super(); }
    public CreatedByArray(Pointer p) { super(p); read(); }
}
