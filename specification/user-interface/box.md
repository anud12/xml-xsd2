# Box

A `Box` is a **layout block** — groups and arranges child content using a grid model. Boxes form a tree rooted at a `Panel`.

```ts
hostApi.ui.box(id, options, children)
```

The `children` callback is called once at load time with proxy objects to build an expression DAG; runtime evaluates it each tick.

See [`concepts.md`](./concepts.md) for shared concepts: **component identity**, **size constraints**, **anchor positioning**, and **conditional rendering**.

---

## Grid Layout

All layout is expressed as a grid defined by `columns` track definitions. Children auto-place row-first by default.

```ts
type GridLayout = {
  columns: TrackDefinition[];          // Column definitions; count = num columns
  rowFirst?: boolean;                  // true (default): left→right, wrap; false: top→bottom
  reverse?: boolean;                   // false (default): reverse child placement order
  gap?: { row?: NumberExpression; column?: NumberExpression };  // Cell spacing
};

type TrackDefinition = SizeConstraint & {
  align?: "start" | "end";             // default: "start" — content alignment within track
};
```

**Common patterns:**
- **Flex row:** `columns: [{ scale: 1 }, { scale: 1 }, ...]`
- **Flex column:** `columns: [{ scale: 1 }]` — single column, children stack in rows
- **Label|value grid:** `columns: [{ min: 80 }, { scale: 1, align: "end" }]`
- **Fixed grid:** `columns: Array(4).fill({ min: 48 })` — 4×N grid

---

## Type

```ts
type BoxOptions = {
  size?: ChildSize;     // Size hint for parent layout
  layout?: GridLayout;  // Grid layout config; omit for non-layout box
};
```

---

## Examples

**Flex row (3 equal columns):**
```ts
hostApi.ui.box("row", {
  layout: { columns: [{ scale: 1 }, { scale: 1 }, { scale: 1 }], gap: { column: 8 } }
}, (state, data) => [/* A */, /* B */, /* C */])
```

**Label | value grid (2-column):**
```ts
hostApi.ui.box("stats", {
  layout: {
    columns: [{ min: 80 }, { scale: 1, align: "end" }],
    gap: { row: 4, column: 8 }
  }
}, (state, data) => [
  text("Health"), text("100/100"),
  text("Mana"), text("50/100"),
])
```

**Inventory grid (4 columns):**
```ts
hostApi.ui.box("inv", {
  layout: {
    columns: Array(4).fill({ min: 48 }),
    gap: { row: 4, column: 4 }
  }
}, (state, data) => [/* 8 slots */])
```

**Conditional children:**
```ts
hostApi.ui.box("status", {
  layout: { columns: [{ scale: 1 }] }
}, (state, data) => [
  text("actor-hp"),
  ...state.value("target").asEntity
    .map(e => [text("target-hp")])
    .orElse([]),
])
```

---

## Cross-references

- [`concepts.md`](./concepts.md) — Identity, size constraints, anchor, conditional rendering, state binding
- [`panel.md`](./panel.md) — Box is the child content of a Panel
- [`text-value.md`](./text-value.md) — Leaf component for text content
- [`ui-state.md`](./ui-state.md) — State and data bindings
