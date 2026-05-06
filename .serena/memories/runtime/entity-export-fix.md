## Entity Export Fix (2026-05-05)

**Problem**: Entities created via `setEntity()` in JS modules were not appearing in `runtime_export_state_struct()`, causing test failures.

**Root Cause**: 
1. `apply_declarations()` in declarations.rs processed entity_data but didn't add entries to last_entity_rows
2. After process_module populated entity rows, load_archive.rs and related functions called `set_last_entity_rows(entity_rows.clone())` with an empty vector, overwriting what was just set

**Fix**:
1. Added `append_entity_rows()` in declarations.rs that adds both createEntity() entities AND setEntity keys to last_entity_rows cache
2. Updated load_archive.rs, process_archive.rs, debug_load_base64.rs, debug_loop.rs, and main.rs to read entity_rows from state AFTER process_module completes

**Key insight**: Entity rows are populated by apply_declarations() within process_module(), not returned as a parameter. Always read from state after calling process_module().
