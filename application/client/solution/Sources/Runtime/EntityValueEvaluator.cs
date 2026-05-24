using System;
using System.Linq;
using System.Runtime.InteropServices;

/// Typed evaluator that wraps an AST root ID and delegates evaluation to the Rust runtime.
/// The AST graph is persisted in global Rust state after compilation; astRootId is an opaque
/// reference into that graph. Evaluation is performed by the Rust FFI which resolves the
/// entity key, looks up the entity number map, and returns the display string (or fallback).
public class EntityValueEvaluator
{
    private readonly uint _astRootId;
    private readonly string _entityId;

    public EntityValueEvaluator(uint astRootId, string entityId)
    {
        _astRootId = astRootId;
        _entityId = entityId;
    }

    public string Evaluate()
    {
        return EvaluateEntityValue(_entityId, _astRootId);
    }

    private static string EvaluateEntityValue(string entityId, uint astRootId)
    {
        var ptr = ffi_evaluate_panel_value(entityId, astRootId);
        if (ptr == IntPtr.Zero) return string.Empty;
        try
        {
            var s = Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
            var sanitized = new string(s.Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t').ToArray());
            return sanitized;
        }
        finally
        {
            runtime_free_string(ptr);
        }
    }

    [DllImport("libxml_xsd2", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr ffi_evaluate_panel_value(
        [MarshalAs(UnmanagedType.LPStr)] string entityId,
        uint astRootId);

    [DllImport("libxml_xsd2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_free_string(IntPtr s);
}
