package com.example.interop.structs;

import com.sun.jna.*;
import java.util.*;

public class ExportedState extends Structure {
    public CStringArray entities;
    public CStringArray actions;
    public CStringArray events;
    public byte has_data; // C bool -> byte

    @Override
    protected List<String> getFieldOrder() {
        return Arrays.asList("entities","actions","events","has_data");
    }

    public ExportedState() { super(); }
    public ExportedState(Pointer p) { super(p); read(); }
}
