## Panel

A Panel is the coordinate and clipping context for placement, anchors, and overlays. All child positioning is expressed in the panel's logical coordinate space. Runtimes must support multiple panels and report events and measurements in each panel's own logical space.

### Panel properties

- id?: string — optional panel identifier
- position?: { x: number, y: number } — logical origin of the panel relative to its parent (default 0,0)
- size?: { width?: number, height?: number } — logical size of the panel; when absent the panel fills the available container
- clip?: "visible" | "hidden" | "scroll" — clipping behaviour for children
- scaleFactor?: number — optional override of devicePixelsPerLogicalUnit for this panel; by default inherited from the runtime
- anchorDefault?: Anchor — default anchor applied to absolutely positioned children (defaults to "top-left")
- overflowPolicy?: "flip-then-clamp" | "clamp" | "allowOverflow" — overlay placement fallback policy for this panel
- children: ComponentNode[] — child nodes contained in the panel

### Coordinate system & scaling

- Logical coordinate space: origin (0,0) is top-left; X increases to the right, Y increases downward.
- Units are logical pixels. The runtime exposes a scaleFactor (devicePixelsPerLogicalUnit) and maps incoming device coordinates into logical coordinates for all event delivery.
- The runtime must provide deterministic text measurement (in logical units) and stable, documented rounding rules for pixel snapping.

### Anchors (edges & corners model)

Anchors define which point on a node is used for absolute placement and overlay alignment. The model is based strictly on edges (side centers) and corners — no center anchor exists.

Allowed anchor values (enum):

- "top-left", "top", "top-right"
- "left", "right"
- "bottom-left", "bottom", "bottom-right"

Canonical normalized mapping (ax, ay where 0..1):

| Anchor       | ax  | ay  |
|-------------|-----|-----|
| top-left    | 0   | 0   |
| top         | 0.5 | 0   |
| top-right   | 1   | 0   |
| left        | 0   | 0.5 |
| right       | 1   | 0.5 |
| bottom-left | 0   | 1   |
| bottom      | 0.5 | 1   |
| bottom-right| 1   | 1   |

Defaults:
- anchor default: "top-left"
- transformOrigin default: same value as anchor unless explicitly overridden

### Node properties related to anchors

- anchor?: Anchor (enum above) — the point on the node used for placement in absolute/stack contexts.
- anchorOffset?: { x: number, y: number } — logical units added after the anchor point is mapped to (ax,ay).
- transformOrigin?: Anchor | { ax: number, ay: number } — overrides the origin for transforms (scale/rotate). Accepts the same enum or an explicit normalized pair.

### Positioning semantics (absolute/stacked nodes)

For a node with layout.x and layout.y (absolute positioning) and explicit or measured width/height:

1. Measure pass: if width/height are "auto", the runtime measures content (text/images/children) to determine size.
2. Compute nodeAnchorPoint = (ax * width, ay * height) + anchorOffset.
3. Place the node so that nodeAnchorPoint sits at (layout.x, layout.y) in the panel's logical coordinate space.

Notes:
- anchor only affects absolute/stacked placement and overlay alignment. In directional layouts (Row/Column), anchor is ignored for layout; transformOrigin may still apply for transforms.
- For relative/flex layouts, the container's alignment properties determine child placement.

### Overlay & popover alignment

Overlays (tooltips, popovers, menus) are positioned relative to a target node using two anchors and an optional offset:

- targetAnchor: anchor on the target node (defaults to "bottom")
- overlayAnchor: anchor on the overlay node (defaults to "top")
- offset?: { x: number, y: number } — logical units applied after anchors are aligned

Semantics:
1. Compute targetPoint = targetTopLeft + (targetAx * targetWidth, targetAy * targetHeight).
2. Compute overlayAnchorPoint = (overlayAx * overlayWidth, overlayAy * overlayHeight).
3. Position the overlay so overlayAnchorPoint == targetPoint + offset.

Placement policy (runtime):
- Default: "flip-then-clamp" — attempt the requested alignment; if the overlay overflows the visible container, flip along the primary axis; if still overflowing, clamp to bounds.
- Override via overlay options: { fallback: "flip" | "clamp" | "allowOverflow" }.

### Rounding & device-pixel snapping

Placement is computed in logical units. To render crisply on device pixels:
1. Convert logical placement to device pixels by multiplying by scaleFactor.
2. Apply consistent snapping (round to nearest device pixel; tie-breaking rule must be documented by the runtime).
3. Optionally convert snapped positions back to logical units for layout caches.

Rounding must be deterministic and documented to make layout snapshot tests reproducible.