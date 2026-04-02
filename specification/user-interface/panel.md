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

## Styling

Panels can declare optional background and border textures:

- **`background`** — Texture used as the panel background. Fills entire panel bounds (respects stretching mode).
- **`border`** — Texture used as the panel border (implementation-specific rendering).

Both are optional. If omitted, no texture is rendered.

Textures are resolved by name at render time. If a texture name is not found, the client uses a platform default. See [`rendering.md`](./rendering.md) for texture resolution details.

---

## Properties

- **`size`** — Panel dimensions in logical units: `{ width: NumberExpression; height: NumberExpression }`
- **`scale`** — Optional per-panel scale multiplier (default 1.0). Multiplies global UI scale.
- **`visible`** — Optional condition; panel hidden when `false` (default: `true`).
- **`background`** — Optional background texture.
- **`border`** — Optional border texture.
- **Default state** — `anchor`, `pivot`, `offset`, `size`, `scale` define initial render. User can reposition/resize at runtime; overrides stored per-client.

---

## Focus & Layering

Panels respond to user clicks **client-side only**. When a user clicks a panel, the client raises it to the front of the rendering stack. This is purely visual; the runtime does not observe clicks.

Initial panel order is determined by **declaration order**: panels declared first render at the back; panels declared last render at the front.

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
  scale?: NumberExpression;           // default: 1.0
  visible?: ConditionExpression;      // default: true
  background?: TextureResource;       // optional
  border?: TextureResource;           // optional
};
```

---

## Examples

**Top-left HUD, 16-unit inset, with background:**
```ts
hostApi.ui.panel("hud", {
  anchor: { x: number.of(0), y: number.of(0) },
  pivot:  { x: number.of(0), y: number.of(0) },
  offset: { x: number.of(16), y: number.of(16) },
  size:   { width: number.of(200), height: number.of(120) },
  background: { name: string.of("ui-panel-bg"), stretch: "fill" },
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
  border: { name: string.of("modal-border"), stretch: "fill" },
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
  background: { name: string.of("minimap-bg"), stretch: "center" },
}, (state, data) => hostApi.ui.box("minimap-content", { /* ... */ }, (state, data) => [/* ... */]));
```

---

## Cross-references

- [`box.md`](./box.md) — Box; the root `Child` returned by the panel callback
- [`ui-state.md`](./ui-state.md) — `UiStateApi` passed as `state` to the callback
- [`rendering.md`](./rendering.md) — Resource resolution, texture stretching, z-ordering, focus tracking
- [`numberExpression.md`](../expressions/numberExpression.md) — `NumberExpression` used for anchor, pivot, offset, size and scale
- [`conditionExpression.md`](../expressions/conditionExpression.md) — `ConditionExpression` used for visibility
- [`runtime.md`](../runtime/runtime.md) — delta streaming; panels are evaluated and diffed each tick
