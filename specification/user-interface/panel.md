# Panel

A `Panel` is a **positioned, sized UI window** declared by a module at load time. Panels are top-level elements — they are never nested inside other components. The client renders panels as floating surfaces; the runtime drives their content via the child factory callback.

Panels are declared with `hostApi.ui.panel(id, options, child)`.

---

## Positioning

Panels position using **anchor** (screen point), **pivot** (panel point), and **offset** (displacement):

- **anchor** `{ x, y }` — Normalised screen position (0,0 = top-left; 1,1 = bottom-right). Recomputed each frame when screen resizes.
- **pivot** `{ x, y }` — Normalised panel point that aligns to anchor (0,0 = panel top-left; 1,1 = panel bottom-right).
- **offset** `{ x, y }` — Displacement in logical units after anchor/pivot alignment.

Example: Pin to top-right, 16 units inset:
```ts
anchor: { x: number.of(1), y: number.of(0) }
pivot:  { x: number.of(1), y: number.of(0) }
offset: { x: number.of(-16), y: number.of(16) }
```

---

## Child

```ts
child: (state: UiStateApi, data: UiDataApi) => Child
```

Called once at load time with state/data proxies. Returns a `Child` node (typically a `Box`). Runtime evaluates the DAG each tick, diffs, and reconciles to client.

---

## Type

```ts
type PanelOptions = {
  anchor: { x: NumberExpression; y: NumberExpression };
  pivot: { x: NumberExpression; y: NumberExpression };
  offset: { x: NumberExpression; y: NumberExpression };
  size: { width: NumberExpression; height: NumberExpression };
  scale?: NumberExpression;           // default: 1.0; multiplies global UI scale
  visible?: ConditionExpression;      // default: true
  background?: TextureResource;       // optional background texture
  border?: TextureResource;           // optional border texture
};
```

Positioning fields (`anchor`, `pivot`, `offset`, `size`, `scale`) define the default state. Users may reposition or resize at runtime; overrides are stored per-client. Clicking raises the panel to the front (client-side z-order; see [`rendering.md`](./rendering.md)).

---

## Examples

**Top-left HUD, 16-unit inset:**
```ts
hostApi.ui.panel("hud", {
  anchor: { x: number.of(0), y: number.of(0) },
  pivot:  { x: number.of(0), y: number.of(0) },
  offset: { x: number.of(16), y: number.of(16) },
  size:   { width: number.of(200), height: number.of(120) },
  background: { name: string.of("ui-panel-bg") },
}, (state, data) => hostApi.ui.box("content", { /* ... */ }, (state, data) => [/* ... */]));
```

**Centered modal, visible when a state value is set:**
```ts
const inspecting = hostApi.ui.state.declare("inspecting")
hostApi.ui.panel("modal", {
  anchor: { x: number.of(0.5), y: number.of(0.5) },
  pivot:  { x: number.of(0.5), y: number.of(0.5) },
  offset: { x: number.of(0), y: number.of(0) },
  size:   { width: number.of(400), height: number.of(300) },
  visible: inspecting.isPresent,
  background: { name: string.of("modal-bg"), stretch: "fit" },
  border: { name: string.of("modal-border") },
}, (state, data) => hostApi.ui.box("modal-content", { /* ... */ }, (state, data) => [/* ... */]));
```

**Bottom-right minimap, 1.25× scale:**
```ts
hostApi.ui.panel("minimap", {
  anchor: { x: number.of(1), y: number.of(1) },
  pivot:  { x: number.of(1), y: number.of(1) },
  offset: { x: number.of(-16), y: number.of(-16) },
  size:   { width: number.of(150), height: number.of(150) },
  scale:  number.of(1.25),
}, (state, data) => hostApi.ui.box("minimap-content", { /* ... */ }, (state, data) => [/* ... */]));
```

---

## Cross-references

- [`box.md`](./box.md) — Box; the root `Child` returned by the panel callback
- [`ui-state.md`](./ui-state.md) — `UiStateApi` passed as `state` to the callback
- [`rendering.md`](./rendering.md) — Coordinate system, z-ordering, focus, resource resolution
- [`concepts.md`](./concepts.md) — Identity, size constraints, conditional rendering
