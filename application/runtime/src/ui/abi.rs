//! FFI ABI for the .ui layer: pure binary POD structs the C# client reads
//! directly (no JSON on the FFI boundary).
//!
//! Layout contract with the C# client (`Sources/UI/UiAbi.cs`). Every type is
//! `#[repr(C)]` with scalars first and raw pointers last. A snapshot or delta
//! is a single slab allocation: struct regions first, a NUL-terminated string
//! arena second; every `u32` string field is a byte offset into that arena.
//!
//! Drift protection: `abi_sizes()` publishes sizes and field offsets; the C#
//! parity test compares them against `Marshal.SizeOf`/`FieldOffset`. Bump
//! `UI_ABI_VERSION` on any layout change.

use std::alloc::{alloc, dealloc, handle_alloc_error, Layout};
use std::collections::HashMap;
use std::mem::{align_of, offset_of, size_of};
use std::ptr;

use serde_json::{Map, Value};

use super::UiNode as DomainNode;
use super::UiDeltaOp as DomainDeltaOp;

pub const UI_ABI_VERSION: u32 = 1;
/// Sentinel string offset: "no string" (absent value/src/optional option).
pub const NO_STR: u32 = u32::MAX;

/// Node kinds (`UiNode::kind`).
pub const UI_KIND_DIVISION: u32 = 0;
pub const UI_KIND_TEXT: u32 = 1;
pub const UI_KIND_FIELD: u32 = 2;
pub const UI_KIND_WINDOW: u32 = 3;
pub const UI_KIND_IMAGE: u32 = 4;

/// Delta ops (`UiDeltaOp::op`).
pub const UI_OP_ADD: u8 = 0;
pub const UI_OP_UPDATE: u8 = 1;
pub const UI_OP_REMOVE: u8 = 2;

/// Field binding maps (`UiBinding::map`).
pub const UI_MAP_NONE: u8 = 0;
pub const UI_MAP_NUMBER: u8 = 1;
pub const UI_MAP_TEXT: u8 = 2;

/// Layout kinds (`UiLayout::kind`).
pub const UI_LAYOUT_NONE: u8 = 0;
pub const UI_LAYOUT_COLUMN: u8 = 1;
pub const UI_LAYOUT_ROW: u8 = 2;
pub const UI_LAYOUT_TRACKS: u8 = 3;

/// `UiTrack::flags` bits.
pub const UI_TRACK_HAS_MIN: u8 = 1;
pub const UI_TRACK_HAS_MAX: u8 = 2;

/// Background kinds (`UiBackground::kind`).
pub const UI_BG_NONE: u8 = 0;
pub const UI_BG_STATIC: u8 = 1;
pub const UI_BG_ANIMATION: u8 = 2;
pub const UI_BG_SPRITE_MAP: u8 = 3;

/// on-click kinds (`UiOnClick::kind`).
pub const UI_CLICK_NONE: u8 = 0;
pub const UI_CLICK_ACTION: u8 = 1;
pub const UI_CLICK_STEPS: u8 = 2;

/// Arg value types (`UiArgPair::vt`).
pub const UI_ARG_STRING: u8 = 0;
pub const UI_ARG_NUMBER: u8 = 1;
pub const UI_ARG_CURSOR: u8 = 2;

// ---------------------------------------------------------------------------
// ABI types (mirrored in C# `Sources/UI/UiAbi.cs`)
// ---------------------------------------------------------------------------

#[repr(C)]
pub struct UiTrack {
    pub min: f32,
    pub max: f32,
    pub scale: f32,
    pub flags: u8,
}

#[repr(C)]
pub struct UiMapLayer {
    pub texture: u32,
    pub layer: u32,
}

#[repr(C)]
pub struct UiArgPair {
    pub key: u32,
    pub value: u32,
    pub vt: u8,
}

#[repr(C)]
pub struct UiClickStep {
    pub action: u32,
    pub arg_count: u32,
    pub args: *const UiArgPair,
}

#[repr(C)]
pub struct UiOnClick {
    pub kind: u8,
    pub action: u32,
    pub step_count: u32,
    pub steps: *const UiClickStep,
}

#[repr(C)]
pub struct UiBinding {
    pub entity: u32,
    pub map: u8,
    pub name: u32,
    pub fallback: u32,
}

#[repr(C)]
pub struct UiLayout {
    pub kind: u8,
    pub row_first: u8,
    pub reverse: u8,
    pub gap_row: f32,
    pub gap_col: f32,
    pub equal_tracks: u32,
    pub col_count: u32,
    pub row_count: u32,
    pub col_tracks: *const UiTrack,
    pub row_tracks: *const UiTrack,
}

#[repr(C)]
pub struct UiBackground {
    pub kind: u8,
    pub loop_: u8,
    pub duration: f32,
    pub path: u32,
    pub name: u32,
    pub map: u32,
    pub layer_count: u32,
    pub layers: *const UiMapLayer,
}

#[repr(C)]
pub struct UiOnHover {
    pub stop_propagation: u8,
    pub thickness: f32,
    pub emit_action: u32,
    pub background: u32,
    pub texture: u32,
}

#[repr(C)]
pub struct UiNodeOptions {
    pub x: f32,
    pub y: f32,
    pub has_xy: u8,
    pub has_size: u8,
    pub anchor: u32,
    pub align: u32,
    pub width: f32,
    pub height: f32,
    pub layout: UiLayout,
    pub background: UiBackground,
    pub border_texture: u32,
    pub border_width: f32,
    pub has_border: u8,
    pub on_click: UiOnClick,
    pub on_hover: UiOnHover,
    pub container: u32,
    pub resizable: u8,
    pub resizable_keep_aspect: u8,
    pub portal_arrow: u8,
}

#[repr(C)]
pub struct UiNode {
    pub kind: u32,
    pub id: u32,
    pub value: u32,
    pub src: u32,
    pub binding: UiBinding,
    pub opt: UiNodeOptions,
    pub child_count: u32,
    pub children: *const u32,
}

#[repr(C)]
pub struct UiAnimation {
    pub name: u32,
    pub frame_count: u32,
    pub duration: f32,
    pub loop_: u8,
    pub frames: *const u32,
}

#[repr(C)]
pub struct UiSnapshot {
    pub version: u32,
    pub node_count: u32,
    pub anim_count: u32,
    pub string_len: u32,
    pub nodes: *const UiNode,
    pub anims: *const UiAnimation,
    pub strings: *const u8,
}

#[repr(C)]
pub struct UiDeltaOp {
    pub op: u8,
    pub node: UiNode,
}

#[repr(C)]
pub struct UiDelta {
    pub version: u32,
    pub op_count: u32,
    pub string_len: u32,
    pub ops: *const UiDeltaOp,
    pub strings: *const u8,
}

/// Sizes and field offsets published to the C# parity test.
#[repr(C)]
pub struct UiAbiSizes {
    pub version: u32,
    pub size_snapshot: u32,
    pub size_node: u32,
    pub size_binding: u32,
    pub size_options: u32,
    pub size_layout: u32,
    pub size_track: u32,
    pub size_background: u32,
    pub size_map_layer: u32,
    pub size_on_click: u32,
    pub size_click_step: u32,
    pub size_arg_pair: u32,
    pub size_on_hover: u32,
    pub size_animation: u32,
    pub size_delta: u32,
    pub size_delta_op: u32,
    pub off_node_kind: u32,
    pub off_node_id: u32,
    pub off_node_value: u32,
    pub off_node_src: u32,
    pub off_node_binding: u32,
    pub off_node_opt: u32,
    pub off_node_child_count: u32,
    pub off_node_children: u32,
    pub off_snap_version: u32,
    pub off_snap_nodes: u32,
    pub off_snap_anims: u32,
    pub off_snap_strings: u32,
    pub off_delta_version: u32,
    pub off_delta_ops: u32,
    pub off_delta_strings: u32,
    pub off_delta_op_op: u32,
    pub off_delta_op_node: u32,
    pub off_opts_layout: u32,
    pub off_opts_background: u32,
    pub off_opts_on_click: u32,
    pub off_opts_on_hover: u32,
    pub off_opts_container: u32,
    pub off_on_click_steps: u32,
    pub off_click_step_args: u32,
    pub off_layout_col_tracks: u32,
    pub off_layout_row_tracks: u32,
    pub off_background_layers: u32,
    pub off_animation_frames: u32,
}

pub fn abi_sizes() -> UiAbiSizes {
    UiAbiSizes {
        version: UI_ABI_VERSION,
        size_snapshot: size_of::<UiSnapshot>() as u32,
        size_node: size_of::<UiNode>() as u32,
        size_binding: size_of::<UiBinding>() as u32,
        size_options: size_of::<UiNodeOptions>() as u32,
        size_layout: size_of::<UiLayout>() as u32,
        size_track: size_of::<UiTrack>() as u32,
        size_background: size_of::<UiBackground>() as u32,
        size_map_layer: size_of::<UiMapLayer>() as u32,
        size_on_click: size_of::<UiOnClick>() as u32,
        size_click_step: size_of::<UiClickStep>() as u32,
        size_arg_pair: size_of::<UiArgPair>() as u32,
        size_on_hover: size_of::<UiOnHover>() as u32,
        size_animation: size_of::<UiAnimation>() as u32,
        size_delta: size_of::<UiDelta>() as u32,
        size_delta_op: size_of::<UiDeltaOp>() as u32,
        off_node_kind: offset_of!(UiNode, kind) as u32,
        off_node_id: offset_of!(UiNode, id) as u32,
        off_node_value: offset_of!(UiNode, value) as u32,
        off_node_src: offset_of!(UiNode, src) as u32,
        off_node_binding: offset_of!(UiNode, binding) as u32,
        off_node_opt: offset_of!(UiNode, opt) as u32,
        off_node_child_count: offset_of!(UiNode, child_count) as u32,
        off_node_children: offset_of!(UiNode, children) as u32,
        off_snap_version: offset_of!(UiSnapshot, version) as u32,
        off_snap_nodes: offset_of!(UiSnapshot, nodes) as u32,
        off_snap_anims: offset_of!(UiSnapshot, anims) as u32,
        off_snap_strings: offset_of!(UiSnapshot, strings) as u32,
        off_delta_version: offset_of!(UiDelta, version) as u32,
        off_delta_ops: offset_of!(UiDelta, ops) as u32,
        off_delta_strings: offset_of!(UiDelta, strings) as u32,
        off_delta_op_op: offset_of!(UiDeltaOp, op) as u32,
        off_delta_op_node: offset_of!(UiDeltaOp, node) as u32,
        off_opts_layout: offset_of!(UiNodeOptions, layout) as u32,
        off_opts_background: offset_of!(UiNodeOptions, background) as u32,
        off_opts_on_click: offset_of!(UiNodeOptions, on_click) as u32,
        off_opts_on_hover: offset_of!(UiNodeOptions, on_hover) as u32,
        off_opts_container: offset_of!(UiNodeOptions, container) as u32,
        off_on_click_steps: offset_of!(UiOnClick, steps) as u32,
        off_click_step_args: offset_of!(UiClickStep, args) as u32,
        off_layout_col_tracks: offset_of!(UiLayout, col_tracks) as u32,
        off_layout_row_tracks: offset_of!(UiLayout, row_tracks) as u32,
        off_background_layers: offset_of!(UiBackground, layers) as u32,
        off_animation_frames: offset_of!(UiAnimation, frames) as u32,
    }
}

// ---------------------------------------------------------------------------
// Slab builder
// ---------------------------------------------------------------------------

/// Single-allocation slab: `[struct regions][string arena]`, built directly
/// in the final raw allocation so every raw pointer written into a struct
/// is valid in the returned slab. The struct region is preallocated in both
/// runs (its length is known arithmetically, see `snapshot_struct_len`), so
/// nothing ever reallocates mid-build. The returned pointer addresses the
/// `UiSnapshot`/`UiDelta` header at offset 0; an 8-byte length prefix just
/// before it lets [`free_slab`] recover the deallocation size.
struct Slab {
    /// Slab data start. Measuring run: scratch Vec (writes discarded).
    /// Final run: inside the raw allocation, 8 past its base.
    base: *mut u8,
    /// Final run: raw allocation base (holds the length prefix). Never read;
    /// kept so the allocation's provenance stays in the struct.
    #[allow(dead_code)]
    alloc_base: *mut u8,
    /// Arena offset within the slab (final run only).
    arena_off: usize,
    /// Total struct-region bytes preallocated.
    struct_len: usize,
    /// Bytes used so far in the struct region.
    struct_used: usize,
    /// Bytes used so far in the arena.
    arena_used: usize,
    measuring: bool,
    /// Measuring run: the arena grows here (struct scratch is thrown away).
    arena_buf: Vec<u8>,
    seen: HashMap<String, u32>,
    /// Measuring run scratch keeping `base` alive (never read).
    #[allow(dead_code)]
    scratch: Vec<[u8; 8]>,
}

impl Slab {
    /// Measuring run: struct scratch is preallocated (writes are discarded),
    /// the arena is counted in `arena_buf`.
    fn measuring(struct_len: usize) -> Self {
        let scratch = vec![[0u8; 8]; ((struct_len + 7) & !7) / 8];
        Slab {
            base: scratch.as_ptr() as *mut u8,
            alloc_base: ptr::null_mut(),
            arena_off: 0,
            struct_len,
            struct_used: 0,
            arena_used: 0,
            measuring: true,
            arena_buf: Vec::new(),
            seen: HashMap::new(),
            scratch,
        }
    }

    /// Final run: one raw allocation `[u64 slab_len][struct region][arena]`,
    /// sized exactly; all writes land in it directly.
    fn final_run(struct_len: usize, arena_len: usize) -> Self {
        let arena_off = (struct_len + 7) & !7;
        let slab_len = arena_off + arena_len;
        let layout = Layout::from_size_align(slab_len + 8, 8).unwrap();
        let alloc_base = unsafe { alloc(layout) as *mut u8 };
        if alloc_base.is_null() {
            handle_alloc_error(layout);
        }
        let base = unsafe { alloc_base.add(8) };
        unsafe {
            *(alloc_base as *mut u64) = slab_len as u64;
        }
        Slab {
            base,
            alloc_base,
            arena_off,
            struct_len,
            struct_used: 0,
            arena_used: 0,
            measuring: false,
            arena_buf: Vec::new(),
            seen: HashMap::new(),
            scratch: Vec::new(),
        }
    }

    /// Drop the wrapper, keeping the final run's raw allocation alive until
    /// [`free_slab`]. Returns the slab data start.
    fn into_raw(self) -> *mut u8 {
        let Slab { base, .. } = self;
        base
    }

    /// Byte offset of `s` in the arena (NUL-terminated).
    fn intern(&mut self, s: &str) -> u32 {
        if let Some(&off) = self.seen.get(s) {
            return off;
        }
        let off = self.arena_used as u32;
        self.arena_buf.extend_from_slice(s.as_bytes());
        self.arena_buf.push(0);
        self.arena_used += s.len() + 1;
        if !self.measuring {
            let o = self.arena_off + off as usize;
            unsafe {
                let dst = self.base.add(o);
                dst.copy_from_nonoverlapping(s.as_bytes().as_ptr(), s.len());
                *dst.add(s.len()) = 0;
            }
        }
        self.seen.insert(s.to_string(), off);
        off
    }

    /// Append an aligned zeroed region for `count` elements to the struct
    /// region. The region is preallocated, so this never reallocates.
    fn alloc<T>(&mut self, count: usize) -> *mut T {
        if count == 0 {
            return ptr::null_mut();
        }
        let align = align_of::<T>();
        let next = (self.struct_used + align - 1) & !(align - 1);
        self.struct_used = next + size_of::<T>() * count;
        debug_assert!(
            self.struct_used <= self.struct_len,
            "slab struct region overflow: {0} > {1}",
            self.struct_used,
            self.struct_len
        );
        unsafe { self.base.add(next).cast::<T>() }
    }
}

fn align_up(v: usize, a: usize) -> usize {
    (v + a - 1) & !(a - 1)
}

/// Struct-region bytes for one node's variable regions, starting at `len`.
/// Must mirror the allocation order in `build_into`/`build_delta_into`.
fn plan_region_len(plan: &NodePlan, mut len: usize) -> usize {
    len = align_up(len, 8) + size_of::<UiClickStep>() * plan.step_args.len();
    for n_args in &plan.step_args {
        len = align_up(len, 4) + size_of::<UiArgPair>() * n_args;
    }
    len = align_up(len, 4) + 4 * plan.children;
    len = align_up(len, 4) + size_of::<UiTrack>() * plan.col_tracks;
    len = align_up(len, 4) + size_of::<UiTrack>() * plan.row_tracks;
    len = align_up(len, 4) + size_of::<UiMapLayer>() * plan.layers;
    len
}

/// Total struct-region bytes of a snapshot, arithmetic only (no strings),
/// so the measuring run can be preallocated before it runs.
fn snapshot_struct_len(nodes: &[DomainNode], animations: &HashMap<String, Value>) -> usize {
    // Must mirror `build_into`'s allocation order exactly: header, nodes,
    // anims, per-node regions, per-anim frames.
    let mut len = size_of::<UiSnapshot>() + size_of::<UiNode>() * nodes.len();
    let mut names: Vec<&String> = animations.keys().collect();
    names.sort();
    len = align_up(len, 8) + size_of::<UiAnimation>() * names.len();
    for n in nodes {
        len = plan_region_len(&plan_node(n), len);
    }
    for n in &names {
        let frames = animations[*n]
            .get("frames")
            .and_then(|f| f.as_array())
            .map(|f| f.len())
            .unwrap_or(0);
        len = align_up(len, 4) + 4 * frames;
    }
    len
}

/// Total struct-region bytes of a delta, arithmetic only (no strings).
fn delta_struct_len(ops: &[DomainDeltaOp]) -> usize {
    let mut len = size_of::<UiDelta>() + size_of::<UiDeltaOp>() * ops.len();
    for op in ops {
        let node = match op {
            DomainDeltaOp::Add { node } | DomainDeltaOp::Update { node } => Some(node),
            DomainDeltaOp::Remove { .. } => None,
        };
        let plan = match node {
            Some(n) => plan_node(n),
            None => NodePlan {
                children: 0,
                col_tracks: 0,
                row_tracks: 0,
                layers: 0,
                step_args: Vec::new(),
            },
        };
        len = plan_region_len(&plan, len);
    }
    len
}

// ---------------------------------------------------------------------------
// Domain -> ABI conversion
// ---------------------------------------------------------------------------

/// Variable-region sizes of one node, computed before any allocation.
struct NodePlan {
    children: usize,
    col_tracks: usize,
    row_tracks: usize,
    layers: usize,
    /// Arg count per click step.
    step_args: Vec<usize>,
}

fn plan_node(node: &DomainNode) -> NodePlan {
    let mut plan = NodePlan {
        children: node_children(node).len(),
        col_tracks: 0,
        row_tracks: 0,
        layers: 0,
        step_args: Vec::new(),
    };
    let opts = match node {
        DomainNode::Division { options, .. }
        | DomainNode::Window { options, .. } => options,
        _ => return plan,
    };
    if let Some(layout) = opts.get("layout").and_then(|v| v.as_object()) {
        plan.col_tracks = layout.get("columns").and_then(|v| v.as_array()).map(|a| a.len()).unwrap_or(0);
        plan.row_tracks = layout.get("rows").and_then(|v| v.as_array()).map(|a| a.len()).unwrap_or(0);
    }
    if let Some(bg) = opts.get("background").and_then(|v| v.as_object()) {
        if bg.get("map").and_then(|v| v.as_str()).is_some() {
            plan.layers = bg.get("layers").and_then(|v| v.as_array()).map(|a| a.len()).unwrap_or(0);
        }
    }
    if let Some(steps) = opts.get("onClick").and_then(|v| v.as_object())
        .and_then(|o| o.get("steps"))
        .and_then(|v| v.as_array())
    {
        for s in steps.iter() {
            let n = s.get("args").and_then(|v| v.as_object()).map(|o| o.len()).unwrap_or(0);
            plan.step_args.push(n);
        }
    }
    plan
}

fn node_children(node: &DomainNode) -> &[String] {
    match node {
        DomainNode::Division { children, .. }
        | DomainNode::Text { children, .. }
        | DomainNode::Field { children, .. }
        | DomainNode::Window { children, .. }
        | DomainNode::Image { children, .. } => children,
    }
}

fn empty_options() -> UiNodeOptions {
    UiNodeOptions {
        x: 0.0,
        y: 0.0,
        has_xy: 0,
        has_size: 0,
        anchor: NO_STR,
        align: NO_STR,
        width: 0.0,
        height: 0.0,
        layout: UiLayout {
            kind: UI_LAYOUT_NONE,
            row_first: 0,
            reverse: 0,
            gap_row: 0.0,
            gap_col: 0.0,
            equal_tracks: 0,
            col_count: 0,
            row_count: 0,
            col_tracks: ptr::null(),
            row_tracks: ptr::null(),
        },
        background: UiBackground {
            kind: UI_BG_NONE,
            loop_: 0,
            duration: 0.0,
            path: NO_STR,
            name: NO_STR,
            map: NO_STR,
            layer_count: 0,
            layers: ptr::null(),
        },
        border_texture: NO_STR,
        border_width: 0.0,
        has_border: 0,
        on_click: UiOnClick {
            kind: UI_CLICK_NONE,
            action: NO_STR,
            step_count: 0,
            steps: ptr::null(),
        },
        on_hover: UiOnHover {
            stop_propagation: 0,
            thickness: 0.0,
            emit_action: NO_STR,
            background: NO_STR,
            texture: NO_STR,
        },
        container: NO_STR,
        resizable: 0,
        resizable_keep_aspect: 0,
        portal_arrow: 0,
    }
}

fn opt_f32(opts: &Map<String, Value>, key: &str) -> f32 {
    opts.get(key).and_then(|v| v.as_f64()).map(|f| f as f32).unwrap_or(0.0)
}

fn opt_num(opts: &Map<String, Value>, key: &str) -> Option<f32> {
    opts.get(key).and_then(|v| v.as_f64()).map(|f| f as f32)
}

fn opt_bool(opts: &Map<String, Value>, key: &str) -> bool {
    opts.get(key).and_then(|v| v.as_bool()).unwrap_or(false)
}

fn opt_str(opts: &Map<String, Value>, key: &str, slab: &mut Slab) -> u32 {
    match opts.get(key).and_then(|v| v.as_str()) {
        Some(s) if !s.is_empty() => slab.intern(s),
        _ => NO_STR,
    }
}

fn track_to_abi(t: &Value) -> UiTrack {
    match t {
        // Numeric track: a fixed size, encoded as min==max with no scale.
        Value::Number(n) => UiTrack {
            min: n.as_f64().unwrap_or(0.0) as f32,
            max: n.as_f64().unwrap_or(0.0) as f32,
            scale: 0.0,
            flags: UI_TRACK_HAS_MIN | UI_TRACK_HAS_MAX,
        },
        _ => {
            let Some(m) = t.as_object() else {
                return UiTrack { min: 0.0, max: 0.0, scale: 1.0, flags: 0 };
            };
            let has_min = m.get("min").and_then(|v| v.as_f64()).is_some();
            let has_max = m.get("max").and_then(|v| v.as_f64()).is_some();
            UiTrack {
                min: opt_f32(m, "min"),
                max: opt_f32(m, "max"),
                scale: opt_num(m, "scale").unwrap_or(1.0),
                flags: (has_min as u8) * UI_TRACK_HAS_MIN | (has_max as u8) * UI_TRACK_HAS_MAX,
            }
        }
    }
}

fn empty_binding() -> UiBinding {
    UiBinding {
        entity: NO_STR,
        map: UI_MAP_NONE,
        name: NO_STR,
        fallback: NO_STR,
    }
}

fn parse_layout(
    opts: &Map<String, Value>,
    _slab: &mut Slab,
    col_base: *mut UiTrack,
    row_base: *mut UiTrack,
) -> UiLayout {
    let mut l = empty_options().layout;
    match opts.get("layout") {
        None => {}
        Some(Value::String(s)) => match s.as_str() {
            "column" => {
                l.kind = UI_LAYOUT_COLUMN;
                l.equal_tracks = 1;
            }
            "row" => {
                l.kind = UI_LAYOUT_ROW;
                l.equal_tracks = 1;
            }
            _ => {}
        },
        Some(Value::Number(n)) => {
            l.kind = UI_LAYOUT_ROW;
            l.equal_tracks = n.as_u64().unwrap_or(0) as u32;
        }
        Some(v) => {
            let Some(m) = v.as_object() else {
                return l;
            };
            l.kind = UI_LAYOUT_TRACKS;
            l.row_first = opt_bool(m, "rowFirst") as u8;
            l.reverse = opt_bool(m, "reverse") as u8;
            if let Some(gap) = m.get("gap").and_then(|g| g.as_object()) {
                l.gap_row = opt_f32(gap, "row");
                l.gap_col = opt_f32(gap, "column");
            }
            if let Some(cols) = m.get("columns").and_then(|c| c.as_array()) {
                for (i, c) in cols.iter().enumerate() {
                    unsafe { *col_base.add(i) = track_to_abi(c) };
                }
                l.col_count = cols.len() as u32;
            }
            if let Some(rows) = m.get("rows").and_then(|r| r.as_array()) {
                for (i, r) in rows.iter().enumerate() {
                    unsafe { *row_base.add(i) = track_to_abi(r) };
                }
                l.row_count = rows.len() as u32;
            }
        }
    }
    l.col_tracks = col_base;
    l.row_tracks = row_base;
    l
}

fn parse_background(
    opts: &Map<String, Value>,
    slab: &mut Slab,
    layer_base: *mut UiMapLayer,
) -> UiBackground {
    let mut b = empty_options().background;
    match opts.get("background") {
        None | Some(Value::Null) => {}
        Some(Value::String(path)) if !path.is_empty() => {
            b.kind = UI_BG_STATIC;
            b.path = slab.intern(path);
        }
        Some(v) => {
            let Some(m) = v.as_object() else {
                return b;
            };
            if m.get("map").and_then(|m| m.as_str()).is_some() {
                b.kind = UI_BG_SPRITE_MAP;
                b.map = opt_str(m, "map", slab);
                if let Some(layers) = m.get("layers").and_then(|l| l.as_array()) {
                    for (i, l) in layers.iter().enumerate() {
                        let lm = l.as_object();
                        unsafe {
                            *layer_base.add(i) = UiMapLayer {
                                texture: lm.map(|m| opt_str(m, "texture", slab)).unwrap_or(NO_STR),
                                layer: lm.and_then(|m| opt_num(m, "layer"))
                                    .map(|f| f as u32)
                                    .unwrap_or(i as u32),
                            }
                        };
                    }
                    b.layer_count = layers.len() as u32;
                }
            } else if m.get("name").and_then(|n| n.as_str()).is_some() {
                b.kind = UI_BG_ANIMATION;
                b.name = opt_str(m, "name", slab);
                b.duration = opt_f32(m, "duration");
                b.loop_ = opt_bool(m, "loop") as u8;
            }
        }
    }
    b.layers = layer_base;
    b
}

fn parse_on_click(
    opts: &Map<String, Value>,
    slab: &mut Slab,
    steps_base: *mut UiClickStep,
    arg_bases: &[*mut UiArgPair],
) -> UiOnClick {
    let mut c = empty_options().on_click;
    match opts.get("onClick") {
        None | Some(Value::Null) => {}
        Some(Value::String(action)) if !action.is_empty() => {
            c.kind = UI_CLICK_ACTION;
            c.action = slab.intern(action);
        }
        Some(v) => {
            if !v.is_object() {
                return c;
            }
            let Some(steps) = v.get("steps").and_then(|s| s.as_array()) else {
                return c;
            };
            c.kind = UI_CLICK_STEPS;
            for (i, step) in steps.iter().enumerate() {
                let action = step.get("action").and_then(|a| a.as_str()).unwrap_or("");
                let mut arg_count = 0usize;
                if let Some(args) = step.get("args").and_then(|a| a.as_object()) {
                    for (k, val) in args.iter() {
                        let (vt, text) = match val {
                            Value::String(s) => (UI_ARG_STRING, s.clone()),
                            Value::Number(n) => (UI_ARG_NUMBER, n.to_string()),
                            Value::Bool(b) => (UI_ARG_NUMBER, if *b { "1".into() } else { "0".into() }),
                            Value::Object(o)
                                if o.get("__cursor").and_then(|c| c.as_str()).is_some() =>
                            {
                                (UI_ARG_CURSOR, o["__cursor"].as_str().unwrap_or("x").to_string())
                            }
                            _ => (UI_ARG_STRING, val.to_string()),
                        };
                        unsafe {
                            *arg_bases[i].add(arg_count) = UiArgPair {
                                key: slab.intern(k),
                                value: slab.intern(&text),
                                vt,
                            }
                        };
                        arg_count += 1;
                    }
                }
                unsafe {
                    *steps_base.add(i) = UiClickStep {
                        action: slab.intern(action),
                        arg_count: arg_count as u32,
                        args: arg_bases[i],
                    }
                };
                c.step_count = i as u32 + 1;
            }
        }
    }
    c.steps = steps_base;
    c
}

fn binding_to_abi(b: &Value, slab: &mut Slab) -> UiBinding {
    let Some(m) = b.as_object() else {
        return empty_binding();
    };
    UiBinding {
        entity: opt_str(m, "entity", slab),
        map: match m.get("map").and_then(|m| m.as_str()) {
            Some("number") => UI_MAP_NUMBER,
            Some("text") => UI_MAP_TEXT,
            _ => UI_MAP_NONE,
        },
        name: opt_str(m, "name", slab),
        fallback: opt_str(m, "fallback", slab),
    }
}

struct NodeRegions {
    children: *mut u32,
    col_tracks: *mut UiTrack,
    row_tracks: *mut UiTrack,
    layers: *mut UiMapLayer,
    steps: *mut UiClickStep,
    arg_bases: Vec<*mut UiArgPair>,
}

/// Intern a node's strings into the slab, fill its variable regions, and
/// produce the ABI node. All regions must already be allocated.
fn node_to_abi(node: &DomainNode, slab: &mut Slab, r: &NodeRegions) -> UiNode {
    let children = node_children(node);
    for (i, c) in children.iter().enumerate() {
        unsafe { *r.children.add(i) = slab.intern(c) };
    }
    let child_count = children.len() as u32;
    let children_ptr = if child_count == 0 { ptr::null() } else { r.children };

    match node {
        DomainNode::Division { id, options, .. } => UiNode {
            kind: UI_KIND_DIVISION,
            id: slab.intern(id),
            value: NO_STR,
            src: NO_STR,
            binding: empty_binding(),
            opt: options_to_abi(options, slab, r),
            child_count,
            children: children_ptr,
        },
        DomainNode::Text { id, value, .. } => UiNode {
            kind: UI_KIND_TEXT,
            id: slab.intern(id),
            value: slab.intern(value),
            src: NO_STR,
            binding: empty_binding(),
            opt: empty_options(),
            child_count,
            children: children_ptr,
        },
        DomainNode::Field { id, binding, value, .. } => UiNode {
            kind: UI_KIND_FIELD,
            id: slab.intern(id),
            value: slab.intern(value),
            src: NO_STR,
            binding: binding_to_abi(binding, slab),
            opt: empty_options(),
            child_count,
            children: children_ptr,
        },
        DomainNode::Window { id, options, .. } => UiNode {
            kind: UI_KIND_WINDOW,
            id: slab.intern(id),
            value: NO_STR,
            src: NO_STR,
            binding: empty_binding(),
            opt: options_to_abi(options, slab, r),
            child_count,
            children: children_ptr,
        },
        DomainNode::Image { id, src, .. } => UiNode {
            kind: UI_KIND_IMAGE,
            id: slab.intern(id),
            value: NO_STR,
            src: slab.intern(src),
            binding: empty_binding(),
            opt: empty_options(),
            child_count,
            children: children_ptr,
        },
    }
}

fn options_to_abi(opts: &Value, slab: &mut Slab, r: &NodeRegions) -> UiNodeOptions {
    let mut o = empty_options();
    let Some(opts) = opts.as_object() else {
        return o;
    };
    let has_x = opt_num(opts, "x").is_some();
    let has_y = opt_num(opts, "y").is_some();
    o.x = opt_f32(opts, "x");
    o.y = opt_f32(opts, "y");
    o.has_xy = (has_x || has_y) as u8;
    o.anchor = opt_str(opts, "anchor", slab);
    o.align = opt_str(opts, "align", slab);
    let has_w = opt_num(opts, "width").is_some();
    let has_h = opt_num(opts, "height").is_some();
    o.width = opt_f32(opts, "width");
    o.height = opt_f32(opts, "height");
    o.has_size = (has_w || has_h) as u8;
    o.layout = parse_layout(opts, slab, r.col_tracks, r.row_tracks);
    o.background = parse_background(opts, slab, r.layers);
    if let Some(border) = opts.get("border").and_then(|b| b.as_object()) {
        o.border_texture = opt_str(border, "texture", slab);
        o.border_width = opt_f32(border, "width");
        o.has_border = 1;
    }
    o.on_click = parse_on_click(opts, slab, r.steps, &r.arg_bases);
    if let Some(h) = opts.get("onHover").and_then(|h| h.as_object()) {
        o.on_hover.emit_action = opt_str(h, "emitAction", slab);
        o.on_hover.stop_propagation = opt_bool(h, "stopPropagation") as u8;
        o.on_hover.background = opt_str(h, "background", slab);
        o.on_hover.texture = opt_str(h, "texture", slab);
        o.on_hover.thickness = opt_f32(h, "thickness");
    }
    o.container = opt_str(opts, "container", slab);
    // resizable: a bare boolean, or an object { keepAspectRatio }. Both enable
    // resize; the object form additionally locks the aspect ratio.
    match opts.get("resizable") {
        Some(Value::Bool(b)) => {
            o.resizable = *b as u8;
        }
        Some(v) if v.is_object() => {
            o.resizable = 1;
            let ka = v.get("keepAspectRatio").and_then(|b| b.as_bool()).unwrap_or(false);
            o.resizable_keep_aspect = ka as u8;
        }
        _ => {}
    }
    o.portal_arrow = opt_bool(opts, "portalArrow") as u8;
    o
}

fn animation_to_abi(name: &str, def: &Value, slab: &mut Slab, frames_base: *mut u32) -> UiAnimation {
    let frames = def.get("frames").and_then(|f| f.as_array());
    let count = frames.map(|f| f.len()).unwrap_or(0);
    if let Some(frames) = frames {
        for (i, f) in frames.iter().enumerate() {
            let sprite = f.get("sprite").and_then(|s| s.as_str()).unwrap_or("");
            unsafe { *frames_base.add(i) = slab.intern(sprite) };
        }
    }
    let mut a = UiAnimation {
        name: slab.intern(name),
        frame_count: count as u32,
        duration: 0.0,
        loop_: 0,
        frames: if count == 0 { ptr::null() } else { frames_base },
    };
    if let Some(m) = def.as_object() {
        a.duration = opt_f32(m, "duration");
        a.loop_ = opt_bool(m, "loop") as u8;
    }
    a
}

// ---------------------------------------------------------------------------
// Builders (one slab allocation per snapshot / delta)
// ---------------------------------------------------------------------------

fn build_into(slab: &mut Slab, nodes: &[DomainNode], animations: &HashMap<String, Value>)
-> (*mut UiSnapshot, *const UiNode, *const UiAnimation) {
    let plans: Vec<NodePlan> = nodes.iter().map(plan_node).collect();

    let header = slab.alloc::<UiSnapshot>(1);
    let nodes_base = slab.alloc::<UiNode>(nodes.len());
    // One deterministic order for both the frame allocations and the writes
    // (HashMap iteration order must not pair anim i with anim j's frames).
    let anim_names: Vec<&String> = {
        let mut v: Vec<&String> = animations.keys().collect();
        v.sort();
        v
    };
    let anim_frame_counts: Vec<usize> = anim_names
        .iter()
        .map(|n| {
            animations[*n]
                .get("frames")
                .and_then(|f| f.as_array())
                .map(|f| f.len())
                .unwrap_or(0)
        })
        .collect();
    let anims_base = slab.alloc::<UiAnimation>(animations.len());

    let mut regions: Vec<NodeRegions> = Vec::with_capacity(nodes.len());
    for p in &plans {
        let mut arg_bases = Vec::with_capacity(p.step_args.len());
        let steps = slab.alloc::<UiClickStep>(p.step_args.len());
        for n_args in &p.step_args {
            arg_bases.push(slab.alloc::<UiArgPair>(*n_args));
        }
        regions.push(NodeRegions {
            children: slab.alloc::<u32>(p.children),
            col_tracks: slab.alloc::<UiTrack>(p.col_tracks),
            row_tracks: slab.alloc::<UiTrack>(p.row_tracks),
            layers: slab.alloc::<UiMapLayer>(p.layers),
            steps,
            arg_bases,
        });
    }
    let mut anim_frame_bases: Vec<*mut u32> = Vec::with_capacity(animations.len());
    for n in &anim_frame_counts {
        anim_frame_bases.push(slab.alloc::<u32>(*n));
    }

    for (i, node) in nodes.iter().enumerate() {
        let ab = node_to_abi(node, slab, &regions[i]);
        unsafe { ptr::write(nodes_base.add(i), ab) };
    }
    for (i, name) in anim_names.iter().enumerate() {
        let ab = animation_to_abi(name, &animations[*name], slab, anim_frame_bases[i]);
        unsafe { ptr::write(anims_base.add(i), ab) };
    }
    (header, nodes_base, anims_base)
}

/// Full UI tree + animations as one slab. The C# side reads the structs and
/// frees with [`free_snapshot`].
pub fn build_snapshot(nodes: &[DomainNode], animations: &HashMap<String, Value>)
-> *mut UiSnapshot {
    // Struct region length is arithmetic; the measuring run only counts the
    // string arena (its struct scratch is preallocated and discarded).
    let struct_len = snapshot_struct_len(nodes, animations);
    let arena_len = {
        let mut m = Slab::measuring(struct_len);
        build_into(&mut m, nodes, animations);
        m.arena_used
    };

    let mut slab = Slab::final_run(struct_len, arena_len);
    let (header, nodes_base, anims_base) = build_into(&mut slab, nodes, animations);
    unsafe {
        ptr::write(
            header,
            UiSnapshot {
                version: UI_ABI_VERSION,
                node_count: nodes.len() as u32,
                anim_count: animations.len() as u32,
                string_len: slab.arena_used as u32,
                nodes: nodes_base,
                anims: anims_base,
                strings: slab.base.add(slab.arena_off),
            },
        );
    }
    slab.into_raw() as *mut UiSnapshot
}

/// Release a slab from [`build_snapshot`].
pub unsafe fn free_snapshot(ptr: *mut UiSnapshot) {
    if ptr.is_null() {
        return;
    }
    free_slab(ptr as *mut u8);
}

/// Pending delta as one slab. The C# side reads the ops and frees with
/// [`free_delta`].
pub fn build_delta(ops: &[DomainDeltaOp]) -> *mut UiDelta {
    // Struct region length is arithmetic; the measuring run only counts the
    // string arena (its struct scratch is preallocated and discarded).
    let struct_len = delta_struct_len(ops);
    let arena_len = {
        let mut m = Slab::measuring(struct_len);
        build_delta_into(&mut m, ops);
        m.arena_used
    };

    let mut slab = Slab::final_run(struct_len, arena_len);
    let (header, ops_base) = build_delta_into(&mut slab, ops);
    unsafe {
        ptr::write(
            header,
            UiDelta {
                version: UI_ABI_VERSION,
                op_count: ops.len() as u32,
                string_len: slab.arena_used as u32,
                ops: ops_base,
                strings: slab.base.add(slab.arena_off),
            },
        );
    }
    slab.into_raw() as *mut UiDelta
}

fn build_delta_into(slab: &mut Slab, ops: &[DomainDeltaOp]) -> (*mut UiDelta, *const UiDeltaOp) {
    let header = slab.alloc::<UiDelta>(1);
    let ops_base = slab.alloc::<UiDeltaOp>(ops.len());

    // Allocate per-op node regions (add/update carry full nodes).
    let mut regions: Vec<NodeRegions> = Vec::with_capacity(ops.len());
    for op in ops.iter() {
        let node = match op {
            DomainDeltaOp::Add { node } | DomainDeltaOp::Update { node } => Some(node),
            DomainDeltaOp::Remove { .. } => None,
        };
        let plan = node.map(plan_node).unwrap_or_else(|| NodePlan {
            children: 0,
            col_tracks: 0,
            row_tracks: 0,
            layers: 0,
            step_args: Vec::new(),
        });
        let mut arg_bases = Vec::with_capacity(plan.step_args.len());
        let steps = slab.alloc::<UiClickStep>(plan.step_args.len());
        for n_args in &plan.step_args {
            arg_bases.push(slab.alloc::<UiArgPair>(*n_args));
        }
        regions.push(NodeRegions {
            children: slab.alloc::<u32>(plan.children),
            col_tracks: slab.alloc::<UiTrack>(plan.col_tracks),
            row_tracks: slab.alloc::<UiTrack>(plan.row_tracks),
            layers: slab.alloc::<UiMapLayer>(plan.layers),
            steps,
            arg_bases,
        });
    }

    for (i, op) in ops.iter().enumerate() {
        let (op_u8, ab_node) = match op {
            DomainDeltaOp::Add { node } => (UI_OP_ADD, node_to_abi(node, slab, &regions[i])),
            DomainDeltaOp::Update { node } => (UI_OP_UPDATE, node_to_abi(node, slab, &regions[i])),
            // Remove ops carry only the id: zeroed node with the id interned.
            DomainDeltaOp::Remove { id } => {
                let mut n = unsafe { std::mem::zeroed::<UiNode>() };
                n.kind = UI_KIND_DIVISION;
                n.id = slab.intern(id);
                n.value = NO_STR;
                n.src = NO_STR;
                (UI_OP_REMOVE, n)
            }
        };
        unsafe {
            ptr::write(ops_base.add(i), UiDeltaOp { op: op_u8, node: ab_node });
        }
    }
    (header, ops_base)
}

/// Release a slab from [`build_delta`].
pub unsafe fn free_delta(ptr: *mut UiDelta) {
    if ptr.is_null() {
        return;
    }
    free_slab(ptr as *mut u8);
}

/// Inverse of [`Slab::final_run`]: reads the length prefix just before the
/// slab start and deallocates the whole allocation.
unsafe fn free_slab(ptr: *mut u8) {
    let base = ptr.sub(8);
    let slab_len = unsafe { *(base as *const u64) } as usize;
    let layout = Layout::from_size_align(slab_len + 8, 8).unwrap();
    unsafe {
        dealloc(base, layout);
    }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

#[cfg(test)]
mod tests {
    use super::*;

    fn v(s: &str) -> Value {
        serde_json::from_str(s).unwrap()
    }

    fn node(json: &str) -> DomainNode {
        serde_json::from_str(json).unwrap()
    }

    fn cstr(arena: *const u8, off: u32) -> String {
        unsafe {
            let p = arena.add(off as usize) as *const i8;
            std::ffi::CStr::from_ptr(p).to_string_lossy().into_owned()
        }
    }

    #[test]
    fn abi_sizes_are_stable() {
        let s = abi_sizes();
        // The C# parity test compares against these live values; this test
        // pins them so an accidental layout change is caught in Rust first.
        assert_eq!(s.version, UI_ABI_VERSION);
        assert_eq!(s.size_track, size_of::<UiTrack>() as u32);
        assert_eq!(s.size_map_layer, 8);
        assert_eq!(s.size_arg_pair, 12);
        assert_eq!(s.size_click_step, 16);
        assert_eq!(s.size_on_click, 24);
        assert_eq!(s.size_binding, 16);
        assert_eq!(s.size_layout, 40);
        assert_eq!(s.size_background, 32);
        assert_eq!(s.size_on_hover, 20);
        assert_eq!(s.size_options, 176);
        assert_eq!(s.size_node, 224);
        assert_eq!(s.size_animation, 24);
        assert_eq!(s.size_snapshot, 40);
        assert_eq!(s.size_delta, 32);
        assert_eq!(s.size_delta_op, 232);
        // Pointer offsets land on 8-byte boundaries.
        for off in [
            s.off_node_children,
            s.off_snap_nodes,
            s.off_snap_anims,
            s.off_snap_strings,
            s.off_delta_ops,
            s.off_delta_strings,
            s.off_on_click_steps,
            s.off_click_step_args,
            s.off_layout_col_tracks,
            s.off_layout_row_tracks,
            s.off_background_layers,
            s.off_animation_frames,
        ] {
            assert_eq!(off % 8, 0, "pointer offset {off} not 8-aligned");
        }
    }

    #[test]
    fn snapshot_round_trips_nodes_options_and_strings() {
        let nodes = vec![
            node(r#"{"kind":"window","id":"hud","options":{"x":10,"y":-20,"anchor":"top-left","align":"top-left","width":200,"height":100,"layout":{"columns":[{"min":50,"scale":1},{"max":120},80],"rows":[],"rowFirst":true,"gap":{"row":4,"column":2}},"background":{"map":"art/map.tiff","layers":[{"texture":"skin/a.png","layer":0},{"texture":"skin/b.png","layer":1}]},"border":{"texture":"art/border.png","width":3},"onClick":{"steps":[{"action":"click-a","args":{"col":{"__cursor":"x"},"row":{"__cursor":"y"},"entity":"ent-1","count":5}},{"action":"click-b","args":{}}]},"onHover":{"emitAction":"hud-hover","stopPropagation":true,"background":"art/hover.png","texture":"art/outline.png","thickness":2},"container":"items"},"children":["title","hp"]}"#),
            node(r#"{"kind":"text","id":"title","value":"Health","children":[]}"#),
            node(r#"{"kind":"field","id":"hp","binding":{"entity":"ent-1","map":"number","name":"hp","fallback":"n/a"},"value":"7","children":[]}"#),
            node(r#"{"kind":"image","id":"icon","src":"art/icon.png","children":[]}"#),
        ];
        let mut anims = HashMap::new();
        anims.insert(
            "blink".to_string(),
            v(r#"{"frames":[{"sprite":"a.png"},{"sprite":"b.png"}],"duration":1.5,"loop":true}"#),
        );

        let snap = unsafe { &*build_snapshot(&nodes, &anims) };
        assert_eq!(snap.version, UI_ABI_VERSION);
        assert_eq!(snap.node_count, 4);
        assert_eq!(snap.anim_count, 1);
        let arena = snap.strings;
        let ns = unsafe { std::slice::from_raw_parts(snap.nodes, snap.node_count as usize) };

        // window: full options
        let hud = ns.iter().find(|n| cstr(arena, n.id) == "hud").unwrap();
        assert_eq!(hud.kind, UI_KIND_WINDOW);
        assert!((hud.opt.x - 10.0).abs() < f32::EPSILON);
        assert_eq!(hud.opt.has_xy, 1);
        assert_eq!(cstr(arena, hud.opt.anchor), "top-left");
        assert_eq!(hud.opt.has_size, 1);
        assert!((hud.opt.width - 200.0).abs() < f32::EPSILON);
        assert_eq!(hud.opt.layout.kind, UI_LAYOUT_TRACKS);
        assert_eq!(hud.opt.layout.row_first, 1);
        assert_eq!(hud.opt.layout.col_count, 3);
        let tracks = unsafe { std::slice::from_raw_parts(hud.opt.layout.col_tracks, 3) };
        assert_eq!(tracks[0].flags, UI_TRACK_HAS_MIN);
        assert!((tracks[0].min - 50.0).abs() < f32::EPSILON);
        assert_eq!(tracks[1].flags, UI_TRACK_HAS_MAX);
        assert!((tracks[1].max - 120.0).abs() < f32::EPSILON);
        // Numeric track: fixed size encoded as min==max, no scale.
        assert_eq!(tracks[2].flags, UI_TRACK_HAS_MIN | UI_TRACK_HAS_MAX);
        assert!((tracks[2].min - 80.0).abs() < f32::EPSILON);
        assert!((tracks[2].max - 80.0).abs() < f32::EPSILON);
        assert!((tracks[2].scale).abs() < f32::EPSILON);
        assert!((hud.opt.layout.gap_row - 4.0).abs() < f32::EPSILON);
        assert_eq!(hud.opt.background.kind, UI_BG_SPRITE_MAP);
        assert_eq!(cstr(arena, hud.opt.background.map), "art/map.tiff");
        assert_eq!(hud.opt.background.layer_count, 2);
        let layers = unsafe { std::slice::from_raw_parts(hud.opt.background.layers, 2) };
        assert_eq!(cstr(arena, layers[1].texture), "skin/b.png");
        assert_eq!(layers[1].layer, 1);
        assert_eq!(hud.opt.has_border, 1);
        assert_eq!(cstr(arena, hud.opt.border_texture), "art/border.png");
        assert_eq!(hud.opt.on_click.kind, UI_CLICK_STEPS);
        assert_eq!(hud.opt.on_click.step_count, 2);
        let steps = unsafe { std::slice::from_raw_parts(hud.opt.on_click.steps, 2) };
        assert_eq!(cstr(arena, steps[0].action), "click-a");
        assert_eq!(steps[0].arg_count, 4);
        let args = unsafe { std::slice::from_raw_parts(steps[0].args, 4) };
        let by_key = |k: &str| args.iter().find(|a| cstr(arena, a.key) == k).unwrap();
        assert_eq!(by_key("col").vt, UI_ARG_CURSOR);
        assert_eq!(cstr(arena, by_key("col").value), "x");
        assert_eq!(by_key("row").vt, UI_ARG_CURSOR);
        assert_eq!(cstr(arena, by_key("row").value), "y");
        assert_eq!(by_key("entity").vt, UI_ARG_STRING);
        assert_eq!(cstr(arena, by_key("entity").value), "ent-1");
        assert_eq!(by_key("count").vt, UI_ARG_NUMBER);
        assert_eq!(cstr(arena, by_key("count").value), "5");
        assert_eq!(steps[1].arg_count, 0);
        assert_eq!(hud.opt.on_hover.stop_propagation, 1);
        assert_eq!(cstr(arena, hud.opt.on_hover.emit_action), "hud-hover");
        assert_eq!(cstr(arena, hud.opt.on_hover.background), "art/hover.png");
        assert!((hud.opt.on_hover.thickness - 2.0).abs() < f32::EPSILON);
        assert_eq!(cstr(arena, hud.opt.container), "items");
        assert_eq!(hud.child_count, 2);
        let children = unsafe { std::slice::from_raw_parts(hud.children, 2) };
        assert_eq!(cstr(arena, children[0]), "title");

        // text / field / image
        let title = ns.iter().find(|n| cstr(arena, n.id) == "title").unwrap();
        assert_eq!(title.kind, UI_KIND_TEXT);
        assert_eq!(cstr(arena, title.value), "Health");
        assert_ne!(title.value, NO_STR);
        let hp = ns.iter().find(|n| cstr(arena, n.id) == "hp").unwrap();
        assert_eq!(hp.kind, UI_KIND_FIELD);
        assert_eq!(hp.binding.map, UI_MAP_NUMBER);
        assert_eq!(cstr(arena, hp.binding.entity), "ent-1");
        assert_eq!(cstr(arena, hp.binding.fallback), "n/a");
        assert_eq!(cstr(arena, hp.value), "7");
        let icon = ns.iter().find(|n| cstr(arena, n.id) == "icon").unwrap();
        assert_eq!(icon.kind, UI_KIND_IMAGE);
        assert_eq!(cstr(arena, icon.src), "art/icon.png");

        // animation
        let anims = unsafe { std::slice::from_raw_parts(snap.anims, snap.anim_count as usize) };
        assert_eq!(cstr(arena, anims[0].name), "blink");
        assert_eq!(anims[0].frame_count, 2);
        assert_eq!(anims[0].loop_, 1);
        let frames = unsafe { std::slice::from_raw_parts(anims[0].frames, 2) };
        assert_eq!(cstr(arena, frames[1]), "b.png");

        unsafe { free_snapshot(snap as *const UiSnapshot as *mut UiSnapshot) };
    }

    #[test]
    fn portal_arrow_option_round_trips() {
        let nodes = vec![node(
            r#"{"kind":"window","id":"p","options":{"x":77,"y":0,"width":6,"height":40,"portalArrow":true,"sector":"portal"},"children":[]}"#,
        )];
        let snap = unsafe { &*build_snapshot(&nodes, &HashMap::new()) };
        let arena = snap.strings;
        let ns = unsafe { std::slice::from_raw_parts(snap.nodes, snap.node_count as usize) };
        let p = ns.iter().find(|n| cstr(arena, n.id) == "p").unwrap();
        assert_eq!(p.opt.portal_arrow, 1);
        unsafe { free_snapshot(snap as *const UiSnapshot as *mut UiSnapshot) };
    }

    #[test]
    fn empty_snapshot_is_freeable() {
        let snap = build_snapshot(&[], &HashMap::new());
        assert!(!snap.is_null());
        unsafe {
            let s = &*snap;
            assert_eq!(s.node_count, 0);
            assert_eq!(s.string_len, 0);
            free_snapshot(snap);
        }
    }

    #[test]
    fn delta_round_trips_add_update_remove() {
        let a = node(r#"{"kind":"division","id":"a","options":{},"children":[]}"#);
        let b1 = node(r#"{"kind":"text","id":"b","value":"one","children":[]}"#);
        let b2 = node(r#"{"kind":"text","id":"b","value":"two","children":[]}"#);
        let ops = vec![
            DomainDeltaOp::Add { node: a },
            DomainDeltaOp::Add { node: b1 },
            DomainDeltaOp::Update { node: b2 },
            DomainDeltaOp::Remove { id: "gone".to_string() },
        ];
        let delta = unsafe { &*build_delta(&ops) };
        assert_eq!(delta.version, UI_ABI_VERSION);
        assert_eq!(delta.op_count, 4);
        let arena = delta.strings;
        let dops = unsafe { std::slice::from_raw_parts(delta.ops, 4) };
        assert_eq!(dops[0].op, UI_OP_ADD);
        assert_eq!(cstr(arena, dops[0].node.id), "a");
        assert_eq!(cstr(arena, dops[1].node.value), "one");
        assert_eq!(dops[2].op, UI_OP_UPDATE);
        assert_eq!(cstr(arena, dops[2].node.value), "two");
        assert_eq!(dops[3].op, UI_OP_REMOVE);
        assert_eq!(cstr(arena, dops[3].node.id), "gone");
        assert_eq!(dops[3].node.child_count, 0);
        assert!(dops[3].node.children.is_null());

        unsafe { free_delta(delta as *const UiDelta as *mut UiDelta) };
    }
}
