# Box

A `Box` is a **layout block** — the building block of all panel content. Boxes form a tree rooted at a `Panel`. Each Box is both a **section** (a grouping of content, analogous to `<div>` in HTML) and a **layout manager** (it arranges its children using a grid model).

Boxes are created with `hostApi.ui.box(id, options, children)`. The `children` callback is called **once at load time** with proxy objects to build an expression DAG; the runtime evaluates it each tick.

---

## Child

`Child` is an opaque type returned by `ui.box`, `ui.text`, and `ui.number`. Do not construct it directly — always use the factory functions.

---

## Identity

Every component requires a mandatory `id` string as the first argument:

```ts
hostApi.ui.box("stats-container", { layout: { ... } }, ...)
hostApi.ui.text("hp-label", { value: ... })
hostApi.ui.number("hp-value", { value: ... })
```

The runtime reconciliation algorithm uses these ids to diff successive evaluations of the expression DAG — matching same-id nodes across ticks to determine what changed. Without stable ids, the runtime cannot reliably match a node from one tick to the next.

**Scope**: ids must be unique within their parent's `children` array. They do not need to be globally unique across the entire panel tree.

**Stability**: ids are declared at load time (inside the `children` callback). They are constants — never dynamic expressions. The runtime assumes they are stable for the lifetime of the module.

---

## Conditional rendering

There is no `visible` flag on any component. To conditionally render a child, **exclude it from the returned array**. Presence in the array equals rendered; absence equals not rendered.

Since `children` is called once at load time with proxy objects, conditional inclusion must be expressed using expression combinators — not runtime booleans.

```ts
// Show a target-HP row only when a target is declared
hostApi.ui.box("content", { layout: { columns: [{ scale: number.of(1) }] } }, (state, data) => [
  hostApi.ui.text("actor-hp-label", { value: string.of("Actor HP") }),
  hostApi.ui.number("actor-hp", { value: state.actor.numberMap.get("hp") }),
  ...state.value("target").asEntity.map(e => [
    hostApi.ui.text("target-hp-label", { value: string.of("Target HP") }),
    hostApi.ui.number("target-hp", { value: e.numberMap.get("hp") }),
  ]).orElse([]),
])
```

---

## Size constraint

All components accept a `size` option with per-axis constraints consumed by the parent layout:

```ts
type SizeConstraint = {
  /** Minimum size in logical units. Never sized below this. */
  min?: NumberExpression;

  /** Maximum size in logical units. Never sized above this. */
  max?: NumberExpression;

  /**
   * Weight for claiming remaining space after min sizes are satisfied.
   * Behaves like CSS flex-grow / fr units.
   * A component with scale 2 claims twice as much remaining space as one with scale 1.
   * Defaults to 0 (sized to min, or to content if no min).
   */
  scale?: NumberExpression;
}

type ChildSize = {
  width?:  SizeConstraint;
  height?: SizeConstraint;
}
```

---

## Anchor positioning

Components can anchor themselves within their grid cell, controlling both positioning and content growth direction. Anchor is specified as part of `ChildSize`:

```ts
type ChildSize = {
  width?:  SizeConstraint;
  height?: SizeConstraint;
  
  anchor?: {
    x?: NumberExpression;  // 0 = left, 0.5 = center (default), 1 = right
    y?: NumberExpression;  // 0 = top, 0.5 = center (default), 1 = bottom
  };
}
```

### Semantics

The anchor point controls two behaviors:

1. **Positioning** — where within the cell the component references itself
2. **Growth direction** — the direction natural content expansion flows from that anchor

| Anchor | Positioning | Growth Direction | Visual |
|--------|-------------|------------------|--------|
| 0.0 | Left/top edge | Rightward/downward | `\|AA---|` |
| 0.5 | Center | Symmetric | `\|--AA--|` |
| 1.0 | Right/bottom edge | Leftward/upward | `\|---AA\|` |

**Key behaviors:**
- Per-axis independent: `x` and `y` work separately. Asymmetric anchors are allowed.
- Default (omitted): `{ x: 0.5, y: 0.5 }` — content centers with symmetric growth.
- Values outside [0, 1] are clamped.
- When natural content exceeds min/max size constraints, it clamps to limits and anchor repositions to maintain growth direction intent.

### Examples

**Left-aligned label in fixed cell:**
```ts
hostApi.ui.text("label", {
  value: string.of("HP"),
  size: { anchor: { x: number.of(0) } }
})
// Text anchors left; wraps rightward if truncated
```

**Right-aligned value in fixed cell:**
```ts
hostApi.ui.number("hp-value", {
  value: state.actor.numberMap.get("hp"),
  size: { anchor: { x: number.of(1) } }
})
// Number anchors right; grows leftward if space needs to expand
```

**Centered content, top-anchored (grows downward):**
```ts
hostApi.ui.box("title", {
  layout: { columns: [{ scale: 1 }] },
  size: { anchor: { x: number.of(0.5), y: number.of(0) } }
}, (state, data) => [ /* ... */ ])
// Box centers horizontally, anchors top; children overflow downward
```

---

## Layout

A Box with a `layout` option acts as a **grid container** for its children.

### Track definition

Column tracks extend `SizeConstraint` with an `align` property:

```ts
type TrackDefinition = {
  min?:   NumberExpression;
  max?:   NumberExpression;
  scale?: NumberExpression;

  /**
   * Alignment of content within this track.
   * "start" (default): content aligns to the track start (left for columns, top for rows).
   * "end": content aligns to the track end (right for columns, bottom for rows).
   */
  align?: "start" | "end";
}
```

### Grid model

All layout is expressed as a grid. Flex-row and flex-column are degenerate cases:

| Intent | How to express it |
|---|---|
| Flex row (equal columns) | `columns: [{ scale:1 }, { scale:1 }, ...]` |
| Flex column | `columns: [{ scale:1 }]` — one column, children stack in rows |
| Fixed label column + stretchy value column | `columns: [{ min:80 }, { scale:1 }]` |
| Synchronized 3-column table | `columns: [{ min:60 }, { scale:1 }, { min:60 }]` |

```ts
type GridLayout = {
  /**
   * Column track definitions. The number of entries determines the number of columns.
   * All children in the same column share the same track width —
   * this synchronizes column widths across rows.
   */
  columns: TrackDefinition[];

  /**
   * Auto-flow direction.
   * true (default): row-first — children fill left→right, then wrap to the next row.
   * false: column-first — children fill top→bottom, then move to the next column.
   */
  rowFirst?: boolean;

  /**
   * When true, children are placed last-to-first rather than first-to-last.
   * Default: false.
   */
  reverse?: boolean;

  /** Space between cells in logical units. */
  gap?: {
    row?:    NumberExpression;
    column?: NumberExpression;
  };
}
```

### Auto-placement

Children are placed automatically in array order, following the `rowFirst` direction.

`rowFirst: true` (default) — row-major:

```
columns: [A, B, C]

  col 0   col 1   col 2
  [ 0 ]   [ 1 ]   [ 2 ]
  [ 3 ]   [ 4 ]   [ 5 ]
  [ 6 ]   ...
```

`rowFirst: false` — column-major:

```
columns: [A, B]

  col 0   col 1
  [ 0 ]   [ 2 ]
  [ 1 ]   [ 3 ]
```

### Track `align`

```
columns: [{ scale:1 }, { scale:1, align:"end" }]
children returns: [A, B, C, D]

  col 0     col 1
  [ A  ]   [  B ]   ← B end-aligned within col 1
  [ C  ]   [  D ]   ← D end-aligned within col 1
```

### Layout `reverse`

```
columns: [{ scale:1 }, { scale:1 }]   reverse: true
children returns: [A, B, C, D, E]

  col 0   col 1
  [ E ]   [ D ]
  [ C ]   [ B ]
  [ A ]
```

---

## Options shape

```ts
type BoxOptions = {
  /**
   * Size hint consumed by the parent layout.
   * - width  is consumed by the parent's column track allocation.
   * - height is consumed by the parent's row sizing.
   */
  size?: ChildSize;

  /**
   * Grid layout for children. When present, this box is a grid container.
   * When absent, the children callback should still be provided and may return
   * an empty array or leaf content managed by the runtime.
   */
  layout?: GridLayout;
}
```

---

## Examples

### Flex row — three equal columns

```ts
hostApi.ui.box(
  "flex-row",
  {
    layout: {
      columns: [{ scale: number.of(1) }, { scale: number.of(1) }, { scale: number.of(1) }],
      gap: { column: number.of(8) },
    },
  },
  (state, data) => [/* A */, /* B */, /* C */],
)
```

```
[ A | B | C ]
```

---

### Flex column — single column, children stack vertically

```ts
hostApi.ui.box(
  "flex-col",
  { layout: { columns: [{ scale: number.of(1) }], gap: { row: number.of(4) } } },
  (state, data) => [/* A */, /* B */, /* C */],
)
```

```
[ A ]
[ B ]
[ C ]
```

---

### Stats panel — label | value grid

```ts
hostApi.ui.box(
  "stats-grid",
  {
    layout: {
      columns: [
        { min: number.of(80) },                  // labels — fixed min width
        { scale: number.of(1), align: "end" },   // values — stretch, end-aligned
      ],
      gap: { row: number.of(4), column: number.of(8) },
    },
  },
  (state, data) => [
    hostApi.ui.text("health-label", { value: string.of("Health") }),
    hostApi.ui.text("health-value", { value: string.of("100/100") }),
    hostApi.ui.text("mana-label", { value: string.of("Mana") }),
    hostApi.ui.text("mana-value", { value: string.of("50/100") }),
  ],
)
```

```
Health    100/100
Mana       50/100
```

---

### Conditional child — only shown when target is present

```ts
hostApi.ui.box(
  "status-box",
  { layout: { columns: [{ scale: number.of(1) }] } },
  (state, data) => [
    hostApi.ui.text("actor-hp-label", { value: string.of("Actor HP") }),
    hostApi.ui.number("actor-hp", { value: state.actor.numberMap.get("hp") }),
    ...state.value("target").asEntity.map(e => [
      hostApi.ui.text("target-hp-label", { value: string.of("Target HP") }),
      hostApi.ui.number("target-hp", { value: e.numberMap.get("hp") }),
    ]).orElse([]),
  ],
)
```

---

### Inventory grid — 4 columns, row-first

```ts
hostApi.ui.box(
  "inventory-grid",
  {
    layout: {
      columns: Array(4).fill({ min: number.of(48) }),
      gap: { row: number.of(4), column: number.of(4) },
    },
  },
  (state, data) => [
    /* slot 0 */, /* slot 1 */, /* slot 2 */, /* slot 3 */,
    /* slot 4 */, /* slot 5 */, /* slot 6 */, /* slot 7 */,
  ],
)
```

```
[ 0 ][ 1 ][ 2 ][ 3 ]
[ 4 ][ 5 ][ 6 ][ 7 ]
```

---

### Column-first flow

```ts
hostApi.ui.box(
  "col-flow",
  {
    layout: {
      columns: [{ scale: number.of(1) }, { scale: number.of(1) }],
      rowFirst: false,
      gap: { column: number.of(8) },
    },
  },
  (state, data) => [/* A */, /* B */, /* C */, /* D */, /* E */],
)
```

```
[ A ][ C ][ E ]
[ B ][ D ][   ]
```

---

## Cross-references

- [`panel.md`](./panel.md) — Panel declaration; Box is the child of a Panel
- [`text-value.md`](./text-value.md) — `ui.text` leaf component
- [`overview.md`](./overview.md) — UI system entry point
- [`ui-state.md`](./ui-state.md) — `UiStateApi` and `UiDataApi` passed to `children`
- [`numberExpression.md`](../expressions/numberExpression.md) — `NumberExpression` for size and gap values
- [`maybeExpression.md`](../expressions/maybeExpression.md) — used for conditional child inclusion
