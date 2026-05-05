package com.example.tests.interop.exportedState;

import com.sun.jna.*;
import java.util.*;

public class PanelArray extends Structure {
    public NativeLong len;
    public Pointer data; // PanelFfi*

    @Override
    protected List<String> getFieldOrder() {
        return Arrays.asList("len", "data");
    }

    public PanelArray() { super(); }
    public PanelArray(Pointer p) { super(p); read(); }
}
