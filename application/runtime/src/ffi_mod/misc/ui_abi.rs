//! ABI layout export for the C# parity test.

use crate::ui::abi;

/// Sizes and field offsets of the `.ui` ABI structs. The C# client compares
/// these against `Marshal.SizeOf`/`FieldOffset` and fails on drift.
#[no_mangle]
pub extern "C" fn runtime_ui_abi_sizes() -> abi::UiAbiSizes {
    abi::abi_sizes()
}
