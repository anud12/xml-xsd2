package com.example.interop.exportedState;

import com.sun.jna.*;
import java.util.*;

public class PanelFfi extends Structure {
    public Pointer id;
    public Pointer background;
    public AnchorFfi anchor;
    public AnchorFfi pivot;
    public OffsetFfi offset;
    public SizeFfi size;
    public Pointer children_callback;

    @Override
    protected List<String> getFieldOrder() {
        return Arrays.asList("id", "background", "anchor", "pivot", "offset", "size", "children_callback");
    }

    public PanelFfi() { super(); }
    public PanelFfi(Pointer p) { super(p); read(); }
}
