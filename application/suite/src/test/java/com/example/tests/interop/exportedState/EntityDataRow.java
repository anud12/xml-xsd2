package com.example.tests.interop.exportedState;

import com.sun.jna.Pointer;

/**
 * Reads EntityDataRow from raw C pointer memory.
 * Does NOT use JNA Structure.read() which would dereference nested NULL pointers.
 */
public class EntityDataRow {
    private final Pointer base; // Points to the C struct in memory

    public EntityDataRow(Pointer p) { this.base = p; }

    /** Read entity id from offset 0 (pointer to string) */
    public String getId() {
        Pointer namePtr = base.getPointer(0);
        if (namePtr == null) return "";
        try {
            return namePtr.getString(0);
        } catch (Throwable e) {
            // namePtr points to invalid memory - just skip this entity row
            return "";
        }
    }

    /** Read text_map_len from offset 8 (usize = 8 bytes on x64) */
    private long getTextMapLen() {
        try { return base.getLong(8); } catch(Throwable e) { return 0; }
    }

    /** Read text_map_keys array pointer from offset 16 */
    private Pointer getTextMapKeysBase() {
        try { return base.getPointer(16); } catch(Throwable e) { return null; }
    }

    /** Read text_map_values array pointer from offset 24 */
    private Pointer getTextMapValuesBase() {
        try { return base.getPointer(24); } catch(Throwable e) { return null; }
    }

    /** Read number_map_len from offset 32 */
    private long getNumberMapLen() {
        try { return base.getLong(32); } catch(Throwable e) { return 0; }
    }

    /** Read number_map_keys array pointer from offset 40 */
    private Pointer getNumberMapKeysBase() {
        try { return base.getPointer(40); } catch(Throwable e) { return null; }
    }

    /** Read number_map_values (doubles) pointer from offset 48 */
    private Pointer getNumberMapValuesBase() {
        try { return base.getPointer(48); } catch(Throwable e) { return null; }
    }

    public String[] getTextMapKeys() {
        long len = getTextMapLen();
        if (len <= 0) return new String[0];
        Pointer b = getTextMapKeysBase();
        if (b == null) return new String[(int)len];
        String[] keys = new String[(int)len];
        for (int i = 0; i < len; i++) {
            Pointer p = b.getPointer(i * 8);
            keys[i] = p != null ? p.getString(0) : "";
        }
        return keys;
    }

    public String[] getTextMapValues() {
        long len = getTextMapLen();
        if (len <= 0) return new String[0];
        Pointer b = getTextMapValuesBase();
        if (b == null) return new String[(int)len];
        String[] vals = new String[(int)len];
        for (int i = 0; i < len; i++) {
            Pointer p = b.getPointer(i * 8);
            vals[i] = p != null ? p.getString(0) : "";
        }
        return vals;
    }

    public String[] getNumberMapKeys() {
        long len = getNumberMapLen();
        if (len <= 0) return new String[0];
        Pointer b = getNumberMapKeysBase();
        if (b == null) return new String[(int)len];
        String[] keys = new String[(int)len];
        for (int i = 0; i < len; i++) {
            Pointer p = b.getPointer(i * 8);
            keys[i] = p != null ? p.getString(0) : "";
        }
        return keys;
    }

    public double[] getNumberMapValues() {
        long len = getNumberMapLen();
        if (len <= 0) return new double[0];
        Pointer b = getNumberMapValuesBase();
        if (b == null) return new double[(int)len];
        double[] vals = new double[(int)len];
        for (int i = 0; i < len; i++) {
            vals[i] = b.getDouble(i * 8);
        }
        return vals;
    }
}
