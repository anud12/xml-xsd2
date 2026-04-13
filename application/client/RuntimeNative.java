import com.sun.jna.Library;
import com.sun.jna.Native;
import com.sun.jna.Pointer;

/**
 * JNA interface mapping to the native runtime exports.
 * Loads libxml_xsd2 (libxml_xsd2.dll on Windows, libxml_xsd2.so on Unix).
 */
public interface RuntimeNative extends Library {
    RuntimeNative INSTANCE = Native.load("libxml_xsd2", RuntimeNative.class);

    /**
     * Processes a module ZIP and returns a malloc'ed C string with the persisted DB path.
     * Caller must free with runtime_free_string.
     */
    Pointer runtime_process_archive(String path);

    /** Free a string returned by runtime_process_archive. */
    void runtime_free_string(Pointer s);

    /** Export current runtime state to a file. */
    boolean runtime_export_state(String path);
}
