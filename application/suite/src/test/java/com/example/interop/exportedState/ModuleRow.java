package com.example.interop.exportedState;

import com.sun.jna.*;
import java.util.*;

public class ModuleRow extends Structure {
    public Pointer id;
    public Pointer name;
    public Pointer version;

    @Override
    protected List<String> getFieldOrder() {
        return Arrays.asList("id", "name", "version");
    }

    public ModuleRow() { super(); }
    public ModuleRow(Pointer p) { super(p); read(); }
}
