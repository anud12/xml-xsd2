use std::ffi::CString;
use libc::c_char;

type PanelFfi = crate::ffi_mod::types::PanelFfi;
type AnchorFfi = crate::ffi_mod::types::AnchorFfi;
type OffsetFfi = crate::ffi_mod::types::OffsetFfi;
type SizeFfi = crate::ffi_mod::types::SizeFfi;

// Test FFI pointer mechanism with libc malloc
#[no_mangle]
pub extern "C" fn test_pointer_return() -> *mut c_char {
    let test_str =
        CString::new("TEST_POINTER_SUCCESS").unwrap();
    let ptr = test_str.into_raw();
    eprintln!("test_pointer_return: ptr={:p}", ptr);
    let _ = std::fs::write(
        "E:\\workspace\\test_pointer_return.txt",
        format!("test_pointer_return: ptr={:p}\n", ptr),
    );
    ptr
}

// Return fixed test struct for marshaling diagnostics
#[no_mangle]
pub extern "C" fn get_test_panel_fixed() -> *mut PanelFfi {
    let id_ptr =
        CString::new("TEST_ID").unwrap().into_raw();
    let bg_ptr =
        CString::new("test_bg.png").unwrap().into_raw();
    let children_ptr =
        CString::new("[{\"id\":\"child1\"}]")
            .unwrap().into_raw();
    let panel_ptr =
        CString::new("{\"test\":true}")
            .unwrap().into_raw();

    let test_panel = Box::new(PanelFfi {
        id: id_ptr,
        background: bg_ptr,
        anchor: AnchorFfi { x: 1.5, y: 2.5 },
        pivot: AnchorFfi { x: 3.5, y: 4.5 },
        offset: OffsetFfi {
            top: 5.5, bottom: 6.5,
            left: 7.5, right: 8.5,
        },
        size: SizeFfi {
            height: 100.0, width: 200.0,
        },
        children_json: children_ptr,
        panel_json: panel_ptr,
    });

    let raw_ptr = Box::into_raw(test_panel);
    let struct_ptr = raw_ptr as *const u8;
    let bytes = unsafe {
        std::slice::from_raw_parts(struct_ptr, 72)
    };
    let hex = bytes.iter()
        .map(|b| format!("{:02X}", b))
        .collect::<Vec<_>>().join(" ");
    eprintln!("get_test_panel_fixed: ptr={:p} bytes={}",
        raw_ptr, hex);
    let _ = std::fs::write(
        "E:\\workspace\\rust_test_panel_fixed.txt",
        format!("get_test_panel_fixed:\nptr={:p}\nbytes={}\n",
            raw_ptr, hex),
    );
    raw_ptr
}
