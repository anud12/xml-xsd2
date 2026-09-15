using System;
using System.Runtime.InteropServices;

namespace NewGameProject.Test;

public class TestPointerReturn
{
    private const string LIB_NAME = "libxml_xsd2";

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr test_pointer_return();

    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern void runtime_free_string(IntPtr ptr);

    public static void TestIt()
    {
        System.Console.WriteLine("Testing FFI pointer mechanism...");
        
        try
        {
            System.Console.WriteLine("Calling test_pointer_return()...");
            IntPtr resultPtr = test_pointer_return();
            System.Console.WriteLine($"Got pointer: 0x{resultPtr.ToInt64():X}");
            
            if (resultPtr == IntPtr.Zero)
            {
                System.Console.WriteLine("ERROR: Returned pointer is null!");
                return;
            }
            
            // Try to read the string from the pointer
            string result = Marshal.PtrToStringAnsi(resultPtr);
            System.Console.WriteLine($"String value: '{result}'");
            
            if (result == "TEST_POINTER_SUCCESS")
            {
                System.Console.WriteLine("✓ SUCCESS: FFI pointer mechanism works correctly!");
                System.Console.WriteLine("  The pointer was allocated by Rust and successfully read by C#.");
            }
            else
            {
                System.Console.WriteLine($"✗ FAIL: Expected 'TEST_POINTER_SUCCESS' but got '{result}'");
            }
            
            // Free the memory
            System.Console.WriteLine("Freeing memory...");
            runtime_free_string(resultPtr);
            System.Console.WriteLine("Memory freed.");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Exception: {ex}");
        }
    }
}
