# Font Loading — `setFont`, `fontRef`, and the C#/Rust ABI

## Status (blocked)

The font feature is **half-finished**. The C# render side is complete and
waiting, but the data path (JS `setFont` + `fontRef` in the factories + Rust
encode) was never implemented, and the C#/Rust `UiNodeOptions` ABIs are out of
sync. Until the data path exists, every module that calls `hostApi.ui.setFont`
fails to install (`not a function`), which is the current blocker for the
`TestTick` module and its duplicate-label investigation.

## Current state per layer

| Layer | File | State |
|---|---|---|
| C# render | `Sources/UI/UiWindow/UiWindow.Content.cs:33` `ApplyFont` + `:13` `LoadFontFile` | **Done** — pulls the TTF out of the archive, hash-caches to `user://fonts/f_<hash>.ttf`, applies `font` / `font_size` / `font_color` to the `Label`. |
| C# decode | `Sources/UI/UiSlab.cs:64` `ToNodeData` | **Done** — reads `HasFont/FontFile/FontSize/FontColor/FontStyle` into `UiNodeData.Font` only when `HasFont != 0`. |
| C# ABI | `Sources/UI/UiAbi.cs:197-202` `UiNodeOptions` | **Ahead** — ends with `Title, HasFont, FontFile, FontSize, FontColor, FontStyle`. |
| JS factory | `application/ui/ui/host.js:816` `text`, `:934` `field` | **Missing param** — `text(id, value)` / `field(id, binding)` take no `fontRef`; a passed ref is dropped. |
| JS `setFont` | (none) | **Missing** — not defined anywhere in the runtime → `not a function`. |
| Rust ABI | `application/runtime/src/ui/abi.rs:155-198` `UiNodeOptions` | **Behind** — ends at `progress_steps`; no `title` and no font fields. |
| Rust encode | (none) | **Missing** — nothing in `runtime/src` sets the font fields. |

### The two blocking defects

1. **`hostApi.ui.setFont` does not exist.** `TestTick`'s `module/interest/ui.js:3`
   calls it during install → the module entry throws → `sim_ctx::install`
   returns `Err` → `process_archive.rs:42` swallows it (stderr, continues,
   returns non-null) → C# `ProcessArchive()` does **not** throw → no windows
   are created → `scene.Window("add-btn")` fails with `No UiWindow named
   "add-btn" in the scene`.

2. **ABI size mismatch.** C# `UiNodeOptions` is larger than Rust `UiNodeOptions`
   (C# has `Title` + 5 font fields Rust lacks). `UiSlab.ReadNodes:23` strides by
   `Marshal.SizeOf<UiAbi.UiNode>()`, so if the sizes differ the node stride is
   wrong. **Verify the sizes empirically before trusting the slab** (see Step 1).

## Design

### Intended flow (per `module_types/ui/Ui.d.ts`)

1. Module calls `hostApi.ui.setFont({ file, size, color })` → returns a
   `fontRef` handle (`{ name, value, toString(), toJSON() }`).
2. Module passes `fontRef` to `text(id, value, fontRef)` / `field(id, binding, fontRef)`.
3. The factory stores the font on the node.
4. Font rides the node → snapshot → Rust slab → C# `UiNodeData.Font`.
5. C# `UiWindow.ApplyFont` loads the TTF and applies it to the `Label`.

`UiFontConfig = { file: string; size: number; color: string }` (color is a CSS
hex `#RRGGBB`). The `fontRef` is what the node stores; on the wire it is the
resolved `{ file, size, color }`.

### Resolution model

`setFont` registers a font by `file` and returns a ref whose identity is the
file path. The factory, given a `fontRef`, resolves it to the concrete
`{ file, size, color }` and stores it on the node's options (top-level
`font` object in the JS node, mirroring how `panel`/`background` are stored).
The Rust encoder reads `options.font` and fills `has_font`/`font_file`/
`font_size`/`font_color`/`font_style`.

## Implementation plan

### Step 0 — unblock the module load (minimal, do first)

Add a `setFont` to the `hostApi.ui` base object in
`application/runtime/src/js_executor/sim_entry.rs` (the `ui:{...}` literal) and
mirror it in the extract/invoke wiring
(`application/runtime/src/js_host_api/script_ui.rs`) so every context has it:

```js
setFont: function (cfg) {
  cfg = cfg || {};
  var name = cfg.file || '';
  return { name: name, value: name, toString: function () { return name; }, toJSON: function () { return name; } };
}
```

This alone makes `TestTick` install (the ref is currently dropped by the
factories, so no font is applied — but the module loads and the duplicate-label
investigation can proceed). **This is the fastest path back to the label bug.**

### Step 1 — verify the ABI (gate for Steps 2–4)

Confirm the C# and Rust `UiNodeOptions`/`UiNode` sizes match. Add a Rust unit
test that asserts `std::mem::size_of::<UiNodeOptions>()` equals the C# layout
(the C# size is the reference: compute it from `UiAbi.cs` field order +
alignment, or read `Marshal.SizeOf` from a tiny C# probe). If they differ,
**reconcile before proceeding** — the slab is unreadable otherwise.

### Step 2 — Rust ABI + encode

In `abi.rs` `UiNodeOptions`, append (in the exact C# order):
`title: u32`, `has_font: u8`, `font_file: u32`, `font_size: f32`,
`font_color: u32`, `font_style: u32`. In the options parser, read
`options.font` (`{ file, size, color }`) and set `has_font = 1`, intern
`font_file`, copy `font_size`, parse the hex `color` into `font_color` (ABGR
or ARGB — must match `UiWindow.Content.cs:40-44` which reads
`a>>24, r>>16, g>>8, b`), and set `font_style`.

### Step 3 — C# ABI parity

Confirm `UiAbi.cs` `UiNodeOptions` matches the Rust field order exactly (it
should, once Step 2 appends in the same order). `UiSlab.ToNodeData` and
`UiWindow.ApplyFont` are already correct.

### Step 4 — JS `setFont` + `fontRef` in factories

In `host.js`:
- Add `setFont` to the `api` object (real registration, not the Step-0 stub).
- `text(id, value, fontRef)` / `field(id, binding, fontRef)`: when `fontRef` is
  present, store `font: { file, size, color }` on the registered node (resolve
  the ref via the `setFont` registry).

## Open notes

- **Step 0 is the priority** — it unblocks the `TestTick` module load and the
  duplicate-label investigation without any ABI work.
- The `UiTexture` paint-spec shape (`{ animation, fill, nineSlice }`) is a
  **separate** WIP feature (see `progress-fill-uiTexture.md`); the font path is
  independent of it.
- `font_style` is defined in the ABI but `UiFontConfig` has no style input;
  default it to `0` until a style source exists.
- Color byte order must be confirmed against `UiWindow.Content.cs:40-44`
  (it reads ARGB) before Step 2.
