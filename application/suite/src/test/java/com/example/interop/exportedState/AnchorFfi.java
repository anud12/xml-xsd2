package com.example.interop.exportedState;

import com.sun.jna.*;
import java.util.*;

public class AnchorFfi extends Structure implements Structure.ByValue {
    public float x;
    public float y;

    @Override
    protected List<String> getFieldOrder() {
        return Arrays.asList("x", "y");
    }

    public AnchorFfi() { super(); }
}
