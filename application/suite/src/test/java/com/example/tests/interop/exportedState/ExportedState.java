package com.example.tests.interop.exportedState;

import com.sun.jna.*;
import java.util.*;

public class ExportedState extends Structure {
    public CStringArray entities;
    public CStringArray actions;
    public CStringArray events;
    public PanelArray panels;
    public ModuleArray modules;
    public FileArray files;
    public CStringArray entity_patterns;
    public CreatedByArray created_by;
    public byte has_data; // C bool -> byte

    @Override
    protected List<String> getFieldOrder() {
        return Arrays.asList("entities","actions","events", "panels","modules","files","entity_patterns","created_by","has_data");
    }

    public ExportedState() { super(); }
    public ExportedState(Pointer p) { super(p); read(); }
}
