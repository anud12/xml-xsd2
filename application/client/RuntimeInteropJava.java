import com.sun.jna.Pointer;

/**
 * Small Java wrapper that uses the RuntimeNative JNA interface.
 */
public class RuntimeInteropJava {
    private static final RuntimeNative LIB = RuntimeNative.INSTANCE;

    public static String processArchive(String path) {
        Pointer p = LIB.runtime_process_archive(path);
        if (p == null) return null;
        try {
            String s = p.getString(0);
            return s;
        } finally {
            LIB.runtime_free_string(p);
        }
    }

    public static boolean exportState(String path) {
        return LIB.runtime_export_state(path);
    }

    // Simple CLI example
    public static void main(String[] args) {
        String module = args.length > 0 ? args[0] : "module.zip";
        String db = processArchive(module);
        System.out.println("Persisted DB: " + db);
    }
}
