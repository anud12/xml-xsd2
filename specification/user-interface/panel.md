# Panel

A `Panel` is a **positioned, sized UI window** declared by a module at load time. Panels are top-level elements — they are never nested inside other components. The client renders panels as floating surfaces; the runtime drives their content and visibility via the `render` function.

---

## Positioning model

A panel is placed on screen using three cooperating values: an **anchor**, a **pivot**, and an **offset**.

```
screen
┌──────────────────────────────────┐
│                                  │
│              anchor (0.5, 0.0)   │
│                  ↓               │
│          ┌───────┬───────┐       │
│          │  panel pivot  │       │
│          │  (0.5, 0.0)   │       │
│          │               │       │
│          └───────────────┘       │
│                                  │
└──────────────────────────────────┘
```

### `anchor`

A normalised point `{ x, y }` in screen space where `(0, 0)` is the **top-left** corner and `(1, 1)` is the **bottom-right** corner.

The anchor is computed against the actual screen (or viewport) dimensions every frame. When the screen resizes, the anchor point moves with it.

```
(0,0) ─────── (0.5,0) ─────── (1,0)
  │                               │
(0,0.5)      (0.5,0.5)      (1,0.5)
  │                               │
(0,1) ─────── (0.5,1) ─────── (1,1)
```

### `pivot`

A normalised point `{ x, y }` on the panel itself — `(0, 0)` is the panel's **top-left** corner, `(1, 1)` is its **bottom-right** corner. The pivot is the point on the panel that aligns with the anchor.

Examples:
- `pivot (0, 0)` — the panel's top-left corner is placed at the anchor.
- `pivot (1, 0)` — the panel's top-right corner is placed at the anchor.
- `pivot (0.5, 0.5)` — the panel's centre is placed at the anchor.

### `offset`

A `{ x, y }` displacement in **logical units** applied after the anchor/pivot alignment. Positive `x` moves right; positive `y` moves down.

Use `offset` to add inset spacing from a screen edge:

```ts
// Panel pinned to top-right, 16 logical units inset from the corner
anchor: { x: number.of(1), y: number.of(0) }
pivot:  { x: number.of(1), y: number.of(0) }
offset: { x: number.of(-16), y: number.of(16) }
```

---

## Default positioning

`anchor`, `pivot`, `offset`, `size`, and `scale` define the panel's **default** position and size. The runtime uses these values for the initial render. The user may reposition or resize the panel at runtime; the runtime stores that override per-client automatically. No module API is required to handle this.

---

## Size

```ts
size: { width: NumberExpression; height: NumberExpression }
```

Width and height are expressed in **logical units**. The renderer converts logical units to device pixels using the effective scale factor.

---

## Scale

```ts
scale?: NumberExpression   // default: 1.0 (inherits global UI scale)
```

A per-panel scale multiplier applied on top of the global UI scale. `scale: number.of(1.5)` renders this panel 1.5× larger than the baseline. The global UI scale is a runtime-level constant; per-panel scale is a module-declared multiplier relative to it.

---

## Child function

```ts
child: (state: UiStateApi, data: UiDataApi) => Child
```

`child` is called **once at load time** with `UiStateApi` and `UiDataApi` proxies. It returns a `Child` tree of expression nodes. The runtime evaluates this expression DAG each tick, diffs the result, and reconciles changes to the client.

`state` provides access to per-client UI state values and the actor. `data` provides access to world data queries (to be expanded).

---

## Visibility

```ts
visible?: ConditionExpression   // default: true
```

Evaluated each tick. When `false` the panel and all its children are not rendered. Typically bound to a UI state value's `isPresent`:

```ts
visible: target.isPresent,
```

---

## Declaration shape

```ts
type PanelDeclaration = {
  /** Unique panel id within the module. */
  id: string;

  /** Default normalised screen-space attachment point. (0,0) = top-left, (1,1) = bottom-right. */
  anchor: { x: NumberExpression; y: NumberExpression };

  /** Default normalised panel-space point that aligns with the anchor. (0,0) = panel top-left, (1,1) = panel bottom-right. */
  pivot: { x: NumberExpression; y: NumberExpression };

  /** Default displacement from the aligned anchor/pivot point, in logical units. */
  offset: { x: NumberExpression; y: NumberExpression };

  /** Default panel dimensions in logical units. */
  size: { width: NumberExpression; height: NumberExpression };

  /** Default per-panel scale multiplier. Defaults to 1.0 (uses global UI scale). */
  scale?: NumberExpression;

  /** Evaluated each tick. Panel is hidden when false. Default: true. */
  visible?: ConditionExpression;

  /**
   * Called once at load time to build the panel's content expression DAG.
   * `state` provides per-client UI state; `data` provides world data queries.
   * The returned Child is evaluated by the runtime each tick.
   */
  child: (state: UiStateApi, data: UiDataApi) => Child;
}
```

---

## Common patterns

### Top-left HUD panel

```ts
hostApi.ui.registerPanel({
  id: "hud-top-left",
  anchor: { x: hostApi.number.of(0), y: hostApi.number.of(0) },
  pivot:  { x: hostApi.number.of(0), y: hostApi.number.of(0) },
  offset: { x: hostApi.number.of(16), y: hostApi.number.of(16) },
  size:   { width: hostApi.number.of(200), height: hostApi.number.of(120) },
  child: (state, data) => ({ /* ... */ }),
});
```

### Centred modal — visible when a value is set

```ts
hostApi.ui.registerPanel({
  id: "modal",
  anchor: { x: hostApi.number.of(0.5), y: hostApi.number.of(0.5) },
  pivot:  { x: hostApi.number.of(0.5), y: hostApi.number.of(0.5) },
  offset: { x: hostApi.number.of(0),   y: hostApi.number.of(0)   },
  size:   { width: hostApi.number.of(400), height: hostApi.number.of(300) },
  visible: inspecting.isPresent,
  child: (state, data) => ({ /* ... */ }),
});
```

### Bottom-right minimap, scaled up

```ts
hostApi.ui.registerPanel({
  id: "minimap",
  anchor: { x: hostApi.number.of(1), y: hostApi.number.of(1) },
  pivot:  { x: hostApi.number.of(1), y: hostApi.number.of(1) },
  offset: { x: hostApi.number.of(-16), y: hostApi.number.of(-16) },
  size:   { width: hostApi.number.of(150), height: hostApi.number.of(150) },
  scale:  hostApi.number.of(1.25),
  child: (state, data) => ({ /* ... */ }),
});
```

---

## Cross-references

- [`box.md`](./box.md) — Box declaration; Box is the `child` of a Panel
- [`ui-state.md`](./ui-state.md) — `UiStateApi` passed as `state` to `render`
- [`numberExpression.md`](../expressions/numberExpression.md) — `NumberExpression` used for anchor, pivot, offset, size and scale
- [`conditionExpression.md`](../expressions/conditionExpression.md) — `ConditionExpression` used for visibility
- [`runtime.md`](../runtime/runtime.md) — delta streaming; panels are evaluated and diffed each tick
