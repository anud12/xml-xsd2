package com.example.tests.interop.exportedState;

import com.sun.jna.NativeLong;
import com.sun.jna.Pointer;
import com.sun.jna.Structure;
import java.util.Arrays;
import java.util.List;

public class EntityDataArray extends Structure {
    public NativeLong len;
    public Pointer data; // Points to array of EntityDataRow

    @Override
    protected List<String> getFieldOrder() {
        return Arrays.asList("len", "data");
    }

    public EntityDataArray() { super(); }
    public EntityDataArray(Pointer p) { super(p); read(); }
}
