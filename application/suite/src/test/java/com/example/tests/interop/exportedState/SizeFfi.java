package com.example.tests.interop.exportedState;

import com.sun.jna.*;
import java.util.*;

public class SizeFfi extends Structure implements Structure.ByValue {
    public float height;
    public float width;

    @Override
    protected List<String> getFieldOrder() {
        return Arrays.asList("height", "width");
    }

    public SizeFfi() { super(); }
}
