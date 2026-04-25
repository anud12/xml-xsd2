package com.example.interop.exportedState;

import com.sun.jna.*;
import java.util.*;

public class OffsetFfi extends Structure implements Structure.ByValue {
    public float top;
    public float bottom;
    public float left;
    public float right;

    @Override
    protected List<String> getFieldOrder() {
        return Arrays.asList("top", "bottom", "left", "right");
    }

    public OffsetFfi() { super(); }
}