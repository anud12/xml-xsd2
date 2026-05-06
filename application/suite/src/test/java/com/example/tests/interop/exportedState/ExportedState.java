package com.example.tests.interop.exportedState;

import com.sun.jna.*;
import java.util.*;
import com.example.tests.interop.RuntimeInteropJava;

public class ExportedState extends Structure {
    public CStringArray entities;
    public CStringArray actions;
    public CStringArray events;
    public PanelArray panels;
    public ModuleArray modules;
    public FileArray files;
    public CStringArray entity_patterns;
    public CreatedByArray created_by;
    // EntityDataArray fields inline to match C layout without nested Structure auto-read issues with JNA
    public NativeLong entity_data_len;
    public Pointer entity_data_ptr;
    public byte has_data; // C bool -> byte

    @Override
    protected List<String> getFieldOrder() {
        return Arrays.asList("entities","actions","events", "panels","modules","files","entity_patterns","created_by","entity_data_len","entity_data_ptr","has_data");
    }

    public ExportedState() { super(); }
    public ExportedState(Pointer p) { super(p); read(); }

    /** Debug: print entity data to stderr */
    public void debugPrintEntityData(RuntimeInteropJava lib) {
        Pointer jsonPtr = lib.runtime_get_entity_maps_json();
        if (jsonPtr != null) {
            System.err.println("[DEBUG ExportedState] JSON: " + jsonPtr.getString(0));
            lib.runtime_free_entity_maps_json(jsonPtr);
        } else {
            System.err.println("[DEBUG ExportedState] no entity maps data");
        }
    }
}
